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
    [Tooltip("敵方視為自己的立場符號，1 代表正向(對應 opponentColor)，-1 代表負向(對應 playerColor)。")]
    public int ownSideSign = 1;
    public bool canAttackNeutral = true;

    [Header("顯示")]
    public Animator characterAnimator;
    public AttackRangeMesh attackRangeMesh;

    [Header("Layer")]
    public LayerMask voterLayer;

    public event Action<float, float> OnAttackShapeChanged;

    private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
    private static readonly int HashAttack = Animator.StringToHash("attack");

    private CharacterController characterController;
    private CinemachineImpulseSource impulseSource;
    private VoterLogic currentTarget;
    private bool isFrozenByHitStop;
    private float lastAttackTime;
    private Vector3 lastMoveDirection = Vector3.forward;

    public float AttackRange => attackRange;
    public float AttackAngle => attackAngle;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Start()
    {
        OnAttackShapeChanged?.Invoke(attackRange, attackAngle);
        attackRangeMesh?.ShowIdle();
        StartCoroutine(TargetingRoutine());
    }

    private void Update()
    {
        if (isFrozenByHitStop)
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
        //transform.rotation = Quaternion.LookRotation(moveDir);

        if (attackRangeMesh != null)
            attackRangeMesh.transform.rotation = Quaternion.LookRotation(moveDir);

        float distance = toTarget.magnitude;
        // 攻擊判定用的是 attackRange，所以「進入攻擊」的距離至少要不小於 attackRange。
        // 否則很容易出現：已經靠近到可命中範圍，但 distance 還沒 <= stoppingDistance，導致沒有播動畫/沒有施加影響。
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

    private IEnumerator TargetingRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(targetRefreshInterval);

        while (true)
        {
            if (!isFrozenByHitStop)
                currentTarget = FindNearestEnemyVoter();

            yield return wait;
        }
    }

    private VoterLogic FindNearestEnemyVoter()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, voterLayer);
        float minSqrDist = float.MaxValue;
        VoterLogic nearest = null;

        foreach (var hit in hits)
        {
            // Collider 可能在子物件上，而 VoterLogic 在父物件（常見於 prefab）。
            // 用 GetComponentInParent 讓目標抓取更穩定，避免 currentTarget 永遠為 null。
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

        VoterData data = voter.GetComponent<VoterData>();
        if (data == null) return false;

        // ⭐ 改成「只要還沒到我方極值，就可以打」
        if (ownSideSign > 0)
        {
            return data.currentPosition < VoterConfig.MAX_POS;
        }
        else
        {
            return data.currentPosition > VoterConfig.MIN_POS;
        }
    }
    private void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;
        PerformAttack();
    }

    private void PerformAttack()
    {
        characterAnimator?.SetTrigger(HashAttack);
        attackRangeMesh?.Show();

        Vector3 attackDir = lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection : transform.forward;
        bool hitAny = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, voterLayer);
        foreach (var hit in hits)
        {
            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            if (voter == null) continue;

            if (!IsValidTarget(voter))
                continue;

            Vector3 toTarget = hit.transform.position - transform.position;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget <= 0.0001f) continue;

            Vector3 dirToTarget = toTarget / distanceToTarget;

            if (Vector3.Angle(attackDir, dirToTarget) >= attackAngle / 2f)
                continue;

            // 影響方向決定於敵人自身立場：
            // ownSideSign = 1 代表推向正向(對應 opponentColor)
            // ownSideSign = -1 代表推向負向(對應 playerColor)
            int sideSign = ownSideSign >= 0 ? 1 : -1;
            int influence = Mathf.Abs(attackInfluence) * sideSign;
            voter.OnInfluence(influence, false, transform.position);
            hitAny = true;
        }

        if (!hitAny) return;

        impulseSource?.GenerateImpulse();
    }

    private void OnHitStopChanged(bool frozen)
    {
        isFrozenByHitStop = frozen;
        if (frozen)
            characterAnimator?.SetBool(HashIsMoving, false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
