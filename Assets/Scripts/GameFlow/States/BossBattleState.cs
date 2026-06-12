using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BossBattleState : IState
{
    private string bossSceneName = "TestSmallBoss";

    public void Enter()
    {
        Debug.Log("[BossBattleState] Enter - 進入 Boss 戰！");

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartCoroutine(LoadBossSceneRoutine());
        }

        // 訂閱戰鬥事件
        BattleEventManager.OnRoomCleared += HandleBossDefeated;
        BattleEventManager.OnPlayerDied += HandlePlayerDied;
    }

    private IEnumerator LoadBossSceneRoutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(bossSceneName, LoadSceneMode.Single);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            Debug.Log($"[BossBattleState] 場景 {bossSceneName} 載入完成！");

            // ⭐️ 場景載入完成後打開 HUD
            if (UIManager.Instance != null) UIManager.Instance.ShowGameplayHUD();
        }
        else
        {
            Debug.LogWarning($"[BossBattleState] 找不到場景 {bossSceneName}，請確認是否加入 Build Settings。");
        }
    }

    private void HandleBossDefeated()
    {
        Debug.Log("[BossBattleState] 收到過關事件，打贏 Boss！前往遊戲結束狀態 (勝利)");
        GameFlowManager.Instance.ChangeState(new GameEndState(true));
    }

    private void HandlePlayerDied()
    {
        Debug.Log("[BossBattleState] 收到玩家死亡事件，前往遊戲結束狀態 (失敗)");
        GameFlowManager.Instance.ChangeState(new GameEndState(false));
    }

    public void Exit()
    {
        Debug.Log("[BossBattleState] Exit - 離開 Boss 戰");
        if (UIManager.Instance != null) UIManager.Instance.HideGameplayHUD();

        // ⚠️ 解除訂閱戰鬥事件
        BattleEventManager.OnRoomCleared -= HandleBossDefeated;
        BattleEventManager.OnPlayerDied -= HandlePlayerDied;
    }

    public void Update() 
    {
        // 按下 Y 鍵：模擬打贏 Boss（會走勝利結束流程）
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("[BossBattleState] 偵測到按下 Y 鍵，模擬擊敗 Boss！");
            BattleEventManager.TriggerRoomCleared();
        }
    }
    public void PhysicsUpdate() { }
}
