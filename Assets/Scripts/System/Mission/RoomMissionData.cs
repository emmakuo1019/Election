using UnityEngine;

/// <summary>
/// 單一房間的任務定義。
/// 類型決定門口圖示語意與結算後的獎勵池。
/// targetValue 只在需要具體數值的類型（ReachVotePercent、Survive）下使用。
///
/// 建立路徑：Assets/Data/Mission/
/// </summary>
[CreateAssetMenu(fileName = "Mission_New", menuName = "Mission/Room Mission")]
public class RoomMissionData : ScriptableObject
{
    [Header("任務類型（決定圖示語意與獎勵池）")]
    public MissionObjectiveType objectiveType = MissionObjectiveType.EliminateAll;

    [Tooltip("目標數值：ReachVotePercent 填百分比(0-100)；Survive 填秒數；EliminateAll 忽略此欄")]
    public int targetValue = 60;

    [Header("門口圖示")]
    [Tooltip("顯示在門口的任務類型圖示")]
    public Sprite icon;

    [Header("進場任務簡報")]
    [Tooltip("進入關卡後由競選助理說明的任務內容，復用 TutorialStepData")]
    public TutorialStepData briefingStep;
}
