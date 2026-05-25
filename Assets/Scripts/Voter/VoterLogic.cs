using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NavMeshAgent))]
public class VoterLogic : MonoBehaviour
{
    private const float ExitArrivalThreshold = 0.05f;
    private const float NavMeshSnapDistance = 3f;

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
    [SerializeField] private float hitAnimationTriggerCooldown = 0.08f;

    [Header("倒數結束離場")]
    [SerializeField] private bool moveToExitWhenBattleEnds = true;
    [SerializeField] private float exitMoveSpeedMultiplier = 1f;

    private static readonly int HashHit = Animator.StringToHash("hit");

    public event Action<int> OnPositionChanged;

    private VoterData data;
    private NavMeshAgent agent;
    private Animator anim;

    private bool isKnockedBack;
    private bool isFrozenByHitStop;
    private bool isGameActive = true;
    private bool isExitingAfterBattle;
    private Coroutine wanderCoroutine;
    private Coroutine dogezaStunCoroutine;
    private Transform playerTransform;
    private float nextDarkRefreshTime;
    private float lastHitAnimationTriggerTime = float.NegativeInfinity;
    private Vector3 exitDestination;
    private readonly Collider[] spreadHitBuffer = new Collider[32];
    private bool hasUsableNavMeshAgent = true;

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
            hasUsableNavMeshAgent = TryInitializeNavMeshAgent();
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
        if (hasUsableNavMeshAgent)
        {
            wanderCoroutine = StartCoroutine(WanderRoutine());
        }
    }

    private void Update()
    {
        if (isExitingAfterBattle)
        {
            UpdateExitMovement();
            return;
        }

        if (!isGameActive || data == null || agent == null || !hasUsableNavMeshAgent || !agent.isOnNavMesh)
        {
            return;
        }

        if (isFrozenByHitStop)
        {
            agent.isStopped = true;
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

    private bool TryInitializeNavMeshAgent()
    {
        if (agent == null)
        {
            return false;
        }

        if (agent.isOnNavMesh)
        {
            return true;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSnapDistance, NavMesh.AllAreas))
        {
            bool previousEnabled = agent.enabled;
            agent.enabled = false;
            transform.position = hit.position;
            agent.enabled = previousEnabled;

            if (agent.isOnNavMesh)
            {
                return true;
            }
        }

        Debug.LogWarning($"[VoterLogic] {name} 找不到可用 NavMesh，已停用 NavMeshAgent。");
        agent.enabled = false;
        return false;
    }

    public bool ReceiveDogeza(float stunTime, float convertChance)
    {
        if (!isGameActive || data == null)
        {
            Debug.LogWarning("[VoterLogic] 無法處理悲情土下座，選民未處於可互動狀態。");
            return false;
        }

        if (data.HasColdAttribute)
        {
            data.ConvertColdIdentityToEmotion();
            ApplyDogezaStun(stunTime);
            Debug.Log($"[VoterLogic] {name} 受到悲情土下座影響：冷感 -> 情緒，暈眩 {stunTime:F2} 秒。");
            return true;
        }

        if (data.EmotionLabelCount <= 0)
        {
            Debug.Log($"[VoterLogic] {name} 不受悲情土下座影響。");
            return false;
        }

        if (Random.value < Mathf.Clamp01(convertChance))
        {
            ForceConvertToPlayer();
            Debug.Log($"[VoterLogic] {name} 受到悲情土下座影響：情緒選民轉化成功。");
            return true;
        }

        Debug.Log($"[VoterLogic] {name} 受到悲情土下座影響：情緒選民轉化失敗。");
        return false;
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

        if (!moveToExitWhenBattleEnds)
        {
            return;
        }

        RoomExitController exitController = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        if (exitController == null)
        {
            return;
        }

        BeginExitMovement(exitController.GetVoterExitPosition());
    }

    public void BeginExitMovement(Vector3 destination)
    {
        if (data == null)
        {
            return;
        }

        StopAllCoroutines();
        wanderCoroutine = null;
        isKnockedBack = false;
        isGameActive = false;
        exitDestination = destination;
        exitDestination.y = transform.position.y;
        isExitingAfterBattle = true;

        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }
    }

    public void OnInfluence(int amount, bool isSkill, Vector3 attackerPosition = default, bool allowSpread = true)
    {
        if (!isGameActive) return;
        if (data == null) return;

        int finalAmount = amount * Mathf.Max(1, 1 + data.EmotionLabelCount);

        data.currentPosition = Mathf.Clamp(
            data.currentPosition + finalAmount,
            VoterConfig.MIN_POS,
            VoterConfig.MAX_POS);

        UpdateConversionState(allowSpread);

        OnPositionChanged?.Invoke(data.currentPosition);
        TriggerHitAnimation();

        if (hasUsableNavMeshAgent && !isKnockedBack && attackerPosition != default)
        {
            Vector3 dir = (transform.position - attackerPosition).normalized;
            dir.y = 0f;
            StartCoroutine(KnockbackRoutine(dir));
        }
    }

    private void TriggerHitAnimation()
    {
        if (anim == null)
        {
            return;
        }

        if (Time.time < lastHitAnimationTriggerTime + hitAnimationTriggerCooldown)
        {
            return;
        }

        lastHitAnimationTriggerTime = Time.time;
        anim.ResetTrigger(HashHit);
        anim.SetTrigger(HashHit);
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

            if (!isKnockedBack && !isFrozenByHitStop && !data.ShouldFollowPlayer && agent.isOnNavMesh)
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

    private void UpdateExitMovement()
    {
        float speed = Mathf.Max(0.01f, (data != null ? data.MoveSpeed : moveSpeed) * exitMoveSpeedMultiplier);
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, exitDestination, speed * Time.deltaTime);
        Vector3 moveDelta = nextPosition - transform.position;

        if (moveDelta.sqrMagnitude > 0.000001f)
        {
            transform.position = nextPosition;
        }

        if ((exitDestination - transform.position).sqrMagnitude <= ExitArrivalThreshold * ExitArrivalThreshold)
        {
            transform.position = exitDestination;
            isExitingAfterBattle = false;
            enabled = false;
        }
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
        if (!isGameActive || agent == null || !hasUsableNavMeshAgent) yield break;

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

    private void ForceConvertToPlayer()
    {
        int requiredInfluence = VoterConfig.MAX_POS - data.currentPosition;
        if (requiredInfluence <= 0)
        {
            return;
        }

        OnInfluence(requiredInfluence, true, transform.position);
    }

    private void ApplyDogezaStun(float stunTime)
    {
        if (stunTime <= 0f)
        {
            return;
        }

        if (dogezaStunCoroutine != null)
        {
            StopCoroutine(dogezaStunCoroutine);
        }

        dogezaStunCoroutine = StartCoroutine(DogezaStunRoutine(stunTime));
    }

    private IEnumerator DogezaStunRoutine(float stunTime)
    {
        isFrozenByHitStop = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        yield return new WaitForSeconds(stunTime);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        isFrozenByHitStop = false;
        dogezaStunCoroutine = null;
    }
}
