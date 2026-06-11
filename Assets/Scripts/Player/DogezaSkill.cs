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

        // 2. 啟動衝刺與判定 Coroutine
        MonoBehaviour coroutineHost = caster.GetComponent<PlayerSkillManager>();
        if (coroutineHost == null)
        {
            coroutineHost = caster.GetComponent<MonoBehaviour>();
        }

        if (coroutineHost == null)
        {
            Debug.LogWarning("[DogezaSkill] caster 身上找不到可啟動 Coroutine 的 MonoBehaviour。");
            return;
        }

        coroutineHost.StartCoroutine(DogezaDashRoutine(caster));
    }

    private IEnumerator DogezaDashRoutine(GameObject caster)
    {
        CharacterController characterController = caster.GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning("[DogezaSkill] caster 身上缺少 CharacterController。");
            yield break;
        }

        PlayerController playerStateMachine = caster.GetComponent<PlayerController>();
        Vector3 dashDirection = caster.transform.forward;

        if (playerStateMachine != null && playerStateMachine.LastMoveDirection.sqrMagnitude > 0.01f)
        {
            dashDirection = playerStateMachine.LastMoveDirection.normalized;
        }
        else if (dashDirection.sqrMagnitude > 0.01f)
        {
            dashDirection = dashDirection.normalized;
        }
        else
        {
            yield break;
        }

        bool hasHitVoter = false;
        HashSet<VoterLogic> hitVoters = new HashSet<VoterLogic>();
        float elapsedTime = 0f;
        while (elapsedTime < dashDuration)
        {
            characterController.Move(dashDirection * dashSpeed * Time.deltaTime);
            DetectVotersDuringDash(caster, hitVoters, ref hasHitVoter);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (hasHitVoter && PolicyEffectRuntimeManager.HasInstance)
        {
            PolicyEffectRuntimeManager.Instance.AddIntegrityHp(-hpCost);
        }
    }

    private void DetectVotersDuringDash(GameObject caster, HashSet<VoterLogic> hitVoters, ref bool hasHitVoter)
    {
        Collider[] hitColliders = Physics.OverlapSphere(caster.transform.position, voterDetectionRadius);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            Collider hitCollider = hitColliders[i];
            if (hitCollider == null)
            {
                continue;
            }

            GameObject hitObject = hitCollider.attachedRigidbody != null
                ? hitCollider.attachedRigidbody.gameObject
                : hitCollider.gameObject;
            if (!hitObject.CompareTag("Voter"))
            {
                continue;
            }

            VoterLogic voterLogic = hitCollider.GetComponentInParent<VoterLogic>();
            if (voterLogic == null || hitVoters.Contains(voterLogic))
            {
                continue;
            }

            hitVoters.Add(voterLogic);
            if (voterLogic.ApplySkillEffect(new DogezaVoterEffect(stunTime, convertChance)))
            {
                hasHitVoter = true;
            }
        }
    }
}
