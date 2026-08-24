using UnityEngine;

/// <summary>
/// 黨內派系資料。每個派系代表一種 Build 流派，由幕後大佬推薦特定政策方向。
/// 在 Project 視窗右鍵 → Create → Election → FactionData 建立新派系。
/// </summary>
[CreateAssetMenu(fileName = "NewFaction", menuName = "Election/FactionData")]
public class FactionData : ScriptableObject
{
    [Header("派系基本資訊")]
    public string factionName;

    [TextArea(2, 4)]
    public string description;

    [Header("起始技能")]
    [Tooltip("選擇此派系後，玩家 J 鍵預設裝備的技能")]
    public SkillData starterSkill;

    [Header("UI 顯示")]
    [Tooltip("派系代表色，用於 UI 高亮")]
    public Color factionColor = Color.white;
}
