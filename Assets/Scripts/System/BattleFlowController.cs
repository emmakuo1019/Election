using UnityEngine;

/// <summary>處理場景端清理；任務判定與戰役推進一律交由 MissionTracker / StageClearState。</summary>
public class BattleFlowController : MonoBehaviour
{
    [SerializeField] private LevelTimer levelTimer;

    private void Start()
    {
        if (levelTimer == null) levelTimer = FindFirstObjectByType<LevelTimer>();
        BattleEventManager.OnObjectiveResolved += HandleObjectiveResolved;
    }

    private void OnDestroy()
    {
        BattleEventManager.OnObjectiveResolved -= HandleObjectiveResolved;
    }

    private void HandleObjectiveResolved(EncounterOutcome outcome)
    {
        levelTimer?.PauseTimer();
        RoomMissionData mission = GameDB.Instance?.Campaign.ActiveRoom.mission;
        if (mission == null || mission.objectiveType == MissionObjectiveType.EliminateAll) return;

        // Survive 與搶票任務一旦結算，就清掉未完成波次與現場敵人。
        // 否則玩家在失勢選卡時仍可能被前一關的敵人打到歸零，形成錯誤的第二次失敗。
        FindFirstObjectByType<EnemySpawner>()?.ClearAllEnemies();
        EnemySpawnTracker.StopTracking();
        DespawnVoters();
    }

    private void DespawnVoters()
    {
        RoomExitController exit = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        if (exit == null) return;
        Vector3 exitPosition = exit.GetVoterExitPosition();
        foreach (VoterLogic voter in FindObjectsByType<VoterLogic>(FindObjectsSortMode.None))
            voter.BeginExitMovement(exitPosition);
    }
}
