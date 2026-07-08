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
    
    // 生存時間結束事件
    public static event Action OnSurvivalTimeUp;
    
    // 玩家死亡事件
    public static event Action OnPlayerDied;

    // 遊戲結束，玩家確認返回總部事件
    public static event Action OnReturnToHQConfirmed;
    
    // 選民被轉化成功事件 (int side 陣營: 1=玩家, -1=敵人)
    public static event Action<int> OnVoterConverted;

    /// <summary>
    /// 當最後一隻怪物死亡，或達成過關條件時呼叫
    /// </summary>
    public static void TriggerRoomCleared()
    {
        Debug.Log("[BattleEventManager] 觸發房間過關事件 (OnRoomCleared)");
        OnRoomCleared?.Invoke();
    }

    /// <summary>
    /// 當生存房間時間結束時呼叫
    /// </summary>
    public static void TriggerOnSurvivalTimeUp()
    {
        Debug.Log("[BattleEventManager] 觸發生存時間結束事件 (OnSurvivalTimeUp)");
        OnSurvivalTimeUp?.Invoke();
    }

    /// <summary>
    /// 當玩家血量歸零時呼叫
    /// </summary>
    public static void TriggerPlayerDied()
    {
        Debug.Log("[BattleEventManager] 觸發玩家死亡事件 (OnPlayerDied)");
        OnPlayerDied?.Invoke();
    }

    /// <summary>
    /// 當玩家在結算面板點擊確認返回總部時呼叫
    /// </summary>
    public static void TriggerReturnToHQConfirmed()
    {
        Debug.Log("[BattleEventManager] 觸發確認返回總部事件 (OnReturnToHQConfirmed)");
        OnReturnToHQConfirmed?.Invoke();
    }

    /// <summary>
    /// 當有選民成功轉化為任何一方支持者時呼叫
    /// </summary>
    /// <param name="side">1 為玩家，-1 為敵人</param>
    public static void TriggerOnVoterConverted(int side)
    {
        OnVoterConverted?.Invoke(side);
    }
}
