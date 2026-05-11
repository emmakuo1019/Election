using UnityEngine;
using UnityEngine.SceneManagement;

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

        if (roomClearFlowController == null)
        {
            roomClearFlowController = FindFirstObjectByType<RoomClearFlowController>();
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
        int playerVotes = VoteManager.Instance != null ? VoteManager.Instance.PlayerVotes : 0;
        int opponentVotes = VoteManager.Instance != null ? VoteManager.Instance.OpponentVotes : 0;

        if (playerVotes > opponentVotes)
        {
            CurrentState = BattleState.Completed;

            if (roomClearFlowController == null)
            {
                roomClearFlowController = FindFirstObjectByType<RoomClearFlowController>();
            }

            if (roomClearFlowController != null)
            {
                roomClearFlowController.OnRoomCleared(showRewardPanel: true);
            }
            else
            {
                Debug.LogWarning("⚠️ 找不到 RoomClearFlowController");
            }
        }
        else
        {
            if (IsFirstBlockFinalRoom())
            {
                CurrentState = BattleState.Failed;
                Time.timeScale = 1f;
                SceneManager.LoadScene("endGamePanel");
                return;
            }

            CurrentState = BattleState.Failed;

            if (roomClearFlowController == null)
            {
                roomClearFlowController = FindFirstObjectByType<RoomClearFlowController>();
            }

            if (roomClearFlowController != null)
            {
                roomClearFlowController.OnRoomCleared(showRewardPanel: false);
            }
            else
            {
                Debug.LogWarning("⚠️ 找不到 RoomClearFlowController");
            }
        }
    }

    private bool IsFirstBlockFinalRoom()
    {
        if (CampaignProgressManager.GetCompletedBlockCount() != 0)
        {
            return false;
        }

        RoomExitController roomExitController = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        return roomExitController != null && roomExitController.IsFinalRoomExit && BlockProgressManager.IsLastRoomInBlock();
    }

    public void OnRewardSelected()
    {
        CurrentState = BattleState.Completed;
    }
}
