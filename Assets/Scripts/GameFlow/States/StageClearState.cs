using UnityEngine;

/// <summary>唯一的結算入口：記錄結果、等待選卡、再自動前進或等待雙門選路。</summary>
public class StageClearState : IState
{
    private readonly EncounterOutcome _outcome;
    private bool _transitioned;

    public StageClearState(EncounterOutcome outcome) => _outcome = outcome;

    public void Enter()
    {
        UIManager.Instance?.ShowGameplayHUD();
        GameDB.Instance?.Campaign.ResolveCurrentEncounter(_outcome);
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.ObjectiveResolved);

        BattleEventManager.OnRewardCollected += HandleRewardCollected;
        BattleEventManager.OnRouteSelected += HandleRouteSelected;
        BattleEventManager.OnExitReached += HandleExitReached;

        CampaignData campaign = GameDB.Instance?.Campaign;
        CampaignNodeDefinition node = campaign?.GetCurrentNode();
        NarrativeDirector.PlaySequence(BuildResolutionBeats(node), BeginPostObjective);
    }

    private NarrativeBeat[] BuildResolutionBeats(CampaignNodeDefinition node)
    {
        if (node == null) return System.Array.Empty<NarrativeBeat>();

        NarrativeBeat resultBeat = _outcome == EncounterOutcome.Success
            ? node.onSuccessBeat
            : node.onFailureBeat;
        if (node.role == EncounterNodeRole.Elite)
            return new[] { resultBeat, node.afterEliteBeat };
        if (node.role == EncounterNodeRole.FinalBoss)
            return new[] { resultBeat, node.finalEndingBeat };
        return new[] { resultBeat };
    }

    private void BeginPostObjective()
    {
        if (GameFlowManager.Instance?.CurrentState != this) return;
        if (GameDB.Instance?.Campaign.CurrentRole == EncounterNodeRole.FinalBoss)
        {
            TransitionToEnd();
            return;
        }

        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.RewardSelection);
        BattleEventManager.TriggerRewardSelectionRequested(_outcome == EncounterOutcome.Failed);
    }

    private void HandleRewardCollected()
    {
        if (_transitioned) return;
        CampaignData campaign = GameDB.Instance?.Campaign;
        if (campaign == null || !campaign.TryPrepareNextStep(out bool needsRouteChoice, out bool isRunComplete))
        {
            Debug.LogError("[StageClearState] 無法準備下一個戰役節點。");
            return;
        }

        if (isRunComplete) { TransitionToEnd(); return; }
        
        // 顯示出口門
        RoomExitController exitController = Object.FindFirstObjectByType<RoomExitController>();
        if (exitController != null)
        {
            exitController.ShowExitDoors(needsRouteChoice);
        }
        
        // 設定對應的階段，等待玩家走到門
        if (needsRouteChoice)
        {
            // 選路模式：等待玩家選擇門（HandleRouteSelected）
            BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.RouteSelection);
        }
        else
        {
            // 單門模式：等待玩家走到門（HandleExitReached）
            BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.RouteSelection);
        }
    }

    private void HandleExitReached()
    {
        Debug.Log("[StageClearState] HandleExitReached 被調用");
        if (_transitioned) return;
        CampaignData campaign = GameDB.Instance?.Campaign;
        if (campaign == null) return;
        
        _transitioned = true;
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Transitioning);
        Debug.Log($"[StageClearState] 準備切換到節點 {campaign.CurrentNodeNumber}");
        GameFlowManager.Instance?.ChangeState(new GameplayState());
    }

    private void HandleRouteSelected(int optionIndex)
    {
        if (_transitioned) return;
        CampaignData campaign = GameDB.Instance?.Campaign;
        if (campaign == null || !campaign.TrySelectRoute(optionIndex)) return;
        _transitioned = true;
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Transitioning);
        GameFlowManager.Instance?.ChangeState(new GameplayState());
    }

    private void TransitionToEnd()
    {
        if (_transitioned) return;
        _transitioned = true;
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Transitioning);
        GameFlowManager.Instance?.ChangeState(new GameEndState(_outcome == EncounterOutcome.Success));
    }

    public void Exit()
    {
        BattleEventManager.OnRewardCollected -= HandleRewardCollected;
        BattleEventManager.OnRouteSelected -= HandleRouteSelected;
        BattleEventManager.OnExitReached -= HandleExitReached;
        // 不再需要手動清理門，RoomExitController 會在下一關場景載入時自動銷毀
    }

    public void Update() { }
    public void PhysicsUpdate() { }
}
