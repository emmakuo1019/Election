using UnityEngine;
public class BattleFlowController : MonoBehaviour
{
    public static BattleFlowController Instance { get; private set; }

    public enum BattleState
    {
        WaitingStart,
        Fighting,
        Completed,
        Failed
    }

    [Header("引用")]
    [SerializeField] private LevelTimer levelTimer;
    [SerializeField] private bool autoStartBattle = false;

    public BattleState CurrentState { get; private set; } = BattleState.WaitingStart;

    public bool IsBattleActive => CurrentState == BattleState.Fighting;

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

        if (autoStartBattle)
        {
            StartBattle();
        }
    }

    public void StartBattle()
    {
        if (CurrentState == BattleState.Fighting)
        {
            return;
        }

        CurrentState = BattleState.Fighting;
        levelTimer?.StartTimer();
    }

    private void OnBattleTimeEnd()
    {
        bool isLastRoomInBlock = BlockProgressManager.HasBlockProgress() && BlockProgressManager.IsLastRoomInBlock();
        int playerVotes = VoteManager.Instance != null ? VoteManager.Instance.PlayerVotes : 0;
        int opponentVotes = VoteManager.Instance != null ? VoteManager.Instance.OpponentVotes : 0;
        bool canClaimReward = playerVotes > opponentVotes;

        if (!isLastRoomInBlock)
        {
            CurrentState = BattleState.Completed;
            UnlockExitAndForceVotersLeave();
            return;
        }

        CurrentState = canClaimReward ? BattleState.Completed : BattleState.Failed;

        if (isLastRoomInBlock && !canClaimReward)
        {
            BlockProgressManager.SetNextSceneOverride("endGamePanel");
            BlockProgressManager.FailCurrentBlock();
        }

        UnlockExitAndForceVotersLeave();
    }

    private void UnlockExitAndForceVotersLeave()
    {
        // 暫停計時
        levelTimer?.PauseTimer();

        // 尋找出口並解鎖
        RoomExitController roomExitController = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        if (roomExitController != null)
        {
            roomExitController.UnlockExit();
            
            // 強制選民離場
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

    public void OnRewardSelected()
    {
        CurrentState = BattleState.Completed;
    }
}
