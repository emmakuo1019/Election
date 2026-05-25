using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DogezaSkill", menuName = "Skills/Party/Dogeza Skill")]
public class DogezaSkill : PartySkillData
{
    [SerializeField] private int mpCost = 10;
    [SerializeField] private int hpCost = 5;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float stunTime = 1.5f;
    [SerializeField] private float convertChance = 0.5f;
    [SerializeField] private float voterDetectionRadius = 1.5f;

    public override void Execute(GameObject caster)
    {
        if (caster == null)
        {
            Debug.LogWarning("[DogezaSkill] caster 為空，無法執行技能。");
            return;
        }

        PlayerMPSystem mpSystem = PlayerMPSystem.Instance != null
            ? PlayerMPSystem.Instance
            : caster.GetComponent<PlayerMPSystem>();
        if (mpSystem == null)
        {
            Debug.LogWarning("[DogezaSkill] 找不到 PlayerMPSystem，無法扣除 MP。");
            return;
        }

        if (!mpSystem.UseMP(mpCost))
        {
            Debug.LogWarning("[DogezaSkill] MP 不足，無法施放悲情土下座。");
            return;
        }

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
            Debug.Log($"[DogezaSkill] 悲情土下座成功影響選民，扣除誠信值 {hpCost}。");
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
            if (voterLogic.ReceiveDogeza(stunTime, convertChance))
            {
                hasHitVoter = true;
            }
        }
    }
}
