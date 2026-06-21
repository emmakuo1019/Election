using UnityEngine;

public class MoveState : IState
{
    private readonly PlayerController _ctx;

    public MoveState(PlayerController ctx) => _ctx = ctx;

    public void Enter()
    {
        if (_ctx.AnimController != null)
        {
            _ctx.AnimController.PlayWalkAnimation(_ctx.lastFacingDirection);
        }
    }

    public void HandleInput()
    {
        if (_ctx.AttackInputThisFrame)
        {
            _ctx.StateMachine.ChangeState(_ctx.AttackState);
            return;
        }

        if (_ctx.DashInputThisFrame && _ctx.CanDash)
        {
            _ctx.StateMachine.ChangeState(new DashState(_ctx));
            return;
        }

        if (TryCastSkill()) return;
    }

    private bool TryCastSkill()
    {
        if (_ctx.SkillJInputThisFrame && _ctx.SkillManager != null && _ctx.SkillManager.CanCastSkill(_ctx.SkillManager.baseSkillJ))
        {
            _ctx.StateMachine.ChangeState(new SkillState(_ctx, _ctx.SkillManager.baseSkillJ));
            return true;
        }
        if (_ctx.SkillKInputThisFrame && _ctx.SkillManager != null && _ctx.SkillManager.CanCastSkill(_ctx.SkillManager.skillK))
        {
            _ctx.StateMachine.ChangeState(new SkillState(_ctx, _ctx.SkillManager.skillK));
            return true;
        }
        if (_ctx.SkillLInputThisFrame && _ctx.SkillManager != null && _ctx.SkillManager.CanCastSkill(_ctx.SkillManager.CurrentPartySkill))
        {
            _ctx.StateMachine.ChangeState(new SkillState(_ctx, _ctx.SkillManager.CurrentPartySkill));
            return true;
        }
        return false;
    }

    public void OnStunned(float duration)
    {
        _ctx.StateMachine.ChangeState(new StunState(_ctx, duration));
    }

    public void Update()
    {
        Vector2 input = _ctx.MoveInput;
        if (input.sqrMagnitude <= 0.01f)
        {
            _ctx.StateMachine.ChangeState(new IdleState(_ctx));
            return;
        }

        Vector3 move = new Vector3(input.x, 0f, input.y);
        _ctx.LastMoveDirection = move.normalized;
        _ctx.CharCon.Move(move * _ctx.moveSpeed * Time.deltaTime);

        if (_ctx.AnimController != null)
        {
            _ctx.AnimController.PlayWalkAnimation(_ctx.lastFacingDirection);
        }
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit() { }
}
