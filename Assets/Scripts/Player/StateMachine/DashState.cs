using UnityEngine;

public class DashState : IState
{
    private readonly PlayerController _ctx;
    private float _endTime;
    private Vector3 _dashDir;

    public DashState(PlayerController ctx) => _ctx = ctx;

    public void Enter()
    {
        Vector2 input = _ctx.MoveInput;
        _dashDir = input.sqrMagnitude > 0.01f
            ? new Vector3(input.x, 0f, input.y).normalized
            : _ctx.LastMoveDirection;

        _endTime = Time.time + _ctx.dashDuration;
        _ctx.SetDashCooldown();

        if (_ctx.AnimController != null)
        {
            Vector2 dashFacingDir = new Vector2(_dashDir.x, _dashDir.z);
            _ctx.AnimController.PlayDashAnimation(dashFacingDir);
        }

        Debug.Log($"[DashState] Enter — 方向: {_dashDir}");
    }

    public void Update()
    {
        if (Time.time < _endTime)
        {
            _ctx.CharCon.Move(_dashDir * _ctx.dashSpeed * Time.deltaTime);
            return;
        }

        if (_ctx.MoveInput.sqrMagnitude > 0.01f)
            _ctx.StateMachine.ChangeState(new MoveState(_ctx));
        else
            _ctx.StateMachine.ChangeState(new IdleState(_ctx));
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit()
    {
        Debug.Log("[DashState] Exit");
    }
}
