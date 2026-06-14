using UnityEngine;

/// <summary>
/// 敵人的攻擊狀態。
/// </summary>
public class EnemyAttackState : IState
{
    private EnemyController ctx;
    private StateMachine stateMachine;

    private float attackTimer;
    private readonly float attackDuration = 1.5f;

    public EnemyAttackState(EnemyController controller, StateMachine stateMachine)
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

        if (ctx.target != null)
        {
            Vector3 lookPos = ctx.target.position;
            lookPos.y = ctx.transform.position.y;
            ctx.transform.LookAt(lookPos);
        }

        ctx.Animator?.SetTrigger("Attack");
        attackTimer = 0f;

        Debug.Log("Enemy: 進入 Attack 狀態，發動攻擊！");
        
        //在這裡或是透過 Animation Event 呼叫選民的 TakeDamage 方法
    }

    public void Update()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackDuration)
        {
            if (ctx.target == null)
            {
                stateMachine.ChangeState(ctx.IdleState);
                return;
            }

            float distance = Vector3.Distance(ctx.transform.position, ctx.target.position);

            if (distance > ctx.escapeRange) // 【遲滯區間】
            {
                ctx.target = null;
                stateMachine.ChangeState(ctx.IdleState);
            }
            else if (distance > ctx.attackRange)
            {
                stateMachine.ChangeState(ctx.MoveState);
            }
            else
            {
                stateMachine.ChangeState(ctx.AttackState);
            }
        }
    }

    public void PhysicsUpdate() { }
    public void Exit() { }
}
