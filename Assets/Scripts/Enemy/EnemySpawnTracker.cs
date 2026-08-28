using UnityEngine;

/// <summary>
/// 追蹤場上敵人存活數量。
/// BattleFlowController 在戰鬥開始時呼叫 StartTracking()，
/// 全部死亡時觸發 BattleEventManager.TriggerAllEnemiesDefeated()。
/// </summary>
public static class EnemySpawnTracker
{
    private static int _aliveCount;
    private static bool _trackingActive;

    /// <summary>場上目前存活的敵人數量（唯讀）</summary>
    public static int AliveCount => _aliveCount;

    /// <summary>
    /// 存活數量變更時觸發，參數為最新的存活數。
    /// EnemyCounterUI 訂閱此事件以即時更新顯示。
    /// </summary>
    public static event System.Action<int> OnAliveCountChanged;

    public static void StartTracking()
    {
        // 先重置，確保上一關的殘留狀態（_trackingActive=true）不影響本關
        // 若不重置，上一關結束後 _trackingActive 仍為 true，
        // 新場景敵人生成時 OnEnable → NotifyEnemySpawned 會提前累加 count，
        // 甚至可能讓 NotifyEnemyDied 在追蹤器正式啟動前就意外觸發全滅事件。
        _trackingActive = false;
        _aliveCount = 0;

        var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        _aliveCount = enemies.Length;
        _trackingActive = true;
        OnAliveCountChanged?.Invoke(_aliveCount);
        Debug.Log($"[EnemySpawnTracker] 開始追蹤，場上敵人數：{_aliveCount}");
    }

    public static void StopTracking()
    {
        _trackingActive = false;
        _aliveCount = 0;
        OnAliveCountChanged?.Invoke(_aliveCount);
    }

    /// <summary>由 EnemyController.Die() 呼叫</summary>
    public static void NotifyEnemyDied()
    {
        if (!_trackingActive) return;
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
        OnAliveCountChanged?.Invoke(_aliveCount);
        Debug.Log($"[EnemySpawnTracker] 敵人死亡，剩餘：{_aliveCount}");

        if (_aliveCount == 0)
        {
            Debug.Log("[EnemySpawnTracker] 場上敵人全滅，解鎖出口！");
            BattleEventManager.TriggerAllEnemiesDefeated();
        }
    }

    /// <summary>
    /// 由 EnemyController.OnEnable 呼叫（波次追加生成時）。
    /// 僅在追蹤器已啟動時才計入，避免 Survive 任務的敵人誤觸全滅判定。
    /// </summary>
    public static void NotifyEnemySpawned()
    {
        if (!_trackingActive) return;
        _aliveCount++;
        OnAliveCountChanged?.Invoke(_aliveCount);
    }
}
