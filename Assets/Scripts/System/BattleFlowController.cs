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
            CurrentState = BattleState.Completed;
            Debug.Log("🏆 [BattleFlow] 玩家獲勝，進入房間結束流程");

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
                Debug.LogWarning("⚠️ 找不到 RoomClearFlowController");
            }
        }
        else
        {
            CurrentState = BattleState.Failed;
            Debug.Log("💀 [BattleFlow] 玩家失敗");
        }
    }

    public void OnRewardSelected()
    {
        CurrentState = BattleState.Completed;
        Debug.Log("🎴 [BattleFlow] 獎勵已選擇");
    }
}
