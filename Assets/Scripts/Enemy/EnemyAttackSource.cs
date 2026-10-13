/// <summary>
/// 敵人受擊時的傷害來源語意。
/// 用於 Reporter 白名單判斷：只有 BasicAttack 與符合白名單的 PlayerSkill 能傷害 Reporter。
/// </summary>
public enum EnemyHitSourceType
{
    /// <summary>玩家普通攻擊（永遠可傷害 Reporter）</summary>
    BasicAttack,

    /// <summary>玩家施放的技能（需對照 Reporter.reporterDamageWhitelist）</summary>
    PlayerSkill,

    /// <summary>其他來源（敵人互打、環境傷害等；Reporter 不接受）</summary>
    Other,
}

/// <summary>
/// 受擊資訊，攜帶傷害來源類型與技能參考（PlayerSkill 才有）。
/// </summary>
public readonly struct EnemyHitInfo
{
    public readonly EnemyHitSourceType sourceType;
    /// <summary>
    /// 技能來源（sourceType == PlayerSkill 時有值，其餘為 null）
    /// </summary>
    public readonly SkillData sourceSkill;

    public EnemyHitInfo(EnemyHitSourceType type, SkillData skill = null)
    {
        sourceType = type;
        sourceSkill = skill;
    }

    /// <summary>玩家普攻的靜態常數，避免每次 new。</summary>
    public static readonly EnemyHitInfo BasicAttack =
        new EnemyHitInfo(EnemyHitSourceType.BasicAttack);
}
