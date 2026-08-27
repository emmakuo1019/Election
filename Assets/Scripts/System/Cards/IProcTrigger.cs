using System;

/// <summary>
/// 觸發條件抽象層。
/// 每張政策卡可掛載一個 IProcTrigger；
/// Register 時訂閱對應的 BattleEventManager 事件，
/// Unregister 時取消訂閱。
/// 當條件滿足時呼叫注入的 onTriggered callback。
/// </summary>
public interface IProcTrigger
{
    /// <summary>訂閱事件，將 onTriggered 儲存為回呼。</summary>
    void Register(Action onTriggered);

    /// <summary>取消訂閱事件，清除回呼。</summary>
    void Unregister();
}
