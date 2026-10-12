using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 載入目前 CampaignData 節點；任務結果只會轉交給 StageClearState。
/// </summary>
public class GameplayState : IState
{
    /// <summary>
    /// 建構 GameplayState，場景由 CampaignData.GetCurrentRoomSceneName() 決定。
    /// </summary>
    public GameplayState() { }

    // ── 終局仲裁：延後一幀的 pending outcome ──────────────────────────
    // null = 尚未收到任何 Objective；有值 = 等待下一幀確認。
    // 防重複：只允許一個 pending，後續 ObjectiveResolved 事件直接忽略。
    // HandlePlayerDied 清除 pending 並立即離開，coroutine 下一幀看到
    // CurrentState != this 後自動放棄，不會再進 StageClearState。
    private EncounterOutcome? _pendingOutcome;

    public void Enter()
    {
        _pendingOutcome = null;
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Loading);
        BattleEventManager.OnObjectiveResolved += HandleObjectiveResolved;
        BattleEventManager.OnPlayerDied += HandlePlayerDied;
        
        // 播放關卡任務背景音樂
        if (AudioManager.Instance != null) AudioManager.Instance.PlayBGM_Mission();
        
        GameFlowManager.Instance?.StartCoroutine(LoadBattleSceneRoutine());
    }

    private IEnumerator LoadBattleSceneRoutine()
    {
        string sceneName = GameDB.Instance?.Campaign?.GetCurrentRoomSceneName();
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            yield break;
        }

        UIManager.Instance?.HideGameplayHUD();
        UIManager.Instance?.HideAllTutorialUI();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            yield break;
        }

        while (!operation.isDone) yield return null;
        if (GameFlowManager.Instance?.CurrentState != this) yield break;

        UIManager.Instance?.ShowGameplayHUD();
        UIManager.Instance?.RebindTutorialUI();

        RoomMissionData mission = GameDB.Instance?.Campaign.ActiveRoom.mission;
        
        bool isTimedMission = mission != null &&
            (mission.objectiveType == MissionObjectiveType.Survive ||
             mission.objectiveType == MissionObjectiveType.ReachVotePercent);
        
        // FinalBoss 場景也需要計時器（時限到未達票數門檻 → 敗選）
        // 使用 LevelTimer.TotalDuration（Inspector 設定），不從 mission.targetValue 讀
        // （targetValue 在 FinalBoss 場景語意是「票數門檻 %」，非計時秒數）
        bool isFinalBoss = GameDB.Instance?.Campaign.CurrentRole == EncounterNodeRole.FinalBoss;
        
        if ((isTimedMission || isFinalBoss) && LevelTimer.Instance != null)
        {
            float duration;
            if (isFinalBoss)
            {
                // FinalBoss：用 LevelTimer Inspector 設定的 TotalDuration（場景設計者決定時限）
                duration = LevelTimer.Instance.TotalDuration > 0f
                    ? LevelTimer.Instance.TotalDuration
                    : 120f; // ponytail: 無設定時保底 120 秒，避免無限等待
            }
            else
            {
                duration = mission != null && mission.targetValue > 0
                    ? mission.targetValue
                    : LevelTimer.Instance.TotalDuration;
            }
            LevelTimer.Instance.StartTimer(duration);
        }

        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Briefing);
        TutorialStepData briefing = mission?.briefingStep;
        if (briefing != null)
        {
            if (TutorialManager.Instance != null) TutorialManager.Instance.ShowDialogue(briefing);
            else UIManager.Instance?.ShowTutorialDialogue(briefing);
        }

        // 對話、Timeline、運鏡都會透過 NarrativeDirector 接在這裡；目前沒有 Beat 時立即進 Active。
        NarrativeDirector.Play(GameDB.Instance?.Campaign.GetCurrentNode()?.onEnterBeat, BeginActive);
    }

    private void BeginActive()
    {
        if (GameFlowManager.Instance?.CurrentState == this)
            BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Active);
    }

    private void HandleObjectiveResolved(EncounterOutcome outcome)
    {
        // 只允許一個 pending objective；後續事件忽略（MissionTracker._resolved 也已防重複）。
        if (_pendingOutcome.HasValue) return;
        _pendingOutcome = outcome;
        GameFlowManager.Instance?.StartCoroutine(CommitObjectiveNextFrame());
    }

    /// <summary>
    /// 延後一幀再提交結算，讓同幀的 PlayerDied 事件有機會搶先離開。
    /// 醒來後的兩個 guard：
    ///   1. CurrentState == this：PlayerDied 已呼叫 ChangeState(GameEndState)，
    ///      GameplayState.Exit() 已取消訂閱，此時 CurrentState 已改變，直接放棄。
    ///   2. IntegrityHp > 0：萬一 PlayerDied 事件還未在本幀觸發，
    ///      仍透過 HP 值阻擋（與 c4459df 的邏輯一致）。
    /// </summary>
    private IEnumerator CommitObjectiveNextFrame()
    {
        yield return null;  // 等一幀

        // guard 1：state 已離開（PlayerDied 已執行）
        if (GameFlowManager.Instance?.CurrentState != this) yield break;

        // guard 2：HP 已歸零（PlayerDied 同幀但晚於 ObjectiveResolved）
        if (GameDB.Instance?.Run?.IntegrityHp <= 0f) yield break;

        EncounterOutcome committed = _pendingOutcome.GetValueOrDefault();
        _pendingOutcome = null;
        GameFlowManager.Instance?.ChangeState(new StageClearState(committed));
    }

    private void HandlePlayerDied()
    {
        // 清除 pending，確保 coroutine 即使還沒執行，下一幀 CurrentState != this
        // 的 guard 也會讓它放棄（Exit() 在 ChangeState 裡同步呼叫）。
        _pendingOutcome = null;
        GameFlowManager.Instance?.ChangeState(new GameEndState(false));
    }

    public void Exit()
    {
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Transitioning);
        BattleEventManager.OnObjectiveResolved -= HandleObjectiveResolved;
        BattleEventManager.OnPlayerDied -= HandlePlayerDied;
    }

    public void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // T 鍵：快速完成當前關卡（開發測試用）
        // 只在 Active 階段允許，避免在獎勵選擇時誤觸
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (BattleEventManager.CurrentEncounterPhase == BattleEventManager.EncounterPhase.Active)
            {
                BattleEventManager.TriggerObjectiveResolved(EncounterOutcome.Success);
            }
            else
            {
            }
        }
#endif
    }

    public void PhysicsUpdate() { }
}
