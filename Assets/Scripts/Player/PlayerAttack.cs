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
    [Tooltip("敵人的圖層，供玩家攻擊與中斷")]
    public LayerMask enemyLayer;

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

        if (PolicyManager.HasInstance)
            PolicyManager.Instance.OnEffectsChanged += RefreshAttackStats;

        if (playerController != null)
            playerController.OnDirectionChanged += OnDirectionChanged;
    }

    void OnDisable()
    {
        if (PolicyManager.HasInstance)
            PolicyManager.Instance.OnEffectsChanged -= RefreshAttackStats;

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
    public Vector3 AttackDirection => GetAttackDirection();

    public void UpdateAttackShape(float range, float angle)
    {
        baseAttackRange = range;
        attackRange = range;
        attackAngle = angle;
        RefreshAttackStats();
    }

    private void RefreshAttackStats()
    {
        PolicyManager effects = PolicyManager.Instance;
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
        if (!SceneContext.IsLevelScene())
        {
            Debug.LogWarning("⚠️ 只能在關卡中進行攻擊！(SceneContext.IsLevelScene() 回傳 false)");
            return false;
        }
        
        if (GameDB.Instance == null)
        {
            Debug.LogWarning("⚠️ 找不到 GameDB，無法施放演說！");
            return false;
        }
        
        if (Time.time < lastAttackTime + currentAttackCooldown)
        {
            Debug.LogWarning("⚠️ 攻擊冷卻中！");
            return false;
        }

        return true;
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
            voterLayer | enemyLayer
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null) continue;

            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();

            Transform targetTransform = null;
            if (voter != null) targetTransform = voter.transform;
            else if (enemy != null) targetTransform = enemy.transform;
            else continue; // 既不是選民也不是敵人，略過

            // 計算向量與距離防呆
            Vector3 toTarget = targetTransform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            Vector3 dirToTarget = toTarget.normalized;

            // 判斷是否在攻擊扇形範圍內
            if (Vector3.Angle(attackDir, dirToTarget) < attackAngle / 2f)
            {
                // 如果是敵人，直接打斷並造成硬直
                if (enemy != null)
                {
                    enemy.TakeDamage(10, 1.0f);
                    hitAny = true;
                }
                // 如果是選民，則進行原本的拉票邏輯
                else if (voter != null)
                {
                    VoterData voterData = voter.Data;
                    
                    // 深色選民免疫玩家普攻
                    if (voterData != null && voterData.HasDarkAttribute)
                    {
                        continue;
                    }

                    voter.OnInfluence(attackInfluence, false, transform.position);
                    TryConvert(voterData);
                    hitAny = true;
                }
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

        PolicyManager effects = PolicyManager.Instance;
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
            int requiredInfluence = VoterConfig.MAX_POS - voter.CurrentPosition;
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
