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
    }

    public void Exit()
    {
        Debug.Log($"[GameEndState] Exit - 離開總結算，是否勝利: {isWin}");
        if (UIManager.Instance != null) UIManager.Instance.HideGameEndPanel();
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
