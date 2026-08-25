using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戰役可用的任務池。
/// 掛在 GameDB 的 Inspector 上（或由 BattleFlowController 引用）。
/// GenerateNextOptions 從此池中隨機抽出兩個不重複的任務。
///
/// 建立路徑：Assets/Data/Mission/
/// </summary>
[CreateAssetMenu(fileName = "MissionPool", menuName = "Mission/Mission Pool")]
public class MissionPool : ScriptableObject
{
    [Tooltip("本戰役所有可能出現的任務，至少放 2 個")]
    public List<RoomMissionData> missions = new List<RoomMissionData>();

    /// <summary>
    /// 從池中隨機抽出 count 個不重複的任務。
    /// 若池中數量不足，允許重複（ponytail: 上限是 missions.Count < count）。
    /// </summary>
    public RoomMissionData[] DrawRandom(int count)
    {
        if (missions == null || missions.Count == 0)
        {
            Debug.LogWarning("[MissionPool] 任務池為空，回傳 null 陣列");
            return new RoomMissionData[count];  // 全 null，DoorController 顯示 fallback 文字
        }

        // 不足 count 時允許重複，避免 crash
        // ponytail: 上限是 missions.Count == 1 時兩個門會顯示同一個任務
        List<RoomMissionData> pool = new List<RoomMissionData>(missions);
        RoomMissionData[] result = new RoomMissionData[count];

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result[i] = pool[idx];
            if (pool.Count > 1) pool.RemoveAt(idx);  // 有餘量才移除，保證不重複
        }

        return result;
    }
}
