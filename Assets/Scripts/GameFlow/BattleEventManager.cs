using System;
using UnityEngine;

/// <summary>
/// 戰鬥全域事件管理器 (極簡版範例)
/// 負責在戰鬥場景中，提供怪物、玩家等物件觸發核心進度事件的管道。
/// </summary>
public static class BattleEventManager
{
    // 怪物清空、過關事件
    public static event Action OnRoomCleared;
    
    // 玩家死亡事件
    public static event Action OnPlayerDied;

    /// <summary>
    /// 當最後一隻怪物死亡，或達成過關條件時呼叫
    /// </summary>
    public static void TriggerRoomCleared()
    {
        Debug.Log("[BattleEventManager] 觸發房間過關事件 (OnRoomCleared)");
        OnRoomCleared?.Invoke();
    }

    /// <summary>
    /// 當玩家血量歸零時呼叫
    /// </summary>
    public static void TriggerPlayerDied()
    {
        Debug.Log("[BattleEventManager] 觸發玩家死亡事件 (OnPlayerDied)");
        OnPlayerDied?.Invoke();
    }
}
