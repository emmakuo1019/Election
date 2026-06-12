using UnityEngine;

public class HQState : IState
{
    public void Enter()
    {
        Debug.Log("[HQState] Enter - 進入總部 (移動、選技能)");
        if (UIManager.Instance != null) UIManager.Instance.ShowHQPanel();
    }
    
    public void Exit()
    {
        Debug.Log("[HQState] Exit");
        if (UIManager.Instance != null) UIManager.Instance.HideHQPanel();
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
