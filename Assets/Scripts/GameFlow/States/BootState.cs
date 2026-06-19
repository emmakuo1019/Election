using UnityEngine;

public class BootState : IState
{
    public void Enter()
    {
        Debug.Log("[BootState] Enter - 遊戲啟動加載中...");
        GameFlowManager.Instance?.ChangeState(new MainMenuState());
    }

    public void Exit()
    {
        Debug.Log("[BootState] Exit");
    }

    public void Update() { }
    public void PhysicsUpdate() { }
}
