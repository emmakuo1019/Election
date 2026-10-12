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

    public void Enter()
    {
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
        GameFlowManager.Instance?.ChangeState(new StageClearState(outcome));
    }

    private void HandlePlayerDied()
    {
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
