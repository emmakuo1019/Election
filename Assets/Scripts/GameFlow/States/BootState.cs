using UnityEngine;
using System.Collections;

public class BootState : IState
{
    public void Enter()
    {
        Debug.Log("[BootState] Enter - 遊戲啟動加載中...");
        // 如果未來有 Loading 面板，可在此處呼叫 UIManager.Instance.ShowLoadingScreen()
        
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartCoroutine(LoadSystemsRoutine());
        }
    }
    
    private IEnumerator LoadSystemsRoutine()
    {
        // 模擬系統加載時間
        yield return new WaitForSeconds(0.5f);
        GameFlowManager.Instance.ChangeState(new MainMenuState());
    }
    
    public void Exit()
    {
        Debug.Log("[BootState] Exit");
        // UIManager.Instance.HideLoadingScreen()
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
