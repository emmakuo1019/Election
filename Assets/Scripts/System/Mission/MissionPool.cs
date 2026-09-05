using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戰役可用的任務池，支援加權隨機抽取。
/// 掛在 GameDB 的 Inspector 上。
/// 每個 entry 包含 mission + weight，weight 越高出現機率越大。
///
/// 建立路徑：Assets/Data/Mission/
/// </summary>
[CreateAssetMenu(fileName = "MissionPool", menuName = "Mission/Mission Pool")]
public class MissionPool : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public RoomMissionData mission;
        [Tooltip("出現權重，數值越大機率越高（例：普通=10，特殊房=2）")]
        [Min(1)] public int weight;
    }

    [Tooltip("本戰役所有可能出現的任務，至少放 2 個")]
    public List<Entry> entries = new List<Entry>();

    /// <summary>
    /// 加權隨機抽出 count 個任務（允許重複，池不足不重複時 fallback）。
    /// </summary>
    public RoomMissionData[] DrawRandom(int count)
    {
        if (entries == null || entries.Count == 0)
        {
            Debug.LogWarning("[MissionPool] 任務池為空，回傳 null 陣列");
            return new RoomMissionData[count]; // 全 null，DoorController 顯示 fallback 文字
        }

        RoomMissionData[] result = new RoomMissionData[count];

        // 計算總權重
        int totalWeight = 0;
        foreach (var e in entries) totalWeight += e.weight;

        // 追蹤已選的索引，盡量不重複（池夠大時）
        var usedIndices = new List<int>();

        for (int i = 0; i < count; i++)
        {
            // 若所有 entry 都用過，重置（允許重複）
            if (usedIndices.Count >= entries.Count)
                usedIndices.Clear();

            // 重算可用總權重
            int availableWeight = 0;
            for (int j = 0; j < entries.Count; j++)
                if (!usedIndices.Contains(j)) availableWeight += entries[j].weight;

            int roll = Random.Range(0, availableWeight);
            int cumulative = 0;
            for (int j = 0; j < entries.Count; j++)
            {
                if (usedIndices.Contains(j)) continue;
                cumulative += entries[j].weight;
                if (roll < cumulative)
                {
                    result[i] = entries[j].mission;
                    usedIndices.Add(j);
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>以固定 seed 從符合節點資格的任務中抽取不重複選項。</summary>
    public RoomMissionData[] DrawEligible(int count, int nodeNumber, int seed, IReadOnlyList<EncounterResult> history)
    {
        if (entries == null)
        {
            Debug.LogError("[MissionPool] 任務列表為空。");
            return System.Array.Empty<RoomMissionData>();
        }

        var candidates = new List<Entry>();
        foreach (Entry entry in entries)
        {
            if (entry.mission != null && entry.weight > 0 && entry.mission.IsEligibleForNode(nodeNumber))
                candidates.Add(entry);
        }

        if (candidates.Count < count)
        {
            Debug.LogError($"[MissionPool] 第 {nodeNumber} 節點只有 {candidates.Count} 個有效任務，至少需要 {count} 個。");
            return System.Array.Empty<RoomMissionData>();
        }

        var random = new System.Random(seed);
        var result = new RoomMissionData[count];
        for (int draw = 0; draw < count; draw++)
        {
            int totalWeight = 0;
            foreach (Entry entry in candidates) totalWeight += entry.weight;

            int roll = random.Next(totalWeight);
            int cumulative = 0;
            for (int index = 0; index < candidates.Count; index++)
            {
                cumulative += candidates[index].weight;
                if (roll >= cumulative) continue;
                result[draw] = candidates[index].mission;
                candidates.RemoveAt(index);
                break;
            }
        }

        return result;
    }
}
