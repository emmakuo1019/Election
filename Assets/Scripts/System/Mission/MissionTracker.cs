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
    private bool _subscribed = false;  // 訂閱完成標記

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
        // 教學場景不使用 MissionTracker（由 TutorialManager 管理）
        if (GameDB.Instance?.Campaign.IsTutorialActive == true)
        {
            Debug.Log("[MissionTracker] 教學場景中，不啟動任務追蹤系統");
            return;
        }

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
        
        // [FIX] 延遲訂閱，等待 EnemySpawner 完成初始化
        StartCoroutine(DelayedSubscribe());
    }
    
    private System.Collections.IEnumerator DelayedSubscribe()
    {
        // 等待 2 幀，確保 EnemySpawner 已生成敵人
        yield return null;
        yield return null;
        
        Debug.Log($"[MissionTracker] 延遲訂閱完成，當前敵人數量: {EnemySpawnTracker.AliveCount}");
        Subscribe();
        _subscribed = true;  // 訂閱完成，允許 Update() 開始檢查
    }

    private void OnDisable()
    {
        Unsubscribe();
    }
    
    private void Update()
    {
        // 搶票任務需要定時檢查即時票數
        // 只在 Active 階段且訂閱完成後檢查，避免場景載入時誤判
        if (_mission != null && 
            _mission.objectiveType == MissionObjectiveType.ReachVotePercent && 
            !_resolved &&
            _subscribed &&
            BattleEventManager.CurrentEncounterPhase == BattleEventManager.EncounterPhase.Active)
        {
            CheckReachVotePercent();
        }
    }

    // ── 事件訂閱 ─────────────────────────────────────────────────────

    private void Subscribe()
    {
        if (IsFinalBossEncounter())
        {
            BattleEventManager.OnFinalBossDefeated += HandleFinalBossDefeated;
            _subscribed = true;  // 最終戰不需延遲訂閱
            return;
        }

        if (_mission == null) return;

        switch (_mission.objectiveType)
        {
            case MissionObjectiveType.EliminateAll:
                BattleEventManager.OnAllEnemiesDefeated += HandleEliminateAll;
                
                // [FIX] 只在 Active 階段才檢查「敵人已全滅」的邊界情況
                // 場景剛載入時（Loading/Briefing 階段）敵人可能還沒生成，此時不應觸發完成
                if (EnemySpawnTracker.AliveCount == 0 && 
                    BattleEventManager.CurrentEncounterPhase == BattleEventManager.EncounterPhase.Active)
                {
                    // 雙重確認：確保場上真的沒有敵人
                    var remaining = Object.FindObjectsByType<EnemyController>(
                        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    if (remaining.Length == 0)
                    {
                        Debug.LogWarning("[MissionTracker] 訂閱時敵人已全滅，立即觸發完成判定。");
                        HandleEliminateAll();
                    }
                    else
                    {
                        Debug.Log($"[MissionTracker] AliveCount=0 但場上有 {remaining.Length} 個敵人，等待追蹤系統更新");
                    }
                }
                else
                {
                    Debug.Log($"[MissionTracker] 任務開始，當前存活敵人: {EnemySpawnTracker.AliveCount}，階段: {BattleEventManager.CurrentEncounterPhase}");
                }
                break;

            case MissionObjectiveType.ReachVotePercent:
                // 改為在 Update 中定時檢查即時票數，不再訂閱 OnVotesChanged
                BattleEventManager.OnTimerExpired += HandleTimerExpired;
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

    /// <summary>檢查即時票數是否達標（搶票任務專用）</summary>
    private void CheckReachVotePercent()
    {
        if (_mission == null || GameDB.Instance == null) return;
        
        // 使用即時統計的場上選民票數
        float percent = GameDB.Instance.Run.GetCurrentVotePercentage() * 100f;
        
        if (percent >= _mission.targetValue)
        {
            // [FIX] 立即停止計時器，避免時間到觸發失敗判定
            if (LevelTimer.Instance != null && LevelTimer.Instance.IsActive)
            {
                LevelTimer.Instance.PauseTimer();
            }
            
            // [FIX] 取消訂閱計時器事件，防止重複觸發
            BattleEventManager.OnTimerExpired -= HandleTimerExpired;
            
            NotifyResult(true);
        }
    }

    private void HandleVotesChanged(int _, int __)
    {
        // 此方法已廢棄，改用 Update 中的 CheckReachVotePercent
        // 保留此方法避免編譯錯誤，但不再使用
    }

    private void HandleTimerExpired()
    {
        // [FIX] 票數任務：時間到時檢查是否已結算，避免重複觸發
        if (_mission != null && 
            _mission.objectiveType == MissionObjectiveType.ReachVotePercent &&
            !_resolved)
        {
            NotifyResult(false);
        }
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
