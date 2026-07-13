using System;

public interface ICardEffect
{
    /// <summary>
    /// 當獲得卡牌或觸發條件滿足時執行
    /// </summary>
    void ApplyEffect();

    /// <summary>
    /// 當失去卡牌或效果結束時執行
    /// </summary>
    void RemoveEffect();
}
