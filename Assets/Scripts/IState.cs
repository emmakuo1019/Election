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

    /// <summary>
    /// 處理輸入邏輯，將輸入判定責任交給狀態本身。
    /// </summary>
    void HandleInput() { }

    /// <summary>
    /// 當受到暈眩/僵直攻擊時觸發，狀態可決定是否被打斷。
    /// </summary>
    void OnStunned(float duration) { }

    /// <summary>
    /// 由外部動畫事件 (Animation Event) 呼叫。預設為空實作。
    /// </summary>
    void AnimationFinishTrigger() { }
}
