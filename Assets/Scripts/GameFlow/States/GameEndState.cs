using UnityEngine;

public class GameEndState : IState
{
    private bool isWin;

    public GameEndState(bool isWin)
    {
        this.isWin = isWin;
    }

    public void Enter()
    {
        Debug.Log($"[GameEndState] Enter - 總結算，是否勝利: {isWin}");
        // 總結算面板可以根據 isWin 來決定顯示勝利還是失敗的資訊
        if (UIManager.Instance != null) UIManager.Instance.ShowGameEndPanel();

        // 狀態進入時，主動訂閱返回總部事件
        BattleEventManager.OnReturnToHQConfirmed += HandleReturnToHQ;
    }

    // 收到 UI 廣播後，由狀態機的當前狀態負責決定下一個狀態
    private void HandleReturnToHQ()
    {
        Debug.Log("[GameEndState] 收到返回總部確認事件，切換至 HQState");
        GameFlowManager.Instance.ChangeState(new HQState());
    }

    public void Exit()
    {
        Debug.Log($"[GameEndState] Exit - 離開總結算，是否勝利: {isWin}");
        if (UIManager.Instance != null) UIManager.Instance.HideGameEndPanel();

        // 嚴格遵守記憶體安全：狀態離開時，確實解除訂閱
        BattleEventManager.OnReturnToHQConfirmed -= HandleReturnToHQ;
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
