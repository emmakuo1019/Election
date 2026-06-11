using UnityEngine;

public class VoterStunState : IState
{
    private VoterLogic _controller;
    private float _stunDuration;
    private float _timer;

    public VoterStunState(VoterLogic controller, float duration)
    {
        _controller = controller;
        _stunDuration = duration;
    }

    public void Enter()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.isStopped = true;
            _controller.Agent.ResetPath();
        }
        _timer = 0f;
    }

    public void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _stunDuration)
        {
            _controller.StateMachine.ChangeState(new VoterIdleState(_controller));
        }
    }

    public void PhysicsUpdate() { }

    public void Exit()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.isStopped = false;
        }
    }
}
