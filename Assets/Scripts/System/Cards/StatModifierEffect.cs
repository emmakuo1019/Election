using System;
using UnityEngine;

/// <summary>
/// 數值修改的計算類型
/// </summary>
public enum ModifierType
{
    Add,        // 加法 (例如: Base + Value)
    Multiply    // 乘法 (例如: Base * Value)
}

/// <summary>
/// 數值修改效果積木：實作 ICardEffect 介面
/// </summary>
[Serializable]
public class StatModifierEffect : ICardEffect
{
    [Header("目標數值")]
    public StatType TargetStat;

    [Header("計算方式")]
    public ModifierType ModType;

    [Header("變更數值")]
    public float Value;

    public void ApplyEffect()
    {
        if (GameDB.Instance == null) return;
        
        var stats = GameDB.Instance.Run.Stats;

        // 根據不同的 StatType 取得當前值並套用修改
        switch (TargetStat)
        {
            case StatType.MaxIntegrityHp:
                float currentMaxHp = GameDB.Instance.Run.MaxIntegrityHp;
                float newMaxHp = CalculateModifiedValue(currentMaxHp);
                GameDB.Instance.Run.SetMaxIntegrityHp(newMaxHp, false);
                break;
            case StatType.MoveSpeed:
                stats.SetModifier(StatType.MoveSpeed, CalculateModifiedValue(stats.ModifiedMoveSpeed));
                break;
            case StatType.AttackRange:
                stats.SetModifier(StatType.AttackRange, CalculateModifiedValue(stats.ModifiedAttackRange));
                break;
            case StatType.AttackInfluence:
                stats.SetModifier(StatType.AttackInfluence, CalculateModifiedValue(stats.ModifiedAttackInfluence));
                break;
            case StatType.ConvertChance:
                stats.SetModifier(StatType.ConvertChance, CalculateModifiedValue(stats.ModifiedConvertChance));
                break;
            case StatType.AttackCooldown:
                stats.SetModifier(StatType.AttackCooldown, CalculateModifiedValue(stats.ModifiedAttackCooldown));
                break;
            case StatType.GlobalNpcSpeed:
                stats.SetModifier(StatType.GlobalNpcSpeed, CalculateModifiedValue(stats.ModifiedGlobalNpcSpeedMultiplier));
                break;
            case StatType.LoseControlRate:
                stats.SetModifier(StatType.LoseControlRate, CalculateModifiedValue(stats.ModifiedLoseControlRate));
                break;
            case StatType.SpreadRadius:
                stats.SetModifier(StatType.SpreadRadius, CalculateModifiedValue(stats.ModifiedSpreadRadius));
                break;
        }
    }

    public void RemoveEffect()
    {
        // 目前設計是獲得卡牌後永久生效，如果要支援「限時效果」或「失去卡牌」
        // 需要記錄初始值或由 PolicyManager (或新機制) 觸發 RecalculateAll()
        // 暫不實作，保留介面
    }

    private float CalculateModifiedValue(float currentValue)
    {
        if (ModType == ModifierType.Add)
        {
            return currentValue + Value;
        }
        else if (ModType == ModifierType.Multiply)
        {
            return currentValue * Value;
        }
        return currentValue;
    }
}
