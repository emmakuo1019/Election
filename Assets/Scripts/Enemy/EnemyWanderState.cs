using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敵人的遊蕩狀態。
/// 當偵測範圍內沒有任何有效目標時觸發，讓敵人在地圖上隨機移動，
/// 邏輯與 VoterWanderState 相同。
/// </summary>
public class EnemyWanderState : IState
{
    private readonly EnemyController ctx;
    private readonly StateMachine stateMachine;

    public EnemyWanderState(EnemyController controller, StateMachine stateMachine)
    {
        ctx = controller;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        if (ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.isStopped = false;

            Vector3 dest = SampleRandomNavMeshPoint();
            if (dest != Vector3.zero)
            {
                ctx.Agent.SetDestination(dest);
            }
            else
            {
                // 找不到可行走點，退回 Idle 等下一輪
                stateMachine.ChangeState(ctx.IdleState);
                return;
            }
        }

        ctx.Animator?.Play("Move");
        Debug.Log("Enemy: 進入 Wander 狀態，開始隨機遊蕩");
    }

    public void Update()
    {
        // 持續更新 Sprite 朝向
        ctx.UpdateFacingDirection();

        // 每幀嘗試偵測目標；一旦找到就立刻切回 Idle（Idle 會馬上轉 Move 追擊）
        ctx.FindNearestTarget();
        if (ctx.target != null)
        {
            stateMachine.ChangeState(ctx.IdleState);
            return;
        }

        // 已走到目的地 → 回 Idle，讓 Idle 計時後再次 Wander
        if (ctx.Agent.isOnNavMesh && !ctx.Agent.pathPending)
        {
            if (ctx.Agent.remainingDistance <= ctx.Agent.stoppingDistance)
            {
                if (!ctx.Agent.hasPath || ctx.Agent.velocity.sqrMagnitude == 0f)
                {
                    stateMachine.ChangeState(ctx.IdleState);
                }
            }
        }
    }

    public void PhysicsUpdate() { }

    public void Exit()
    {
        if (ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.ResetPath();
        }
    }

    // -------------------------------------------------------
    // 工具方法
    // -------------------------------------------------------

    /// <summary>在 wanderRadius 範圍內隨機取樣一個 NavMesh 上的點。</summary>
    private Vector3 SampleRandomNavMeshPoint()
    {
        // 使用與 VoterWanderState 完全相同的半徑欄位名稱
        // EnemyController 沒有 wanderRadius 時回退到 detectionRange 的一半
        float radius = ctx.wanderRadius > 0f ? ctx.wanderRadius : ctx.detectionRange * 0.5f;

        Vector3 candidate = ctx.transform.position + new Vector3(
            Random.Range(-radius, radius),
            0f,
            Random.Range(-radius, radius));

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
    }
}
