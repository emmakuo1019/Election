using System;
using System.Collections.Generic;
using UnityEngine;

public enum EncounterNodeRole
{
    Opening,
    MissionChoice,
    Elite,
    FinalBoss,
}

[Serializable]
public class CampaignNodeDefinition
{
    [Range(1, 8)] public int nodeNumber;
    public EncounterNodeRole role;
    [Tooltip("固定節點使用的任務；二選一節點留空。")]
    public RoomMissionData fixedMission;
    [Tooltip("Elite / FinalBoss 可直接指定專用場景。")]
    public string sceneName;
    [Header("Narrative")]
    [Tooltip("進入節點時播放；由 NarrativeDirector 接管對話、運鏡、Timeline 與角色動畫。")]
    public NarrativeBeat onEnterBeat;
    public NarrativeBeat onSuccessBeat;
    public NarrativeBeat onFailureBeat;
    [Tooltip("只在 Elite 節點的結果 Beat 後播放。")]
    public NarrativeBeat afterEliteBeat;
    [Tooltip("只在 FinalBoss 節點的結果 Beat 後播放。")]
    public NarrativeBeat finalEndingBeat;
}

[CreateAssetMenu(fileName = "CampaignDefinition", menuName = "Mission/Campaign Definition")]
public class CampaignDefinition : ScriptableObject
{
    public const int FormalNodeCount = 8;

    [Header("Run")]
    [SerializeField] private int defaultSeed = 20260905;
    [SerializeField] private RoomMissionData tutorialMission;
    [SerializeField] private GameObject routeDoorPrefab;
    [SerializeField] private List<CampaignNodeDefinition> nodes = new List<CampaignNodeDefinition>();

    public int DefaultSeed => defaultSeed;
    public RoomMissionData TutorialMission => tutorialMission;
    public GameObject RouteDoorPrefab => routeDoorPrefab;
    public IReadOnlyList<CampaignNodeDefinition> Nodes => nodes;

    public CampaignNodeDefinition GetNode(int nodeNumber)
    {
        return nodes?.Find(node => node != null && node.nodeNumber == nodeNumber);
    }

    public bool IsValid(out string error)
    {
        if (nodes == null || nodes.Count != FormalNodeCount)
        {
            error = "CampaignDefinition 必須剛好定義 8 個正式節點。";
            return false;
        }

        for (int nodeNumber = 1; nodeNumber <= FormalNodeCount; nodeNumber++)
        {
            CampaignNodeDefinition node = GetNode(nodeNumber);
            if (node == null)
            {
                error = $"缺少第 {nodeNumber} 節點。";
                return false;
            }

            EncounterNodeRole expectedRole = nodeNumber switch
            {
                1 => EncounterNodeRole.Opening,
                2 or 3 or 5 or 6 or 7 => EncounterNodeRole.MissionChoice,
                4 => EncounterNodeRole.Elite,
                FormalNodeCount => EncounterNodeRole.FinalBoss,
                _ => EncounterNodeRole.MissionChoice,
            };
            if (node.role != expectedRole)
            {
                error = $"第 {nodeNumber} 節點角色必須是 {expectedRole}，目前為 {node.role}。";
                return false;
            }

            if (node.role == EncounterNodeRole.MissionChoice && node.fixedMission != null)
            {
                error = $"第 {nodeNumber} 是二選一節點，不能設定固定任務。";
                return false;
            }

            if ((node.role == EncounterNodeRole.Opening || node.role == EncounterNodeRole.Elite) && node.fixedMission == null)
            {
                error = $"固定節點 {nodeNumber} 必須設定任務。";
                return false;
            }

            if (node.role == EncounterNodeRole.FinalBoss && string.IsNullOrWhiteSpace(node.sceneName))
            {
                error = "第 8 節點 FinalBoss 必須設定專用場景。";
                return false;
            }
        }

        if (routeDoorPrefab == null)
        {
            error = "CampaignDefinition 必須指定實際的雙門 Prefab。";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
