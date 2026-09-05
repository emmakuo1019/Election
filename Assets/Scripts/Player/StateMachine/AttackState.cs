using UnityEngine;

public class AttackState : IState
{
    private readonly PlayerController _ctx;
    private float attackTimer;

    public AttackState(PlayerController ctx) => _ctx = ctx;

    public void Enter()
    {
        Debug.Log("[AttackState] ═══ Enter 開始執行 ═══");
        
        // 1. 檢查是否可以攻擊（冷卻限制邏輯）
        if (_ctx.PlayerAttack == null)
        {
            Debug.LogError("[AttackState] 錯誤：_ctx.PlayerAttack 為 null！");
            attackTimer = _ctx.attackDuration;
            return;
        }
        
        Debug.Log($"[AttackState] PlayerAttack 存在，準備檢查 CanAttack()");
        
        if (!_ctx.PlayerAttack.CanAttack())
        {
            Debug.LogWarning("[AttackState] CanAttack() 回傳 false，中斷攻擊！");
            // 如果在 CD 中，立刻將計時器設滿，讓 Update 迴圈下一幀直接跳回 Idle
            attackTimer = _ctx.attackDuration;
            return;
        }

        Debug.Log("[AttackState] CanAttack() 通過，開始攻擊流程");

        // 2. 初始化動作
        attackTimer = 0f;
        
        // 鎖定玩家移動
        _ctx.CharCon.Move(Vector3.zero);

        // 3. 播放攻擊動畫
        if (_ctx.AnimController != null)
        {
            Debug.Log("[AttackState] 播放攻擊動畫");
            _ctx.AnimController.PlayAttackAnimation(_ctx.lastFacingDirection);
        }

        // 4. 呼叫 PlayerAttack 的物理機制與數值判定
        Debug.Log($"[AttackState] 呼叫 PerformAttack，方向={_ctx.LastMoveDirection}");
        _ctx.PlayerAttack.PerformAttack(_ctx.LastMoveDirection);
        
        Debug.Log("[AttackState] ═══ Enter 執行完畢 ═══");
    }

    public void Update()
    {
        // 狀態內計時器累加
        attackTimer += Time.deltaTime;

        // 與 Animator 箭頭同步安全退出
        if (attackTimer >= _ctx.attackDuration)
        {
            _ctx.StateMachine.ChangeState(new IdleState(_ctx));
        }
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit()
    {
        // 狀態退出時的清理
    }

    public void OnStunned(float duration)
    {
        _ctx.StateMachine.ChangeState(new StunState(_ctx, duration));
    }
}
