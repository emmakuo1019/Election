using UnityEngine;

public class AttackState : IState
{
    private readonly PlayerController _ctx;
    private float attackDuration = 0.4f; // 對應攻擊動畫的長度
    private float attackTimer;

    public AttackState(PlayerController ctx) => _ctx = ctx;

    public void Enter()
    {
        // 進入普攻時，重置計時器
        attackTimer = 0f;

        // 立刻將玩家的移動速度鎖定為 0
        _ctx.CharCon.Move(Vector3.zero);

        // 使用獨立出來的動畫控制器播放對應方向的攻擊動畫
        if (_ctx.AnimController != null)
        {
            _ctx.AnimController.PlayAttackAnimation(_ctx.lastFacingDirection);
        }

        // 執行言語攻擊判定
        _ctx.PlayerAttack?.PerformSpeech();
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
