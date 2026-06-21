using UnityEngine;

public class VoterFollowState : IState
{
    private VoterLogic _controller;
    private float _nextRefreshTime;

    public VoterFollowState(VoterLogic controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.isStopped = false;
        }
        
        if (_controller.Visuals != null)
        {
            _controller.Visuals.SetMovingAnimation(true);
        }

        _nextRefreshTime = 0f;
    }

    public void Update()
    {
        if (_controller.Data == null || !_controller.Data.ShouldFollowPlayer)
        {
            _controller.StateMachine.ChangeState(new VoterIdleState(_controller));
            return;
        }

        if (_controller.PlayerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(_controller.transform.position, _controller.PlayerTransform.position);
        
        if (distanceToPlayer <= _controller.darkApproachStoppingDistance)
        {
            _controller.StateMachine.ChangeState(new VoterIdleState(_controller));
            return;
        }

        if (Time.time >= _nextRefreshTime)
        {
            _nextRefreshTime = Time.time + _controller.darkDestinationRefreshInterval;
            
            if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
            {
                _controller.Agent.SetDestination(_controller.PlayerTransform.position);
            }
        }
    }

    public void PhysicsUpdate() { }

    public void Exit()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.ResetPath();
        }
        
        if (_controller.Visuals != null)
        {
            _controller.Visuals.SetMovingAnimation(false);
        }
    }
}
