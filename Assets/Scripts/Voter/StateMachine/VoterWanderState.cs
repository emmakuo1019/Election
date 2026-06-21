using UnityEngine;
using UnityEngine.AI;

public class VoterWanderState : IState
{
    private VoterLogic _controller;

    public VoterWanderState(VoterLogic controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.isStopped = false;
            
            Vector3 dest = SampleRandomNavMeshPoint();
            if (dest != Vector3.zero)
            {
                _controller.Agent.SetDestination(dest);
            }
            else
            {
                _controller.StateMachine.ChangeState(new VoterIdleState(_controller));
            }
        }

        if (_controller.Visuals != null)
        {
            _controller.Visuals.SetMovingAnimation(true);
        }
    }

    public void Update()
    {
        if (_controller.Data != null && _controller.Data.ShouldFollowPlayer)
        {
            _controller.StateMachine.ChangeState(new VoterFollowState(_controller));
            return;
        }

        if (_controller.Agent != null && !_controller.Agent.pathPending)
        {
            if (_controller.Agent.remainingDistance <= _controller.Agent.stoppingDistance)
            {
                if (!_controller.Agent.hasPath || _controller.Agent.velocity.sqrMagnitude == 0f)
                {
                    _controller.StateMachine.ChangeState(new VoterIdleState(_controller));
                }
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

    private Vector3 SampleRandomNavMeshPoint()
    {
        float radius = _controller.wanderRadius;
        Vector3 candidate = _controller.transform.position + new Vector3(
            Random.Range(-radius, radius),
            0f,
            Random.Range(-radius, radius));

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
    }
}
