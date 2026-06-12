using UnityEngine;

public class AttackState : IState
{
    private readonly PlayerController _ctx;
    private float attackDuration = 0.2f; // 對應攻擊動畫的長度
    private float attackTimer;

    public AttackState(PlayerController ctx) => _ctx = ctx;

    public void Enter()
    {
        Debug.Log("[AttackState] Enter 觸發了！");
        // 1. 檢查是否可以攻擊（冷卻限制邏輯）
        if (_ctx.PlayerAttack == null)
        {
            Debug.LogWarning("[AttackState] 錯誤：_ctx.PlayerAttack 為空！");
            attackTimer = attackDuration;
            return;
        }
        if (!_ctx.PlayerAttack.CanAttack())
        {
            Debug.LogWarning("[AttackState] CanAttack() 回傳 false，中斷攻擊！");
            // 如果在 CD 中，立刻將計時器設滿，讓 Update 迴圈下一幀直接跳回 Idle
            attackTimer = attackDuration;
            return;
        }

        // 2. 初始化動作
        attackTimer = 0f;
        
        // 鎖定玩家移動
        _ctx.CharCon.Move(Vector3.zero);

        // 3. 播放攻擊動畫
        if (_ctx.AnimController != null)
        {
            _ctx.AnimController.PlayAttackAnimation(_ctx.lastFacingDirection);
        }

        // 4. 呼叫 PlayerAttack 的物理機制與數值判定
        _ctx.PlayerAttack.PerformAttack(_ctx.LastMoveDirection);
    }

    public void Update()
    {
        // 狀態內計時器累加
        attackTimer += Time.deltaTime;

        // 與 Animator 箭頭同步安全退出
        if (attackTimer >= attackDuration)
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
}
