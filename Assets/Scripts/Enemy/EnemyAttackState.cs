using UnityEngine;

/// <summary>
/// 敵人的攻擊狀態。
/// </summary>
public class EnemyAttackState : IState
{
    private EnemyController ctx;
    private StateMachine stateMachine;

    private float attackTimer;
    private readonly float attackDuration = 1.5f; // 動畫總長度
    private readonly float hitTime = 0.5f;        // 傷害判定點 (前搖結束時機)
    private bool hasHit;                          // 是否已造成傷害的布林值

    public EnemyAttackState(EnemyController controller, StateMachine stateMachine)
    {
        this.ctx = controller;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        // 1. 停止 Agent 移動
        if (ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.isStopped = true;
        }

        // 確保剛進入攻擊狀態時面向選民 (移除 LookAt，改用 Sprite 翻面)
        ctx.UpdateFacingDirection();

        // 2. 重置計時器與標記
        attackTimer = 0f;
        hasHit = false;

        // 3. 直接呼叫 Animator.CrossFade 播放動畫 (不依賴 Trigger 與連線)
        ctx.Animator?.CrossFade("Attack", 0.1f);
        ctx.attackRangeMesh?.Show();

        Debug.Log("Enemy: 進入 Attack 狀態，對選民發動拉票！");
    }

    public void Update()
    {
        // 持續更新 Sprite 朝向
        ctx.UpdateFacingDirection();

        // 1. 累加計時器
        attackTimer += Time.deltaTime;

        // 2. 當到達判定點，且尚未發動拉票時，執行物理偵測與拉票
        if (attackTimer >= hitTime && !hasHit)
        {
            hasHit = true;
            ctx.PerformAttackHit();
        }

        // 3. 當動畫總時長結束時，切換狀態
        if (attackTimer >= attackDuration)
        {
            // 攻擊結束後，強制重新尋找下一個最近的選民
            ctx.FindNearestVoter();

            if (ctx.target == null)
            {
                stateMachine.ChangeState(ctx.IdleState);
            }
            else
            {
                // 如果找到選民，切換到 MoveState 繼續追擊
                // (如果距離已經夠近，MoveState 下一幀就會自動切回 AttackState)
                stateMachine.ChangeState(ctx.MoveState);
            }
        }
    }

    public void PhysicsUpdate() { }
    public void Exit() { }
}
