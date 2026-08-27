using UnityEngine;

/// <summary>
/// 場景層級的任務追蹤器。
/// 從 GameDB.Campaign.ActiveRoom.mission 讀取當前任務定義，
/// 在關卡結束時評估是否達成，並透過 OnMissionCompleted / OnMissionFailed 事件通知外部。
///
/// 單一職責：只做「任務是否達成」的判斷，不處理 UI 或場景切換。
/// 放置於戰鬥場景的 GameObject 上（與 BattleFlowController 同場景）。
/// </summary>
public class MissionTracker : MonoBehaviour
{
    public static MissionTracker Instance { get; private set; }

    /// <summary>任務達成時觸發</summary>
    public static event System.Action OnMissionCompleted;

    /// <summary>任務失敗時觸發</summary>
    public static event System.Action OnMissionFailed;

    /// <summary>
    /// 本關任務的最終結果。StageClearState 在 Enter 時讀取一次。
    /// 無任務時預設 true（視為達成，走原本獎勵流程）。
    /// </summary>
    public static bool LastResult { get; private set; } = true;

    /// <summary>
    /// 重置為預設通過狀態。由 StageClearState.Enter() 主動呼叫，
    /// 確保沒有放置 MissionTracker 的場景（如安全房）也不會殘留上一關結果。
    /// </summary>
    public static void Reset() => LastResult = true;

    private RoomMissionData _mission;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // StageClearState.Enter() 已在進入結算前主動呼叫 Reset()
        // 這裡再重置一次作為防禦，確保直接從場景開始測試時也能正確初始化
        LastResult = true;

        _mission = GameDB.Instance?.Campaign.ActiveRoom.mission;

        if (_mission == null)
        {
            Debug.Log("[MissionTracker] 當前房間無指定任務，追蹤器閒置。");
            return;
        }

        Debug.Log($"[MissionTracker] 載入任務：{_mission.objectiveType}，目標值：{_mission.targetValue}");
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // ── 事件訂閱 ─────────────────────────────────────────────────────

    private void Subscribe()
    {
        switch (_mission.objectiveType)
        {
            case MissionObjectiveType.EliminateAll:
                BattleEventManager.OnAllEnemiesDefeated += HandleEliminateAll;
                break;

            case MissionObjectiveType.ReachVotePercent:
                // 在出口觸發時評估票數，不需要即時監聽
                BattleEventManager.OnRoomCleared += HandleRoomClearedVoteCheck;
                break;

            case MissionObjectiveType.Survive:
                BattleEventManager.OnSurvivalTimeUp += HandleSurvivalTimeUp;
                break;
        }
    }

    private void Unsubscribe()
    {
        BattleEventManager.OnAllEnemiesDefeated -= HandleEliminateAll;
        BattleEventManager.OnRoomCleared        -= HandleRoomClearedVoteCheck;
        BattleEventManager.OnSurvivalTimeUp     -= HandleSurvivalTimeUp;
    }

    // ── 任務判斷 ─────────────────────────────────────────────────────

    private void HandleEliminateAll()
    {
        // EliminateAll 的「過關」由 BattleFlowController 驅動，這裡只標記達成
        NotifyResult(true);
    }

    private void HandleRoomClearedVoteCheck()
    {
        if (_mission == null) return;

        float percent = GameDB.Instance != null
            ? GameDB.Instance.Run.PlayerVotePercentage * 100f
            : 0f;

        bool achieved = percent >= _mission.targetValue;
        Debug.Log($"[MissionTracker] 票數結算：{percent:F1}% vs 目標 {_mission.targetValue}%，達成：{achieved}");
        NotifyResult(achieved);
    }

    private void HandleSurvivalTimeUp()
    {
        // 時間到代表成功撐過去
        NotifyResult(true);
    }

    private void NotifyResult(bool success)
    {
        LastResult = success;
        if (success)
        {
            Debug.Log("[MissionTracker] 任務達成！");
            OnMissionCompleted?.Invoke();
        }
        else
        {
            Debug.Log("[MissionTracker] 任務失敗。");
            OnMissionFailed?.Invoke();
        }
    }
}
