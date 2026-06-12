using UnityEngine;

public class StageClearState : IState
{
    private int roomNumber;

    public StageClearState(int roomNumber)
    {
        this.roomNumber = roomNumber;
    }

    public void Enter()
    {
        Debug.Log($"[StageClearState] Enter - 小結算，結束房號: {roomNumber}");
        if (UIManager.Instance != null) UIManager.Instance.ShowStageClearPanel();
        
        // (此處保留 5/10 關選技能預留邏輯：例如將 roomNumber 傳入 UIManager 或其他系統，去決定是否額外顯示技能選擇)
    }

    public void Exit()
    {
        Debug.Log($"[StageClearState] Exit - 離開小結算，房號: {roomNumber}");
        if (UIManager.Instance != null) UIManager.Instance.HideStageClearPanel();
    }
    
    public void Update() { }
    public void PhysicsUpdate() { }
}
