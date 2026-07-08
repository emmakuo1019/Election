using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class HQState : IState
{
    private string hqSceneName = "headquarters";
    private bool isTransitioning = false;

    public void Enter()
    {
        Debug.Log("[HQState] Enter - 進入總部 (實體觸發流程)");
        isTransitioning = true; // 鎖定狀態，直到場景載入完成
        Time.timeScale = 1f; // 確保時間流動正常，否則玩家無法移動！

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
            
            if (UIManager.Instance != null) 
            {
                // 只開啟基礎的 HQ HUD，不跳出按鍵提示
                UIManager.Instance.ShowHQPanel();
            }
            Debug.Log($"[HQState] 場景 {hqSceneName} 載入完成！玩家可自由行動。");
            
            // 注意：我們不再停用 PlayerController。
            // 讓玩家在總部內可以自由跑動去撞擊實體方塊。
            
            isTransitioning = false;
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
    
    public void Update() 
    { 
        // 由於已經改為實體觸發流程，這裡完全不需要監聽任何按鍵。
        // 所有選擇行為都交給 HQSkillTrigger 和 HQExitTrigger 處理。
    }

    public void PhysicsUpdate() { }
}
