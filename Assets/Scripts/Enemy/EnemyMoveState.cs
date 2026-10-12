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
        Debug.Log("Enemy: 進入 Move 狀態，開始追擊目標");
    }

    public void Update()
    {
        // 持續更新 Sprite 朝向
        ctx.UpdateFacingDirection();

        // 目標失效（選民被轉化、玩家死亡或消失等）→ 退回 Idle 重新索敵
        if (!ctx.IsTargetValid(ctx.target))
        {
            ctx.target = null;
            stateMachine.ChangeState(ctx.IdleState);
            return;
        }

        if (ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.SetDestination(ctx.target.position);
        }

        float distance = Vector3.Distance(ctx.transform.position, ctx.target.position);

        // 停步距離 = 命中球半徑 + 球心前方偏移 z，確保停下後 OverlapSphere 必能涵蓋目標。
        // 不使用 attackRange 欄位，因為其語意已由 attackHitRadius + attackHitOffset.z 精確定義。
        // ponytail: offset.z=0 時等同原 attackHitRadius；若日後加入前方偏移請同步更新 AttackRangeMesh 位置。
        float effectiveStopRange = ctx.attackHitRadius + Mathf.Max(0f, ctx.attackHitOffset.z);

        if (distance <= effectiveStopRange)
        {
            // 當距離小於等於有效攻擊距離時，切換到攻擊狀態發動拉票
            stateMachine.ChangeState(ctx.AttackState);
        }
        else if (distance > ctx.escapeRange) // 【遲滯區間】使用獨立的脫戰距離
        {
            // 選民逃出脫戰距離，放棄追擊並清空目標
            ctx.target = null;
            stateMachine.ChangeState(ctx.IdleState);
        }
    }

    public void PhysicsUpdate() { }
    public void Exit() { }
}
