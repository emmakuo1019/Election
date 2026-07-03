using UnityEngine;

public class VoterApatheticState : IState
{
    private VoterLogic voter;
    private Transform player;
    private float nextRefreshTime;
    private const float RefreshInterval = 0.5f; // 每 0.5 秒重新計算一次逃跑路徑

    public VoterApatheticState(VoterLogic logic)
    {
        this.voter = logic;
    }

    public void Enter()
    {
        player = voter.PlayerTransform;
        voter.Visuals?.SetMovingAnimation(true);
        if (voter.Agent != null && voter.Data != null)
        {
            // 冷感選民逃跑時給予加速 (可從 Inspector 調整)
            voter.Agent.speed = voter.Data.MoveSpeed * voter.Data.apatheticEscapeSpeedMultiplier;
        }
        nextRefreshTime = 0f;
    }

    public void Update()
    {
        if (player == null || voter.Data == null) return;

        // 若已經被成功轉化，離開逃跑狀態
        if (voter.Data.isConverted)
        {
            voter.StateMachine.ChangeState(new VoterIdleState(voter));
            return;
        }

        float distance = Vector3.Distance(voter.transform.position, player.position);

        if (distance < voter.Data.apathyAvoidanceRadius)
        {
            if (Time.time >= nextRefreshTime)
            {
                nextRefreshTime = Time.time + RefreshInterval;

                // 計算遠離玩家的反向向量
                Vector3 avoidDirection = (voter.transform.position - player.position).normalized;
                Vector3 candidateDestination = voter.transform.position + avoidDirection * 5f;
                
                // 確保逃跑目標點位於 NavMesh 上，避免 Agent 卡住
                if (UnityEngine.AI.NavMesh.SamplePosition(candidateDestination, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    if (voter.Agent != null && voter.Agent.isOnNavMesh)
                    {
                        voter.Agent.isStopped = false;
                        voter.Agent.SetDestination(hit.position);
                    }
                }
            }
        }
        else
        {
            // 距離足夠遠時，過渡回 IdleState
            voter.StateMachine.ChangeState(new VoterIdleState(voter));
        }
    }

    public void PhysicsUpdate() { }

    public void Exit()
    {
        if (voter.Agent != null && voter.Agent.isOnNavMesh)
        {
            voter.Agent.ResetPath();
            voter.RefreshMovementSpeed(); // 恢復正常速度
        }
        voter.Visuals?.SetMovingAnimation(false);
    }

    public void AnimationFinishTrigger() { }
}
