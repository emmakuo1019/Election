using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SafeRoomState : IState
{
    private int roomNumber;
    private bool _subscribed = false;

    public SafeRoomState(int roomNumber)
    {
        this.roomNumber = roomNumber;
    }

    public void Enter()
    {
        Debug.Log($"[SafeRoomState] Enter - 進入安全房，房號: {roomNumber}");
        GameFlowManager.Instance.StartCoroutine(LoadSafeRoomRoutine());
    }

    private IEnumerator LoadSafeRoomRoutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("TestSpecial");
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 安全檢查：若狀態已經被切走（例如載入途中玩家死亡），則中止訂閱
        if (GameFlowManager.Instance.CurrentState != this) yield break;

        Debug.Log("[SafeRoomState] 安全房場景載入完成！");

        BattleEventManager.OnRoomCleared += HandleRoomExit;
        _subscribed = true;
    }

    private void HandleRoomExit()
    {
        Debug.Log("[SafeRoomState] 抵達出口，準備過渡...");

        if (roomNumber == 15)
        {
            Debug.Log("[SafeRoomState] 第 15 關結束，進入 Boss 戰！");
            GameFlowManager.Instance.ChangeState(new BossBattleState());
        }
        else
        {
            Debug.Log($"[SafeRoomState] 進入下一關戰鬥: {roomNumber + 1}");
            GameFlowManager.Instance.ChangeState(new GameplayState(roomNumber + 1));
        }
    }

    public void Exit()
    {
        Debug.Log($"[SafeRoomState] Exit - 離開安全房，房號: {roomNumber}");
        
        if (_subscribed)
        {
            BattleEventManager.OnRoomCleared -= HandleRoomExit;
            _subscribed = false;
        }
    }

    public void Update() { }
    public void PhysicsUpdate() { }
}
