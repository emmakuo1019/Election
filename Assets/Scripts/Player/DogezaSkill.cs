using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DogezaSkill", menuName = "Skills/Party/Dogeza Skill")]
public class DogezaSkill : SkillData
{
    [Header("土下座特殊特效參數")]
    [SerializeField] private Vector3 effectSpawnOffset;
    [SerializeField] private bool useCasterRotation = true;

    [Header("技能數值")]
    [SerializeField] private int mpCost = 10;
    [SerializeField] private int hpCost = 5;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float stunTime = 1.5f;
    [SerializeField] private float convertChance = 0.5f;
    [SerializeField] private float voterDetectionRadius = 1.5f;

    public int MpCost => mpCost;
    public float DashDuration => dashDuration;
    public float DashSpeed => dashSpeed;
    public float StunTime => stunTime;
    public float ConvertChance => convertChance;
    public float VoterDetectionRadius => voterDetectionRadius;
    public int HpCost => hpCost;
    public override void ExecuteSkill(GameObject caster)
    {
        if (caster == null)
        {
            Debug.LogWarning("[DogezaSkill] caster 為空，無法執行技能。");
            return;
        }

        // 資源檢查與消耗 (取代了舊的 CanExecute / TryConsumeResources)
        PlayerMPSystem mpSystem = PlayerMPSystem.Instance != null
            ? PlayerMPSystem.Instance
            : caster.GetComponent<PlayerMPSystem>();
            
        if (mpSystem != null && !mpSystem.UseMP(mpCost))
        {
            Debug.LogWarning("[DogezaSkill] MP 不足，無法施放悲情土下座。");
            return;
        }

        // 自訂的特效生成邏輯 (取代 base.ExecuteSkill)
        if (skillEffectPrefab != null)
        {
            Quaternion effectRotation = useCasterRotation ? caster.transform.rotation : Quaternion.identity;
            Vector3 effectPosition = caster.transform.TransformPoint(effectSpawnOffset);
            GameObject effectInstance = PoolManager.Instance.Get(skillEffectPrefab, effectPosition, effectRotation);
            
            if (effectInstance != null)
            {
                FollowTargetWhileActive followTarget = effectInstance.GetComponent<FollowTargetWhileActive>();
                if (followTarget == null)
                {
                    followTarget = effectInstance.AddComponent<FollowTargetWhileActive>();
                }
                followTarget.Bind(caster.transform, effectSpawnOffset, useCasterRotation);
            }
        }

        // 2. 啟動衝刺與判定 Coroutine (已移除，改由 SkillState.PhysicsUpdate 驅動)
    }
}
