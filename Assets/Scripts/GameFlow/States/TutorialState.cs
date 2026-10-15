using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 教學關卡流程狀態。
/// 載入 TeachScenes 場景，啟動 HUD，並在教學完成後
/// 透過 BattleEventManager.TriggerRoomCleared() 銜接回正常流程。
/// </summary>
public class TutorialState : IState
{
    private const string TutorialSceneName = "TeachScenes";

    public void Enter()
    {
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Loading);

        // 教學關獨立於正式 8 節點，仍使用同一個 CampaignData 作為唯一狀態來源。
        if (GameDB.Instance?.Campaign.StartTutorial(GameDB.Instance.TutorialMission) != true)
        {
            return;
        }
        
        // 播放關卡任務背景音樂（教學關使用與其他關卡相同的音樂）
        if (AudioManager.Instance != null) AudioManager.Instance.PlayBGM_Mission();

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.StartCoroutine(LoadTutorialSceneRoutine());

        BattleEventManager.OnRoomCleared += HandleTutorialComplete;
        BattleEventManager.OnPlayerDied  += HandlePlayerDied;
    }

    private IEnumerator LoadTutorialSceneRoutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(TutorialSceneName, LoadSceneMode.Single);
        if (asyncLoad == null)
        {
            yield break;
        }

        while (!asyncLoad.isDone)
            yield return null;

        // 安全檢查：若狀態已切走則中止
        if (GameFlowManager.Instance?.CurrentState != this) yield break;


        // 場景切換後重新綁定場景內的 Tutorial UI（舊場景引用在切換後失效）
        UIManager.Instance?.RebindTutorialUI();

        // 顯示 HUD
        UIManager.Instance?.ShowGameplayHUD();

        // 顯示教學開場對話（若教學 mission 有設定 briefingStep）
        // 必須在 RebindTutorialUI 之後呼叫，確保 TutorialDialogueUI 引用是場景 instance
        TutorialStepData briefing = GameDB.Instance?.TutorialMission?.briefingStep;
        if (briefing != null)
        {
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.ShowDialogue(briefing);
            else
                UIManager.Instance?.ShowTutorialDialogue(briefing);
        }

        // 教學場景不開倒數計時；它也遵守同一個輸入階段，而不計入正式節點。
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Active);
        
        // 延遲一段時間後自動顯示出口門（讓玩家可以直接選擇下一關）
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.StartCoroutine(AutoShowExitDoorsAfterDelay());
    }
    
    /// <summary>
    /// 教學場景載入後，延遲顯示出口門，讓玩家可以直接選擇下一關。
    /// </summary>
    private IEnumerator AutoShowExitDoorsAfterDelay()
    {
        // 等待 3 秒，讓教學對話有時間顯示
        yield return new WaitForSeconds(3f);
        
        BattleEventManager.TriggerRoomCleared();
    }

    private void HandleTutorialComplete()
    {

        var campaign = GameDB.Instance?.Campaign;
        if (campaign == null)
        {
            return;
        }

        // 啟動正式戰役（會生成節點 1 的 PendingOptions）
        if (!campaign.StartFormalCampaign())
        {
            return;
        }

        // 檢查節點 1 是否為 MissionChoice（需要選路）
        if (campaign.PendingOptions == null || campaign.PendingOptions.Length != 2)
        {
            // 節點 1 是固定關（不太可能，但保險處理）
            GameFlowManager.Instance?.ChangeState(new GameplayState());
            return;
        }

        // 節點 1 是二選一，在教學場景內顯示雙門讓玩家選擇

        // 設定階段為「路線選擇」
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.RouteSelection);

        // 使用場景中的 RoomExitController 顯示雙門
        RoomExitController exitController = Object.FindFirstObjectByType<RoomExitController>();
        if (exitController != null)
        {
            
            // 顯示雙門並自動解鎖（教學已完成，不需要等待敵人全滅）
            exitController.ShowExitDoors(needsRouteChoice: true);
            
        }
        else
        {
        }

        // 訂閱路線選擇事件（玩家走進哪扇門）
        BattleEventManager.OnRouteSelected += HandleFirstNodeRouteSelected;
    }

    /// <summary>
    /// 玩家在教學場景內選擇了節點 1 的其中一個任務（走進哪扇門）。
    /// </summary>
    private void HandleFirstNodeRouteSelected(int optionIndex)
    {

        // 取消訂閱，避免二次觸發
        BattleEventManager.OnRouteSelected -= HandleFirstNodeRouteSelected;

        var campaign = GameDB.Instance?.Campaign;
        if (campaign == null)
        {
            return;
        }

        // 將選擇寫入 CampaignData
        if (!campaign.TrySelectRoute(optionIndex))
        {
            return;
        }

        // 切換到 GameplayState，載入節點 1 選中的任務場景
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Transitioning);
        GameFlowManager.Instance?.ChangeState(new GameplayState());
    }

    private void HandlePlayerDied()
    {
        // 教學死亡直接重跑教學，不顯示結算
        GameFlowManager.Instance?.ChangeState(new TutorialState());
    }

    public void Exit()
    {
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Transitioning);
        UIManager.Instance?.HideGameplayHUD();

        BattleEventManager.OnRoomCleared -= HandleTutorialComplete;
        BattleEventManager.OnPlayerDied  -= HandlePlayerDied;
        BattleEventManager.OnRouteSelected -= HandleFirstNodeRouteSelected;  // 清理選路訂閱
    }

    public void Update()
    {
        // 移除：開發模式跳過教學功能已移除，確保所有玩家統一走完整教學流程
    }

    public void PhysicsUpdate() { }
}
