using UnityEngine;

/// <summary>
/// 追蹤場上敵人存活數量。
/// 不再使用計數器，改為每次死亡時直接掃描場上敵人。
/// </summary>
public static class EnemySpawnTracker
{
    private static bool _trackingActive;

    /// <summary>場上目前存活的敵人數量（即時查詢）</summary>
    public static int AliveCount => GetAliveEnemies().Length;

    /// <summary>
    /// 存活數量變更時觸發，參數為最新的存活數。
    /// EnemyCounterUI 訂閱此事件以即時更新顯示。
    /// </summary>
    public static event System.Action<int> OnAliveCountChanged;

    /// <summary>
    /// 開始追蹤敵人。由 EnemySpawner 在生成完畢後呼叫。
    /// </summary>
    public static void StartTracking()
    {
        _trackingActive = true;
        int count = GetAliveEnemies().Length;
        OnAliveCountChanged?.Invoke(count);
        Debug.Log($"[EnemySpawnTracker] 開始追蹤，場上敵人數：{count}");

        // 防呆：若場上完全沒有敵人就立刻觸發
        if (count == 0)
        {
            Debug.LogWarning("[EnemySpawnTracker] StartTracking 時場上已無敵人，立即觸發全滅事件。");
            BattleEventManager.TriggerAllEnemiesDefeated();
        }
    }

    /// <summary>
    /// 停止追蹤。場景切換時呼叫。
    /// </summary>
    public static void StopTracking()
    {
        _trackingActive = false;
        OnAliveCountChanged?.Invoke(0);
        Debug.Log("[EnemySpawnTracker] 停止追蹤");
    }

    /// <summary>
    /// 由 EnemyController.Die() 呼叫。
    /// 直接檢查場上敵人數量，如果為 0 則觸發全滅事件。
    /// </summary>
    public static void NotifyEnemyDied()
    {
        if (!_trackingActive)
        {
            Debug.LogWarning("[EnemySpawnTracker] NotifyEnemyDied 被呼叫但追蹤器未啟動，已忽略。");
            return;
        }

        // 等待一幀，確保 Destroy 完成
        CoroutineHelper.Instance.StartCoroutine(CheckEnemiesAfterFrame());
    }

    /// <summary>
    /// 等待一幀後檢查場上敵人數量。
    /// 確保 Destroy() 已經執行完畢。
    /// </summary>
    private static System.Collections.IEnumerator CheckEnemiesAfterFrame()
    {
        yield return null; // 等待一幀

        if (!_trackingActive) yield break;

        var enemies = GetAliveEnemies();
        int count = enemies.Length;
        
        Debug.Log($"[EnemySpawnTracker] 敵人死亡，場上剩餘：{count}");
        OnAliveCountChanged?.Invoke(count);

        if (count == 0)
        {
            Debug.Log("[EnemySpawnTracker] 場上敵人全滅，觸發 OnAllEnemiesDefeated 事件！");
            BattleEventManager.TriggerAllEnemiesDefeated();
        }
    }

    /// <summary>
    /// 由 EnemyController.OnEnable 呼叫（波次追加生成時）。
    /// </summary>
    public static void NotifyEnemySpawned()
    {
        if (!_trackingActive) return;
        int count = GetAliveEnemies().Length;
        OnAliveCountChanged?.Invoke(count);
        Debug.Log($"[EnemySpawnTracker] 敵人生成，場上數量：{count}");
    }

    /// <summary>
    /// 取得場上所有活著的敵人。
    /// </summary>
    private static EnemyController[] GetAliveEnemies()
    {
        return Object.FindObjectsByType<EnemyController>(
            FindObjectsInactive.Exclude, 
            FindObjectsSortMode.None);
    }
}
