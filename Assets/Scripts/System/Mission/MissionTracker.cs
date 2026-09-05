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

    private RoomMissionData _mission;
    private bool _resolved;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 提前讀取任務資料，避免在 Start() 時 GameDB 尚未就緒
        _mission = GameDB.Instance?.Campaign.ActiveRoom.mission;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // _mission 在 Awake() 已讀取，此時只需檢查和訂閱
        if (IsFinalBossEncounter())
        {
            Debug.Log("[MissionTracker] 最終戰：等待對手生命歸零。");
            Subscribe();
            return;
        }

        if (_mission == null)
        {
            Debug.LogWarning("[MissionTracker] 當前房間無指定任務，追蹤器閒置。");
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
        if (IsFinalBossEncounter())
        {
            BattleEventManager.OnFinalBossDefeated += HandleFinalBossDefeated;
            return;
        }

        if (_mission == null) return;

        switch (_mission.objectiveType)
        {
            case MissionObjectiveType.EliminateAll:
                BattleEventManager.OnAllEnemiesDefeated += HandleEliminateAll;
                
                // 立即檢查是否已經全滅（處理訂閱延遲的邊界情況）
                if (EnemySpawnTracker.AliveCount == 0)
                {
                    // 雙重確認：確保場上真的沒有敵人
                    var remaining = Object.FindObjectsByType<EnemyController>(
                        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    if (remaining.Length == 0)
                    {
                        Debug.LogWarning("[MissionTracker] 訂閱時敵人已全滅，立即觸發完成判定。");
                        HandleEliminateAll();
                    }
                }
                break;

            case MissionObjectiveType.ReachVotePercent:
                RunData run = GameDB.Instance?.Run;
                if (run != null) run.OnVotesChanged += HandleVotesChanged;
                BattleEventManager.OnTimerExpired += HandleTimerExpired;
                HandleVotesChanged(0, 0);
                break;

            case MissionObjectiveType.Survive:
                BattleEventManager.OnSurvivalTimeUp += HandleSurvivalTimeUp;
                break;
        }
    }

    private void Unsubscribe()
    {
        BattleEventManager.OnFinalBossDefeated -= HandleFinalBossDefeated;
        BattleEventManager.OnAllEnemiesDefeated -= HandleEliminateAll;
        if (GameDB.Instance != null)
            GameDB.Instance.Run.OnVotesChanged -= HandleVotesChanged;
        BattleEventManager.OnTimerExpired       -= HandleTimerExpired;
        BattleEventManager.OnSurvivalTimeUp     -= HandleSurvivalTimeUp;
    }

    // ── 任務判斷 ─────────────────────────────────────────────────────

    private void HandleEliminateAll()
    {
        // EliminateAll 的「過關」由 BattleFlowController 驅動，這裡只標記達成
        NotifyResult(true);
    }

    private void HandleFinalBossDefeated()
    {
        NotifyResult(true);
    }

    private void HandleVotesChanged(int _, int __)
    {
        if (_mission == null || GameDB.Instance == null) return;
        float percent = GameDB.Instance.Run.PlayerVotePercentage * 100f;
        if (percent >= _mission.targetValue)
            NotifyResult(true);
    }

    private void HandleTimerExpired()
    {
        if (_mission != null && _mission.objectiveType == MissionObjectiveType.ReachVotePercent)
            NotifyResult(false);
    }

    private void HandleSurvivalTimeUp()
    {
        // 時間到代表成功撐過去
        NotifyResult(true);
    }

    private void NotifyResult(bool success)
    {
        if (_resolved) return;
        _resolved = true;
        
        if (success)
        {
            OnMissionCompleted?.Invoke();
        }
        else
        {
            OnMissionFailed?.Invoke();
        }
        
        BattleEventManager.TriggerObjectiveResolved(success ? EncounterOutcome.Success : EncounterOutcome.Failed);
    }

    private static bool IsFinalBossEncounter()
    {
        return GameDB.Instance?.Campaign.CurrentRole == EncounterNodeRole.FinalBoss;
    }
}
