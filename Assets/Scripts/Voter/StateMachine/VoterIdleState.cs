using UnityEngine;

public class VoterIdleState : IState
{
    private VoterLogic _controller;
    private float _idleTimer;
    private float _idleDuration;

    public VoterIdleState(VoterLogic controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.isStopped = true;
        }

        if (_controller.Visuals != null)
        {
            _controller.Visuals.SetMovingAnimation(false);
        }

        _idleDuration = Random.Range(_controller.wanderIntervalMin, _controller.wanderIntervalMax);
        _idleTimer = 0f;
    }

    public void Update()
    {
        if (_controller.Data == null) return;

        if (_controller.Data.ShouldFollowPlayer && _controller.PlayerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(_controller.transform.position, _controller.PlayerTransform.position);
            if (distanceToPlayer > _controller.darkApproachStoppingDistance)
            {
                _controller.StateMachine.ChangeState(new VoterFollowState(_controller));
                return;
            }
        }
        else if (!_controller.Data.ShouldFollowPlayer)
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _idleDuration)
            {
                _controller.StateMachine.ChangeState(new VoterWanderState(_controller));
                return;
            }
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
