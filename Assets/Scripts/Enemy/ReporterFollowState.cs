using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 記者專用 AI 狀態：跟隨最近的有效候選人（玩家或 EliteBoss）並保持監視距離。
///
/// 不進入 AttackState，不拉票，不對選民索敵。
/// 暈眩由 EnemyStunState 接管；暈眩結束後回到此狀態。
/// 死亡後 _isDead 旗標已在 EnemyController 設定，Update 會立即停止。
///
/// 監視邏輯（處罰判斷）由 ReporterMonitor 元件負責，此狀態只做移動。
/// </summary>
public class ReporterFollowState : IState
{
    private readonly EnemyController _ctx;
    private readonly StateMachine    _sm;

    // ── 目標搜尋 ─────────────────────────────────────────────────────
    private Transform _followTarget;          // 目前跟隨的候選人
    private float     _searchTimer;           // 距上次掃描的計時器

    public ReporterFollowState(EnemyController controller, StateMachine stateMachine)
    {
        _ctx = controller;
        _sm  = stateMachine;
    }

    // ── IState 生命週期 ───────────────────────────────────────────────

    public void Enter()
    {
        if (_ctx.Agent != null && _ctx.Agent.isOnNavMesh)
            _ctx.Agent.isStopped = false;

        _ctx.Animator?.Play("Move");
        _searchTimer = _ctx.targetSearchInterval; // 進場立刻掃一次
    }

    public void Update()
    {
        _ctx.UpdateFacingDirection();

        // ── 定時重新尋找候選人 ──────────────────────────────────────
        _searchTimer += Time.deltaTime;
        if (_searchTimer >= _ctx.targetSearchInterval)
        {
            _searchTimer = 0f;
            RefreshTarget();
        }

        // ── 目標失效（死亡、場景切換）→ 換目標 ──────────────────────
        if (!IsFollowTargetValid(_followTarget))
        {
            _followTarget = null;
            RefreshTarget();
        }

        if (_followTarget == null)
        {
            // 沒有候選人：停下等待
            if (_ctx.Agent != null && _ctx.Agent.isOnNavMesh && !_ctx.Agent.isStopped)
                _ctx.Agent.isStopped = true;
            _ctx.Animator?.Play("Idle");
            return;
        }

        // ── 移動與距離控制 ───────────────────────────────────────────
        float dist = Vector3.Distance(_ctx.transform.position, _followTarget.position);

        if (dist <= _ctx.followStopDistance)
        {
            // 進入保持距離：停下，但繼續面向目標
            if (_ctx.Agent.isOnNavMesh && !_ctx.Agent.isStopped)
            {
                _ctx.Agent.isStopped = true;
                _ctx.Agent.ResetPath();
            }
            _ctx.Animator?.Play("Idle");
        }
        else if (dist > _ctx.followLoseDistance)
        {
            // 目標超出追蹤距離：放棄，等待下次掃描
            _followTarget = null;
        }
        else
        {
            // 正常追蹤
            if (_ctx.Agent.isOnNavMesh)
            {
                if (_ctx.Agent.isStopped) _ctx.Agent.isStopped = false;
                _ctx.Agent.SetDestination(_followTarget.position);
            }
            _ctx.Animator?.Play("Move");
        }
    }

    public void PhysicsUpdate() { }

    public void Exit()
    {
        // 讓後續狀態（StunState）自行控制 Agent
    }

    // ── 公開 API（供外部查詢當前目標，例如 Gizmos）──────────────────

    /// <summary>目前正在跟隨的候選人（可為 null）</summary>
    public Transform CurrentFollowTarget => _followTarget;

    // ── 內部 ─────────────────────────────────────────────────────────

    /// <summary>在 followDetectionRange 內找最近的有效候選人</summary>
    private void RefreshTarget()
    {
        // ponytail: OverlapSphere + GetComponent 掃描；記者數 < 10，每隔幾秒一次，可接受。
        float bestDist = _ctx.followDetectionRange;
        Transform best = null;

        // 1. 玩家（唯一一個）
        PlayerController player = _ctx._cachedPlayerPublic;
        if (player != null && player.gameObject.activeInHierarchy)
        {
            float d = Vector3.Distance(_ctx.transform.position, player.transform.position);
            if (d <= bestDist) { bestDist = d; best = player.transform; }
        }

        // 2. 場上所有 EliteBoss 角色
        // ponytail: FindObjectsByType 每次搜尋間隔呼叫；EliteBoss 數 < 5，可接受。
        var enemies = Object.FindObjectsByType<EnemyController>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == _ctx) continue;
            if (e.enemyRole != EnemyRole.EliteBoss) continue;
            float d = Vector3.Distance(_ctx.transform.position, e.transform.position);
            if (d < bestDist) { bestDist = d; best = e.transform; }
        }

        // 只在目前無目標或目標失效時更換（不要每次掃描都跳轉）
        if (_followTarget == null || !IsFollowTargetValid(_followTarget))
            _followTarget = best;
    }

    private bool IsFollowTargetValid(Transform t)
    {
        if (t == null) return false;
        if (!t.gameObject.activeInHierarchy) return false;
        // 若目標是 EnemyController 且已死，也視為失效
        EnemyController ec = t.GetComponentInParent<EnemyController>();
        if (ec != null && ec.IsDead) return false;
        return true;
    }
}
