using System;
using UnityEngine;

/// <summary>
/// 戰鬥全域事件管理器 (極簡版範例)
/// 負責在戰鬥場景中，提供怪物、玩家等物件觸發核心進度事件的管道。

/// <summary>
/// 戰鬥全域事件管理器 (極簡版範例)
/// 負責在戰鬥場景中，提供怪物、玩家等物件觸發核心進度事件的管道。
/// </summary>
public static class BattleEventManager
{
    /// <summary>房間流程唯一允許的生命週期。只有 State 可以推進這個階段。</summary>
    public enum EncounterPhase
    {
        None,
        Loading,
        Briefing,
        Active,
        ObjectiveResolved,
        RewardSelection,
        RouteSelection,
        Transitioning,
    }

    // 怪物清空、過關事件
    public static event Action OnRoomCleared;

    // 新流程：任務已判定、獎勵已選、路線已選、實體出口已抵達。
    // 保留 OnRoomCleared 僅供舊場景過渡，不再用於推進戰役。
    public static event Action<EncounterOutcome> OnObjectiveResolved;
    public static event Action<bool> OnRewardSelectionRequested;
    public static event Action<int> OnRouteSelected;
    public static event Action OnExitReached;
    public static event Action<EncounterPhase> OnEncounterPhaseChanged;

    public static EncounterPhase CurrentEncounterPhase { get; private set; } = EncounterPhase.None;

    // 敵人全滅事件（解鎖出口用，尚未結算）
    public static event Action OnAllEnemiesDefeated;
    // 最終 Boss 專用：只有標記為最終對手的 EnemyController 死亡時才會發送。
    // 不能以「所有敵人全滅」代替，因為最終場日後可安全加入護衛或召喚物。
    public static event Action OnFinalBossDefeated;
    
    // 生存時間結束事件
    public static event Action OnSurvivalTimeUp;
    public static event Action OnTimerExpired;
    
    // 玩家死亡事件
    public static event Action OnPlayerDied;

    // 獎勵選取完成事件（玩家選完卡牌後觸發，DoorController 以此作為第二道解鎖條件）
    public static event Action OnRewardCollected;

    // 遊戲結束，玩家確認返回總部事件
    public static event Action OnReturnToHQConfirmed;
    
    // 選民被轉化成功事件 (int side 陣營: 1=玩家, -1=敵人)
    public static event Action<int> OnVoterConverted;

    // 玩家使用任意技能 (J/K/L) 事件 (SkillData 為施放的技能)
    public static event Action<SkillData> OnAnySkillUsed;

    // 技能真正執行事件（Task 06：供記者監視判斷用）
    // 只在 ExecuteSkill 真正發動後觸發；冷卻未完成、資源不足、前搖被中斷均不觸發。
    // 普通攻擊不在此範圍。
    public static event Action<SkillCastInfo> OnSkillCast;

    // 玩家轉化深色（Dark）選民事件
    public static event Action OnDarkVoterConverted;

    /// <summary>
    /// 當最後一隻怪物死亡，或達成過關條件時呼叫
    /// </summary>
    public static void TriggerRoomCleared()
    {
        OnRoomCleared?.Invoke();
    }

    public static void TriggerObjectiveResolved(EncounterOutcome outcome)
    {
        OnObjectiveResolved?.Invoke(outcome);
    }

    public static void TriggerRewardSelectionRequested(bool useDistressReward)
    {
        OnRewardSelectionRequested?.Invoke(useDistressReward);
    }

    public static void SetEncounterPhase(EncounterPhase phase)
    {
        if (CurrentEncounterPhase == phase) return;
        CurrentEncounterPhase = phase;
        OnEncounterPhaseChanged?.Invoke(phase);
    }

    public static void TriggerRouteSelected(int optionIndex)
    {
        OnRouteSelected?.Invoke(optionIndex);
    }

    public static void TriggerExitReached()
    {
        OnExitReached?.Invoke();
    }

    /// <summary>
    /// 敵人全滅，出口解鎖（玩家尚未走到出口）
    /// </summary>
    public static void TriggerAllEnemiesDefeated()
    {
        OnAllEnemiesDefeated?.Invoke();
    }

    public static void TriggerFinalBossDefeated()
    {
        OnFinalBossDefeated?.Invoke();
    }

    /// <summary>
    /// 當生存房間時間結束時呼叫
    /// </summary>
    public static void TriggerOnSurvivalTimeUp()
    {
        OnSurvivalTimeUp?.Invoke();
    }

    public static void TriggerTimerExpired()
    {
        OnTimerExpired?.Invoke();
    }

    /// <summary>
    /// 當玩家血量歸零時呼叫
    /// </summary>
    public static void TriggerPlayerDied()
    {
        OnPlayerDied?.Invoke();
    }

    /// <summary>
    /// 當玩家在場景中選取完獎勵物件（政策卡或技能）後呼叫。
    /// RewardItemSpawner 在套用卡牌效果後通知 StageClearState，由它決定自動前進或顯示雙門。
    /// </summary>
    public static void TriggerRewardCollected()
    {
        OnRewardCollected?.Invoke();
    }

    /// <summary>
    /// 當玩家在結算面板點擊確認返回總部時呼叫
    /// </summary>
    public static void TriggerReturnToHQConfirmed()
    {
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

    /// <summary>
    /// 當玩家施放任意技能 (J/K/L) 時呼叫
    /// </summary>
    public static void TriggerOnAnySkillUsed(SkillData skill)
    {
        OnAnySkillUsed?.Invoke(skill);
    }

    /// <summary>
    /// 技能真正執行後呼叫（Task 06 監視用）。
    /// 與 OnAnySkillUsed 完全分開；前者是「按下鍵」語意，後者是「技能效果發生」語意。
    /// </summary>
    public static void TriggerOnSkillCast(SkillCastInfo info)
    {
        OnSkillCast?.Invoke(info);
    }

    /// <summary>
    /// 當玩家轉化深色（Dark）選民時呼叫
    /// </summary>
    public static void TriggerOnDarkVoterConverted()
    {
        OnDarkVoterConverted?.Invoke();
    }

    // Fix-B：Play Mode 進入時重置靜態狀態，防止關閉 Domain Reload 的 Editor 跨 Session 殘留
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        CurrentEncounterPhase = EncounterPhase.None;
        // 靜態事件清空（訂閱者本身會在自己的 OnEnable/OnDisable 管理）
        OnRoomCleared              = null;
        OnObjectiveResolved        = null;
        OnRewardSelectionRequested = null;
        OnRouteSelected            = null;
        OnExitReached              = null;
        OnEncounterPhaseChanged    = null;
        OnAllEnemiesDefeated       = null;
        OnFinalBossDefeated        = null;
        OnSurvivalTimeUp           = null;
        OnTimerExpired             = null;
        OnPlayerDied               = null;
        OnRewardCollected          = null;
        OnReturnToHQConfirmed      = null;
        OnVoterConverted           = null;
        OnAnySkillUsed             = null;
        OnSkillCast                = null;
        OnDarkVoterConverted       = null;
    }
}
