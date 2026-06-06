using UnityEngine;

public class DashState : IPlayerState
{
    private static readonly int HashDash = Animator.StringToHash("dash");

    private readonly PlayerStateMachine _ctx;
    private float _endTime;
    private Vector3 _dashDir;

    public DashState(PlayerStateMachine ctx) => _ctx = ctx;

    public void Enter()
    {
        // 以目前移動方向衝刺，若站立不動則用面朝方向
        Vector2 input = _ctx.MoveInput;
        _dashDir = input.sqrMagnitude > 0.01f
            ? new Vector3(input.x, 0f, input.y).normalized
            : _ctx.LastMoveDirection;

        _endTime = Time.time + _ctx.dashDuration;
        _ctx.SetDashCooldown();

        _ctx.characterAnimator?.SetTrigger(HashDash);
        Debug.Log($"[DashState] Enter — 方向: {_dashDir}");
    }

    public void Update()
    {
        if (Time.time < _endTime)
        {
            _ctx.CharCon.Move(_dashDir * _ctx.dashSpeed * Time.deltaTime);
            return;
        }

        // 衝刺結束，根據有無輸入回到對應狀態
        if (_ctx.MoveInput.sqrMagnitude > 0.01f)
            _ctx.ChangeState(new MoveState(_ctx));
        else
            _ctx.ChangeState(new IdleState(_ctx));
    }

    public void Exit()
    {
        Debug.Log("[DashState] Exit");
    }
}
