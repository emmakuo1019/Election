using UnityEngine;

/// <summary>
/// 敵人的閒置狀態。
/// </summary>
public class EnemyIdleState : IState
{
    private EnemyController ctx;
    private StateMachine stateMachine;

    public EnemyIdleState(EnemyController controller, StateMachine stateMachine)
    {
        this.ctx = controller;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        if (ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.isStopped = true;
        }
        
        // 防呆：如果沒有抓到 Animator 也不會報錯
        ctx.Animator?.Play("Idle");
        
        Debug.Log("Enemy: 進入 Idle 狀態");
    }

    public void Update()
    {
        // 1. 如果目前沒有目標，透過範圍掃描尋找 targetLayer 的物件
        if (ctx.target == null)
        {
            Collider[] colliders = Physics.OverlapSphere(ctx.transform.position, ctx.detectionRange, ctx.targetLayer);
            if (colliders.Length > 0)
            {
                ctx.target = colliders[0].transform;
            }
            else
            {
                return; // 沒找到任何目標，繼續保持 Idle
            }
        }

        // 2. 計算與目標的距離
        float distance = Vector3.Distance(ctx.transform.position, ctx.target.position);

        // 3. 判斷是否進入偵測範圍
        if (distance <= ctx.detectionRange)
        {
            // 【GC 優化】使用快取的狀態實例
            stateMachine.ChangeState(ctx.MoveState);
        }
        else if (distance > ctx.escapeRange)
        {
            // 如果某種原因鎖定了目標但目標在脫戰範圍外，就清空目標重新尋找
            ctx.target = null;
        }
    }

    public void PhysicsUpdate() { }
    public void Exit() { }
}
