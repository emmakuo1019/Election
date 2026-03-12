using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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

    private VoterData data;
    private VoterVisuals visuals;
    private NavMeshAgent agent;
    private bool isKnockedBack = false;

    void Awake()
    {
        data = GetComponent<VoterData>();
        visuals = GetComponent<VoterVisuals>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.angularSpeed = 0f;       // 2D Sprite 不需要旋轉
        agent.updateRotation = false;
    }

    void Start()
    {
        StartCoroutine(WanderRoutine());
    }

    /// <summary>
    /// 施加影響力，並根據攻擊者位置觸發擊退效果。
    /// </summary>
    /// <param name="amount">影響力數值</param>
    /// <param name="isSkill">是否為技能攻擊</param>
    /// <param name="attackerPosition">攻擊者世界座標，用於計算擊退方向</param>
    public void OnInfluence(int amount, bool isSkill, Vector3 attackerPosition = default)
    {
        if (data.isConverted) return;

        if (data.tag == VoterTag.HatePolitics && !isSkill) return;

        int finalAmount = amount;
        if (data.tag == VoterTag.DontKnow) finalAmount *= 2;

        data.currentPosition += finalAmount;
        data.currentPosition = Mathf.Clamp(data.currentPosition, VoterData.MIN_POS, VoterData.MAX_POS);

        visuals.UpdateBubbleVisual(data.currentPosition);

        if (!isKnockedBack && attackerPosition != default)
        {
            Vector3 knockbackDir = (transform.position - attackerPosition).normalized;
            knockbackDir.y = 0f;
            StartCoroutine(KnockbackRoutine(knockbackDir));
        }

        if (data.currentPosition <= VoterData.MIN_POS)
        {
            ExecuteConversion();
        }
    }

    // --- 隨機遊蕩 ---

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(wanderIntervalMin, wanderIntervalMax));

            if (!isKnockedBack && !data.isConverted)
            {
                Vector3 destination = SampleRandomNavMeshPoint();
                if (destination != Vector3.zero)
                    agent.SetDestination(destination);
            }
        }
    }

    private Vector3 SampleRandomNavMeshPoint()
    {
        Vector3 candidate = transform.position + new Vector3(
            Random.Range(-wanderRadius, wanderRadius),
            0f,
            Random.Range(-wanderRadius, wanderRadius)
        );

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            return hit.position;

        return Vector3.zero;
    }

    // --- 擊退 ---

    private IEnumerator KnockbackRoutine(Vector3 direction)
    {
        isKnockedBack = true;
        agent.isStopped = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction * knockbackDistance;

        // 確保目標點在 NavMesh 上，撞牆時原地搖晃
        if (!NavMesh.SamplePosition(targetPos, out NavMeshHit hit, knockbackDistance, NavMesh.AllAreas))
            targetPos = startPos;
        else
            targetPos = hit.position;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;
            // Ease-out cubic：前段快、後段緩，模擬打擊感
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            agent.Warp(Vector3.Lerp(startPos, targetPos, eased));
            yield return null;
        }

        agent.isStopped = false;
        isKnockedBack = false;
    }

    // --- 轉化 ---

    private void ExecuteConversion()
    {
        data.isConverted = true;
        agent.isStopped = true;
        visuals.ShowConversionDialogue("這個法案有道理！");
        Debug.Log(data.currentPosition);
        // LevelManager.Instance.AddVotes(data.isDieHard ? 10 : 1);
    }
}
