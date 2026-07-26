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

        // 3. 顯示範圍提示預覽
        // 注意：不可呼叫 Show() 或 ShowIdle()，兩者內部都會用 IAttackSource 的值
        // 覆蓋掉我們剛設好的 blastRadius/blastAngle。
        // 直接 SetShape 後手動啟用 MeshRenderer，繞過覆蓋問題。
        if (_ctx.attackRangeMesh != null && _ctx.equippedSkill != null)
        {
            _ctx.attackRangeMesh.SetShape(_ctx.equippedSkill.blastRadius, _ctx.equippedSkill.blastAngle);
            MeshRenderer mr = _ctx.attackRangeMesh.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;
            Debug.Log($"[SkillState] 範圍圈已顯示。blastRadius={_ctx.equippedSkill.blastRadius}, angle={_ctx.equippedSkill.blastAngle}");
        }

        // 4. 觸發技能動畫
        if (_ctx.equippedSkill != null && !string.IsNullOrEmpty(_ctx.equippedSkill.animationTriggerName))
        {
            Animator animator = _ctx.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(_ctx.equippedSkill.animationTriggerName);
            }
        }
    }

    public void Update()
    {
        // 防呆保護：若技能資料為空，直接切換回閒置狀態
        if (_ctx.equippedSkill == null)
        {
            _sm.ChangeState(_ctx.IdleState);
            return;
        }

        // 1. 推進計時器
        _timer += Time.deltaTime;

        // 2. 持續更新朝向
        _ctx.UpdateFacingDirection();

        // 3. 檢查是否達到前搖時間且尚未發動技能
        if (_timer >= _ctx.equippedSkill.skillWindupTime && !_hasExecuted)
        {
            _hasExecuted = true;
            _ctx.PerformSkillHit();
        }

        // 4. 檢查技能總時間是否結束
        if (_timer >= _ctx.equippedSkill.duration)
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
