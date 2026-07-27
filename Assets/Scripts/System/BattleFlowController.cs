using UnityEngine;
public class BattleFlowController : MonoBehaviour
{
    public static BattleFlowController Instance { get; private set; }

    [Header("引用")]
    [SerializeField] private LevelTimer levelTimer;

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
        if (levelTimer != null)
            levelTimer.OnTimerEnd += OnBattleTimeEnd;
    }

    private void OnDisable()
    {
        if (levelTimer != null)
            levelTimer.OnTimerEnd -= OnBattleTimeEnd;
    }

    private void Start()
    {
        if (levelTimer == null)
        {
            levelTimer = FindFirstObjectByType<LevelTimer>();
        }
    }

    private void OnBattleTimeEnd()
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

        UnlockExitAndForceVotersLeave();
    }

    private void UnlockExitAndForceVotersLeave()
    {
        levelTimer?.PauseTimer();

        RoomExitController roomExitController = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        if (roomExitController != null)
        {
            roomExitController.UnlockExit();
            UIManager.Instance?.ShowExitPrompt();
            
            Vector3 exitPosition = roomExitController.GetVoterExitPosition();
            VoterLogic[] voters = FindObjectsByType<VoterLogic>(FindObjectsSortMode.None);
            foreach (VoterLogic voter in voters)
            {
                voter.BeginExitMovement(exitPosition);
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 找不到 RoomExitController，無法開啟出口或強制選民離場");
        }
    }
}
