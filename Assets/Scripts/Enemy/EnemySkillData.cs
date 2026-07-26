// TODO: [BattleEventManager] Add TriggerOnPlayerStunned(float duration) 
// and call after player.ApplyStun(). Pending UI/SFX integration.

using UnityEngine;

[CreateAssetMenu(fileName = "EnemySkillData", menuName = "Skills/Enemy Skill Data")]
public class EnemySkillData : SkillData
{
    [Header("技能設定 (Enemy Skill)")]
    public float blastRadius = 5f;           // 技能爆炸影響半徑
    public float blastAngle = 360f;          // 爆炸角度（預設全向）
    public int voterInfluenceAmount = 7;     // 對選民的影響值（正值=拉向敵方）
    public LayerMask voterLayerMask;         // 選民所在 Layer
    public float playerStunDuration = 1.5f;  // 玩家被暈眩持續時間（0 = 不暈眩）
    public LayerMask playerLayerMask;        // 玩家所在 Layer
    public float skillWindupTime = 0.8f;     // 前搖時間（供 EnemySkillState 使用）

    public override void ExecuteSkill(GameObject caster)
    {
        // 範圍提示圈的顯示/隱藏生命週期由 EnemySkillState 全權管理
        // ExecuteSkill 只負責判定邏輯，不觸碰 AttackRangeMesh

        // 1. 對範圍內選民施加影響
        Collider[] voterHits = Physics.OverlapSphere(caster.transform.position, blastRadius, voterLayerMask);
        foreach (var col in voterHits)
        {
            VoterLogic voter = col.GetComponent<VoterLogic>();
            if (voter != null)
            {
                voter.OnInfluence(voterInfluenceAmount, true, caster.transform.position);
            }
        }

        // 2. 對範圍內玩家施加暈眩（playerStunDuration > 0 才判定）
        if (playerStunDuration > 0f)
        {
            Collider[] playerHits = Physics.OverlapSphere(caster.transform.position, blastRadius, playerLayerMask);
            foreach (var col in playerHits)
            {
                PlayerController player = col.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.ApplyStun(playerStunDuration);
                }
            }
        }

        // 3. 播放 VFX（若有設定）
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
