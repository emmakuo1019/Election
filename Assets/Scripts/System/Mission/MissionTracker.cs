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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private float _diagLogTimer = 0f;     // 診斷 log 節流計時器
    private const float DiagLogInterval = 3f;
#endif

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
            return;
        }

        // _mission 在 Awake() 已讀取，此時只需檢查和訂閱
        if (IsFinalBossEncounter())
        {
            Subscribe();
            return;
        }

        if (_mission == null)
        {
            return;
        }

        
        // [FIX] 延遲訂閱，等待 EnemySpawner 完成初始化
        StartCoroutine(DelayedSubscribe());
    }
    
    private System.Collections.IEnumerator DelayedSubscribe()
    {
        // 等待 2 幀，確保 EnemySpawner 已生成敵人
        yield return null;
        yield return null;
        
        Subscribe();
        _subscribed = true;  // 訂閱完成，允許 Update() 開始檢查
    }

    private void OnDisable()
    {
        Unsubscribe();
    }
    
    private void Update()
    {
        if (_resolved) return;
        if (BattleEventManager.CurrentEncounterPhase != BattleEventManager.EncounterPhase.Active) return;
        if (!_subscribed) return;

        // 搶票任務（普通關）：Update 輪詢即時票數
        if (_mission != null && _mission.objectiveType == MissionObjectiveType.ReachVotePercent)
        {
            CheckReachVotePercent();
            return;
        }

        // FinalBoss：以 M_Boss.targetValue 作為票數門檻（%），Update 輪詢
        // 時限到期走 HandleTimerExpired，玩家 HP 歸零由 GameplayState.HandlePlayerDied 負責
        if (IsFinalBossEncounter())
        {
            CheckFinalBossVotePercent();
        }
    }

    // ── 事件訂閱 ─────────────────────────────────────────────────────

    private void Subscribe()
    {
        if (IsFinalBossEncounter())
        {
            // FinalBoss：訂閱 OnFinalBossDefeated（票數達標由 CheckFinalBossVotePercent 呼叫）
            // 並訂閱 OnTimerExpired（時限到未達標 → 敗選）
            BattleEventManager.OnFinalBossDefeated += HandleFinalBossDefeated;
            BattleEventManager.OnTimerExpired      += HandleTimerExpired;
            _subscribed = true;
            return;
        }

        if (_mission == null) return;

        switch (_mission.objectiveType)
        {
            case MissionObjectiveType.EliminateAll:
                // Reporter 清場任務：訂閱 ReporterMissionTracker 的完成事件，而非全敵人全滅事件。
                if (_mission.isReporterMission)
                {
                    ReporterMissionTracker.OnAllReportersDefeated += HandleEliminateAll;
                    _subscribed = true;
                    // 不做 AliveCount == 0 的提前判斷；registration 未完成前不可觸發。
                    break;
                }

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
                        HandleEliminateAll();
                    }
                    else
                    {
                    }
                }
                else
                {
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
        ReporterMissionTracker.OnAllReportersDefeated -= HandleEliminateAll;
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
        NotifyResult(true, "HandleFinalBossDefeated");
    }

    /// <summary>
    /// FinalBoss 票數輪詢：以 _mission.targetValue 作為門檻（%），
    /// 達標後暫停計時器並觸發 OnFinalBossDefeated，讓 HandleFinalBossDefeated 完成結算。
    /// ponytail: Update 每幀輪詢，N 選民數通常 &lt; 50，可接受；有效能疑慮時改事件驅動。
    /// </summary>
    private void CheckFinalBossVotePercent()
    {
        if (GameDB.Instance == null) return;
        float targetPct = _mission != null ? _mission.targetValue : 0f;
        if (targetPct <= 0f) return;  // 防呆：未設定門檻不自動勝利

        float percent = GameDB.Instance.Run.GetCurrentVotePercentage() * 100f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 每 DiagLogInterval 秒印一次診斷摘要，避免每幀洗版
        _diagLogTimer += Time.deltaTime;
        if (_diagLogTimer >= DiagLogInterval)
        {
            _diagLogTimer = 0f;
            int pVotes = GameDB.Instance.Run.GetCurrentPlayerVotes();
            int total  = GameDB.Instance.Run.GetCurrentTotalVotes();
            float rem  = LevelTimer.Instance != null ? LevelTimer.Instance.RemainingTime : -1f;
            Debug.Log($"[MissionTracker][FinalBoss 輪詢] 票數={pVotes}/{total}（{percent:F1}%） " +
                      $"門檻={targetPct}% | 剩餘={rem:F1}s | Phase={BattleEventManager.CurrentEncounterPhase}");
        }
#endif

        if (percent >= targetPct)
        {
            // 停計時器防止同幀時限觸發失敗
            if (LevelTimer.Instance != null && LevelTimer.Instance.IsActive)
                LevelTimer.Instance.PauseTimer();
            BattleEventManager.OnTimerExpired -= HandleTimerExpired;
            BattleEventManager.TriggerFinalBossDefeated();
        }
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
        // 普通搶票任務：時間到未達標 → 失敗
        if (_mission != null && 
            _mission.objectiveType == MissionObjectiveType.ReachVotePercent &&
            !_resolved)
        {
            NotifyResult(false);
            return;
        }

        // FinalBoss：時限到未達票數門檻 → 敗選
        if (IsFinalBossEncounter() && !_resolved)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GameDB.Instance != null)
            {
                int pv = GameDB.Instance.Run.GetCurrentPlayerVotes();
                int tv = GameDB.Instance.Run.GetCurrentTotalVotes();
                float pct = tv > 0 ? (float)pv / tv * 100f : 0f;
                Debug.Log($"[MissionTracker][FinalBoss 時限到] 敗選 | 票數={pv}/{tv}（{pct:F1}%） 門檻={( _mission != null ? _mission.targetValue : 0f )}%");
            }
#endif
            NotifyResult(false, "HandleTimerExpired");
        }
    }

    private void HandleSurvivalTimeUp()
    {
        // 時間到代表成功撐過去
        NotifyResult(true);
    }

    private void NotifyResult(bool success, string caller = "unknown")
    {
        if (_resolved) return;
        _resolved = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ── 診斷 log：結算來源 ────────────────────────────────────────
        if (IsFinalBossEncounter() && GameDB.Instance != null)
        {
            int playerVotes  = GameDB.Instance.Run.GetCurrentPlayerVotes();
            int totalVoters  = GameDB.Instance.Run.GetCurrentTotalVotes();
            float pct        = totalVoters > 0 ? (float)playerVotes / totalVoters * 100f : 0f;
            float threshold  = _mission != null ? _mission.targetValue : 0f;
            float remaining  = LevelTimer.Instance != null ? LevelTimer.Instance.RemainingTime : -1f;
            var phase        = BattleEventManager.CurrentEncounterPhase;
            Debug.Log($"[MissionTracker][FinalBoss 結算] 結果={( success ? "勝利" : "失敗" )} " +
                      $"| 呼叫來源={caller} " +
                      $"| Phase={phase} " +
                      $"| 票數={playerVotes}/{totalVoters}（{pct:F1}%） 門檻={threshold}% " +
                      $"| 剩餘時間={remaining:F1}s " +
                      $"| _subscribed={_subscribed}");
        }
#endif

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
