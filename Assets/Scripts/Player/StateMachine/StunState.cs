using UnityEngine;

public class StunState : IState
{
    private readonly PlayerController _ctx;
    private readonly float _duration;
    private float _endTime;

    public StunState(PlayerController ctx, float duration)
    {
        _ctx = ctx;
        _duration = duration;
    }

    public void Enter()
    {
        _endTime = Time.time + _duration;
        // TODO: 播放暈眩動畫
        Debug.Log($"[StunState] Enter — duration: {_duration}");
    }

    public void Update()
    {
        if (Time.time >= _endTime)
            _ctx.StateMachine.ChangeState(new IdleState(_ctx));
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit()
    {
        Debug.Log("[StunState] Exit");
    }
}
