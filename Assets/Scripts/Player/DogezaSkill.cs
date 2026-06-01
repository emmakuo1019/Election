using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DogezaSkill", menuName = "Skills/Party/Dogeza Skill")]
public class DogezaSkill : PartySkillData
{
    [Header("特效")]
    [SerializeField] private GameObject skillEffectPrefab;
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

    public override bool CanExecute(GameObject caster, out string failureReason)
    {
        failureReason = string.Empty;

        if (caster == null)
        {
            failureReason = "[DogezaSkill] caster 為空，無法執行技能。";
            return false;
        }

        PlayerMPSystem mpSystem = PlayerMPSystem.Instance != null
            ? PlayerMPSystem.Instance
            : caster.GetComponent<PlayerMPSystem>();
        if (mpSystem == null)
        {
            failureReason = "[DogezaSkill] 找不到 PlayerMPSystem，無法確認 MP。";
            return false;
        }

        if (!mpSystem.HasEnoughMP(mpCost))
        {
            failureReason = "⚠️ MP 不足，無法施放政黨技能。";
            return false;
        }

        return true;
    }

    public override bool TryConsumeResources(GameObject caster, out string failureReason)
    {
        failureReason = string.Empty;

        PlayerMPSystem mpSystem = PlayerMPSystem.Instance != null
            ? PlayerMPSystem.Instance
            : caster != null ? caster.GetComponent<PlayerMPSystem>() : null;
        if (mpSystem == null)
        {
            failureReason = "[DogezaSkill] 找不到 PlayerMPSystem，無法扣除 MP。";
            return false;
        }

        if (!mpSystem.UseMP(mpCost))
        {
            failureReason = "[DogezaSkill] MP 不足，無法施放悲情土下座。";
            return false;
        }

        return true;
    }

    public override void Execute(GameObject caster)
    {
        if (caster == null)
        {
            Debug.LogWarning("[DogezaSkill] caster 為空，無法執行技能。");
            return;
        }

        PlaySkillAnimation(caster);

        PlaySkillEffect(caster);
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

    private void PlaySkillAnimation(GameObject caster)
    {
        Animator animator = caster.GetComponent<Animator>();
        if (animator == null)
        {
            animator = caster.GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning("[DogezaSkill] caster 身上找不到 Animator，略過技能動作播放。");
            return;
        }

        if (string.IsNullOrWhiteSpace(animationTriggerName))
        {
            Debug.LogWarning("[DogezaSkill] animationTriggerName 為空，無法播放技能動作。");
            return;
        }

        if (!HasTriggerParameter(animator, animationTriggerName))
        {
            Debug.LogWarning($"[DogezaSkill] Animator 缺少 Trigger 參數：{animationTriggerName}");
            return;
        }

        animator.ResetTrigger(animationTriggerName);
        animator.SetTrigger(animationTriggerName);
    }

    private static bool HasTriggerParameter(Animator animator, string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void PlaySkillEffect(GameObject caster)
    {
        if (skillEffectPrefab == null)
        {
            return;
        }

        Quaternion effectRotation = useCasterRotation ? caster.transform.rotation : Quaternion.identity;
        Vector3 effectPosition = caster.transform.TransformPoint(effectSpawnOffset);
        GameObject effectInstance = PoolManager.Instance.Get(skillEffectPrefab, effectPosition, effectRotation);
        if (effectInstance == null)
        {
            return;
        }

        FollowTargetWhileActive followTarget = effectInstance.GetComponent<FollowTargetWhileActive>();
        if (followTarget == null)
        {
            followTarget = effectInstance.AddComponent<FollowTargetWhileActive>();
        }

        followTarget.Bind(caster.transform, effectSpawnOffset, useCasterRotation);
    }

    private IEnumerator DogezaDashRoutine(GameObject caster)
    {
        CharacterController characterController = caster.GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning("[DogezaSkill] caster 身上缺少 CharacterController。");
            yield break;
        }

        PlayerController playerController = caster.GetComponent<PlayerController>();
        Vector3 dashDirection = caster.transform.forward;

        if (playerController != null && playerController.LastMoveDirection.sqrMagnitude > 0.01f)
        {
            dashDirection = playerController.LastMoveDirection.normalized;
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
