using System;
using UnityEngine;

/// <summary>
/// 可以被修改的屬性類型
/// </summary>
public enum StatType
{
    MaxIntegrityHp,   // 最大誠信值
    MoveSpeed,        // 移動速度
    AttackRange,      // 攻擊範圍
    AttackInfluence,  // 攻擊力/說服力 (新增)
    ConvertChance,    // 轉化機率
    AttackCooldown,   // 攻擊冷卻
    GlobalNpcSpeed,   // 全局NPC速度
    LoseControlRate,  // 失控率/流失率
    SpreadRadius      // 擴散半徑
}

/// <summary>
/// 玩家與全域戰鬥屬性，作為 Single Source of Truth
/// </summary>
[Serializable]
public class PlayerStatsData
{
    // 基礎值 (Base Value)
    public float BaseMoveSpeed = 5f;
    public float BaseAttackRange = 3f;
    public float BaseAttackInfluence = 1f; // 基礎攻擊力/說服力
    public float BaseConvertChance = 0.3f;
    public float BaseAttackCooldown = 0f;
    public float BaseGlobalNpcSpeedMultiplier = 1f;
    public float BaseLoseControlRate = 0f;
    public float BaseSpreadRadius = 0f;

    // 當前修改值 (Modified Value)
    public float ModifiedMoveSpeed { get; private set; }
    public float ModifiedAttackRange { get; private set; }
    public float ModifiedAttackInfluence { get; private set; }
    public float ModifiedConvertChance { get; private set; }
    public float ModifiedAttackCooldown { get; private set; }
    public float ModifiedGlobalNpcSpeedMultiplier { get; private set; }
    public float ModifiedLoseControlRate { get; private set; }
    public float ModifiedSpreadRadius { get; private set; }

    public event Action OnStatsChanged;

    public PlayerStatsData()
    {
        ResetModifiers();
    }

    /// <summary>
    /// 重置所有修改值回歸基礎值
    /// </summary>
    public void ResetModifiers()
    {
        ModifiedMoveSpeed = BaseMoveSpeed;
        ModifiedAttackRange = BaseAttackRange;
        ModifiedAttackInfluence = BaseAttackInfluence;
        ModifiedConvertChance = BaseConvertChance;
        ModifiedAttackCooldown = BaseAttackCooldown;
        ModifiedGlobalNpcSpeedMultiplier = BaseGlobalNpcSpeedMultiplier;
        ModifiedLoseControlRate = BaseLoseControlRate;
        ModifiedSpreadRadius = BaseSpreadRadius;
        NotifyStatsChanged();
    }

    /// <summary>
    /// 更新特定的數值
    /// </summary>
    public void SetModifier(StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.MoveSpeed:
                ModifiedMoveSpeed = value; break;
            case StatType.AttackRange:
                ModifiedAttackRange = value; break;
            case StatType.AttackInfluence:
                ModifiedAttackInfluence = value; break;
            case StatType.ConvertChance:
                ModifiedConvertChance = Mathf.Clamp01(value); break;
            case StatType.AttackCooldown:
                ModifiedAttackCooldown = Mathf.Max(0f, value); break;
            case StatType.GlobalNpcSpeed:
                ModifiedGlobalNpcSpeedMultiplier = Mathf.Max(0.1f, value); break;
            case StatType.LoseControlRate:
                ModifiedLoseControlRate = Mathf.Max(0f, value); break;
            case StatType.SpreadRadius:
                ModifiedSpreadRadius = Mathf.Max(0f, value); break;
            case StatType.MaxIntegrityHp:
                // 交由 GameDB.RunData 處理
                break;
        }
        NotifyStatsChanged();
    }

    public void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
}
