using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class HQState : IState
{
    private string hqSceneName = "headquarters";

    public void Enter()
    {
        Debug.Log("[HQState] Enter - 進入總部 (移動、選技能)");
        if (UIManager.Instance != null) UIManager.Instance.ShowHQPanel();

        // 啟動非同步場景載入
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartCoroutine(LoadHQSceneRoutine());
        }
    }
    
    private IEnumerator LoadHQSceneRoutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(hqSceneName);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            Debug.Log($"[HQState] 場景 {hqSceneName} 載入完成！");
        }
        else
        {
            Debug.LogWarning($"[HQState] 找不到場景 {hqSceneName}，請確認是否加入 Build Settings。");
        }
    }

    public void Exit()
    {
        Debug.Log("[HQState] Exit");
        if (UIManager.Instance != null) UIManager.Instance.HideHQPanel();
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
