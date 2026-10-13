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

        // 防呆：若場上完全沒有敵人就立刻觸發
        if (count == 0)
        {
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
    }

    /// <summary>
    /// 由 EnemyController.Die() 呼叫。
    /// 直接檢查場上敵人數量，如果為 0 則觸發全滅事件。
    /// </summary>
    public static void NotifyEnemyDied()
    {
        if (!_trackingActive)
        {
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
        
        OnAliveCountChanged?.Invoke(count);

        if (count == 0)
        {
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
    }

    /// <summary>
    /// 取得場上所有參與 EliminateAll 計數的敵人（排除 Reporter，Reporter 走獨立清場路徑）。
    /// </summary>
    private static EnemyController[] GetAliveEnemies()
    {
        var all = Object.FindObjectsByType<EnemyController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        // Reporter 不參與 EliminateAll 計數：走 ReporterMissionTracker 獨立路徑
        // ponytail: LINQ 在此 N < 30，每次死亡觸發一次，可接受；有效能需求再改 cached list
        int validCount = 0;
        for (int i = 0; i < all.Length; i++)
            if (all[i].enemyRole != EnemyRole.Reporter) validCount++;

        if (validCount == all.Length) return all;  // 全部都不是 Reporter，直接回傳

        var result = new EnemyController[validCount];
        int idx = 0;
        for (int i = 0; i < all.Length; i++)
            if (all[i].enemyRole != EnemyRole.Reporter) result[idx++] = all[i];
        return result;
    }
}
