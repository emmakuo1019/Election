using UnityEngine;

/// <summary>
/// Boss強力拉票技能
/// 大範圍（12米）影響選民，快速拉票
/// Elite: 25秒CD, 影響力-12
/// FinalBoss: 15秒CD, 影響力-15
/// </summary>
[CreateAssetMenu(fileName = "EnemySkill_MassPersuasion", menuName = "Skills/Enemy/Mass Persuasion")]
public class EnemySkill_MassPersuasion : EnemySkillData
{
    // 使用父類的 blastRadius, voterInfluenceAmount, voterLayerMask 等屬性
    
    public override void ExecuteSkill(GameObject caster)
    {
        // 大範圍影響選民
        Collider[] voterHits = Physics.OverlapSphere(
            caster.transform.position, 
            blastRadius, 
            voterLayerMask
        );

        int affectedCount = 0;

        foreach (var col in voterHits)
        {
            VoterLogic voter = col.GetComponent<VoterLogic>();
            if (voter != null && voter.CanReceiveSkillEffect)
            {
                voter.OnInfluence(voterInfluenceAmount, true, caster.transform.position);
                affectedCount++;
            }
        }

        // 播放VFX（衝擊波特效）
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, caster.transform.position, Quaternion.identity);
            if (vfxDuration > 0f)
            {
                Destroy(vfx, vfxDuration);
            }
        }
    }
}
