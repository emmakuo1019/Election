using UnityEngine;

public class MainMenuState : IState
{
    public void Enter()
    {
        Debug.Log("[MainMenuState] Enter - 進入主畫面");
        if (UIManager.Instance != null) UIManager.Instance.ShowMainMenu();
    }
    
    public void Exit()
    {
        Debug.Log("[MainMenuState] Exit");
        if (UIManager.Instance != null) UIManager.Instance.HideMainMenu();
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
