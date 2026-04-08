using UnityEngine;

public class BattleFlowController : MonoBehaviour
{
    public static BattleFlowController Instance { get; private set; }

    public enum BattleState
    {
        WaitingStart,
        Fighting,
        RewardSelecting,
        Completed,
        Failed
    }

    [Header("引用")]
    [SerializeField] private LevelTimer levelTimer;
    [SerializeField] private RewardPanelController rewardUI;
    [SerializeField] private RoomExitController roomExitController;
    [SerializeField] private RoomClearFlowController roomClearFlowController;

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
        if (roomClearFlowController == null)
        {
            roomClearFlowController = FindFirstObjectByType<RoomClearFlowController>();
        }

        StartBattle();
    }

    public void StartBattle()
    {
        VoteManager.Instance?.ResetVotes();

        CurrentState = BattleState.Fighting;
        levelTimer?.StartTimer();

        Debug.Log("⚔️ [BattleFlow] 戰鬥開始");
    }

    private void OnBattleTimeEnd()
    {
        int playerVotes = VoteManager.Instance != null ? VoteManager.Instance.PlayerVotes : 0;
        int opponentVotes = VoteManager.Instance != null ? VoteManager.Instance.OpponentVotes : 0;

        if (playerVotes >= opponentVotes)
        {
            CurrentState = BattleState.RewardSelecting;
            Debug.Log("🏆 [BattleFlow] 玩家獲勝，進入選卡階段");

            if (roomClearFlowController == null)
            {
                roomClearFlowController = FindFirstObjectByType<RoomClearFlowController>();
            }

            if (roomClearFlowController != null)
            {
                roomClearFlowController.OnRoomCleared();
            }
            else
            {
                rewardUI?.ShowRewardPanel();
            }
        }
        else
        {
            CurrentState = BattleState.Failed;
            Debug.Log("💀 [BattleFlow] 玩家失敗");
            // 顯示失敗 UI-------------------------------------
        }
    }

    public void OnRewardSelected()
    {
        CurrentState = BattleState.Completed;
        Debug.Log("🚪 [BattleFlow] 獎勵已選擇，開放下一關");

        roomExitController?.UnlockExit();
    }
}
