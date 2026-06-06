using UnityEngine;

public class IdleState : IPlayerState
{
    private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
    private readonly PlayerStateMachine _ctx;

    public IdleState(PlayerStateMachine ctx) => _ctx = ctx;

    public void Enter() => _ctx.characterAnimator?.SetBool(HashIsMoving, false);

    public void Update()
    {
        if (_ctx.SkillInputThisFrame.HasValue)
        {
            _ctx.ChangeState(new SkillState(_ctx, _ctx.SkillInputThisFrame.Value));
            return;
        }

        if (_ctx.AttackInputThisFrame)
        {
            _ctx.ChangeState(new AttackState(_ctx));
            return;
        }

        if (_ctx.DashInputThisFrame && _ctx.CanDash)
        {
            _ctx.ChangeState(new DashState(_ctx));
            return;
        }

        if (_ctx.MoveInput.sqrMagnitude > 0.01f)
            _ctx.ChangeState(new MoveState(_ctx));
    }

    public void Exit() { }
}
