using UnityEngine;

public class VoterWaverState : IState
{
    private VoterLogic _controller;
    private float _originalSpeed;

    public VoterWaverState(VoterLogic controller)
    {
        _controller = controller;
    }

    public void Enter()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            // 將 NavMeshAgent 的速度減半並暫停尋路
            _originalSpeed = _controller.Agent.speed;
            _controller.Agent.speed *= 0.5f;
            _controller.Agent.isStopped = true;
        }

        if (_controller.Visuals != null)
        {
            // 停止移動動畫
            _controller.Visuals.SetMovingAnimation(false);
            
            // 顯示問號 UI
            _controller.Visuals.ShowEmote(EmoteType.Waver);

            // 開始微幅左右抖動
            _controller.Visuals.StartWaverShake();
        }
    }

    public void Update()
    {
        // 如果立場已經被轉化（無論是玩家還是敵人），就離開搖擺狀態
        if (_controller.Data != null && _controller.Data.isConverted)
        {
            _controller.StateMachine.ChangeState(new VoterIdleState(_controller));
        }
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            // 恢復原本速度並繼續尋路
            _controller.Agent.speed = _originalSpeed;
            _controller.Agent.isStopped = false;
        }

        if (_controller.Visuals != null)
        {
            // 隱藏表情 UI
            _controller.Visuals.HideEmote();

            // 停止微幅抖動並恢復原位
            _controller.Visuals.StopWaverShake();
        }
    }
}
