using UnityEngine;

public class CharacterSelectState : IState
{
    public void Enter()
    {
        Debug.Log("[CharacterSelectState] Enter - 進入選角畫面");
        if (UIManager.Instance != null) UIManager.Instance.ShowCharacterSelect();
    }
    
    public void Exit()
    {
        Debug.Log("[CharacterSelectState] Exit");
        if (UIManager.Instance != null) UIManager.Instance.HideCharacterSelect();
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
