using UnityEngine;

public class BootState : IState
{
    public void Enter()
    {
        // 確保新局從乾淨狀態開始（CampaignData 是唯一進度來源）。
        GameDB.Instance?.ResetCampaignData();
        GameFlowManager.Instance?.ChangeState(new MainMenuState());
    }

    public void Exit()
    {
    }

    public void Update() { }
    public void PhysicsUpdate() { }
}
