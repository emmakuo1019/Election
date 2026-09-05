using UnityEngine;

/// <summary>
/// 敵人的閒置狀態。
/// </summary>
public class EnemyIdleState : IState
{
    private EnemyController ctx;
    private StateMachine stateMachine;

    private float _idleTimer;
    private float _idleDuration;

    public EnemyIdleState(EnemyController controller, StateMachine stateMachine)
    {
        this.ctx = controller;
        this.stateMachine = stateMachine;
    }

    public void Enter()
    {
        // 強制停止導航（EnemyController.Start 已確保 Agent 在 NavMesh 上）
        ctx.Agent.isStopped = true;
        
        // 隨機等待時間，等待完後遊蕩
        _idleDuration = UnityEngine.Random.Range(ctx.wanderIntervalMin, ctx.wanderIntervalMax);
        _idleTimer = 0f;

        ctx.Animator?.Play("Idle");
        
        Debug.Log("Enemy: 進入 Idle 狀態");
    }

    public void Update()
    {
        // 持續更新 Sprite 朝向
        ctx.UpdateFacingDirection();

        // 確保目標合法，如果不合法就清空
        if (!ctx.IsTargetValid(ctx.target))
        {
            ctx.target = null;
        }

        // 1. 如果目前沒有目標，尋找最近的目標（選民或玩家）
        if (ctx.target == null)
        {
            ctx.FindNearestTarget();

            if (ctx.target == null)
            {
                // 仍然找不到目標：計時後切換到遊蕩狀態
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= _idleDuration)
                {
                    stateMachine.ChangeState(ctx.WanderState);
                }
                return;
            }
        }

        // 2. 計算與目標的距離
        float distance = Vector3.Distance(ctx.transform.position, ctx.target.position);

        // 3. 判斷是否進入偵測範圍
        if (distance <= ctx.detectionRange)
        {
            stateMachine.ChangeState(ctx.MoveState);
        }
        else if (distance > ctx.escapeRange)
        {
            // 目標太遠，清空並下一輪重新尋找
            ctx.target = null;
        }
    }

    public void PhysicsUpdate() { }
    public void Exit() { }
}
