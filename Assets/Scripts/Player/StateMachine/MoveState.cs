using UnityEngine;

public class MoveState : IPlayerState
{
    private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
    private readonly PlayerStateMachine _ctx;

    public MoveState(PlayerStateMachine ctx) => _ctx = ctx;

    public void Enter() => _ctx.characterAnimator?.SetBool(HashIsMoving, true);

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

        Vector2 input = _ctx.MoveInput;
        if (input.sqrMagnitude <= 0.01f)
        {
            _ctx.ChangeState(new IdleState(_ctx));
            return;
        }

        Vector3 move = new Vector3(input.x, 0f, input.y);
        _ctx.LastMoveDirection = move.normalized;
        _ctx.CharCon.Move(move * _ctx.moveSpeed * Time.deltaTime);
    }

    public void Exit() { }
}
