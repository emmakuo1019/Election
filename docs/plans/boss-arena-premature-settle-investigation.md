# Boss 關間歇性提前結算與 Boss 停止移動：調查與修復記錄

日期：2026-10-15  
最後更新：2026-10-15  
PR：#3  
Branch：`docs/enemy-behavior-plan-20261006`  
狀態：**提前結算已修復並通過使用者實測；Boss 偶發不移動尚未重現，根因未確認。**

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
  objectiveType: 0  ← 已修正（原為 1/ReachVotePercent，現為 0/EliminateAll）
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
```

---

## 三、問題一：Boss 關提前結算

### 根因（已確認）

**`M_Boss.asset` 的 `objectiveType` 設為 `1`（`ReachVotePercent`），應為 `0`（`EliminateAll`）。**

`MissionObjectiveType` 定義（`Assets/Scripts/System/Mission/MissionObjectiveType.cs`）：
```csharp
public enum MissionObjectiveType
{
    EliminateAll,      // 0
    ReachVotePercent,  // 1
    Survive,           // 2
}
```

### 錯誤程式路徑

`MissionTracker.Update()` 的判斷順序（`MissionTracker.cs` L89–L108）：

```csharp
// 搶票任務（普通關）：Update 輪詢即時票數
if (_mission != null && _mission.objectiveType == MissionObjectiveType.ReachVotePercent)
{
    CheckReachVotePercent();   // ← M_Boss objectiveType=1 時永遠走這條
    return;
}

// FinalBoss 路徑（只在上方 if 不成立時才執行）
if (IsFinalBossEncounter())
{
    CheckFinalBossVotePercent();
}
```

因為 `objectiveType == ReachVotePercent` 先被命中，`IsFinalBossEncounter()` 的分支永遠不執行。`CheckReachVotePercent()`（L232–L256）以 `GetCurrentVotePercentage() * 100f >= targetValue` 直接觸發結算，分母是場上**實際有效選民數**。

### 放大因子

`Boss_Arena.unity` 的 `VoterSpawner` `initialSpawnCount = 30`，但 spawnPoints 部分位置在 NavMesh 範圍外，`Failed to create agent` 使部分選民未進入 `allActiveVoters`。診斷 log 實測時出現分母 = 1 的情況，導致任何一隻玩家陣營選民即達 100% ≥ 70%，立即觸發勝利結算。

### 診斷 log 實測結果（使用者提供，第 3 次測試）

```
[Spawner] Failed to create agent because it is not close enough to the NavMesh
[EnemyController][NavMesh 診斷] Boss_ (Clone) | 位置=(11.90, 0.05, 22.11) | isOnNavMesh=True（無需 Warp）
[MissionTracker][FinalBoss 結算] 結果=勝利 | Phase=Active | 票數=1/1（100.0%） 門檻=70% | 剩餘時間=60.0s | _subscribed=True
  → stack trace: MissionTracker:CheckReachVotePercent() ← 確認走錯分支
```

### 修正

**`Assets/Data/Mission/M_Boss.asset`** `objectiveType: 1` → `objectiveType: 0`

Commit：`d7243c5`  
修正後使用者實測：**通過**（不再提前結算）。

### 未實作的項目

- **VoterSpawnReady gate**：診斷結果顯示根因是 objectiveType 設錯，分母偏小只是放大因子。修正 objectiveType 後，FinalBoss 路徑不再依賴 `CheckReachVotePercent()`，`VoterSpawnReady gate` 不需要實作。

---

## 四、問題二：Boss_Arena Spawner NavMesh 失敗

### 現況（已確認，尚未修正）

`Boss_Arena.unity` 的 VoterSpawner `initialSpawnCount = 30`，spawnPoints 部分位置在 NavMesh 外，造成部分選民無法生成 NavMeshAgent。

診斷 log 每次進 Boss 關均出現：
```
Failed to create agent because it is not close enough to the NavMesh
  → Spawner:SpawnAtRandomPoint() → PoolManager:Get → SetActive
```

Working tree 已有 `Assets/Scenes/Boss_Arena.unity` 與 `Assets/Scenes/Boss_Arena/NavMesh-NavMesh Surface.asset` 的本地修改（場景美術更新），使用者計劃後續一起 push。

### 後續工作

- 確認 Boss_Arena 所有 spawnPoints 的世界座標是否在 NavMesh 上
- 重新烘焙或調整 spawnPoints 位置後確認 `[Spawner][初始生成完成]` log 中失敗數為 0
- **不在本次修正範圍內**

---

## 五、問題三：Boss 偶發不移動

### 現況（未重現，根因未確認）

初始報告中第 2 次出現 Boss 不移動但仍施法的症狀。診斷加入後的三次實測中，NavMesh 診斷 log 均顯示：
```
[EnemyController][NavMesh 診斷] Boss_ (Clone) | isOnNavMesh=True（無需 Warp）
```
未重現不移動症狀。

### 已知的可能路徑（靜態分析，未從 log 確認）

若 `isOnNavMesh=False` 且 `Warp` 失敗：`EnemyMoveState.Update()` 的 `if (ctx.Agent.isOnNavMesh)` 判斷不成立，`SetDestination` 不被呼叫，Boss 停在原地但技能排程不受影響，故仍可施法。

### 狀態

**不標記為已修復。** 三次實測未重現，不能確認問題消失。等下次重現時提供完整 `[NavMesh 診斷]` log。

---

## 六、診斷 log 說明

以下三處診斷 log 已在 commit `1a6cb6a` 加入，均以 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 保護：

| 位置 | 觸發條件 | 說明 |
|---|---|---|
| `MissionTracker.NotifyResult()` | 每次結算（一次性） | 記錄勝敗、Phase、票數分子/分母/百分比、門檻、剩餘時間 |
| `MissionTracker.CheckFinalBossVotePercent()` | FinalBoss Active 階段，每 3 秒 | 輪詢摘要，避免每幀洗版 |
| `MissionTracker.HandleTimerExpired()` | FinalBoss 時限到 | 記錄敗選時的票數 |
| `EnemyController.Start()` | EliteBoss 角色初始化（一次性） | NavMesh SamplePosition/Warp 結果 |
| `Spawner.StartSpawningRoutine()` | 初始生成完成（一次性） | 預計/實際/失敗選民數 |

---

## 七、本次相關 commit 清單

| SHA | 說明 |
|---|---|
| `c0769c1` | 初始調查計畫文件 |
| `1a6cb6a` | 診斷 log 加入（MissionTracker、EnemyController、Spawner） |
| `d7243c5` | **根因修正**：M_Boss.asset objectiveType 0（EliminateAll） |

---

## 八、待辦

| 項目 | 優先 | 說明 |
|---|---|---|
| Boss_Arena spawnPoints NavMesh 覆蓋確認 | 中 | 重新烘焙或調整位置，使 Failed to create agent 歸零 |
| Boss 偶發不移動根因確認 | 低 | 等下次重現時提供 NavMesh 診斷 log |
| Task 08 索敵節流與 Inspector 整理 | 計畫中 | 見 enemy-behavior-implementation-plan.md Task 08 |
