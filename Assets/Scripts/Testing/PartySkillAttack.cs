using System;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 政黨技能執行器：政策論述 & 煽動情緒
/// 由 PlayerSkillManager 控制，玩家只能選擇一個技能
/// </summary>
public class PartySkillAttack : MonoBehaviour, IAttackSource
{
    [Header("技能設定 - 政策論述")]
    [SerializeField] private float policyRange = 5f;
    [SerializeField] private float policyAngle = 90f;
    [SerializeField] private int policyInfluence = 2;
    [SerializeField] private float policyCooldown = 2f;

    [Header("技能設定 - 煽動情緒")]
    [SerializeField] private float emotionalRange = 8f;
    [SerializeField] private float emotionalAngle = 120f;
    [SerializeField] private int emotionalInfluence = 2;
    [SerializeField] private float emotionalCooldown = 3f;

    [Header("顯示")]
    [SerializeField] private AttackRangeMesh attackRangeMesh;

    [Header("Layer")]
    [SerializeField] private LayerMask voterLayer;

    private CinemachineImpulseSource impulseSource;
    private Animator characterAnimator;
    private PlayerController playerController;

    public event Action<float, float> OnAttackShapeChanged;

    private static readonly int HashPartyAttack = Animator.StringToHash("partyAttack");

    private PlayerSkillManager.PartySkillType currentSkill = PlayerSkillManager.PartySkillType.None;
    private float lastSkillTime = -999f;

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        characterAnimator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        if (attackRangeMesh != null)
            attackRangeMesh.ShowIdle();
    }

    public float AttackRange
    {
        get
        {
            return currentSkill switch
            {
                PlayerSkillManager.PartySkillType.PolicyDebate => policyRange,
                PlayerSkillManager.PartySkillType.EmotionalStirring => emotionalRange,
                _ => 0f
            };
        }
    }

    public float AttackAngle
    {
        get
        {
            return currentSkill switch
            {
                PlayerSkillManager.PartySkillType.PolicyDebate => policyAngle,
                PlayerSkillManager.PartySkillType.EmotionalStirring => emotionalAngle,
                _ => 0f
            };
        }
    }

    public void Initialize(PlayerSkillManager.PartySkillType skillType)
    {
        currentSkill = skillType;
        UpdateAttackShape();
        Debug.Log($"[PartySkill] 初始化技能: {skillType}");
    }

    public void PerformPartySkill()
    {
        if (LevelTimer.Instance != null && LevelTimer.Instance.IsTimeUp)
        {
            Debug.LogWarning("⏰ 時間已用完，無法使用技能！");
            return;
        }

        if (!SceneContext.IsLevelScene())
        {
            Debug.LogWarning("⚠️ 只能在關卡中使用政黨技能！");
            return;
        }
        
        if (!SceneContext.IsLevelScene())
        {
            Debug.LogWarning("⚠️ 只能在關卡中使用政黨技能！");
            return;
        }

        if (currentSkill == PlayerSkillManager.PartySkillType.None)
        {
            Debug.LogWarning("⚠️ 政黨技能未初始化");
            return;
        }

        float cooldown = currentSkill == PlayerSkillManager.PartySkillType.PolicyDebate
            ? policyCooldown
            : emotionalCooldown;

        if (Time.time < lastSkillTime + cooldown)
        {
            Debug.LogWarning($"⏳ 技能冷卻中... {(lastSkillTime + cooldown - Time.time):F1} 秒");
            return;
        }

        lastSkillTime = Time.time;

        switch (currentSkill)
        {
            case PlayerSkillManager.PartySkillType.PolicyDebate:
                PerformPolicyDebate();
                break;

            case PlayerSkillManager.PartySkillType.EmotionalStirring:
                PerformEmotionalStirring();
                break;
        }
    }

    private void PerformPolicyDebate()
    {
        Debug.Log("🗣️ [PartySkill] 執行: 政策論述");

        characterAnimator?.SetTrigger(HashPartyAttack);
        attackRangeMesh?.Show();

        Vector3 attackDir = GetAttackDirection();
        int hitCount = 0;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            policyRange,
            voterLayer
        );

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<VoterLogic>(out var voter))
            {
                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;

                if (Vector3.Angle(attackDir, dirToTarget) < policyAngle / 2f)
                {
                    voter.OnInfluence(policyInfluence, true, transform.position);
                    hitCount++;
                }
            }
        }

        if (hitCount > 0)
        {
            impulseSource?.GenerateImpulse();

            // 玩家自身立場（如果你有 CampaignManager）----------------------------------
            //if (CampaignManager.Instance != null)
            //{
              //  CampaignManager.Instance.AddRationalism(hitCount);
            //}

            // 全球社會風氣
            if (SocialAtmosphereManager.Instance != null)
            {
                SocialAtmosphereManager.Instance.OnSkillUsed(true, hitCount);
            }

            Debug.Log($"📊 政策論述命中 {hitCount} 名選民");
        }
    }

    private void PerformEmotionalStirring()
    {
        Debug.Log("[PartySkill] 執行: 煽動情緒");

        characterAnimator?.SetTrigger(HashPartyAttack);
        attackRangeMesh?.Show();

        Vector3 attackDir = GetAttackDirection();
        int hitCount = 0;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            emotionalRange,
            voterLayer
        );

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<VoterLogic>(out var voter))
            {
                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;

                if (Vector3.Angle(attackDir, dirToTarget) < emotionalAngle / 2f)
                {
                    voter.OnInfluence(emotionalInfluence, true, transform.position);
                    hitCount++;
                }
            }
        }

        if (hitCount > 0)
        {
            impulseSource?.GenerateImpulse();

            // 玩家自身立場（如果你有 CampaignManager）----------------------
            //if (CampaignManager.Instance != null)
            //{
              //  CampaignManager.Instance.AddEmotion(hitCount);
            //}

            // 全球社會風氣
            if (SocialAtmosphereManager.Instance != null)
            {
                SocialAtmosphereManager.Instance.OnSkillUsed(false, hitCount);
            }

            Debug.Log($"煽動情緒命中 {hitCount} 名選民");
        }
    }

    private Vector3 GetAttackDirection()
    {
        if (playerController != null && playerController.LastMoveDirection != Vector3.zero)
            return playerController.LastMoveDirection;

        return transform.forward;
    }

    private void UpdateAttackShape()
    {
        OnAttackShapeChanged?.Invoke(AttackRange, AttackAngle);
    }
}