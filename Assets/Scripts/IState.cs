/// <summary>
/// 定義狀態機中每個狀態的基本生命週期。
/// </summary>
public interface IState
{
    /// <summary>
    /// 進入狀態時呼叫。
    /// </summary>
    void Enter();

    /// <summary>
    /// 處理每幀的邏輯，對應 MonoBehaviour.Update()。
    /// </summary>
    void Update();

    /// <summary>
    /// 處理物理邏輯，對應 MonoBehaviour.FixedUpdate()。
    /// </summary>
    void PhysicsUpdate();

    /// <summary>
    /// 離開狀態時呼叫。
    /// </summary>
    void Exit();
}
