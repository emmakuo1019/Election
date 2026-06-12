using UnityEngine;

public class BootState : IState
{
    public void Enter()
    {
        Debug.Log("[BootState] Enter - 遊戲啟動加載中...");
        // 如果未來有 Loading 面板，可在此處呼叫 UIManager.Instance.ShowLoadingScreen()
    }
    
    public void Exit()
    {
        Debug.Log("[BootState] Exit");
        // UIManager.Instance.HideLoadingScreen()
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
