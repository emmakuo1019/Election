using UnityEngine;

/// <summary>
/// 敵人的受擊與硬直狀態。
/// 當敵人受到玩家攻擊或技能影響時進入，中斷目前的行動（例如拉票）。
/// </summary>
public class EnemyStunState : IState
{
    private EnemyController ctx;
    private StateMachine stateMachine;
    
    private float stunDuration;
    private float stunTimer;

    public EnemyStunState(EnemyController controller, StateMachine stateMachine)
    {
        this.ctx = controller;
        this.stateMachine = stateMachine;
    }

    /// <summary>
    /// 設定此狀態的硬直時間
    /// </summary>
    public void SetStunDuration(float duration)
    {
        stunDuration = duration;
    }

    public void Enter()
    {
        // 1. 強制停止導航移動
        if (ctx.Agent != null && ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.isStopped = true;
            ctx.Agent.ResetPath();
        }

        // 2. 隱藏攻擊範圍網格 (避免攻擊被打斷後網格還殘留)
        if (ctx.attackRangeMesh != null)
        {
            ctx.attackRangeMesh.Hide();
        }

        // 3. 播放受擊動畫 (與玩家共用 Animator Controller，所以假設有 Hit 狀態，或者直接回到 Idle)
        // 若敵人沒有專屬的 Hit 動畫，可以暫時 CrossFade 到 Idle 或專屬受擊狀態
        ctx.Animator?.CrossFade("Idle", 0.1f); // 根據你的動畫機調整為 "Hit"
        
        stunTimer = 0f;
        Debug.Log($"Enemy: 進入 Stun 狀態，硬直時間 {stunDuration} 秒，已中斷原先行動！");
    }

    public void Update()
    {
        stunTimer += Time.deltaTime;

        // 硬直時間結束，切換回 Idle 重新索敵
        if (stunTimer >= stunDuration)
        {
            stateMachine.ChangeState(ctx.IdleState);
        }
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit()
    {
    }
}
