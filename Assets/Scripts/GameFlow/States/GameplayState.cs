using UnityEngine;

public class GameplayState : IState
{
    private int roomNumber;

    public GameplayState(int roomNumber)
    {
        this.roomNumber = roomNumber;
    }

    public void Enter()
    {
        Debug.Log($"[GameplayState] Enter - 進入戰鬥房間，房號: {roomNumber}");
        if (UIManager.Instance != null) UIManager.Instance.ShowGameplayHUD();
    }
    
    public void Exit()
    {
        Debug.Log($"[GameplayState] Exit - 離開戰鬥房間，房號: {roomNumber}");
        if (UIManager.Instance != null) UIManager.Instance.HideGameplayHUD();
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
