using System.Collections.Generic;
using UnityEngine;

public enum CardRarity
{
    Common,     // 普通
    Rare,       // 稀有
    Legendary   // 傳說
}

public enum CardType
{
    Policy,     // 政策論述 (偏理性)
    Emotion,    // 情緒動員 (偏感性)
    Tactic      // 戰術輔助
}

[CreateAssetMenu(fileName = "PolicyCard", menuName = "Game/Policy Card V2")]
public class PolicyCardData : ScriptableObject
{
    [Header("基本資料")]
    public string cardName;

    [TextArea(2, 5)]
    public string description;

    [Header("卡牌屬性")]
    public CardType Type;
    public CardRarity Rarity;

    [Header("社會風氣影響")]
    [Tooltip("正值代表更偏情緒動員，負值代表更偏理性。")]
    public int socialClimateDelta = 0;

    [Header("卡牌效果 (Strategy Pattern積木)")]
    [SerializeReference]
    public List<ICardEffect> Effects = new List<ICardEffect>();

    /// <summary>
    /// 套用卡牌的所有效果
    /// </summary>
    public void ApplyAllEffects()
    {
        // 1. 套用社會風氣變更 (如果有的話)
        if (socialClimateDelta != 0 && GameDB.Instance != null)
        {
            GameDB.Instance.Run.ModifyAtmosphere(socialClimateDelta);
        }

        // 2. 套用所有積木效果
        if (Effects != null)
        {
            foreach (var effect in Effects)
            {
                effect?.ApplyEffect();
            }
        }
    }
}
