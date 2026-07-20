using UnityEngine;
using UnityEngine.AI;

public class VoterHitState : IState
{
    private VoterLogic _controller;
    private Vector3 _attackerPos;
    private float _timer;
    private Vector3 _startPos;
    private Vector3 _targetPos;

    private float _customDistance;
    private float _customDuration;

    public VoterHitState(VoterLogic controller, Vector3 attackerPos, float customDistance = -1f, float customDuration = -1f)
    {
        _controller = controller;
        _attackerPos = attackerPos;
        _customDistance = customDistance >= 0f ? customDistance : controller.knockbackDistance;
        _customDuration = customDuration >= 0f ? customDuration : controller.knockbackDuration;
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
        _targetPos = _startPos + direction * _customDistance;

        if (NavMesh.SamplePosition(_targetPos, out NavMeshHit hit, _customDistance, NavMesh.AllAreas))
        {
            _targetPos = hit.position;
        }
    }

    public void Update()
    {
        // 僅保留擊退的物理 Lerp 運算，狀態切換交由 AnimationFinishTrigger
        if (_timer < _customDuration)
        {
            _timer += Time.deltaTime;
            float duration = _customDuration > 0f ? _customDuration : 0.01f; // 避免除以零
            float t = _timer / duration;

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
