using UnityEngine;

/// <summary>
/// 選民離場狀態：敵人全滅後，選民朝指定出口移動並自動回收。
/// 此狀態優先於 IsGameActive 限制（VoterLogic.Update 中有特別處理）。
/// </summary>
public class VoterExitState : IState
{
    private readonly VoterLogic _controller;
    private readonly Vector3 _destination;

    private const float ArrivalThreshold = 1.5f;

    public VoterExitState(VoterLogic controller, Vector3 destination)
    {
        _controller = controller;
        _destination = destination;
    }

    public void Enter()
    {
        if (_controller.Agent != null)
        {
            _controller.Agent.enabled = true;

            if (_controller.Agent.isOnNavMesh)
            {
                _controller.Agent.isStopped = false;
                _controller.Agent.SetDestination(_destination);
            }
        }

        if (_controller.Visuals != null)
        {
            _controller.Visuals.SetMovingAnimation(true);
        }
    }

    public void Update()
    {
        if (_controller.Agent == null || !_controller.Agent.isOnNavMesh) return;

        // 到達出口附近後自動回收
        float distance = Vector3.Distance(_controller.transform.position, _destination);
        if (distance <= ArrivalThreshold)
        {
            ReturnToPool();
            return;
        }

        // NavMesh 路徑已完成但還沒走到（例如出口在 NavMesh 邊界外）
        if (!_controller.Agent.pathPending && _controller.Agent.remainingDistance <= ArrivalThreshold)
        {
            ReturnToPool();
        }
    }

    public void PhysicsUpdate() { }

    public void Exit()
    {
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.isStopped = true;
            _controller.Agent.ResetPath();
        }

        if (_controller.Visuals != null)
        {
            _controller.Visuals.SetMovingAnimation(false);
        }
    }

    private void ReturnToPool()
    {
        if (PoolManager.HasInstance)
        {
            PoolManager.Instance.Release(_controller.gameObject);
        }
        else
        {
            Object.Destroy(_controller.gameObject);
        }
    }
}
