# Boss 關間歇性提前結算與 Boss 停止移動：調查與修復計畫

日期：2026-10-15  
PR：#3  
Branch：`docs/enemy-behavior-plan-20261006`  
參考 SHA：5b8d897a37e88e4486f852beb478532f63098a8e  
狀態：**調查完成，修復計畫待 ChatGPT 審核；尚未修改任何實作。**

---

## 一、使用者實測症狀

| 次數 | 症狀 |
|---|---|
| 第 1 次 | 進 Boss 關後直接結算（不明原因，未與 Boss 交戰） |
| 第 2 次 | Boss 不移動，但仍施放技能 |
| 第 3 次 | 正常 |

間歇性 + 第三次恢復正常，指向**與時序或初始化狀態有關**的邊界條件，而非穩定邏輯錯誤。

---

## 二、資產鏈確認（已靜態核對）

```
M_Boss.asset
  objectiveType: 1  (EliminateAll)
  targetValue:   70 (票數門檻 %)
  sceneName:     Boss_Arena
  spawnConfig:   S_Boss.asset

S_Boss.asset  (EliminateAllSpawnConfig)
  totalEnemyCount: 1
  enemyPrefabs[0]: Boss_ .prefab (GUID b6a59ef2...)

Boss_ .prefab (EnemyController)
  enemyRole:          2 (EliteBoss)
  isFinalBossOpponent: 1
  equippedSkills:     [B_Stun, B_Damage, B_Summon]

Boss_Arena.unity
  EnemySpawner   → spawnConfig = S_Boss
  VoterSpawner   → initialSpawnCount = 30, spawnInterval = 0, spawnPerTick = 0
  LevelTimer     → (Inspector 設定，未知值，需 Unity 確認)
  MissionTracker → (場景中存在)
  RewardItemSpawner → (場景中存在)
```

**注意**：`FinalBossSpawnConfig.asset` 存在於 `Assets/Data/Mission/`，但 `M_Boss.asset` 的 `spawnConfig` 指向的是 `S_Boss.asset`（`EliminateAllSpawnConfig`），不是 `FinalBossSpawnConfig.asset`。  
`FinalBossSpawnConfig.asset` 目前 `enemyPrefabs[0]: {fileID: 0}`（空引用），**未被使用**，不是根因。

---

## 三、調查結果：已確認問題、待驗證假設、觸發序列

### 3-A：提前結算——票數分母問題（已確認路徑，發生機率高）

**觸發序列**：

```
GameplayState.Enter()
  └─ LevelTimer.StartTimer()
  └─ BattleEventManager.SetEncounterPhase(Briefing)
  └─ NarrativeDirector.Play(onEnterBeat=null, BeginActive)
       └─ beat==null → 立即同步呼叫 BeginActive()
            └─ SetEncounterPhase(Active)

同一幀（或下一幀）：
  Spawner.Start()
    └─ StartSpawningRoutine()
         └─ for i < 30: SpawnAtRandomPoint()
              └─ 每 5 隻 yield return null（6 幀才生成完畢）
  VoterLogic.OnEnable() → allActiveVoters.Add(this)   ← 逐幀累積

MissionTracker.Start()
  └─ IsFinalBossEncounter() == true
       └─ Subscribe()（同步，不走 DelayedSubscribe）
            └─ _subscribed = true
                 → Update() 開始輪詢 CheckFinalBossVotePercent()

CheckFinalBossVotePercent() 每幀計算：
  percent = GetCurrentVotePercentage() × 100f
          = (玩家陣營選民數 / 場上總選民數) × 100f

問題在此：
  - 第 1 幀：allActiveVoters 可能只有 0~5 人（Spawner 協程剛開始）
  - 若初始前幾隻選民全部是玩家陣營（中立選民初始 ConvertedSide = 玩家？）
    或場上選民數為 0（GetCurrentVotePercentage 回傳 0f，不觸發），
  - 但若選民是由物件池 reset 的，Data.ConvertedSide 殘留上一關的值 → 瞬間偏高
```

**關鍵程式碼：**

`GameDB.cs` L375-L378（`GetCurrentVotePercentage`）：
```csharp
public float GetCurrentVotePercentage()
{
    int total = GetCurrentTotalVotes();                            // 場上所有選民
    return total > 0 ? (float)GetCurrentPlayerVotes() / total : 0f;
}
```

若 `total == 0` 回傳 `0f`，不觸發。但若 Spawner 已生成少數選民（例如 1~4 隻），而 `ConvertedSide` 因物件池 reset 殘留玩家方，就可能：
```
percent = (3 玩家方 / 4 總數) × 100 = 75f ≥ 70f → 立即結算！
```

`MissionTracker.cs` L55-L58（FinalBoss 路徑，不走 DelayedSubscribe）：
```csharp
if (IsFinalBossEncounter())
{
    Subscribe();   // ← 同步，立即設 _subscribed = true
    return;
}
```

`MissionTracker.cs` L86-L91（Update 輪詢，無「生成完成」保護）：
```csharp
if (BattleEventManager.CurrentEncounterPhase != Active) return;  // Active 才輪詢
if (!_subscribed) return;
// FinalBoss 路徑
if (IsFinalBossEncounter()) CheckFinalBossVotePercent();
```

**BeginActive 觸發時機**（`NarrativeDirector.cs`）：
```csharp
if (beat == null)
{
    onComplete?.Invoke();   // ← 同步立即呼叫，不等任何 frame
    return;
}
```

**結論：已確認路徑**。Phase 切為 Active 時 Spawner 協程尚未完成，選民分母未完整，`CheckFinalBossVotePercent()` 可能在同幀或第二幀就看到偏高比例並觸發結算。此路徑在 Boss 關必定存在，間歇性是因為物件池 reset 後 `ConvertedSide` 是否殘留而有所不同。

**待 Unity 確認**：
- `VoterLogic.OnEnable()` 後 `Data.ConvertedSide` 的初始值（是否呼叫 Reset？）
- `LevelTimer.TotalDuration` 的 Inspector 設定值
- 物件池是否在重進關卡時正確 reset 選民狀態

---

### 3-B：Boss 不移動——NavMesh 初始化失敗（已確認路徑）

**觸發序列**：

```
EnemySpawner.SpawnEliminateAll()
  └─ SpawnEnemy(Boss_ .prefab)
       └─ 生成位置 = spawnPoint.position + Random(-0.8~0.8, 0, -0.8~0.8)

EnemyController.Start()
  └─ if (!Agent.isOnNavMesh)
       └─ NavMesh.SamplePosition(position, out hit, 5f, AllAreas)
            ├─ 成功 → Agent.Warp(hit.position)
            │         └─ if Warp 失敗 → LogWarning，繼續初始化
            └─ 失敗 → LogError，繼續初始化

  └─ StateMachine.Initialize(IdleState)   ← 無論 NavMesh 是否有效都執行

EnemyMoveState.Update()：
  if (ctx.Agent.isOnNavMesh)
  {
      ctx.Agent.SetDestination(ctx.target.position);   ← isOnNavMesh==false 時不執行
  }
  // 距離判斷仍執行，但 Agent 不移動 → Boss 停在原地
```

**關鍵程式碼：**

`EnemyController.cs` Start() L479-L506：Warp 失敗後不中止初始化，狀態機正常啟動。  
`EnemyMoveState.cs` Update() L21-L25：`isOnNavMesh` 判斷只阻止 `SetDestination`，不阻止狀態機繼續執行。

**為何技能仍可施放**：技能排程（`EnemyController.Update()` L638-L730）在 `isOnNavMesh` 無效時不受影響，只要 `canUseSkills && !isActing && isActive && playerOk && sharedReady`，就觸發 `EnemySkillState`。`EnemySkillState.Enter()` 呼叫 `Agent.isStopped = true`，這在 `isOnNavMesh == false` 時不崩潰，只是 no-op。施法動畫與 `ExecuteSkill()` 正常執行。

**結論：已確認路徑**。Boss 的隨機偏移（±0.8m）加上 Boss_Arena 的 NavMesh 可能有間隙（尤其在生成點附近邊緣），導致偶發性 Warp 失敗。

**待 Unity 確認**：
- Boss_Arena 的 NavMesh 在 Boss 生成點附近是否完整（是否重新烘焙過）
- Boss 生成 spawnPoint 的確切世界座標

---

### 3-C：提前結算——EnemySpawnTracker 在 StartTracking 時誤觸（待驗證）

**假設路徑**：

`EnemySpawnTracker.StartTracking()` L24-L33：
```csharp
public static void StartTracking()
{
    _trackingActive = true;
    int count = GetAliveEnemies().Length;
    if (count == 0)
        BattleEventManager.TriggerAllEnemiesDefeated();   // ← 立即觸發！
}
```

`GetAliveEnemies()` 過濾 `enemyRole != Reporter`，Boss 的 `enemyRole == EliteBoss`，**應該被計入**，正常情況 count = 1。

**但邊界情況**：`SpawnEliminateAll()` 每隻敵人生成後需兩個 `WaitForFixedUpdate`，若 Boss 的 `EnemyController.Start()` 或 `OnEnable()` 在同幀內完成但 `GetAliveEnemies()` 呼叫時序在 `OnEnable()` 之前，就可能出現 count = 0。

`EnemyController.OnEnable()` 中：
```csharp
if (_currentHP > 0)
    EnemySpawnTracker.NotifyEnemySpawned();   // ← 通知追蹤器有新敵人
```

此 `NotifyEnemySpawned()` 只在 `_trackingActive == true` 時有效，但 `StartTracking()` 是在 SpawnEliminateAll 協程**末尾**才呼叫（所有敵人生成完後），所以正常流程下 Boss 的 `OnEnable()` 在 `StartTracking()` 之前已執行，不會有 count = 0 的問題。

**評估**：此路徑在正常流程下不觸發。但若場景初始化序列異常（例如 Boss 的 `Awake` 延遲到 `StartTracking()` 之後），仍可能發生。**此假設未確認，需 Console 日誌驗證**。

**待確認**：Console 有無 `[EnemyController] 找不到有效 NavMesh` 或 `Warp 失敗` 訊息，以及 `TriggerAllEnemiesDefeated` 的觸發來源。

---

### 3-D：T 鍵快速結算（已確認存在，間歇性誤觸可能性低）

`GameplayState.cs` Update()：
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
if (Input.GetKeyDown(KeyCode.T) &&
    BattleEventManager.CurrentEncounterPhase == Active)
{
    BattleEventManager.TriggerObjectiveResolved(EncounterOutcome.Success);
}
#endif
```

在 Editor 或 `DEVELOPMENT_BUILD` 下，Active 階段按 T 鍵即強制成功結算。**不是此次症狀的主要根因**（提前結算是在進關後立即發生，不像是誤按），但應在診斷 log 中標記結算來源以便區分。

---

## 四、最小修正範圍與預計修改檔案

### 修正一：FinalBoss 票數輪詢加入「選民生成完成」保護

**問題**：Phase 切 Active 時 Spawner 協程尚未完成，`CheckFinalBossVotePercent()` 就開始輪詢偏高的比例。

**最小方案**：在 `MissionTracker` 加入 `_votersReady` 旗標，由 Spawner 或 VoterSpawner 在初始生成完成後設定，`CheckFinalBossVotePercent()` 只在旗標就緒後才開始輪詢。

**初始化完成條件**：不使用固定等待幀數或秒數。就緒訊號由 `Spawner.StartSpawningRoutine()` 協程完成初始生成迴圈後，呼叫 `BattleEventManager.TriggerVoterSpawnReady()`；`MissionTracker` 訂閱此事件後才將 `_votersReady` 設為 `true`。

**預計修改檔案**：
- `Assets/Scripts/GameFlow/BattleEventManager.cs`：新增 `OnVoterSpawnReady` 靜態事件與 `TriggerVoterSpawnReady()` 方法
- `Assets/Scripts/System/Spawner.cs`：初始生成迴圈結束後觸發 `TriggerVoterSpawnReady()`
- `Assets/Scripts/System/Mission/MissionTracker.cs`：FinalBoss 路徑加入 `_votersReady` 旗標；`CheckFinalBossVotePercent()` 加入 `if (!_votersReady) return;`；訂閱/解除 `OnVoterSpawnReady`
- **不修改**：Boss 技能設定、70% 門檻、選民 AI 邏輯

---

### 修正二：Boss 生成點 NavMesh 保護與恢復策略

**問題**：Boss 隨機偏移落在 NavMesh 範圍外，Warp 失敗後 Boss 靜止不動。

**最小方案**：`EnemyController.Start()` 中若 `Warp` 失敗，以固定間隔（非每幀）重試一次（上限 3 次），仍失敗才記 LogError 並標記 `_navMeshFailed = true`；`EnemyMoveState.Enter()` 檢查此旗標，若失敗則不進入 MoveState，改回 IdleState 並於下幀再試。

**有限重試條件**：
- 重試間隔：不短於 0.5 秒（避免每幀昂貴尋路）
- 重試上限：3 次
- 超過上限：停在最近的 NavMesh 點（若 SamplePosition 有找到）；若連 SamplePosition 也失敗則 Boss 原地等待，不崩潰
- **不做無限 Warp**：避免 Boss 漂移

根本修正（建議配合）：重新確認 Boss_Arena 的 NavMesh 烘焙是否覆蓋所有生成點，並縮小隨機偏移（±0.8m → ±0.3m），或改用 `NavMesh.SamplePosition` 結果作為最終生成位置。

**預計修改檔案**：
- `Assets/Scripts/Enemy/EnemyController.cs`：Start() 加有限重試 Coroutine，新增 `_navMeshFailed` 旗標
- `Assets/Scripts/Enemy/EnemyMoveState.cs`：Enter() 加 NavMesh 失效保護
- `Assets/Scripts/Enemy/EnemySpawner.cs`：SpawnEnemy() 生成後以 `NavMesh.SamplePosition` 結果作為最終位置（替代純隨機偏移）
- **不修改**：Boss 技能、Skills SO、Prefab 的 Inspector 數值

---

### 修正三：診斷日誌（`#if UNITY_EDITOR || DEVELOPMENT_BUILD` 保護）

**預計修改檔案**：
- `Assets/Scripts/System/Mission/MissionTracker.cs`
- `Assets/Scripts/GameFlow/States/GameplayState.cs`
- `Assets/Scripts/Enemy/EnemyController.cs`

（詳見第六節）

---

## 五、InitialVoterSpawnReady 訊號設計

**不使用**固定等待幀數/秒數作為「生成完成」依據，原因：幀數受 FPS 影響，秒數受機器效能影響，無法保證確實等到生成完成。

**訊號傳遞鏈**：

```
Spawner.StartSpawningRoutine()
  └─ 初始生成迴圈：for (int i = 0; i < initialSpawnCount; i++)
       └─ SpawnAtRandomPoint()
       └─ if (i % 5 == 0) yield return null
  └─ 迴圈結束後（第一個 while(true) 之前）：
       BattleEventManager.TriggerVoterSpawnReady()   ← 新增

BattleEventManager.OnVoterSpawnReady   ← 新增靜態事件

MissionTracker（FinalBoss 路徑）：
  Subscribe() 時訂閱 OnVoterSpawnReady
  OnVoterSpawnReady 到達 → _votersReady = true
  CheckFinalBossVotePercent() 加入 if (!_votersReady) return;
  Unsubscribe() 時一併解除訂閱
```

**訊號觸發條件**：初始生成迴圈結束後立即觸發，不等波次生成。此後 `spawnPerTick = 0` 的 Boss_Arena 場景不再生成新選民，所以初始完成即最終完成。

**邊界情況**：若 `OnVoterSpawnReady` 先於 `Subscribe()` 觸發（Spawner.Start() 先於 MissionTracker.Start() 執行），MissionTracker 會錯過訊號，`_votersReady` 永不設為 true，FinalBoss 永遠不結算。  
→ 對策：MissionTracker.Subscribe() 內，若 Spawner 已完成生成，直接設 `_votersReady = true`（檢查 Spawner 的 `IsInitialSpawnComplete` 旗標，而非依賴事件）。

---

## 六、診斷日誌方案

所有診斷日誌以 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 包覆，限制頻率，不在正式 Build 輸出。

### 6-A：結算診斷（MissionTracker）

觸發 `NotifyResult()` 時記錄：

```
[MissionTracker][結算] 來源={Success/Failure}
  Phase={Active/Briefing/...}
  場景={ActiveRoom.sceneName}
  節點={CurrentNodeNumber}
  票數={GetCurrentPlayerVotes()}/{GetCurrentTotalVotes()} ({percent:F1}%)
  門檻={targetValue}%
  _votersReady={true/false}
  _subscribed={true/false}
  剩餘時間={LevelTimer.Remaining:F1}s（若計時中）
```

觸發 T 鍵結算時記錄（`GameplayState.Update()`）：
```
[GameplayState][T鍵] 強制成功結算 @ Phase=Active
```

### 6-B：Boss 導航診斷（EnemyController）

Start() 與重試 Coroutine 中記錄（**非每幀**，只在狀態改變時）：

```
[EnemyController][NavMesh] {name}
  位置={transform.position}
  isOnNavMesh={Agent.isOnNavMesh}
  嘗試SamplePosition={成功/失敗}  命中點={hit.position}
  Warp={成功/失敗}   isOnNavMesh_after={Agent.isOnNavMesh}
  重試次數={n}/3
```

EnemyMoveState 每次進入時記錄（限每個 Boss 實例只印一次）：

```
[EnemyMoveState][Enter] {name}
  isOnNavMesh={true/false}
  isStopped={Agent.isStopped}
  target={target?.name ?? null}
  pathStatus={Agent.pathStatus}  pathPending={Agent.pathPending}
  velocity={Agent.velocity.magnitude:F2}
```

**頻率限制**：NavMesh 相關訊息只在狀態首次進入或失敗時輸出；EnemyMoveState 的 Update 中不加 Debug.Log，避免每幀洗版。

---

## 七、驗收矩陣

每個場景至少測試 3 次，建議連續 10 次，記錄每次結果。

| 測試情境 | 預期結果 | 驗收狀態 |
|---|---|---|
| 首次進入 Boss_Arena（冷啟動） | 選民全數生成後才開始票數輪詢，無提前結算 | 待 Unity 測試 |
| 連續重進 Boss_Arena（第 2、3 次） | 物件池 reset 後選民初始票數不殘留上一關數值 | 待 Unity 測試 |
| 由第 11 關正常進入第 12 關（完整流程） | Boss 正常生成並移動，票數輪詢從正確基準開始 | 待 Unity 測試 |
| Boss 生成點隨機偏移後仍在 NavMesh 上 | Boss 正常移動（Console 無 Warp 失敗訊息） | 待 Unity 測試 |
| Boss 生成點落在 NavMesh 邊緣（模擬失敗） | 重試後成功定位，或回報失敗後原地待機而非崩潰 | 待 Unity 測試 |
| Boss 施法並暈眩玩家後恢復移動 | 技能結束後 IdleState → MoveState 正常銜接 | 待 Unity 測試 |
| 票數達到 70% 後正常勝選 | `CheckFinalBossVotePercent()` 觸發，計時停止，正確走 StageClear | 待 Unity 測試 |
| 時限到未達 70% 後正常敗選 | `HandleTimerExpired()` 觸發，走 GameEnd | 待 Unity 測試 |
| 玩家 HP 歸零與票數達標同幀 | 玩家死亡優先（`CommitObjectiveNextFrame` 的 guard 生效） | 待 Unity 測試 |
| T 鍵在 Active 階段觸發 | 正確結算成功，Console 印出 T 鍵來源標記 | 待 Unity 測試 |
| 連續 10 次完整流程 | 記錄每次是否提前結算、Boss 是否移動、結算來源 | 待 Unity 測試 |

**特別要求**：不得宣稱未在 Unity 執行的測試已通過。

---

## 八、計畫文件 vs 舊節點設定差異

| 比對項目 | 計畫文件假設 | 實際 repo 設定 | 採用依據 |
|---|---|---|---|
| Boss 技能數量 | — | 三招（B_Stun / B_Damage / B_Summon） | 以 `Boss_ .prefab` equippedSkills 為準 |
| 勝利門檻 | 70% | `M_Boss.targetValue = 70` | 一致，維持不變 |
| Boss 關 SpawnConfig | FinalBossSpawnConfig.asset | 實際使用 `S_Boss.asset`（EliminateAllSpawnConfig，count=1） | 以 M_Boss.asset 引用為準；FinalBossSpawnConfig 存在但未被使用 |
| 初始選民數 | 未知 | `Spawner.initialSpawnCount = 30, spawnPerTick = 0` | 以 Boss_Arena.unity Inspector 為準 |

---

## 九、尚待確認事項（無法從 repo 靜態取得）

1. `VoterLogic.OnEnable()` 後 `Data.ConvertedSide` 的初始值——物件池 reset 時是否清除？若否，進關時分母完整但分子已有殘留值，是提前結算的確切觸發條件。
2. `LevelTimer.TotalDuration` 在 Boss_Arena 場景的 Inspector 設定值。
3. Boss_Arena 的 NavMesh 是否已重新烘焙、覆蓋所有生成點。
4. Boss 的生成 spawnPoint 確切世界座標。
5. `Spawner.Start()` 與 `MissionTracker.Start()` 在同場景中的執行順序（Unity 保證在同一幀，但 `Start()` 的順序由 Script Execution Order 決定）——若 Spawner 先完成再觸發 `OnVoterSpawnReady`，MissionTracker 是否已完成 Subscribe？

---

## 十、下一步

1. ChatGPT 審核此計畫
2. 使用者在 Unity Console 中確認：
   - 是否有 `[EnemyController] Warp 失敗` 訊息
   - 是否有 `[MissionTracker]` 提前結算訊息（需先加入診斷 log 後測試）
3. 確認第九節五項待確認事項
4. ChatGPT 確認後，依核准範圍實作修正一、二、三

---

**維持不變**：Boss 三技能（B_Stun / B_Damage / B_Summon）、70% 勝利門檻、選民 AI 邏輯、Task 07/08 的既有工作。
