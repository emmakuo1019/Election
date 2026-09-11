using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 驗證工具：檢查 MissionPool 對於每個 MissionChoice 節點是否有足夠的可用任務。
/// </summary>
public class MissionPoolValidator : EditorWindow
{
    [MenuItem("Tools/Validate Mission Pool Coverage")]
    public static void ValidateCoverage()
    {
        // 載入 MissionPool
        string[] guids = AssetDatabase.FindAssets("t:MissionPool");
        if (guids.Length == 0)
        {
            Debug.LogError("[MissionPoolValidator] 找不到 MissionPool 資產！");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        MissionPool pool = AssetDatabase.LoadAssetAtPath<MissionPool>(path);
        if (pool == null)
        {
            Debug.LogError("[MissionPoolValidator] 無法載入 MissionPool！");
            return;
        }

        // 新架構：12 節點
        // 節點 1-5, 7-11 為 MissionChoice（需要至少 2 個可用任務）
        // 節點 6 為 Elite（固定）
        // 節點 12 為 FinalBoss（固定）
        int[] missionChoiceNodes = new int[] { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11 };

        Debug.Log("=== Mission Pool Coverage Report ===");
        Debug.Log($"總任務數：{pool.entries.Count}");
        Debug.Log("");

        bool hasIssues = false;

        foreach (int nodeNumber in missionChoiceNodes)
        {
            int eligibleCount = 0;
            var eligibleMissions = new List<string>();

            foreach (var entry in pool.entries)
            {
                if (entry.mission != null && entry.weight > 0 && entry.mission.IsEligibleForNode(nodeNumber))
                {
                    eligibleCount++;
                    eligibleMissions.Add($"{entry.mission.name} (weight={entry.weight}, range={entry.mission.minCampaignNode}-{entry.mission.maxCampaignNode})");
                }
            }

            string status = eligibleCount >= 2 ? "✓ OK" : "❌ INSUFFICIENT";
            if (eligibleCount < 2)
            {
                hasIssues = true;
                Debug.LogWarning($"節點 {nodeNumber}: {status} - 只有 {eligibleCount} 個可用任務（需要至少 2 個）");
            }
            else
            {
                Debug.Log($"節點 {nodeNumber}: {status} - {eligibleCount} 個可用任務");
            }

            foreach (string mission in eligibleMissions)
            {
                Debug.Log($"  - {mission}");
            }
            Debug.Log("");
        }

        Debug.Log("=== Validation Complete ===");
        if (hasIssues)
        {
            Debug.LogError("發現任務池覆蓋不足的節點！請檢查上方警告訊息。");
        }
        else
        {
            Debug.Log("所有 MissionChoice 節點的任務池覆蓋率正常。");
        }
    }
}
