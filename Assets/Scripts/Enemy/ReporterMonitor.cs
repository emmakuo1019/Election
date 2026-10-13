using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 記者監視元件：掛在 Reporter Prefab 上，負責監聽技能施放事件並套用處罰。
///
/// 與 ReporterFollowState 完全分開：
///   FollowState  → 控制移動（我要跟著誰）
///   ReporterMonitor → 監視邏輯（我有沒有抓到人）
///
/// 去重：由靜態 PenaltyArbiter 跨所有記者共享 CastId，
///       確保同一次施法最多只扣一次玩家 HP。
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

    [Tooltip("同一記者兩次處罰之間的最短冷卻（秒），避免短時間內連續觸發")]
    [Min(0f)] public float penaltyCooldown = 3f;

    // ══════════════════════════════════════════════════════════════════
    // 靜態跨記者去重仲裁器
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 跨所有記者的施法去重：同一 CastId 最多觸發一次玩家處罰、一次 Boss 暈眩。
    /// Encounter 結束（OnObjectiveResolved）或場景切換時清除。
    /// ponytail: HashSet 無限期保留直到 Clear()；施法間隔 > penaltyCooldown 且玩家不會
    ///           在一局內觸發上萬次，不限制大小；若有極端需求可加 MaxSize 剔除舊 ID。
    /// </summary>
    private static readonly HashSet<System.Guid> _processedCastIds = new();
    private static bool _arbiterSubscribed = false;

    private static void EnsureArbiterSubscribed()
    {
        if (_arbiterSubscribed) return;
        _arbiterSubscribed = true;
        BattleEventManager.OnObjectiveResolved += _ => ClearArbiter();
    }

    private static void ClearArbiter()
    {
        _processedCastIds.Clear();
        _arbiterSubscribed = false;
    }

    // ══════════════════════════════════════════════════════════════════
    // 實例狀態
    // ══════════════════════════════════════════════════════════════════

    private EnemyController _ctrl;
    private float _lastPenaltyTime = -999f;

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
    }

    private void OnDisable()
    {
        BattleEventManager.OnSkillCast -= HandleSkillCast;
    }

    // ══════════════════════════════════════════════════════════════════
    // 事件處理
    // ══════════════════════════════════════════════════════════════════

    private void HandleSkillCast(SkillCastInfo info)
    {
        // ── 基本門檻 ──────────────────────────────────────────────────
        if (_ctrl.IsDead) return;
        if (BattleEventManager.CurrentEncounterPhase != BattleEventManager.EncounterPhase.Active) return;
        if (info.Caster == null) return;
        if (info.Skill == null) return;

        // ── 技能是否在監視清單 ────────────────────────────────────────
        if (!IsMonitored(info.Skill)) return;

        // ── 距離判斷（XZ 平面）────────────────────────────────────────
        Vector2 casterXZ = new Vector2(info.Caster.position.x, info.Caster.position.z);
        Vector2 selfXZ   = new Vector2(transform.position.x,   transform.position.z);
        if (Vector2.Distance(casterXZ, selfXZ) > monitorRange) return;

        // ── 此記者的個人處罰冷卻 ─────────────────────────────────────
        if (Time.time - _lastPenaltyTime < penaltyCooldown) return;

        // ── 跨記者去重：同一 CastId 已處理過，不重複處罰 ─────────────
        if (!_processedCastIds.Add(info.CastId)) return;

        _lastPenaltyTime = Time.time;
        ApplyPenalty(info);
    }

    private void ApplyPenalty(SkillCastInfo info)
    {
        // 識別施法者類型
        PlayerController player = info.Caster.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            // 玩家：扣誠信 HP
            if (playerPenaltyDamage > 0f && PlayerHealthSystem.HasInstance)
            {
                PlayerHealthSystem.Instance.TakeDamage(playerPenaltyDamage);
                Debug.Log($"[Reporter] 記者 {name} 抓到 {info.Caster.name} 施放 [{info.Skill.skillName}]，" +
                          $"扣除誠信 {playerPenaltyDamage}。CastId={info.CastId}");
            }
            return;
        }

        EnemyController enemy = info.Caster.GetComponentInParent<EnemyController>();
        if (enemy != null && enemy.enemyRole == EnemyRole.EliteBoss)
        {
            // 精英／Boss：暈眩（不扣 HP；已在暈眩中不疊加）
            if (eliteBossStunDuration > 0f)
            {
                enemy.TakeDamage(0, eliteBossStunDuration);
                Debug.Log($"[Reporter] 記者 {name} 抓到 {info.Caster.name}（EliteBoss）施放 [{info.Skill.skillName}]，" +
                          $"暈眩 {eliteBossStunDuration}s。CastId={info.CastId}");
            }
        }
    }

    private bool IsMonitored(SkillData skill)
    {
        if (monitoredSkills == null || monitoredSkills.Length == 0) return false;
        foreach (var s in monitoredSkills)
            if (s != null && s == skill) return true;
        return false;
    }

    // ══════════════════════════════════════════════════════════════════
    // Gizmos（Scene 視圖監視範圍視覺化）
    // ══════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 監視範圍（黃色）
        UnityEditor.Handles.color = new Color(1f, 0.9f, 0f, 0.25f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, monitorRange);
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, monitorRange);

        // 跟隨狀態顯示（若有 EnemyController）
        if (_ctrl == null) _ctrl = GetComponent<EnemyController>();
        if (_ctrl != null && _ctrl.ReporterFollowState != null)
        {
            Transform followTarget = _ctrl.ReporterFollowState.CurrentFollowTarget;
            if (followTarget != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, followTarget.position);
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2.5f,
                    $"→ {followTarget.name}");
            }
        }

        // 監視範圍名稱
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.5f,
            $"Monitor: {monitorRange}m");
    }
#endif
}
