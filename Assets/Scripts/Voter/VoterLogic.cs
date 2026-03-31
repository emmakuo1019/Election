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

    private static readonly int HashHit = Animator.StringToHash("hit");

    public event Action<int> OnPositionChanged;

    private VoterData data;
    private NavMeshAgent agent;
    private Animator anim;

    private bool isKnockedBack;
    private bool isFrozenByHitStop;
    private bool isGameActive = true;
    private Coroutine wanderCoroutine;

    void Awake()
    {
        data = GetComponent<VoterData>();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        agent.speed = moveSpeed;
        agent.angularSpeed = 0f;
        agent.updateRotation = false;
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

        Debug.Log("🛑 [VoterLogic] 遊戲結束，選民停止移動");
    }

    public void OnInfluence(int amount, bool isSkill, Vector3 attackerPosition = default)
    {
        if (!isGameActive) return;

        if (data.Tag == VoterTag.HatePolitics && !isSkill) return;

        int finalAmount = (data.Tag == VoterTag.DontKnow) ? amount * 2 : amount;

        data.currentPosition = Mathf.Clamp(
            data.currentPosition + finalAmount,
            VoterConfig.MIN_POS,
            VoterConfig.MAX_POS);

        UpdateConversionState();

        OnPositionChanged?.Invoke(data.currentPosition);
        anim?.SetTrigger(HashHit);

        if (!isKnockedBack && attackerPosition != default)
        {
            Vector3 dir = (transform.position - attackerPosition).normalized;
            dir.y = 0f;
            StartCoroutine(KnockbackRoutine(dir));
        }
    }

    private void UpdateConversionState()
    {
        if (Mathf.Abs(data.currentPosition) >= VoterConfig.MAX_POS)
        {
            int oldSide = data.convertedSide;

            data.isConverted = true;
            data.convertedSide = data.currentPosition > 0 ? 1 : -1;

            if (oldSide != data.convertedSide && oldSide == 0)
            {
                VoteManager.Instance?.AddVote(data.convertedSide);
            }
        }
        else
        {
            data.isConverted = false;
            data.convertedSide = 0;
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (isGameActive)
        {
            yield return new WaitForSeconds(Random.Range(wanderIntervalMin, wanderIntervalMax));

            if (!isGameActive) yield break;

            if (!isKnockedBack && !data.isConverted && agent.isOnNavMesh)
            {
                Vector3 dest = SampleRandomNavMeshPoint();
                if (dest != Vector3.zero)
                    agent.SetDestination(dest);
            }
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