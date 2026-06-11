using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour, IAttackSource
{
    [Header("攻擊設定")]
    public float attackRange = 3f;
    public float attackAngle = 60f;
    public int attackInfluence = 1;
    [SerializeField] private float attackCooldown = 0f;
    [SerializeField] private float convertChance = 0.3f;
    [SerializeField] private float darkVoterConvertChance = 0.8f;

    [Header("顯示")]
    public AttackRangeMesh attackRangeMesh;
    private CinemachineImpulseSource impulseSource;
    private PlayerController playerController;

    public event Action<float, float> OnAttackShapeChanged;
    public event Action OnAttackPerformed;

    private const int HitBufferSize = 64;
    private readonly Collider[] hitBuffer = new Collider[HitBufferSize];
    private float lastAttackTime = -999f;
    private float baseAttackRange;
    private float currentAttackRange;
    private float currentAttackCooldown;
    private float temporaryAttackRangeMultiplier = 1f;
    private Coroutine rangeBoostCoroutine;

    [Header("Layer")]
    public LayerMask voterLayer;

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        
        playerController = GetComponent<PlayerController>();
        baseAttackRange = attackRange;
        currentAttackRange = attackRange;
        currentAttackCooldown = attackCooldown;
    }

    void OnEnable()
    {
        RefreshAttackStats();
        SyncAttackRangeMeshRotation();

        if (PolicyEffectRuntimeManager.HasInstance)
            PolicyEffectRuntimeManager.Instance.OnEffectsChanged += RefreshAttackStats;

        if (playerController != null)
            playerController.OnDirectionChanged += OnDirectionChanged;
    }

    void OnDisable()
    {
        if (PolicyEffectRuntimeManager.HasInstance)
            PolicyEffectRuntimeManager.Instance.OnEffectsChanged -= RefreshAttackStats;

        if (playerController != null)
            playerController.OnDirectionChanged -= OnDirectionChanged;
    }

    void Start()
    {
        SyncAttackRangeMeshRotation();
        if (attackRangeMesh != null)
            attackRangeMesh.ShowIdle();
    }

    public float AttackRange => attackRange;
    public float AttackAngle => attackAngle;

    public void UpdateAttackShape(float range, float angle)
    {
        baseAttackRange = range;
        attackRange = range;
        attackAngle = angle;
        RefreshAttackStats();
    }

    private void RefreshAttackStats()
    {
        PolicyEffectRuntimeManager effects = PolicyEffectRuntimeManager.Instance;
        float policyAdjustedRange = effects != null ? effects.GetModifiedAttackRange(baseAttackRange) : baseAttackRange;
        currentAttackRange = policyAdjustedRange * temporaryAttackRangeMultiplier;
        currentAttackCooldown = effects != null ? effects.GetModifiedAttackCooldown(attackCooldown) : attackCooldown;
        attackRange = currentAttackRange;
        OnAttackShapeChanged?.Invoke(currentAttackRange, attackAngle);
    }

    public void ApplyTemporaryRangeBoost(float multiplier, float duration)
    {
        if (multiplier <= 1f || duration <= 0f)
        {
            return;
        }

        if (rangeBoostCoroutine != null)
        {
            StopCoroutine(rangeBoostCoroutine);
        }

        rangeBoostCoroutine = StartCoroutine(TemporaryRangeBoostRoutine(multiplier, duration));
    }

    /// <summary>
    /// 檢查攻擊是否在冷卻中
    /// </summary>
    public bool CanAttack()
    {
        if (!SceneContext.IsLevelScene()) return false;
        if (PlayerMPSystem.Instance == null) return false;
        
        return Time.time >= lastAttackTime + currentAttackCooldown;
    }

    /// <summary>
    /// 執行物理攻擊判定與數值處理
    /// </summary>
    public void PerformAttack(Vector3 facingDirection)
    {
        if (!CanAttack()) return;

        lastAttackTime = Time.time;

        OnAttackPerformed?.Invoke();
        attackRangeMesh?.Show();

        Vector3 attackDir = facingDirection.sqrMagnitude > 0.001f ? facingDirection.normalized : transform.forward;
        SyncAttackRangeMeshRotation(attackDir);
        bool hitAny = false;

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            currentAttackRange,
            hitBuffer,
            voterLayer
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null) continue;

            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            if (voter == null)
            {
                continue;
            }

            VoterData voterData = voter.Data;
            if (voterData != null && voterData.HasDarkAttribute)
            {
                continue;
            }

            Vector3 toTarget = voter.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            Vector3 dirToTarget = toTarget.normalized;

            if (Vector3.Angle(attackDir, dirToTarget) < attackAngle / 2f)
            {
                voter.OnInfluence(attackInfluence, false, transform.position);
                TryConvert(voterData);
                hitAny = true;
            }
        }

        if (hitAny)
        {
            impulseSource?.GenerateImpulse();
        }
    }

    private Vector3 GetAttackDirection()
    {
        if (playerController != null && playerController.LastMoveDirection.sqrMagnitude > 0.001f)
            return playerController.LastMoveDirection.normalized;

        return transform.forward;
    }

    private void OnDirectionChanged(Vector3 direction)
    {
        SyncAttackRangeMeshRotation(direction);
    }

    private void SyncAttackRangeMeshRotation()
    {
        SyncAttackRangeMeshRotation(GetAttackDirection());
    }

    private void SyncAttackRangeMeshRotation(Vector3 direction)
    {
        if (attackRangeMesh == null || direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        attackRangeMesh.transform.rotation = Quaternion.LookRotation(direction.normalized);
    }
    
    public void TryConvert(VoterData voter)
    {
        if (voter == null)
            return;

        PolicyEffectRuntimeManager effects = PolicyEffectRuntimeManager.Instance;
        float chance = effects != null
            ? effects.GetModifiedConvertChance(convertChance)
            : convertChance;

        if (voter.HasDarkAttribute)
            chance = effects != null
                ? effects.GetModifiedConvertChance(darkVoterConvertChance)
                : darkVoterConvertChance;

        if (UnityEngine.Random.value < chance)
        {
            ForceConvert(voter);
        }
    }

    private void ForceConvert(VoterData voter)
    {
        if (voter.TryGetComponent<VoterLogic>(out var logic))
        {
            int requiredInfluence = VoterConfig.MAX_POS - voter.currentPosition;
            if (requiredInfluence > 0)
            {
                logic.OnInfluence(requiredInfluence, true, transform.position);
            }
        }
    }

    private IEnumerator TemporaryRangeBoostRoutine(float multiplier, float duration)
    {
        temporaryAttackRangeMultiplier = multiplier;
        RefreshAttackStats();

        yield return new WaitForSeconds(duration);

        temporaryAttackRangeMultiplier = 1f;
        RefreshAttackStats();
        rangeBoostCoroutine = null;
    }
}
