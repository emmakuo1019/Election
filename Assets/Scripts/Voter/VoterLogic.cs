using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NavMeshAgent))]
public class VoterLogic : MonoBehaviour
{
    [Header("隨機移動")]
    public float wanderRadius = 6f;
    public float wanderIntervalMin = 2f;
    public float wanderIntervalMax = 5f;
    public float moveSpeed = 1.2f;

    [Header("擊退設定")]
    public float knockbackDistance = 1.2f;
    public float knockbackDuration = 0.25f;

    [Header("深色選民追擊")]
    [SerializeField] private float darkApproachStoppingDistance = 1.5f;
    [SerializeField] private float darkDestinationRefreshInterval = 0.25f;

    private static readonly int HashHit = Animator.StringToHash("hit");

    public event Action<int> OnPositionChanged;

    private VoterData data;
    private NavMeshAgent agent;
    private Animator anim;

    private bool isKnockedBack;
    private bool isFrozenByHitStop;
    private bool isGameActive = true;
    private Coroutine wanderCoroutine;
    private Transform playerTransform;
    private float nextDarkRefreshTime;
    private readonly Collider[] spreadHitBuffer = new Collider[32];

    public VoterData Data => data;

    void Awake()
    {
        data = GetComponent<VoterData>();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        playerTransform = playerObject != null ? playerObject.transform : null;

        data?.InitializeFromConfig();

        if (agent != null)
        {
            agent.speed = data != null ? data.MoveSpeed : moveSpeed;
            agent.angularSpeed = 0f;
            agent.updateRotation = false;
        }
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

    void Start()
    {
        wanderCoroutine = StartCoroutine(WanderRoutine());
    }

    private void Update()
    {
        if (!isGameActive || data == null || agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        UpdateLoyaltyDecay();

        if (!data.ShouldFollowPlayer || isKnockedBack)
        {
            return;
        }

        UpdateDarkApproach();
    }

    public void RefreshMovementSpeed()
    {
        if (agent != null)
        {
            agent.speed = data != null ? data.MoveSpeed : moveSpeed;
        }
    }

    private void OnGameEnd()
    {
        isGameActive = false;

        if (wanderCoroutine != null)
        {
            StopCoroutine(wanderCoroutine);
            wanderCoroutine = null;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        //Debug.Log("🛑 [VoterLogic] 遊戲結束，選民停止移動");
    }

    public void OnInfluence(int amount, bool isSkill, Vector3 attackerPosition = default, bool allowSpread = true)
    {
        if (!isGameActive) return;

        if (data.Tag == VoterTag.cold && !isSkill) return;

        int finalAmount = (data.Tag == VoterTag.emotion) ? amount * 2 : amount;

        data.currentPosition = Mathf.Clamp(
            data.currentPosition + finalAmount,
            VoterConfig.MIN_POS,
            VoterConfig.MAX_POS);

        UpdateConversionState(allowSpread);

        OnPositionChanged?.Invoke(data.currentPosition);
        anim?.SetTrigger(HashHit);

        if (!isKnockedBack && attackerPosition != default)
        {
            Vector3 dir = (transform.position - attackerPosition).normalized;
            dir.y = 0f;
            StartCoroutine(KnockbackRoutine(dir));
        }
    }

    private void UpdateConversionState(bool allowSpread)
    {
        int oldSide = data.convertedSide;
        int newSide = data.EvaluateSideFromPosition();

        data.convertedSide = newSide;
        data.isConverted = newSide != VoterData.NeutralSideSign;

        if (oldSide != newSide)
        {
            data.loyalty = 1f;
        }

        if (oldSide != newSide)
        {
            VoteManager.Instance?.ApplyAlignmentChange(oldSide, newSide);

            if (newSide == VoterData.PlayerSideSign && oldSide != VoterData.PlayerSideSign)
            {
                PlayerMPSystem.Instance?.RecoverMP(1);
                Debug.Log("🗳️ 玩家成功轉化選民，回復 1 MP");

                if (allowSpread)
                {
                    SpreadInfluenceToNearbyVoters();
                }
            }
        }
    }

    public void RevertToNeutral()
    {
        int oldSide = data.convertedSide;
        data.currentPosition = 0;
        data.convertedSide = VoterData.NeutralSideSign;
        data.isConverted = false;
        data.loyalty = 1f;

        VoteManager.Instance?.ApplyAlignmentChange(oldSide, VoterData.NeutralSideSign);
        OnPositionChanged?.Invoke(data.currentPosition);

        Debug.Log("↩️ [VoterLogic] 選民忠誠耗盡，回到中立");
    }

    private void UpdateLoyaltyDecay()
    {
        PolicyEffectRuntimeManager effects = PolicyEffectRuntimeManager.Instance;
        if (effects == null || !data.isConverted || effects.LoseControlRate <= 0f)
        {
            return;
        }

        data.loyalty = Mathf.Clamp01(data.loyalty - effects.LoseControlRate * Time.deltaTime);

        if (data.loyalty <= 0f)
        {
            RevertToNeutral();
        }
    }

    private void SpreadInfluenceToNearbyVoters()
    {
        PolicyEffectRuntimeManager effects = PolicyEffectRuntimeManager.Instance;
        if (effects == null || effects.SpreadRadius <= 0f)
        {
            return;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, effects.SpreadRadius, spreadHitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = spreadHitBuffer[i];
            if (hit == null)
            {
                continue;
            }

            VoterLogic nearbyVoter = hit.GetComponentInParent<VoterLogic>();
            if (nearbyVoter == null || nearbyVoter == this || nearbyVoter.Data == null)
            {
                continue;
            }

            if (nearbyVoter.Data.IsPlayerAligned)
            {
                continue;
            }

            nearbyVoter.OnInfluence(1, true, transform.position, false);
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (isGameActive)
        {
            yield return new WaitForSeconds(Random.Range(wanderIntervalMin, wanderIntervalMax));

            if (!isGameActive) yield break;

            if (!isKnockedBack && !data.ShouldFollowPlayer && agent.isOnNavMesh)
            {
                Vector3 dest = SampleRandomNavMeshPoint();
                if (dest != Vector3.zero)
                    agent.SetDestination(dest);
            }
        }
    }

    private void UpdateDarkApproach()
    {
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            playerTransform = playerObject != null ? playerObject.transform : null;
        }

        if (playerTransform == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= darkApproachStoppingDistance)
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
            }

            return;
        }

        if (Time.time < nextDarkRefreshTime)
        {
            return;
        }

        nextDarkRefreshTime = Time.time + darkDestinationRefreshInterval;
        agent.SetDestination(playerTransform.position);
    }

    private Vector3 SampleRandomNavMeshPoint()
    {
        Vector3 candidate = transform.position + new Vector3(
            Random.Range(-wanderRadius, wanderRadius),
            0f,
            Random.Range(-wanderRadius, wanderRadius));

        return NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas)
            ? hit.position
            : Vector3.zero;
    }

    private IEnumerator KnockbackRoutine(Vector3 direction)
    {
        if (!isGameActive) yield break;

        isKnockedBack = true;
        if (agent.isOnNavMesh) agent.isStopped = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction * knockbackDistance;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, knockbackDistance, NavMesh.AllAreas))
            targetPos = hit.position;

        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            if (!isGameActive) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;
            agent.Warp(Vector3.Lerp(startPos, targetPos, t));
            yield return null;
        }

        if (agent.isOnNavMesh)
            agent.isStopped = false;

        isKnockedBack = false;
    }
}
