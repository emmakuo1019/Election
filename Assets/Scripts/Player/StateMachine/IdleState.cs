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

    public void HandleInput()
    {
        if (_ctx.AttackInputThisFrame)
        {
            Debug.Log("[IdleState] 偵測到攻擊輸入，切換至 AttackState");
            _ctx.StateMachine.ChangeState(_ctx.AttackState);
            return;
        }

        if (_ctx.DashInputThisFrame && _ctx.CanDash)
        {
            Debug.Log("[IdleState] 偵測到衝刺輸入，切換至 DashState");
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
        if (_ctx.MoveInput.sqrMagnitude > 0.01f)
            _ctx.StateMachine.ChangeState(new MoveState(_ctx));
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit() { }
}
