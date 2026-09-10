using UnityEngine;

/// <summary>
/// Boss冷卻干擾技能（FinalBoss專屬）
/// 重置玩家J/K技能冷卻並暫時暈眩玩家
/// FinalBoss: 40秒CD
/// </summary>
[CreateAssetMenu(fileName = "EnemySkill_Disrupt", menuName = "Skills/Enemy/Disrupt Player")]
public class EnemySkill_DisruptPlayer : EnemySkillData
{
    // 使用父類的 blastRadius, playerLayerMask, playerStunDuration 等屬性

    public override void ExecuteSkill(GameObject caster)
    {
        // 範圍內玩家受影響
        Collider[] playerHits = Physics.OverlapSphere(
            caster.transform.position, 
            blastRadius, 
            playerLayerMask
        );

        int disruptedCount = 0;

        foreach (var col in playerHits)
        {
            PlayerSkillManager skillManager = col.GetComponent<PlayerSkillManager>();
            if (skillManager != null)
            {
                // 重置J/K技能冷卻
                if (skillManager.baseSkillJ != null)
                {
                    skillManager.ResetSkillCooldown(skillManager.baseSkillJ);
                }
                if (skillManager.skillK != null)
                {
                    skillManager.ResetSkillCooldown(skillManager.skillK);
                }
                disruptedCount++;
            }

            // 暈眩玩家
            if (playerStunDuration > 0f)
            {
                PlayerController player = col.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.ApplyStun(playerStunDuration);
                }
            }
        }

        // 播放干擾特效（紅色波紋）
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
