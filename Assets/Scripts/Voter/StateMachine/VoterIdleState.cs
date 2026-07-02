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

        // 【安全防護】判斷是否進入搖擺狀態。
        // 注意：如果你使用 <= 0.5f，代表 0 (絕對中立) 也會一直處於搖擺！
        // 這裡幫你加上條件，只在「有偏向但尚未轉化」時搖擺（你可以根據企劃需求決定用 >= 還是 <=）
        bool isWavering = !_controller.Data.isConverted && 
                          Mathf.Abs(_controller.Data.CurrentPosition) > 0 &&
                          Mathf.Abs(_controller.Data.CurrentPosition) <= _controller.Data.MaxSupportValue * 0.5f;

        if (isWavering)
        {
            _controller.StateMachine.ChangeState(new VoterWaverState(_controller));
            return;
        }

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
