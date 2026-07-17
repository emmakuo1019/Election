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
        float beforeValue = 0f;
        float afterValue = 0f;

        // 根據不同的 StatType 取得當前值並套用修改
        switch (TargetStat)
        {
            case StatType.MaxIntegrityHp:
                beforeValue = GameDB.Instance.Run.MaxIntegrityHp;
                float newMaxHp = CalculateModifiedValue(beforeValue);
                GameDB.Instance.Run.SetMaxIntegrityHp(newMaxHp, false);
                afterValue = GameDB.Instance.Run.MaxIntegrityHp;
                break;
            case StatType.MoveSpeed:
                beforeValue = stats.ModifiedMoveSpeed;
                stats.SetModifier(StatType.MoveSpeed, CalculateModifiedValue(beforeValue));
                afterValue = stats.ModifiedMoveSpeed;
                break;
            case StatType.AttackRange:
                beforeValue = stats.ModifiedAttackRange;
                stats.SetModifier(StatType.AttackRange, CalculateModifiedValue(beforeValue));
                afterValue = stats.ModifiedAttackRange;
                break;
            case StatType.AttackInfluence:
                beforeValue = stats.ModifiedAttackInfluence;
                stats.SetModifier(StatType.AttackInfluence, CalculateModifiedValue(beforeValue));
                afterValue = stats.ModifiedAttackInfluence;
                break;
            case StatType.ConvertChance:
                beforeValue = stats.ModifiedConvertChance;
                stats.SetModifier(StatType.ConvertChance, CalculateModifiedValue(beforeValue));
                afterValue = stats.ModifiedConvertChance;
                break;
            case StatType.AttackCooldown:
                beforeValue = stats.ModifiedAttackCooldown;
                stats.SetModifier(StatType.AttackCooldown, CalculateModifiedValue(beforeValue));
                afterValue = stats.ModifiedAttackCooldown;
                break;
            case StatType.GlobalNpcSpeed:
                beforeValue = stats.ModifiedGlobalNpcSpeedMultiplier;
                stats.SetModifier(StatType.GlobalNpcSpeed, CalculateModifiedValue(beforeValue));
                afterValue = stats.ModifiedGlobalNpcSpeedMultiplier;
                break;
            case StatType.LoseControlRate:
                beforeValue = stats.ModifiedLoseControlRate;
                stats.SetModifier(StatType.LoseControlRate, CalculateModifiedValue(beforeValue));
                afterValue = stats.ModifiedLoseControlRate;
                break;
            case StatType.SpreadRadius:
                beforeValue = stats.ModifiedSpreadRadius;
                stats.SetModifier(StatType.SpreadRadius, CalculateModifiedValue(beforeValue));
                afterValue = stats.ModifiedSpreadRadius;
                break;
        }

        Debug.Log($"[StatModifierEffect] 數值套用偵錯 => 屬性: {TargetStat.ToString()}, 計算: {ModType.ToString()}, 數值: {Value}, 變更前: {beforeValue} -> 變更後: {afterValue} (SSOT GameDB已同步更新: {(beforeValue != afterValue)})");
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
