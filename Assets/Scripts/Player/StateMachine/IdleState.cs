using UnityEngine;

public class IdleState : IState
{
    private readonly PlayerController _ctx;

    public IdleState(PlayerController ctx) => _ctx = ctx;

    public void Enter()
    {
        if (_ctx.AnimController != null)
        {
            _ctx.AnimController.PlayIdleAnimation(_ctx.lastFacingDirection);
        }
    }

    public void Update()
    {
        if (_ctx.SkillInputThisFrame.HasValue)
        {
            _ctx.StateMachine.ChangeState(new SkillState(_ctx, _ctx.SkillInputThisFrame.Value));
            return;
        }

        if (_ctx.AttackInputThisFrame)
        {
            _ctx.StateMachine.ChangeState(new AttackState(_ctx));
            return;
        }

        if (_ctx.DashInputThisFrame && _ctx.CanDash)
        {
            _ctx.StateMachine.ChangeState(new DashState(_ctx));
            return;
        }

        if (_ctx.MoveInput.sqrMagnitude > 0.01f)
            _ctx.StateMachine.ChangeState(new MoveState(_ctx));
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit() { }
}
