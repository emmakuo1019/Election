using UnityEngine;

/// <summary>
/// 選民受吸引狀態：當被立牌或特定技能影響時，強制選民走向目標點。
/// </summary>
public class VoterAttractedState : IState
{
    private VoterLogic _controller;
    private Transform _targetStandee;
    private float _stoppingDistance = 1.5f;

    public VoterAttractedState(VoterLogic controller, Transform targetStandee)
    {
        _controller = controller;
        _targetStandee = targetStandee;
    }

    public void Enter()
    {
        // 確保 Agent 存在且啟用中，才設定目標點
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.isStopped = false;
            if (_targetStandee != null)
            {
                // 使用 NavMesh.SamplePosition 確保目標點位於導航網格上，避免立牌懸空導致無法尋路
                if (UnityEngine.AI.NavMesh.SamplePosition(_targetStandee.position, out UnityEngine.AI.NavMeshHit hit, 3.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    _controller.Agent.SetDestination(hit.position);
                }
                else
                {
                    _controller.Agent.SetDestination(_targetStandee.position);
                }
            }
        }

        // [Task 2: 吸引視覺回饋] 利用現有的受擊閃爍/動畫作為被吸引的提示
        if (_controller.Visuals != null)
        {
            _controller.Visuals.TriggerHitFlash(Color.yellow, 0.3f);
            // _controller.Visuals.PlayHitAnimation(); // 也可以同時播放受擊動畫
        }
    }

    public void Update()
    {
        // 防呆檢查：如果目標立牌已被銷毀或關閉，則退回閒置狀態
        if (_targetStandee == null || !_targetStandee.gameObject.activeInHierarchy)
        {
            _controller.StateMachine.ChangeState(new VoterIdleState(_controller));
            return;
        }

        // 到達判定：排除 Y 軸高度差，純粹計算平面距離
        Vector3 voterPos = _controller.transform.position;
        Vector3 targetPos = _targetStandee.position;
        voterPos.y = 0;
        targetPos.y = 0;

        float distance = Vector3.Distance(voterPos, targetPos);
        if (distance <= _stoppingDistance)
        {
            _controller.StateMachine.ChangeState(new VoterIdleState(_controller));
        }
    }

    public void PhysicsUpdate()
    {
        // 留空，目前無需在此狀態處理物理更新
    }

    public void Exit()
    {
        // 離開狀態時重置路徑，避免選民繼續往舊目標走
        if (_controller.Agent != null && _controller.Agent.isOnNavMesh)
        {
            _controller.Agent.ResetPath();
        }
    }
}
