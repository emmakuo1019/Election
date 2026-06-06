using UnityEngine;

public class AttackState : IPlayerState
{
    private readonly PlayerStateMachine _ctx;

    public AttackState(PlayerStateMachine ctx) => _ctx = ctx;

    public void Enter()
    {
        _ctx.PlayerAttack?.PerformSpeech();
        if (_ctx.MoveInput.sqrMagnitude > 0.01f)
            _ctx.ChangeState(new MoveState(_ctx));
        else
            _ctx.ChangeState(new IdleState(_ctx));
    }

    public void Update() { }
    public void Exit() { }
}
