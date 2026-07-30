using UnityEngine;

public class BattleFlowController : MonoBehaviour
{
    public static BattleFlowController Instance { get; private set; }

    [Header("引用")]
    [SerializeField] private LevelTimer levelTimer;
    [SerializeField] private Transform[] voterExitPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        BattleEventManager.OnAllEnemiesDefeated += OnAllEnemiesDefeated;
        BattleEventManager.OnRoomCleared += OnRoomCleared;
    }

    private void OnDisable()
    {
        BattleEventManager.OnAllEnemiesDefeated -= OnAllEnemiesDefeated;
        BattleEventManager.OnRoomCleared -= OnRoomCleared;
    }

    private void Start()
    {
        if (levelTimer == null)
            levelTimer = FindFirstObjectByType<LevelTimer>();

        // 啟動敵人追蹤，以敵人全滅作為房間結束條件
        EnemySpawnTracker.StartTracking();
    }

    private void OnDestroy()
    {
        EnemySpawnTracker.StopTracking();
    }

    /// <summary>
    /// 敵人全滅：解鎖出口，選民開始離場
    /// </summary>
    private void OnAllEnemiesDefeated()
    {
        levelTimer?.PauseTimer();
        UnlockExitAndForceVotersLeave();
    }

    /// <summary>
    /// 玩家走到出口：執行勝負結算
    /// </summary>
    private void OnRoomCleared()
    {
        bool isLastRoomInBlock = GameDB.Instance != null && GameDB.Instance.Campaign != null &&
                                 GameDB.Instance.Campaign.HasBlockProgress() &&
                                 GameDB.Instance.Campaign.IsLastRoomInBlock();
        int playerVotes = GameDB.Instance != null ? GameDB.Instance.Run.PlayerVotes : 0;
        int opponentVotes = GameDB.Instance != null ? GameDB.Instance.Run.OpponentVotes : 0;
        bool canClaimReward = playerVotes > opponentVotes;

        if (isLastRoomInBlock && !canClaimReward)
        {
            if (GameDB.Instance != null && GameDB.Instance.Campaign != null)
            {
                GameDB.Instance.Campaign.SetNextSceneOverride("endGamePanel");
                GameDB.Instance.Campaign.FailCurrentBlock();
            }
        }
    }

    private void UnlockExitAndForceVotersLeave()
    {
        RoomExitController roomExitController = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        if (roomExitController != null)
        {
            roomExitController.UnlockExit();
            UIManager.Instance?.ShowExitPrompt();
        }
        else
        {
            Debug.LogWarning("⚠️ 找不到 RoomExitController，無法開啟出口");
        }

        VoterLogic[] voters = FindObjectsByType<VoterLogic>(FindObjectsSortMode.None);
        foreach (VoterLogic voter in voters)
        {
            voter.BeginExitMovement(GetRandomExitPoint());
        }
    }

    private Vector3 GetRandomExitPoint()
    {
        if (voterExitPoints != null && voterExitPoints.Length > 0)
            return voterExitPoints[Random.Range(0, voterExitPoints.Length)].position;

        // ponytail: fallback 用 RoomExitController，若場景未設定退場點才走這裡
        RoomExitController exit = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        return exit != null ? exit.GetVoterExitPosition() : Vector3.zero;
    }
}
