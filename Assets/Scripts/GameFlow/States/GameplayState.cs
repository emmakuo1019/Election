using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>載入目前 CampaignData 節點；任務結果只會轉交給 StageClearState。</summary>
public class GameplayState : IState
{
    public GameplayState(int ignoredRoomNumber = 0) { }

    public void Enter()
    {
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Loading);
        BattleEventManager.OnObjectiveResolved += HandleObjectiveResolved;
        BattleEventManager.OnPlayerDied += HandlePlayerDied;
        GameFlowManager.Instance?.StartCoroutine(LoadBattleSceneRoutine());
    }

    private IEnumerator LoadBattleSceneRoutine()
    {
        string sceneName = GameDB.Instance?.Campaign?.GetCurrentRoomSceneName();
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[GameplayState] 尚未設定可載入的戰役節點。");
            yield break;
        }

        UIManager.Instance?.HideGameplayHUD();
        UIManager.Instance?.HideAllTutorialUI();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError($"[GameplayState] 找不到場景 {sceneName}。");
            yield break;
        }

        while (!operation.isDone) yield return null;
        if (GameFlowManager.Instance?.CurrentState != this) yield break;

        UIManager.Instance?.ShowGameplayHUD();
        UIManager.Instance?.RebindTutorialUI();

        RoomMissionData mission = GameDB.Instance?.Campaign.ActiveRoom.mission;
        
        bool isTimedMission = mission != null &&
            (mission.objectiveType == MissionObjectiveType.Survive || mission.objectiveType == MissionObjectiveType.ReachVotePercent);
        
        if (isTimedMission && LevelTimer.Instance != null)
        {
            float duration = mission != null && mission.targetValue > 0
                ? mission.targetValue
                : LevelTimer.Instance.TotalDuration;
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
        if (Input.GetKeyDown(KeyCode.Y))
            BattleEventManager.TriggerObjectiveResolved(EncounterOutcome.Success);
#endif
    }

    public void PhysicsUpdate() { }
}
