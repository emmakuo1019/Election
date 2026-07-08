using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameplayState : IState
{
    private int roomNumber;
    private string battleSceneName = "TestMVP";

    public GameplayState(int roomNumber)
    {
        this.roomNumber = roomNumber;
    }

    public void Enter()
    {
        Debug.Log($"[GameplayState] Enter - 進入戰鬥房間，房號: {roomNumber}");
        // ⚠️ 把 ShowGameplayHUD() 移到場景載入完成後，確保 LevelTimer 先存在

        // 啟動非同步場景載入
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartCoroutine(LoadBattleSceneRoutine());
        }

        // 訂閱戰鬥事件
        BattleEventManager.OnRoomCleared += HandleRoomCleared;
        BattleEventManager.OnPlayerDied += HandlePlayerDied;
    }

    private IEnumerator LoadBattleSceneRoutine()
    {
        // 這裡如果是 Additive 載入，原本 s0 場景的 UI 會留著
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Single);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            // 安全檢查：若狀態已經被切走，則中止協程
            if (GameFlowManager.Instance.CurrentState != this) yield break;

            Debug.Log($"[GameplayState] 場景 {battleSceneName} 載入完成！");

            // 步驟 A: UI 先掛載並綁定
            if (UIManager.Instance != null) UIManager.Instance.ShowGameplayHUD();
            
            // 步驟 B: 動態給定 60 秒並啟動
            if (LevelTimer.Instance != null) LevelTimer.Instance.StartTimer(60f);
        }
        else
        {
            Debug.LogWarning($"[GameplayState] 找不到場景 {battleSceneName}，請確認是否加入 Build Settings。");
        }
    }

    private void HandleRoomCleared()
    {
        Debug.Log($"[GameplayState] 收到過關事件，準備前往結算 (房號: {roomNumber})");
        GameFlowManager.Instance.ChangeState(new StageClearState(roomNumber));
    }

    private void HandlePlayerDied()
    {
        Debug.Log("[GameplayState] 收到玩家死亡事件，前往遊戲結束狀態");
        GameFlowManager.Instance.ChangeState(new GameEndState(false));
    }
    
    public void Exit()
    {
        Debug.Log($"[GameplayState] Exit - 離開戰鬥房間，房號: {roomNumber}");
        if (UIManager.Instance != null) UIManager.Instance.HideGameplayHUD();

        // ⚠️ 解除訂閱戰鬥事件，避免 Memory Leak 或重複觸發
        BattleEventManager.OnRoomCleared -= HandleRoomCleared;
        BattleEventManager.OnPlayerDied -= HandlePlayerDied;
    }
    
    public void Update() 
    {
        // 按下 Y 鍵：模擬正常過關事件（會走結算 UI 流程，結束後 roomNumber + 1）
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log($"[GameplayState] 偵測到按下 Y 鍵，模擬過關！當前關卡: {roomNumber}");
            BattleEventManager.TriggerRoomCleared();
        }

        // 按下 N 鍵：直接跳過結算 UI，進入下一關 roomNumber + 1 (如果是 14 則進入 Boss 戰)
        if (Input.GetKeyDown(KeyCode.N))
        {
            int nextRoom = roomNumber + 1;
            Debug.Log($"[GameplayState] 偵測到按下 N 鍵，直接跳過結算 UI！關卡切換: {roomNumber} -> {nextRoom}");
            // 當完成第 14 關時，下一關即為第 15 關 (Boss 戰)
            if (roomNumber == 14)
            {
                GameFlowManager.Instance.ChangeState(new BossBattleState());
            }
            else
            {
                GameFlowManager.Instance.ChangeState(new GameplayState(nextRoom));
            }
        }
    }
    public void PhysicsUpdate() { }
}
