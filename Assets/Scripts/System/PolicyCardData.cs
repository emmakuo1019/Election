using System.Collections.Generic;
using UnityEngine;

public enum CardRarity
{
    Common,     // 普通
    Rare,       // 稀有
    Legendary   // 傳說
}

[CreateAssetMenu(fileName = "PolicyCard", menuName = "Game/Policy Card V2")]
public class PolicyCardData : ScriptableObject
{
    [Header("基本資料")]
    public string cardName;

    [TextArea(2, 5)]
    public string description;

    [Header("卡牌屬性")]
    public CardRarity Rarity;

    [Tooltip("所屬派系（null = 通用，任何 build 皆可使用）")]
    public FactionData faction;

    [Header("卡牌圖像")]
    [Tooltip("卡牌主圖，每張卡獨立設定")]
    public Sprite cardArtwork;

    [Header("社會風氣影響")]
    [Tooltip("正值代表更偏情緒動員，負值代表更偏理性。")]
    public int socialClimateDelta = 0;

    [Header("觸發條件 (留空 = 獲得即永久生效)")]
    [SerializeReference]
    public IProcTrigger trigger;

    [Header("卡牌效果 (Strategy Pattern積木)")]
    [SerializeReference]
    public List<ICardEffect> Effects = new List<ICardEffect>();

    /// <summary>
    /// 獲得卡牌時呼叫。
    /// 有 trigger 的卡只套用社會風氣；效果等觸發條件成立再執行。
    /// 沒有 trigger 的卡立即套用所有效果（永久被動）。
    /// </summary>
    public void ApplyAllEffects()
    {
        // 社會風氣永遠在獲得時套用
        if (socialClimateDelta != 0 && GameDB.Instance != null)
            GameDB.Instance.Run.ModifyAtmosphere(socialClimateDelta);

        // 無觸發條件 → 永久被動，立即套用
        if (trigger == null)
        {
            foreach (var effect in Effects)
                effect?.ApplyEffect();
        }
        // 有觸發條件 → 效果交由 ProcTriggerBridge 在條件滿足時呼叫 FireProcEffects()
    }

    /// <summary>
    /// 由 ProcTriggerBridge 在觸發條件成立時呼叫，執行本卡所有效果。
    /// </summary>
    public void FireProcEffects()
    {
        foreach (var effect in Effects)
            effect?.ApplyEffect();
    }
}
