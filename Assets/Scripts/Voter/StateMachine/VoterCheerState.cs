using UnityEngine;

public class VoterCheerState : IState
{
    private VoterLogic _controller;
    private Vector3 _lookTarget;

    public VoterCheerState(VoterLogic controller, Vector3 lookTarget = default)
    {
        _controller = controller;
        _lookTarget = lookTarget;
    }

    public void Enter()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.isStopped = true;
            _controller.Agent.ResetPath();
        }

        if (_lookTarget != default)
        {
            Vector3 dir = (_lookTarget - _controller.transform.position).normalized;
            dir.y = 0f;
            if (dir != Vector3.zero)
            {
                _controller.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        if (_controller.Visuals != null)
        {
            _controller.Visuals.PlayCheerAnimation();
        }
    }

    public void Update()
    {
        // 完全交給動畫事件 AnimationFinishTrigger 來切換狀態
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
