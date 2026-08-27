using UnityEngine;

public class BootState : IState
{
    public void Enter()
    {
        Debug.Log("[BootState] Enter - 遊戲啟動加載中...");
        // 確保新局從乾淨狀態開始（TotalRoomNumber、CampaignData 全部歸零）
        GameDB.Instance?.ResetCampaignData();
        GameFlowManager.Instance?.ChangeState(new MainMenuState());
    }

    public void Exit()
    {
        Debug.Log("[BootState] Exit");
    }

    public void Update() { }
    public void PhysicsUpdate() { }
}
