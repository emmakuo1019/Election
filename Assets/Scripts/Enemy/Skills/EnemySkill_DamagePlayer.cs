using UnityEngine;

/// <summary>
/// Boss 技能 B：有明確前搖的誠信 HP 傷害。
/// 傷害統一走 PlayerHealthSystem.TakeDamage()（護盾、CheckGameOver 完整流程）。
/// 不直接修改 GameDB.IntegrityHp。
///
/// 使用父類 EnemySkillData 的欄位：
///   blastRadius     — 命中範圍半徑
///   blastAngle      — 命中範圍角度
///   playerLayerMask — 玩家圖層
///   skillWindupTime — 前搖（玩家可在此期間閃避）
///   duration        — 技能總時長
///   cooldown        — 冷卻（在 EnemyController 管理）
///   initialDelay    — 首次施放延遲
/// 新增欄位：
///   playerDamage    — 扣除的誠信 HP 量
/// </summary>
[CreateAssetMenu(fileName = "EnemySkill_DamagePlayer", menuName = "Skills/Enemy/Damage Player")]
public class EnemySkill_DamagePlayer : EnemySkillData
{
    [Header("傷害設定")]
    [Tooltip("命中玩家時扣除的誠信 HP，走 PlayerHealthSystem.TakeDamage()")]
    [Min(0f)] public float playerDamage = 15f;

    public override void ExecuteSkill(GameObject caster)
    {
        if (playerDamage <= 0f) return;

        // 以施法者當前朝向做扇形判定（Enter 時已鎖定方向）
        Vector3 castDir = caster.transform.forward;

        Collider[] hits = Physics.OverlapSphere(caster.transform.position, blastRadius, playerLayerMask);
        // 去重：同一次技能最多命中玩家一次
        bool alreadyHit = false;
        foreach (var col in hits)
        {
            if (alreadyHit) break;

            PlayerController player = col.GetComponentInParent<PlayerController>();
            if (player == null) continue;

            // 扇形角度判斷
            if (blastAngle < 360f)
            {
                Vector3 toPlayer = (player.transform.position - caster.transform.position);
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.001f &&
                    Vector3.Angle(castDir, toPlayer.normalized) > blastAngle * 0.5f)
                    continue;
            }

            alreadyHit = true;

            if (!PlayerHealthSystem.HasInstance)
            {
                Debug.LogError("[EnemySkill_DamagePlayer] PlayerHealthSystem 不存在，傷害未套用。");
                continue;
            }
            PlayerHealthSystem.Instance.TakeDamage(playerDamage);
            Debug.Log($"[EnemySkill_DamagePlayer] 命中 {player.name}，扣除誠信 {playerDamage}。");
        }

        // VFX
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, caster.transform.position, Quaternion.identity);
            if (vfxDuration > 0f) Destroy(vfx, vfxDuration);
        }
    }
}
