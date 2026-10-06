using UnityEngine;

public class MainMenuState : IState
{
    public void Enter()
    {
        Debug.Log("[MainMenuState] Enter - 進入主畫面");
        if (UIManager.Instance != null) UIManager.Instance.ShowMainMenu();
        
        // 播放主選單背景音樂（與總部使用同一首）
        if (AudioManager.Instance != null) AudioManager.Instance.PlayBGM_MenuAndHQ();
    }
    
    public void Exit()
    {
        Debug.Log("[MainMenuState] Exit");
        if (UIManager.Instance != null) UIManager.Instance.HideMainMenu();
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
