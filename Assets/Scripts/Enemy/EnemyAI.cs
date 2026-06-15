using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour, IAttackSource
{
    [Header("移動")]
    public float moveSpeed = 4f;
    public float targetRefreshInterval = 0.2f;
    public float stoppingDistance = 1.8f;
    public float detectionRadius = 12f;

    [Header("攻擊設定")]
    public float attackRange = 3f;
    public float attackAngle = 60f;
    public int attackInfluence = 1;
    public float attackCooldown = 0.8f;

    [Header("陣營設定")]
    [Tooltip("敵方攻擊應為負值推進，預設 -1。")]
    public int ownSideSign = -1;

    [Header("顯示")]
    public Animator characterAnimator;
    public AttackRangeMesh attackRangeMesh;

    [Header("Layer")]
    public LayerMask voterLayer;

    [Header("搜尋快取")]
    [SerializeField] private int targetSearchBufferSize = 64;
    [SerializeField] private int attackHitBufferSize = 32;

    public event Action<float, float> OnAttackShapeChanged;

    private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
    private static readonly int HashAttack = Animator.StringToHash("attack");

    private CharacterController characterController;
    private CinemachineImpulseSource impulseSource;
    private VoterLogic currentTarget;
    private float lastAttackTime;
    private Vector3 lastMoveDirection = Vector3.forward;
    private bool isGameActive = true;
    private bool isStunned;
    private Coroutine targetingCoroutine;
    private Coroutine stunCoroutine;
    private Collider[] targetSearchBuffer;
    private Collider[] attackHitBuffer;

    public float AttackRange => attackRange;
    public float AttackAngle => attackAngle;
    public Vector3 AttackDirection => transform.forward;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        targetSearchBuffer = new Collider[Mathf.Max(8, targetSearchBufferSize)];
        attackHitBuffer = new Collider[Mathf.Max(8, attackHitBufferSize)];
    }

    private void OnEnable()
    {
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd += OnGameEnd;
        }
    }

    private void OnDisable()
    {
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd -= OnGameEnd;
        }
    }

    private void Start()
    {
        OnAttackShapeChanged?.Invoke(attackRange, attackAngle);
        attackRangeMesh?.ShowIdle();

        targetingCoroutine = StartCoroutine(TargetingRoutine());
    }

    private void Update()
    {
        if (!isGameActive)
            return;

        if (isStunned)
        {
            characterAnimator?.SetBool(HashIsMoving, false);
            return;
        }

        if (currentTarget == null || !IsValidTarget(currentTarget))
        {
            characterAnimator?.SetBool(HashIsMoving, false);
            return;
        }

        Vector3 targetPos = currentTarget.transform.position;
        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            characterAnimator?.SetBool(HashIsMoving, false);
            return;
        }

        Vector3 moveDir = toTarget.normalized;
        lastMoveDirection = moveDir;

        if (attackRangeMesh != null)
            attackRangeMesh.transform.rotation = Quaternion.LookRotation(moveDir);

        float distance = toTarget.magnitude;
        float effectiveStoppingDistance = Mathf.Max(stoppingDistance, attackRange);
        bool inAttackDistance = distance <= effectiveStoppingDistance;

        if (!inAttackDistance)
        {
            characterController.Move(moveDir * moveSpeed * Time.deltaTime);
            characterAnimator?.SetBool(HashIsMoving, true);
        }
        else
        {
            characterAnimator?.SetBool(HashIsMoving, false);
            TryAttack();
        }
    }

    private void OnGameEnd()
    {
        isGameActive = false;
        isStunned = false;
        currentTarget = null;

        if (targetingCoroutine != null)
        {
            StopCoroutine(targetingCoroutine);
            targetingCoroutine = null;
        }

        characterAnimator?.SetBool(HashIsMoving, false);

    }

    public void ApplyStun(float duration)
    {
        if (!isGameActive || duration <= 0f)
            return;

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator TargetingRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(targetRefreshInterval);

        while (isGameActive)
        {
            if (ShouldRefreshTarget())
            {
                currentTarget = FindNearestEnemyVoter();
            }

            yield return wait;
        }
    }

    private bool ShouldRefreshTarget()
    {
        if (currentTarget == null)
        {
            return true;
        }

        if (!IsValidTarget(currentTarget))
        {
            return true;
        }

        float detectionRadiusSqr = detectionRadius * detectionRadius;
        return (currentTarget.transform.position - transform.position).sqrMagnitude > detectionRadiusSqr;
    }

    private VoterLogic FindNearestEnemyVoter()
    {
        float minSqrDist = float.MaxValue;
        VoterLogic nearest = null;
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, targetSearchBuffer, voterLayer);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = targetSearchBuffer[i];
            if (hit == null) continue;

            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            if (voter == null) continue;

            if (!IsValidTarget(voter))
                continue;

            float sqrDist = (voter.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest = voter;
            }
        }

        return nearest;
    }

    private bool IsValidTarget(VoterLogic voter)
    {
        if (voter == null) return false;

        VoterData data = voter.Data;
        if (data == null) return false;

        int attackSign = GetAttackSign();
        if (attackSign > 0)
        {
            return data.currentPosition < VoterConfig.MAX_POS;
        }

        if (attackSign < 0)
        {
            return data.currentPosition > VoterConfig.MIN_POS;
        }

        return false;
    }

    private void TryAttack()
    {
        if (!isGameActive) return;

        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;
        PerformAttack();
    }

    private void PerformAttack()
    {
        if (!isGameActive) return;

        characterAnimator?.SetTrigger(HashAttack);
        attackRangeMesh?.Show();

        Vector3 attackDir = lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection : transform.forward;
        bool hitAny = false;

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, attackRange, attackHitBuffer, voterLayer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = attackHitBuffer[i];
            if (hit == null) continue;

            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            if (voter == null) continue;

            VoterData voterData = voter.Data;
            if (voterData != null && voterData.HasDarkAttribute)
                continue;

            if (!IsValidTarget(voter))
                continue;

            Vector3 toTarget = hit.transform.position - transform.position;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget <= 0.0001f) continue;

            Vector3 dirToTarget = toTarget / distanceToTarget;

            if (Vector3.Angle(attackDir, dirToTarget) >= attackAngle / 2f)
                continue;

            int influence = Mathf.Abs(attackInfluence) * GetAttackSign();
            voter.OnInfluence(influence, false, transform.position);
            hitAny = true;
        }

        if (!hitAny) return;

        impulseSource?.GenerateImpulse();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    private int GetAttackSign()
    {
        return ownSideSign >= 0 ? VoterData.PlayerSideSign : VoterData.EnemySideSign;
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        currentTarget = null;
        characterAnimator?.SetBool(HashIsMoving, false);

        if (attackRangeMesh != null)
        {
            attackRangeMesh.Hide();
        }

        yield return new WaitForSeconds(duration);

        isStunned = false;
        stunCoroutine = null;
    }
}
