using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SafeRoomState : IState
{
    private int roomNumber;

    public SafeRoomState(int roomNumber)
    {
        this.roomNumber = roomNumber;
    }

    public void Enter()
    {
        Debug.Log($"[SafeRoomState] Enter - 進入安全房，房號: {roomNumber}");

        // 關閉戰鬥 HUD (依賴 UIManager 的實作)
        if (UIManager.Instance != null)
        {
            // 假設已有 HideGameplayHUD 或類似方法，先將其暫時關閉
            // UIManager.Instance.HideGameplayHUD(); 
        }

        // 啟動 Coroutine 處理非同步載入，確保載入完成後才進行後續動作
        GameFlowManager.Instance.StartCoroutine(LoadSafeRoomRoutine());
    }

    private IEnumerator LoadSafeRoomRoutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("TestSpecial");
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("[SafeRoomState] 安全房場景載入完成！");

        // 場景載入完成後，才訂閱走到出口的事件
        BattleEventManager.OnRoomCleared += HandleRoomExit;
    }

    private void HandleRoomExit()
    {
        Debug.Log("[SafeRoomState] 抵達出口，準備過渡...");

        // 邊角狀況處理：如果在第 15 關進入安全房，出來必須銜接 Boss 戰
        if (roomNumber == 15)
        {
            Debug.Log("[SafeRoomState] 第 15 關結束，進入 Boss 戰！");
            GameFlowManager.Instance.ChangeState(new BossBattleState());
        }
        else
        {
            // 無縫進入下一關戰鬥 (房號 + 1)
            Debug.Log($"[SafeRoomState] 進入下一關戰鬥: {roomNumber + 1}");
            GameFlowManager.Instance.ChangeState(new GameplayState(roomNumber + 1));
        }
    }

    public void Exit()
    {
        Debug.Log($"[SafeRoomState] Exit - 離開安全房，房號: {roomNumber}");
        
        // 務必解除事件訂閱，避免記憶體洩漏或切換狀態時發生錯誤
        BattleEventManager.OnRoomCleared -= HandleRoomExit;
    }

    public void Update() { }
    
    public void PhysicsUpdate() { }
}
