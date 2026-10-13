/// <summary>
/// 技能真正執行時傳遞的施法資訊。
/// 供記者監視判斷（Task 06）使用，與 OnAnySkillUsed 的語意完全分開。
///
/// CastId 由施法方在觸發前生成（用 System.Guid.NewGuid()），
/// 每次施法唯一，供跨記者去重使用。
/// </summary>
public readonly struct SkillCastInfo
{
    /// <summary>施法者的 Transform（玩家或敵人）</summary>
    public readonly UnityEngine.Transform Caster;

    /// <summary>實際執行的技能資料</summary>
    public readonly SkillData Skill;

    /// <summary>本次施法的唯一識別碼，避免多名記者重複處罰同一次施法</summary>
    public readonly System.Guid CastId;

    public SkillCastInfo(UnityEngine.Transform caster, SkillData skill)
    {
        Caster = caster;
        Skill   = skill;
        CastId  = System.Guid.NewGuid();
    }
}
