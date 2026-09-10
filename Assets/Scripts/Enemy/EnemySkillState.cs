using UnityEngine;

public class EnemySkillState : IState
{
    // ==========================================
    // 狀態依賴參照
    // ==========================================
    private readonly EnemyController _ctx;
    private readonly StateMachine _sm;

    // ==========================================
    // 內部狀態變數
    // ==========================================
    private float _timer;
    private bool _hasExecuted;

    public EnemySkillState(EnemyController controller, StateMachine stateMachine)
    {
        _ctx = controller;
        _sm = stateMachine;
    }

    public void Enter()
    {
        // 1. 停止導航移動
        if (_ctx.Agent != null && _ctx.Agent.isOnNavMesh)
        {
            _ctx.Agent.isStopped = true;
        }

        // 2. 重置計時器與標記
        _timer = 0f;
        _hasExecuted = false;

        // 3. 取得當前要執行的技能
        EnemySkillData currentSkill = _ctx.GetCurrentSkill();
        if (currentSkill == null)
        {
            Debug.LogWarning("[EnemySkillState] 當前技能為空，切換回Idle");
            _sm.ChangeState(_ctx.IdleState);
            return;
        }

        // 4. 顯示範圍提示預覽
        if (_ctx.attackRangeMesh != null)
        {
            _ctx.attackRangeMesh.SetShape(currentSkill.blastRadius, currentSkill.blastAngle);
            MeshRenderer mr = _ctx.attackRangeMesh.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;
            Debug.Log($"[SkillState] 範圍圈已顯示。blastRadius={currentSkill.blastRadius}, angle={currentSkill.blastAngle}");
        }

        // 5. 觸發技能動畫
        if (!string.IsNullOrEmpty(currentSkill.animationTriggerName))
        {
            Animator animator = _ctx.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(currentSkill.animationTriggerName);
            }
        }
    }

    public void Update()
    {
        // 取得當前技能
        EnemySkillData currentSkill = _ctx.GetCurrentSkill();
        
        // 防呆保護：若技能資料為空，直接切換回閒置狀態
        if (currentSkill == null)
        {
            _sm.ChangeState(_ctx.IdleState);
            return;
        }

        // 1. 推進計時器
        _timer += Time.deltaTime;

        // 2. 持續更新朝向
        _ctx.UpdateFacingDirection();

        // 3. 檢查是否達到前搖時間且尚未發動技能
        if (_timer >= currentSkill.skillWindupTime && !_hasExecuted)
        {
            _hasExecuted = true;
            _ctx.PerformSkillHit();
        }

        // 4. 檢查技能總時間是否結束
        if (_timer >= currentSkill.duration)
        {
            _sm.ChangeState(_ctx.IdleState);
        }
    }

    public void PhysicsUpdate()
    {
        // 本技能狀態不涉及自定義物理運算
    }

    public void Exit()
    {
        // 1. 恢復導航移動
        if (_ctx.Agent != null && _ctx.Agent.isOnNavMesh)
        {
            _ctx.Agent.isStopped = false;
        }

        // 2. 隱藏範圍提示圈，恢復普通攻擊的形狀
        if (_ctx.attackRangeMesh != null)
        {
            // 先恢復普通攻擊的 shape，再隱藏
            _ctx.attackRangeMesh.SetShape(_ctx.AttackRange, _ctx.AttackAngle);
            MeshRenderer mr = _ctx.attackRangeMesh.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        // 3. 重置標記
        _hasExecuted = false;
    }
}
