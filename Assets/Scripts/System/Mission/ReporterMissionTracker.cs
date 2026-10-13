using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 追蹤本次 Mission 指定生成的 Reporter 是否全數永久退場。
/// 只用於 isReporterMission = true 的任務，與 EnemySpawnTracker 完全獨立。
///
/// 流程：
///   1. EnemySpawner 生成每隻 Reporter 後呼叫 Register(reporter)。
///   2. 生成完畢後呼叫 FinishRegistration()，開始監聽。
///   3. Reporter.Die() 呼叫 NotifyReporterExited(reporter)。
///   4. 所有已登記 Reporter 全部退場後觸發 OnAllReportersDefeated 事件，
///      MissionTracker 收到後呼叫 NotifyResult(true)。
///
/// 安全保護：
///   - registration 完成前不判斷勝利（避免空目標自動成功）。
///   - 目標數為 0 時回報設定錯誤，任務不自動成功。
///   - 重複通知同一 Reporter 被忽略（死亡 guard 由 EnemyController._isDead 保護）。
/// </summary>
public class ReporterMissionTracker : MonoBehaviour
{
    public static ReporterMissionTracker Instance { get; private set; }

    /// <summary>所有指定 Reporter 全數退場時觸發。</summary>
    public static event System.Action OnAllReportersDefeated;

    private readonly HashSet<EnemyController> _targets = new HashSet<EnemyController>();
    private readonly HashSet<EnemyController> _exited  = new HashSet<EnemyController>();
    private bool _registrationDone = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 公開 API ─────────────────────────────────────────────────────

    /// <summary>
    /// EnemySpawner 在每次生成 Reporter 後立即呼叫，將其登記為本次目標。
    /// FinishRegistration() 呼叫之前可以持續呼叫。
    /// </summary>
    public void Register(EnemyController reporter)
    {
        if (reporter == null) return;
        _targets.Add(reporter);
    }

    /// <summary>
    /// 所有 Reporter 生成完畢後由 EnemySpawner 呼叫。
    /// 此後才開始監聽退場；目標數為 0 時視為配置錯誤。
    /// </summary>
    public void FinishRegistration()
    {
        _registrationDone = true;

        if (_targets.Count == 0)
        {
            Debug.LogError("[ReporterMissionTracker] Reporter 清場任務：目標數為 0！" +
                "請確認 SpawnConfig 中有指定 Reporter Prefab，且 totalEnemyCount > 0。" +
                "任務將保持未完成。");
            return;
        }

        // 防呆：若所有 Reporter 在 registration 完成前就已退場（極罕見），立即判斷。
        CheckCompletion();
    }

    /// <summary>
    /// 由 EnemyController.Die()（Reporter 角色）呼叫，通知此 Reporter 已永久退場。
    /// </summary>
    public static void NotifyReporterExited(EnemyController reporter)
    {
        if (Instance == null) return;
        Instance.HandleReporterExited(reporter);
    }

    // ── 內部邏輯 ─────────────────────────────────────────────────────

    private void HandleReporterExited(EnemyController reporter)
    {
        if (reporter == null) return;
        // 只處理本次任務登記的 Reporter
        if (!_targets.Contains(reporter)) return;
        // 重複通知忽略
        if (!_exited.Add(reporter)) return;

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        // registration 未完成前不判斷（避免空目標自動成功）
        if (!_registrationDone) return;
        if (_targets.Count == 0) return;  // 配置錯誤已由 FinishRegistration 報錯

        if (_exited.Count >= _targets.Count)
        {
            OnAllReportersDefeated?.Invoke();
        }
    }
}
