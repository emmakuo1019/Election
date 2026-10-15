# Boss 關間歇性提前結算與 Boss 停止移動：調查與修復記錄

日期：2026-10-15  
最後更新：2026-10-15（診斷補正）  
PR：#3  
Branch：`docs/enemy-behavior-plan-20261006`  
狀態：**objectiveType 設定已修正，使用者實測未再發生；提前結算根因尚待完整證明。**

---

## 一、使用者實測症狀

| 次數 | 症狀 |
|---|---|
| 第 1 次 | 進 Boss 關後直接結算（未與 Boss 交戰） |
| 第 2 次 | Boss 不移動，但仍施放技能 |
| 第 3 次 | 正常 |

---

## 二、資產鏈（已靜態核對）

```
M_Boss.asset
  objectiveType: 0  ← commit d7243c5 修正（原為 1/ReachVotePercent）
  targetValue:   70 (FinalBoss 票數門檻 %)
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
  LevelTimer     → TotalDuration = 06s（使用者確認）
  MissionTracker → 場景中存在

注意：Boss_Arena.unity 與 NavMesh Surface.asset 本地有未提交修改
（場景美術更新），本次分析基準為 HEAD d7243c5，未納入本地修改。
```

---

## 三、問題一：Boss 關提前結算

### 3-A 設定修正

**`M_Boss.asset objectiveType: 1 → 0`**（commit `d7243c5`）

`MissionObjectiveType` 定義（`Assets/Scripts/System/Mission/MissionObjectiveType.cs`）：
```csharp
public enum MissionObjectiveType
{
    EliminateAll,      // 0
    ReachVotePercent,  // 1
    Survive,           // 2
}
```

`objectiveType=1` 使 `MissionTracker.Update()` L89–91 的判斷先被命中：
```csharp
// MissionTracker.cs L89
if (_mission != null && _mission.objectiveType == MissionObjectiveType.ReachVotePercent)
{
    CheckReachVotePercent();   // 此路徑直接呼叫 NotifyResult(true)
    return;                    // FinalBoss 分支永遠不執行
}
```

修正後 `objectiveType=0`，此 if 不成立，執行 `IsFinalBossEncounter()` 分支。

### 3-B 兩條路徑的實際差異（完整呼叫鏈）

**路徑 A（修正前，objectiveType=1）：`CheckReachVotePercent`**

```
Start() → DelayedSubscribe() → Subscribe()
  → case ReachVotePercent: BattleEventManager.OnTimerExpired += HandleTimerExpired

Update() → objectiveType == ReachVotePercent → CheckReachVotePercent()  [MissionTracker.cs L232]
  percent = GetCurrentVotePercentage() * 100f
  if percent >= _mission.targetValue          // 同樣是 targetValue=70
    PauseTimer()
    OnTimerExpired -= HandleTimerExpired
    NotifyResult(true)                        // 直接結算
```

**路徑 B（修正後，objectiveType=0）：`CheckFinalBossVotePercent`**

```
Start() → IsFinalBossEncounter() == true → Subscribe()
  → OnFinalBossDefeated += HandleFinalBossDefeated
  → OnTimerExpired += HandleTimerExpired

Update() → IsFinalBossEncounter() → CheckFinalBossVotePercent()  [MissionTracker.cs L199]
  percent = GetCurrentVotePercentage() * 100f
  if percent >= targetPct                     // 同樣是 targetValue=70
    PauseTimer()
    OnTimerExpired -= HandleTimerExpired
    TriggerFinalBossDefeated()               // 觸發事件
      → HandleFinalBossDefeated()
        → NotifyResult(true)                 // 多一層事件跳轉，最終結果相同
```

**結論：在相同票數（1/1 = 100%）、相同門檻（70%）、相同 Phase（Active）下，兩條路徑均以 `NotifyResult(true)` 結算勝利。路徑 B 多一層 `TriggerFinalBossDefeated → HandleFinalBossDefeated` 的事件跳轉，但不改變結算結果。**

因此，objectiveType 修正可以**證明分支已改變**，但**不足以單獨證明消除提前結算**——若分母仍偏小，路徑 B 同樣會在分母為 1 時達到 100% ≥ 70% 並觸發勝利。

### 3-C 診斷 log 實測結果（使用者提供，第 3 次觸發時）

```
Failed to create agent because it is not close enough to the NavMesh
  → Spawner:SpawnAtRandomPoint()（選民生成失敗，進入 allActiveVoters 的數量偏少）

[EnemyController][NavMesh 診斷] Boss_ (Clone) | 位置=(11.90, 0.05, 22.11) | isOnNavMesh=True（無需 Warp）

[MissionTracker][FinalBoss 結算] 結果=勝利 | Phase=Active | 票數=1/1（100.0%） 門檻=70% | 剩餘時間=60.0s | _subscribed=True
  stack trace: MissionTracker:CheckReachVotePercent() ← 確認走路徑 A
```

此次分母為 1，一隻玩家陣營選民即達 100%，屬於路徑 A 的確認案例。

### 3-D 使用者實測（修正後）

修正 objectiveType 後，使用者進行實測，**未再發生提前結算**。

**但需注意**：使用者本地的 `Boss_Arena.unity` 與 `NavMesh Surface.asset` 在實測前已有修改（場景美術更新含 NavMesh 相關 asset 變動，475 行差異）。實測時的 NavMesh 烘焙狀態可能與 HEAD 不同，分母是否仍偏小未能確認。

因此，當前實測結論為：**設定修正 + 本地場景狀態下未再發生**，提前結算根因尚待以下對照驗證確認。

### 3-E 最小對照驗證設計

以下測試項目均尚未執行，標記 **待測**。

**目標**：在相同票數條件下，比較兩條路徑的行為差異。

**測試 1：路徑改變確認（待測）**

- HEAD（objectiveType=0）進 Boss_Arena，不做任何動作，等待 3 秒
- 預期 log：
  ```
  [MissionTracker][FinalBoss 輪詢] ...（路徑 B 的 3 秒輪詢）
  ```
  不應出現 `CheckReachVotePercent` 的結算 log
- 記錄：輪詢 log 的票數分母是否 ≥ 2

**測試 2：分母偏小時路徑 B 的行為（待測）**

- 若 Spawner 生成完成 log 顯示實際在場 ≤ 2，記錄：
  - 是否出現提前結算
  - 結算 log 中的「呼叫來源」欄位（修正後 log 新增此欄）
  - 若結算，確認是 `HandleFinalBossDefeated`（路徑 B 正常）還是其他來源

**測試 3：正常進場的時序（待測）**

記錄以下 log 的出現順序：
```
[Spawner][初始生成完成] ...   ← 初始選民生成完成
[MissionTracker][FinalBoss 輪詢] ...  ← 第一次 3 秒輪詢
[MissionTracker][FinalBoss 結算] ...  ← 若觸發
```
確認結算是否發生在「初始生成完成」之後。若結算早於生成完成，代表分母不完整時就達標。

**紀錄格式**（每次測試填寫）：
```
測試次數：
Spawner log（預計/實際/差值）：
第一條輪詢 log（票數/門檻/Phase）：
是否提前結算（Y/N）：
結算 log 呼叫來源：
時序：生成完成在結算之前 Y/N：
```

---

## 四、問題二：Boss_Arena Spawner 選民數量偏少

### 現況（已確認，尚未修正）

多次進 Boss 關均出現 `Failed to create agent because it is not close enough to the NavMesh`，來源是 `Spawner.SpawnAtRandomPoint()`。部分 spawnPoints 位置不在有效 NavMesh 上。

**注意**：「實際在場」與「預計生成」的差值可能來自 NavMesh 失敗、物件池不足或其他原因，需對照 Console 中 `Failed to create agent` 的出現次數確認。

### 本地未提交修改

使用者本地 `Boss_Arena.unity`（+376/-99 行）與 `NavMesh Surface.asset`（57588→59772 bytes）已有修改，可能包含 NavMesh 重新烘焙。待使用者提交後，以 `[Spawner][初始生成完成]` log 確認差值是否歸零。

### 後續工作（待測）

- 提交 Boss_Arena 場景更新後，進 Boss 關確認 `[Spawner][初始生成完成]` 中差值
- 若差值不為 0，確認哪些 spawnPoints 座標在 NavMesh 外並調整
- **不在本次提交範圍**

---

## 五、問題三：Boss 偶發不移動

### 現況（未重現，根因未確認）

初始報告第 2 次出現 Boss 不移動但仍施法。診斷 log 加入後三次實測均顯示：
```
[EnemyController][NavMesh 診斷] Boss_ (Clone) | isOnNavMesh=True（無需 Warp）
```

**不標記為已修復。** 三次實測未重現，不能確認問題消失。

已知可能路徑（靜態分析，未從 log 確認）：若 `isOnNavMesh=False` 且 Warp 失敗，`EnemyMoveState.Update()` 的 `if (ctx.Agent.isOnNavMesh)` 不成立，`SetDestination` 不執行，Boss 靜止但技能排程不受影響。

等下次重現時提供完整 `[NavMesh 診斷]` log（含「原始位置」、「Warp 位移」欄位，commit `本次` 已補正）。

---

## 六、診斷 log 現況

所有 log 均以 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 保護。

| 位置 | 觸發條件 | 本次補正 |
|---|---|---|
| `MissionTracker.NotifyResult()` | FinalBoss 結算（一次性） | 新增「呼叫來源」欄位 |
| `MissionTracker.CheckFinalBossVotePercent()` | Active 階段每 3 秒 | 無變更 |
| `MissionTracker.HandleTimerExpired()` | FinalBoss 時限到 | 無變更 |
| `EnemyController.Start()` | EliteBoss 初始化（一次性） | Warp 前保存原始位置，輸出 Warp 位移距離 |
| `Spawner.StartSpawningRoutine()` | 初始生成完成（一次性） | 「差值」說明改為中性，不直接稱 NavMesh 失敗數 |

---

## 七、commit 清單

| SHA | 說明 |
|---|---|
| `c0769c1` | 初始調查計畫文件 |
| `1a6cb6a` | 診斷 log 加入（MissionTracker、EnemyController、Spawner） |
| `d7243c5` | M_Boss.asset objectiveType 1→0 |
| `5edfd0d` | 交付記錄（首版，已被本次補正） |
| 本次 | 診斷補正：呼叫鏈差異分析、log 修正、對照驗證設計 |

---

## 八、待辦

| 項目 | 優先 | 狀態 |
|---|---|---|
| 對照驗證測試 1/2/3（見 3-E） | 高 | ⏳ 待測 |
| 提交 Boss_Arena 場景更新後確認分母 | 高 | ⏳ 待使用者提交 |
| Boss 偶發不移動根因確認 | 低 | ⏳ 等重現 |
| Task 08 索敵節流與 Inspector 整理 | 計畫中 | 見 enemy-behavior-implementation-plan.md |
