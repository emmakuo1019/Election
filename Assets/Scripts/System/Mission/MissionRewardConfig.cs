using System;
using UnityEngine;

/// <summary>
/// 任務類型對應獎勵設定。
/// 掛在 GameDB 的 GameObject 上（Inspector 綁定），或作為獨立 SO asset。
///
/// 查表邏輯：MissionObjectiveType → 允許的 CardType[]
/// StageClearState 在走獎勵流程時，用此設定從全牌池篩出符合的卡牌。
///
/// 建立路徑：Assets/Data/Mission/
/// </summary>
[CreateAssetMenu(fileName = "MissionRewardConfig", menuName = "Mission/Reward Config")]
public class MissionRewardConfig : ScriptableObject
{
    [Serializable]
    public struct RewardRule
    {
        public MissionObjectiveType objectiveType;
        [Tooltip("此任務類型完成後，獎勵卡牌只從這些 CardType 中抽取")]
        public CardType[] allowedCardTypes;
    }

    public RewardRule[] rules;

    /// <summary>
    /// 查詢指定任務類型允許的 CardType 陣列。
    /// 找不到對應規則時回傳 null（代表不限制，走原本的隨機抽牌邏輯）。
    /// </summary>
    public CardType[] GetAllowedCardTypes(MissionObjectiveType objectiveType)
    {
        if (rules == null) return null;
        foreach (var rule in rules)
        {
            if (rule.objectiveType == objectiveType)
                return rule.allowedCardTypes;
        }
        return null;
    }
}
