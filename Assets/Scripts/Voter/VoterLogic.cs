using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NavMeshAgent))]
public class VoterLogic : MonoBehaviour
{
    [Header("隨機移動")]
    public float wanderRadius      = 6f;
    public float wanderIntervalMin = 2f;
    public float wanderIntervalMax = 5f;
    public float moveSpeed         = 1.2f;

    [Header("擊退設定")]
    public float knockbackDistance = 1.2f;
    public float knockbackDuration = 0.25f;
    private static readonly int HashHit = Animator.StringToHash("hit");


    // --- Events ---
    //立場值改變時發出
    public event Action<int> OnPositionChanged;

    //選民被成功轉化時發出，帶入轉化台詞
    public event Action<string> OnConverted;

    private VoterData    data;
    private NavMeshAgent agent;
    private bool         isKnockedBack;
    private bool         IsOnNavMesh => agent.isOnNavMesh;

    void Awake()
    {
        data  = GetComponent<VoterData>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed         = moveSpeed;
        agent.angularSpeed  = 0f;
        agent.updateRotation = false;
    }

    void Start()
    {
        StartCoroutine(WanderRoutine());
    }

    /// <summary>
    /// 施加影響力，並根據攻擊者位置觸發擊退效果。
    /// </summary>
    public void OnInfluence(int amount, bool isSkill, Vector3 attackerPosition = default)
    {
        if (data.isConverted) return;
        if (data.Tag == VoterTag.HatePolitics && !isSkill) return;

        int finalAmount = (data.Tag == VoterTag.DontKnow) ? amount * 2 : amount;

        data.currentPosition = Mathf.Clamp(
            data.currentPosition + finalAmount,
            VoterConfig.MIN_POS,
            VoterConfig.MAX_POS);

        OnPositionChanged?.Invoke(data.currentPosition);
        GetComponentInChildren<Animator>()?.SetTrigger(HashHit);


        if (!isKnockedBack && attackerPosition != default)
        {
            Vector3 dir = (transform.position - attackerPosition).normalized;
            dir.y = 0f;
            StartCoroutine(KnockbackRoutine(dir));
        }

        if (data.currentPosition <= VoterConfig.MIN_POS)
            ExecuteConversion();
    }

    // --- 隨機遊蕩 ---

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(wanderIntervalMin, wanderIntervalMax));

            if (!isKnockedBack && !data.isConverted && IsOnNavMesh)
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

    // --- 擊退 ---

    private IEnumerator KnockbackRoutine(Vector3 direction)
    {
        isKnockedBack = true;
        if (IsOnNavMesh) agent.isStopped = true;

        Vector3 startPos  = transform.position;
        Vector3 targetPos = startPos + direction * knockbackDistance;

        if (!NavMesh.SamplePosition(targetPos, out NavMeshHit hit, knockbackDistance, NavMesh.AllAreas))
            targetPos = startPos;
        else
            targetPos = hit.position;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float eased = 1f - Mathf.Pow(1f - elapsed / knockbackDuration, 3f);
            agent.Warp(Vector3.Lerp(startPos, targetPos, eased));
            yield return null;
        }

        if (IsOnNavMesh) agent.isStopped = false;
        isKnockedBack = false;
    }

    // --- 轉化 ---

    private void ExecuteConversion()
    {
        data.isConverted = true;
        if (IsOnNavMesh) agent.isStopped = true;
        OnConverted?.Invoke("這個法案有道理！");
        // LevelManager.Instance.AddVotes(data.IsDieHard ? 10 : 1);
    }
}
