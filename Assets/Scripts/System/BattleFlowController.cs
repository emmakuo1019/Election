using UnityEngine;

public class BattleFlowController : MonoBehaviour
{
    public static BattleFlowController Instance { get; private set; }

    [Header("引用")]
    [SerializeField] private LevelTimer levelTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (levelTimer != null)
            levelTimer.OnTimerEnd += OnBattleTimeEnd;

        // EliminateAll 任務：選完獎勵後顯示出口提示
        BattleEventManager.OnRewardCollected += OnRewardCollected;
    }

    private void OnDisable()
    {
        if (levelTimer != null)
            levelTimer.OnTimerEnd -= OnBattleTimeEnd;

        BattleEventManager.OnRewardCollected -= OnRewardCollected;
    }

    private void Start()
    {
        if (levelTimer == null)
            levelTimer = FindFirstObjectByType<LevelTimer>();
    }

    // ── 事件處理 ─────────────────────────────────────────────────────

    /// <summary>計時結束：僅 Survive 任務才執行清場與獎勵流程；其他任務類型忽略此事件</summary>
    private void OnBattleTimeEnd()
    {
        // 只有 Survive 任務才依賴計時器結束作為勝利條件
        // EliminateAll / ReachVotePercent 等任務有各自的結算路徑，計時器對它們無意義
        var mission = GameDB.Instance?.Campaign.ActiveRoom.mission;
        if (mission == null || mission.objectiveType != MissionObjectiveType.Survive)
        {
            Debug.Log($"[BattleFlowController] 計時結束，但任務類型為 {mission?.objectiveType.ToString() ?? "null"}，略過 Survive 流程。");
            return;
        }

        bool isLastRoomInBlock = GameDB.Instance?.Campaign != null &&
                                 GameDB.Instance.Campaign.HasBlockProgress() &&
                                 GameDB.Instance.Campaign.IsLastRoomInBlock();
        int playerVotes   = GameDB.Instance?.Run.PlayerVotes   ?? 0;
        int opponentVotes = GameDB.Instance?.Run.OpponentVotes ?? 0;

        if (isLastRoomInBlock && playerVotes <= opponentVotes)
        {
            GameDB.Instance?.Campaign.SetNextSceneOverride("endGamePanel");
            GameDB.Instance?.Campaign.FailCurrentBlock();
        }

        // Survive 任務計時結束後：
        // 1. 停止敵人生成並清除場上所有敵人
        // 2. 重置 EnemySpawnTracker（確保 alive count 歸零，不觸發 counter UI）
        // 3. 強制讓選民離場
        // 4. 觸發獎勵流程（與 EliminateAll 相同路徑）
        var spawner = FindFirstObjectByType<EnemySpawner>();
        spawner?.ClearAllEnemies();
        EnemySpawnTracker.StopTracking();

        DespawnVoters();
        Debug.Log("[BattleFlowController] Survive 計時結束，觸發獎勵流程");
        BattleEventManager.TriggerAllEnemiesDefeated();
    }

    /// <summary>任何任務：玩家選完獎勵 → 暫停計時器、開出口、顯示出口提示</summary>
    private void OnRewardCollected()
    {
        levelTimer?.PauseTimer();
        ShowExit();
    }

    // ── 出口處理 ─────────────────────────────────────────────────────

    /// <summary>強制讓場上所有選民走向出口（Survive 計時結束時用）</summary>
    private void DespawnVoters()
    {
        var exit = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        if (exit != null)
        {
            Vector3 exitPos = exit.GetVoterExitPosition();
            foreach (var voter in FindObjectsByType<VoterLogic>(FindObjectsSortMode.None))
                voter.BeginExitMovement(exitPos);
        }
    }

    /// <summary>解鎖出口並顯示出口提示</summary>
    private void ShowExit()
    {
        var exit = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        if (exit != null)
        {
            exit.UnlockExit();
            UIManager.Instance?.ShowExitPrompt();
        }
        else
        {
            Debug.LogWarning("⚠️ 找不到 RoomExitController");
        }
    }
}
