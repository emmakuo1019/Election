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

    public static void StartTracking()
    {
        var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        _aliveCount = enemies.Length;
        _trackingActive = true;
        Debug.Log($"[EnemySpawnTracker] 開始追蹤，場上敵人數：{_aliveCount}");
    }

    public static void StopTracking()
    {
        _trackingActive = false;
        _aliveCount = 0;
    }

    /// <summary>由 EnemyController.Die() 呼叫</summary>
    public static void NotifyEnemyDied()
    {
        if (!_trackingActive) return;
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
        Debug.Log($"[EnemySpawnTracker] 敵人死亡，剩餘：{_aliveCount}");

        if (_aliveCount == 0)
        {
            Debug.Log("[EnemySpawnTracker] 場上敵人全滅，解鎖出口！");
            BattleEventManager.TriggerAllEnemiesDefeated();
        }
    }

    /// <summary>由 EnemyController.OnEnable 呼叫（波次追加生成時）</summary>
    public static void NotifyEnemySpawned()
    {
        if (!_trackingActive) return;
        _aliveCount++;
    }
}
