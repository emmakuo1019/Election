using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class HQState : IState
{
    private string hqSceneName = "headquarters";

    public void Enter()
    {
        Debug.Log("[HQState] Enter - 進入總部 (純介面流程)");
        Time.timeScale = 1f;

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
            
            Debug.Log($"[HQState] 場景 {hqSceneName} 載入完成，啟動 UI 選角流程。");

            // 禁用玩家移動（純介面操作，不需要玩家跑動）
            DisablePlayerMovement();

            // 等一幀讓 HQSceneController 完成 Start()，再啟動流程
            yield return null;

            // 啟動總部 UI 流程：切到男角鏡頭 → 顯示選角介面
            if (HQSceneController.Instance != null)
            {
                HQSceneController.Instance.BeginHQFlow();
            }
            else
            {
                Debug.LogWarning("[HQState] 找不到 HQSceneController，請確認場景內有掛載此腳本。");
                // Fallback：至少打開 UI
                if (UIManager.Instance != null) UIManager.Instance.ShowHQCandidateStep(true);
            }
        }
        else
        {
            Debug.LogWarning($"[HQState] 找不到場景 {hqSceneName}，請確認是否加入 Build Settings。");
        }
    }

    /// <summary>
    /// 停用場景內的玩家移動輸入，讓玩家角色原地站立當展示用
    /// </summary>
    private void DisablePlayerMovement()
    {
        // 找場景內所有 PlayerController，停用其 Update 的輸入讀取
        var players = GameObject.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            p.enabled = false;
            Debug.Log($"[HQState] 已停用 PlayerController: {p.gameObject.name}");
        }
    }

    public void Exit()
    {
        Debug.Log("[HQState] Exit");
        if (UIManager.Instance != null) UIManager.Instance.HideHQPanel();

        // 出發前恢復玩家輸入（進入戰鬥場景後 PlayerController 會重新初始化，這裡保險用）
        var players = GameObject.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            p.enabled = true;
        }
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
