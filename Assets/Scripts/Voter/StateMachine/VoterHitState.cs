using UnityEngine;
using UnityEngine.AI;

public class VoterHitState : IState
{
    private VoterLogic _controller;
    private Vector3 _attackerPos;
    private float _timer;
    private Vector3 _startPos;
    private Vector3 _targetPos;

    public VoterHitState(VoterLogic controller, Vector3 attackerPos)
    {
        _controller = controller;
        _attackerPos = attackerPos;
    }

    public void Enter()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.isStopped = true;
        }

        if (_controller.Visuals != null)
        {
            _controller.Visuals.PlayHitAnimation();
        }

        _timer = 0f;

        Vector3 direction = (_controller.transform.position - _attackerPos).normalized;
        direction.y = 0f;

        _startPos = _controller.transform.position;
        _targetPos = _startPos + direction * _controller.knockbackDistance;

        if (NavMesh.SamplePosition(_targetPos, out NavMeshHit hit, _controller.knockbackDistance, NavMesh.AllAreas))
        {
            _targetPos = hit.position;
        }
    }

    public void Update()
    {
        // 僅保留擊退的物理 Lerp 運算，狀態切換交由 AnimationFinishTrigger
        if (_timer < _controller.knockbackDuration)
        {
            _timer += Time.deltaTime;
            float t = _timer / _controller.knockbackDuration;

            if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
            {
                _controller.Agent.Warp(Vector3.Lerp(_startPos, _targetPos, t));
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

    // 由動畫事件 (Animation Event) 觸發
    public void AnimationFinishTrigger()
    {
        _controller.StateMachine.ChangeState(new VoterIdleState(_controller));
    }
}
