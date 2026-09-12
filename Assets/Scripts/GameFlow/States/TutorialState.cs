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
        Debug.Log("[TutorialState] Enter - 載入教學場景");
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Loading);

        // 教學關獨立於正式 8 節點，仍使用同一個 CampaignData 作為唯一狀態來源。
        if (GameDB.Instance?.Campaign.StartTutorial(GameDB.Instance.TutorialMission) != true)
        {
            Debug.LogError("[TutorialState] 教學任務尚未設定。");
            return;
        }

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
            Debug.LogWarning($"[TutorialState] 找不到場景 {TutorialSceneName}，請確認已加入 Build Settings。");
            yield break;
        }

        while (!asyncLoad.isDone)
            yield return null;

        // 安全檢查：若狀態已切走則中止
        if (GameFlowManager.Instance?.CurrentState != this) yield break;

        Debug.Log("[TutorialState] 教學場景載入完成");

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
    }

    private void HandleTutorialComplete()
    {
        Debug.Log("[TutorialState] HandleTutorialComplete 被調用！");
        Debug.Log("[TutorialState] 教學完成，準備顯示節點 1 的二選一選路");

        var campaign = GameDB.Instance?.Campaign;
        if (campaign == null)
        {
            Debug.LogError("[TutorialState] GameDB.Campaign 為 null！");
            return;
        }
        Debug.Log("[TutorialState] ✓ Campaign 存在");

        // 啟動正式戰役（會生成節點 1 的 PendingOptions）
        Debug.Log("[TutorialState] 正在呼叫 StartFormalCampaign()...");
        if (!campaign.StartFormalCampaign())
        {
            Debug.LogError("[TutorialState] 無法啟動正式戰役第一節點。");
            return;
        }
        Debug.Log("[TutorialState] ✓ StartFormalCampaign() 成功");

        // 檢查節點 1 是否為 MissionChoice（需要選路）
        Debug.Log($"[TutorialState] 檢查 PendingOptions - 是否為 null: {campaign.PendingOptions == null}, 長度: {campaign.PendingOptions?.Length ?? -1}");
        if (campaign.PendingOptions == null || campaign.PendingOptions.Length != 2)
        {
            // 節點 1 是固定關（不太可能，但保險處理）
            Debug.LogWarning($"[TutorialState] 節點 1 不是二選一（PendingOptions 長度={campaign.PendingOptions?.Length ?? 0}），直接進入固定關。");
            GameFlowManager.Instance?.ChangeState(new GameplayState());
            return;
        }
        Debug.Log("[TutorialState] ✓ PendingOptions 有 2 個選項");

        // 節點 1 是二選一，在教學場景內顯示雙門讓玩家選擇
        Debug.Log($"[TutorialState] 節點 1 任務選項：[0] {campaign.PendingOptions[0].mission?.name}, [1] {campaign.PendingOptions[1].mission?.name}");

        // 設定階段為「路線選擇」
        Debug.Log("[TutorialState] 設定階段為 RouteSelection");
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.RouteSelection);

        // 使用場景中的 RoomExitController 顯示雙門
        Debug.Log("[TutorialState] 正在尋找 RoomExitController...");
        RoomExitController exitController = Object.FindFirstObjectByType<RoomExitController>();
        if (exitController != null)
        {
            Debug.Log($"[TutorialState] ✓ 找到 RoomExitController: {exitController.gameObject.name}");
            Debug.Log("[TutorialState] 正在呼叫 ShowExitDoors(needsRouteChoice: true)...");
            
            // 顯示雙門並自動解鎖（教學已完成，不需要等待敵人全滅）
            exitController.ShowExitDoors(needsRouteChoice: true);
            
            Debug.Log("[TutorialState] ✓ ShowExitDoors() 已執行");
            Debug.Log("[TutorialState] 已在教學場景內顯示節點 1 的雙門選路");
        }
        else
        {
            Debug.LogError("[TutorialState] ✗ 教學場景中找不到 RoomExitController！無法顯示選路門。");
            Debug.LogError("[TutorialState] 請確認教學場景（TeachScenes）已放置 RoomExitController 和雙門 Prefab。");
        }

        // 訂閱路線選擇事件（玩家走進哪扇門）
        Debug.Log("[TutorialState] 訂閱 OnRouteSelected 事件");
        BattleEventManager.OnRouteSelected += HandleFirstNodeRouteSelected;
        Debug.Log("[TutorialState] ✓ HandleTutorialComplete 執行完畢");
    }

    /// <summary>
    /// 玩家在教學場景內選擇了節點 1 的其中一個任務（走進哪扇門）。
    /// </summary>
    private void HandleFirstNodeRouteSelected(int optionIndex)
    {
        Debug.Log($"[TutorialState] 玩家選擇了節點 1 的任務選項 {optionIndex}");

        // 取消訂閱，避免二次觸發
        BattleEventManager.OnRouteSelected -= HandleFirstNodeRouteSelected;

        var campaign = GameDB.Instance?.Campaign;
        if (campaign == null)
        {
            Debug.LogError("[TutorialState] Campaign 為 null！");
            return;
        }

        // 將選擇寫入 CampaignData
        if (!campaign.TrySelectRoute(optionIndex))
        {
            Debug.LogError($"[TutorialState] 無法選擇路線 {optionIndex}！");
            return;
        }

        // 切換到 GameplayState，載入節點 1 選中的任務場景
        Debug.Log($"[TutorialState] 進入節點 {campaign.CurrentNodeNumber}，場景：{campaign.GetCurrentRoomSceneName()}");
        BattleEventManager.SetEncounterPhase(BattleEventManager.EncounterPhase.Transitioning);
        GameFlowManager.Instance?.ChangeState(new GameplayState());
    }

    private void HandlePlayerDied()
    {
        Debug.Log("[TutorialState] 玩家死亡，重新載入教學");
        // 教學死亡直接重跑教學，不顯示結算
        GameFlowManager.Instance?.ChangeState(new TutorialState());
    }

    public void Exit()
    {
        Debug.Log("[TutorialState] Exit");
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
