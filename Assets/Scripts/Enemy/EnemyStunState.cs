using UnityEngine;

/// <summary>
/// 敵人的受擊與硬直狀態。
/// 當敵人受到玩家攻擊或技能影響時進入，中斷目前的行動（例如拉票）。
/// </summary>
public class EnemyStunState : IState
{
    private EnemyController ctx;
    private StateMachine stateMachine;
    
    private float stunDuration;
    private float stunTimer;
    private GameObject _stunVfxInstance; // 暈眩 VFX 實例，Exit 時銷毀

    public EnemyStunState(EnemyController controller, StateMachine stateMachine)
    {
        this.ctx = controller;
        this.stateMachine = stateMachine;
    }

    /// <summary>
    /// 設定此狀態的硬直時間
    /// </summary>
    public void SetStunDuration(float duration)
    {
        stunDuration = duration;
    }

    public void Enter()
    {
        // 1. 強制停止導航移動
        if (ctx.Agent != null && ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.isStopped = true;
            ctx.Agent.ResetPath();
        }

        // 2. 隱藏攻擊範圍網格 (避免攻擊被打斷後網格還殘留)
        if (ctx.attackRangeMesh != null)
        {
            ctx.attackRangeMesh.Hide();
        }

        // 3. 播放受擊動畫
        ctx.Animator?.CrossFade("Idle", 0.1f);

        // 4. 生成暈眩 VFX，設為子物件使其跟隨角色
        if (ctx.stunVfxPrefab != null)
        {
            Vector3 spawnPos = ctx.transform.position + ctx.stunVfxOffset;
            _stunVfxInstance = Object.Instantiate(ctx.stunVfxPrefab, spawnPos, Quaternion.identity);
            _stunVfxInstance.transform.SetParent(ctx.transform, worldPositionStays: true);
        }

        stunTimer = 0f;
        Debug.Log($"Enemy: 進入 Stun 狀態，硬直時間 {stunDuration} 秒，已中斷原先行動！");
    }

    public void Update()
    {
        stunTimer += Time.deltaTime;

        // 硬直時間結束，切換回 Idle 重新索敵
        if (stunTimer >= stunDuration)
        {
            stateMachine.ChangeState(ctx.IdleState);
        }
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit()
    {
        // 離開暈眩狀態時銷毀 VFX
        if (_stunVfxInstance != null)
        {
            Object.Destroy(_stunVfxInstance);
            _stunVfxInstance = null;
        }
    }
}
