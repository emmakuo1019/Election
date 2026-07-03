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
        if (_controller.Data == null) return;

        // 【安全防護】如果是未被轉化的冷感選民，偵測玩家距離進行逃跑
        if (!_controller.Data.isConverted && _controller.Data.Attribute == VoterAttribute.Cold && _controller.PlayerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(_controller.transform.position, _controller.PlayerTransform.position);
            if (distanceToPlayer < _controller.Data.apathyAvoidanceRadius)
            {
                _controller.StateMachine.ChangeState(new VoterApatheticState(_controller));
                return;
            }
        }

        // 【安全防護】判斷是否進入搖擺狀態。
        bool isWavering = !_controller.Data.isConverted && 
                          Mathf.Abs(_controller.Data.CurrentPosition) > 0 &&
                          Mathf.Abs(_controller.Data.CurrentPosition) <= _controller.Data.MaxSupportValue * 0.5f;

        if (isWavering)
        {
            _controller.StateMachine.ChangeState(new VoterWaverState(_controller));
            return;
        }

        if (_controller.Data.ShouldFollowPlayer)
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
