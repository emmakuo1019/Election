using UnityEngine;

/// <summary>
/// 敵人的移動(追擊)狀態。
/// </summary>
public class EnemyMoveState : IState
{
    private EnemyController ctx;
    private StateMachine stateMachine;

    public EnemyMoveState(EnemyController controller, StateMachine stateMachine)
    {
        this.ctx = controller;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        if (ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.isStopped = false;
        }

        ctx.Animator?.Play("Move");
        Debug.Log("Enemy: 進入 Move 狀態，開始追擊");
    }

    public void Update()
    {
        if (ctx.target == null)
        {
            stateMachine.ChangeState(ctx.IdleState);
            return;
        }

        if (ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.SetDestination(ctx.target.position);
        }

        float distance = Vector3.Distance(ctx.transform.position, ctx.target.position);

        if (distance <= ctx.attackRange)
        {
            // 【GC 優化】使用快取的狀態實例
            stateMachine.ChangeState(ctx.AttackState);
        }
        else if (distance > ctx.escapeRange) // 【遲滯區間】使用獨立的脫戰距離
        {
            // 玩家逃出脫戰距離，放棄追擊並清空目標
            ctx.target = null;
            stateMachine.ChangeState(ctx.IdleState);
        }
    }

    public void PhysicsUpdate() { }
    public void Exit() { }
}
