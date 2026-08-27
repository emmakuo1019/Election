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

    /// <summary>Survive 任務：計時結束 → 生成獎勵卡（與 EliminateAll 共用相同流程）</summary>
    private void OnBattleTimeEnd()
    {
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

        // Survive 任務計時結束後，強制讓選民離場，再觸發獎勵流程
        // 出口的開放與 EliminateAll 相同：等玩家選完獎勵（OnRewardCollected）後才顯示
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
