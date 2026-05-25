using System;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 技能執行器：暈眩對手 / 悲情土下座
/// </summary>
public class PartySkillAttack : MonoBehaviour, IAttackSource
{
    [Header("技能設定 - 暈眩對手")]
    [SerializeField] private float stunRange = 6f;
    [SerializeField] private float stunAngle = 120f;
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private float stunCooldown = 5f;

    [Header("技能設定 - 悲情土下座")]
    [SerializeField] private float dogezaDisplayRange = 1.5f;
    [SerializeField] private float dogezaDisplayAngle = 360f;
    [SerializeField] private DogezaSkill dogezaSkill;

    [Header("顯示")]
    [SerializeField] private AttackRangeMesh attackRangeMesh;

    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayer;

    private CinemachineImpulseSource impulseSource;
    private Animator characterAnimator;
    private PlayerController playerController;
    public event Action<float, float> OnAttackShapeChanged;

    private static readonly int HashPartyAttack = Animator.StringToHash("partyAttack");
    private const int HitBufferSize = 128;

    private PlayerSkillManager.PartySkillType currentSkill = PlayerSkillManager.PartySkillType.None;
    private float lastSkillTime = -999f;
    private readonly Collider[] hitBuffer = new Collider[HitBufferSize];

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        characterAnimator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();

        if (dogezaSkill == null)
        {
            dogezaSkill = ScriptableObject.CreateInstance<DogezaSkill>();
            dogezaSkill.skillName = "悲情土下座";
            dogezaSkill.baseCooldown = 5f;
            dogezaSkill.animationTriggerName = "partyAttack";
        }
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
                PlayerSkillManager.PartySkillType.PolicyDebate => stunRange,
                PlayerSkillManager.PartySkillType.Dogeza => dogezaDisplayRange,
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
                PlayerSkillManager.PartySkillType.PolicyDebate => stunAngle,
                PlayerSkillManager.PartySkillType.Dogeza => dogezaDisplayAngle,
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

        float cooldown = currentSkill switch
        {
            PlayerSkillManager.PartySkillType.PolicyDebate => stunCooldown,
            PlayerSkillManager.PartySkillType.Dogeza when dogezaSkill != null => dogezaSkill.baseCooldown,
            _ => 0f
        };

        if (Time.time < lastSkillTime + cooldown)
        {
            Debug.LogWarning($"⏳ 技能冷卻中... {(lastSkillTime + cooldown - Time.time):F1} 秒");
            return;
        }

        lastSkillTime = Time.time;

        switch (currentSkill)
        {
            case PlayerSkillManager.PartySkillType.PolicyDebate:
                PerformStunSkill();
                break;

            case PlayerSkillManager.PartySkillType.Dogeza:
                PerformDogezaSkill();
                break;
        }
    }

    private void PerformStunSkill()
    {
        Debug.Log("🛑 [PartySkill] 執行: 暈眩對手");

        characterAnimator?.SetTrigger(HashPartyAttack);
        attackRangeMesh?.Show();

        Vector3 attackDir = GetAttackDirection();
        int stunCount = 0;

        int overlapCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            stunRange,
            hitBuffer,
            enemyLayer
        );

        for (int i = 0; i < overlapCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null) continue;

            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;

                if (Vector3.Angle(attackDir, dirToTarget) < stunAngle / 2f)
                {
                    enemy.ApplyStun(stunDuration);
                    stunCount++;
                }
            }
        }

        if (stunCount > 0)
        {
            impulseSource?.GenerateImpulse();
            Debug.Log($"🛑 暈眩了 {stunCount} 名對手，持續 {stunDuration:F1} 秒");
        }
    }

    private void PerformDogezaSkill()
    {
        if (dogezaSkill == null)
        {
            Debug.LogWarning("⚠️ [PartySkill] DogezaSkill 未指定");
            return;
        }

        Debug.Log("🙇 [PartySkill] 執行: 悲情土下座");

        characterAnimator?.SetTrigger(HashPartyAttack);
        attackRangeMesh?.Show();
        dogezaSkill.Execute(gameObject);
        impulseSource?.GenerateImpulse();
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
