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
        bool isFinalBoss = GameDB.Instance?.Campaign.CurrentRole == EncounterNodeRole.FinalBoss;

        // EliminateAll 任務：敵人已死，不需主動清場；但 FinalBoss 是特例——
        // Boss 無敵永遠不死，結算後仍在場上繼續攻擊，所以 FinalBoss 結算後強制清場。
        if (!isFinalBoss && (mission == null || mission.objectiveType == MissionObjectiveType.EliminateAll))
            return;

        // Survive、搶票任務，以及 FinalBoss：結算後清掉未完成波次與現場敵人，
        // 避免玩家在選卡或結局演出時仍被攻擊。
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
