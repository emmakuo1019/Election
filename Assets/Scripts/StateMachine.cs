using UnityEngine;

/// <summary>
/// 獨立的狀態機類別，負責管理狀態的切換與生命週期（非 MonoBehaviour）。
/// </summary>
public class StateMachine
{
    /// <summary>
    /// 當前正在執行的狀態。
    /// </summary>
    public IState CurrentState { get; private set; }

    /// <summary>
    /// 初始化狀態機並設定起始狀態。
    /// </summary>
    /// <param name="startingState">起始狀態</param>
    public void Initialize(IState startingState)
    {
        CurrentState = startingState;
        CurrentState?.Enter();
    }

    /// <summary>
    /// 切換到新的狀態。
    /// </summary>
    /// <param name="newState">欲切換的新狀態</param>
    public void ChangeState(IState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
    }
}
