using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Boss召喚小怪技能
/// 在Boss周圍隨機位置生成2-3個小怪
/// Elite: 30秒CD, 召喚2個
/// FinalBoss: 20秒CD, 召喚3個
/// </summary>
[CreateAssetMenu(fileName = "EnemySkill_Summon", menuName = "Skills/Enemy/Summon Minions")]
public class EnemySkill_Summon : EnemySkillData
{
    [Header("召喚設定")]
    [Tooltip("要召喚的小怪Prefab")]
    public GameObject minionPrefab;
    
    [Tooltip("召喚數量")]
    public int summonCount = 3;
    
    [Tooltip("生成半徑（Boss周圍多少米）")]
    public float spawnRadius = 5f;

    public override void ExecuteSkill(GameObject caster)
    {
        if (minionPrefab == null)
        {
            Debug.LogError("[EnemySkill_Summon] minionPrefab 未設定！請在 ScriptableObject 中指定小怪 Prefab。");
            return;
        }

        int successfulSpawns = 0;
        
        for (int i = 0; i < summonCount; i++)
        {
            // 在Boss周圍隨機位置生成
            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            randomOffset.y = 0; // 保持在地面
            Vector3 spawnPos = caster.transform.position + randomOffset;

            // 確保生成在有效的NavMesh上
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                GameObject minion = Instantiate(minionPrefab, hit.position, Quaternion.identity);
                successfulSpawns++;
            }
            else
            {
                Debug.LogWarning($"[EnemySkill_Summon] 位置 {spawnPos} 附近找不到有效 NavMesh，跳過此次生成");
            }
        }

        // 播放召喚特效
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
