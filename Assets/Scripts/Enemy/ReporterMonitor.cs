using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 記者監視元件：掛在 Reporter Prefab，負責監聽技能施放事件並套用處罰。
///
/// FollowState  → 移動（我要跟著誰）
/// ReporterMonitor → 監視邏輯（我有沒有抓到人）
///
/// 去重架構：
///   CastId 去重  — _processedCastIds（有界 50 筆，最舊淘汰）
///   候選人共享冷卻 — _candidatePenaltyTime（以施法者 InstanceId 為 key）
///   仲裁規則      — 冷卻取所有存活 Reporter 中的最大值，結果與訂閱順序無關
///   登記時機      — 只有真正成功套用處罰後才登記（Fix-B）
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class ReporterMonitor : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════
    // Inspector 欄位
    // ══════════════════════════════════════════════════════════════════

    [Header("監視設定")]
    [Tooltip("技能監視範圍半徑（XZ 平面距離）")]
    [Min(0f)] public float monitorRange = 8f;

    [Tooltip("此記者會監視的技能清單（獨立於 reporterDamageWhitelist）")]
    public SkillData[] monitoredSkills = new SkillData[0];

    [Header("處罰設定")]
    [Tooltip("玩家在監視範圍內施放指定技能時，扣除的誠信 HP")]
    [Min(0f)] public float playerPenaltyDamage = 10f;

    [Tooltip("精英／Boss 在監視範圍內施放指定技能時，受到的暈眩時間（秒）")]
    [Min(0f)] public float eliteBossStunDuration = 2f;

    [Tooltip("處罰冷卻（秒）。多名 Reporter 共用同一候選人的冷卻，取所有記者中的最大值。")]
    [Min(0f)] public float penaltyCooldown = 3f;

#if UNITY_EDITOR
    [Header("開發測試")]
    [Tooltip("勾選後在 Editor 中 Phase=None 時也允許監視（僅開發用，正式遊戲請勿勾選）")]
    public bool allowMonitorInNonePhase = false;
#endif

    // ══════════════════════════════════════════════════════════════════
    // 靜態共享仲裁資料
    // ══════════════════════════════════════════════════════════════════

    // CastId → 記錄時間；有界 50 筆，超過時淘汰最舊 20 筆
    // ponytail: 每關施法次數 < 200，50 的上限在正常遊玩下不會觸發
    private static readonly Dictionary<System.Guid, float> _processedCastIds = new();

    // 候選人 InstanceId → 上次成功處罰時間
    private static readonly Dictionary<int, float> _candidatePenaltyTime = new();

    // 追蹤所有存活 Reporter，供冷卻仲裁使用
    private static readonly HashSet<ReporterMonitor> _activeMonitors = new();

    private static bool _arbiterSubscribed = false;

    private static void EnsureArbiterSubscribed()
    {
        if (_arbiterSubscribed) return;
        _arbiterSubscribed = true;
        BattleEventManager.OnObjectiveResolved += OnEncounterEnded;
    }

    private static void OnEncounterEnded(EncounterOutcome _) => ClearArbiter();

    private static void ClearArbiter()
    {
        _processedCastIds.Clear();
        _candidatePenaltyTime.Clear();
        BattleEventManager.OnObjectiveResolved -= OnEncounterEnded;
        _arbiterSubscribed = false;
    }

    // Play Mode 進入時重置靜態資料（防止關閉 Domain Reload 的跨 Session 殘留）
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _processedCastIds.Clear();
        _candidatePenaltyTime.Clear();
        _activeMonitors.Clear();
        if (_arbiterSubscribed)
        {
            BattleEventManager.OnObjectiveResolved -= OnEncounterEnded;
            _arbiterSubscribed = false;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 實例狀態
    // ══════════════════════════════════════════════════════════════════

    private EnemyController _ctrl;

    // ══════════════════════════════════════════════════════════════════
    // 生命週期
    // ══════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _ctrl = GetComponent<EnemyController>();
        EnsureArbiterSubscribed();
    }

    private void OnEnable()
    {
        BattleEventManager.OnSkillCast += HandleSkillCast;
        _activeMonitors.Add(this);
    }

    private void OnDisable()
    {
        BattleEventManager.OnSkillCast -= HandleSkillCast;
        _activeMonitors.Remove(this);
    }

    // ══════════════════════════════════════════════════════════════════
    // 事件處理
    // ══════════════════════════════════════════════════════════════════

    private void HandleSkillCast(SkillCastInfo info)
    {
        if (_ctrl.IsDead) return;

        var phase = BattleEventManager.CurrentEncounterPhase;
        bool phaseOk = phase == BattleEventManager.EncounterPhase.Active;
#if UNITY_EDITOR
        if (!phaseOk && allowMonitorInNonePhase)
            phaseOk = phase == BattleEventManager.EncounterPhase.None;
#endif
        if (!phaseOk)
        {
            Debug.Log($"[ReporterMonitor] 過濾：Phase={phase}（只在 Active 處罰）");
            return;
        }

        if (info.Caster == null || info.Skill == null) return;

        if (!IsMonitored(info.Skill))
        {
            Debug.Log($"[ReporterMonitor] 過濾：{info.Skill.skillName} 不在 monitoredSkills");
            return;
        }

        // XZ 距離判斷
        float dx = info.Caster.position.x - transform.position.x;
        float dz = info.Caster.position.z - transform.position.z;
        if (dx * dx + dz * dz > monitorRange * monitorRange)
        {
            Debug.Log($"[ReporterMonitor] 過濾：距離超出 monitorRange {monitorRange}m");
            return;
        }

        // CastId 去重（先檢查，不先登記）
        if (_processedCastIds.ContainsKey(info.CastId))
        {
            Debug.Log("[ReporterMonitor] 過濾：CastId 已由其他記者處理");
            return;
        }

        // 候選人共享冷卻（先檢查，不先登記）
        int candidateId = info.Caster.GetInstanceID();
        float sharedCooldown = GetMaxPenaltyCooldown();
        if (_candidatePenaltyTime.TryGetValue(candidateId, out float lastTime))
        {
            float remain = sharedCooldown - (Time.time - lastTime);
            if (remain > 0f)
            {
                Debug.Log($"[ReporterMonitor] 過濾：候選人冷卻剩餘 {remain:F1}s");
                return;
            }
        }

        // 嘗試套用處罰；只有真正成功後才登記（Fix-B）
        bool penaltyApplied = ApplyPenalty(info);
        if (!penaltyApplied) return;

        // Fix-C：有界淘汰
        if (_processedCastIds.Count >= 50) PruneOldCastIds();
        _processedCastIds[info.CastId]     = Time.time;
        _candidatePenaltyTime[candidateId] = Time.time;
    }

    // Fix-C：移除最舊 20 筆 CastId
    private static void PruneOldCastIds()
    {
        for (int i = 0; i < 20 && _processedCastIds.Count > 0; i++)
        {
            System.Guid oldest = default;
            float oldestTime = float.MaxValue;
            foreach (var kv in _processedCastIds)
                if (kv.Value < oldestTime) { oldestTime = kv.Value; oldest = kv.Key; }
            if (oldest != default) _processedCastIds.Remove(oldest);
        }
    }

    private static float GetMaxPenaltyCooldown()
    {
        float max = 0f;
        foreach (var m in _activeMonitors)
            if (m != null && m.penaltyCooldown > max) max = m.penaltyCooldown;
        return max;
    }

    // 回傳 true = 處罰成功套用（含護盾吸收）；false = 無有效目標或設定值為 0
    private bool ApplyPenalty(SkillCastInfo info)
    {
        PlayerController player = info.Caster.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            if (playerPenaltyDamage <= 0f) return false;
            if (!PlayerHealthSystem.HasInstance)
            {
                Debug.LogError("[ReporterMonitor] PlayerHealthSystem 不存在！" +
                               "請確認 S0.unity 的 GameDB GameObject 上已掛載 PlayerHealthSystem。");
                return false;
            }
            PlayerHealthSystem.Instance.TakeDamage(playerPenaltyDamage);
            Debug.Log($"[Reporter] {name} 抓到 {info.Caster.name} 施放 [{info.Skill.skillName}]，" +
                       $"扣除誠信 {playerPenaltyDamage}。CastId={info.CastId}");
            return true;
        }

        EnemyController enemy = info.Caster.GetComponentInParent<EnemyController>();
        if (enemy != null && enemy.enemyRole == EnemyRole.EliteBoss)
        {
            if (eliteBossStunDuration <= 0f) return false;
            enemy.TakeDamage(0, eliteBossStunDuration);
            Debug.Log($"[Reporter] {name} 抓到 {info.Caster.name}（EliteBoss）" +
                       $"施放 [{info.Skill.skillName}]，暈眩 {eliteBossStunDuration}s。");
            return true;
        }

        return false;
    }

    private bool IsMonitored(SkillData skill)
    {
        if (monitoredSkills == null || monitoredSkills.Length == 0) return false;
        foreach (var s in monitoredSkills)
            if (s != null && s == skill) return true;
        return false;
    }

    // ══════════════════════════════════════════════════════════════════
    // Gizmos
    // ══════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.color = new Color(1f, 0.9f, 0f, 0.25f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, monitorRange);
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, monitorRange);

        if (_ctrl == null) _ctrl = GetComponent<EnemyController>();
        if (_ctrl?.ReporterFollowState != null)
        {
            Transform t = _ctrl.ReporterFollowState.CurrentFollowTarget;
            if (t != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, t.position);
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, $"→ {t.name}");
            }
        }
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"Monitor: {monitorRange}m");
    }
#endif
}
