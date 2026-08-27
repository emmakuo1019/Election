using UnityEngine;

/// <summary>
/// 單一房間的任務定義。
/// sceneName 決定要載入哪個 Unity 場景；空白時由系統 fallback 至 TestMVP。
/// objectiveType 決定任務目標；targetValue 只在 ReachVotePercent / Survive 時使用。
///
/// 建立路徑：Assets/Data/Mission/
/// 範例：
///   普通關 EliminateAll  → sceneName 空白（用 TestMVP）
///   特殊房（恢復資源）   → sceneName = "TestSpecial"，objectiveType = EliminateAll，targetValue = 0
///   搶票關              → sceneName 空白，objectiveType = ReachVotePercent，targetValue = 60
/// </summary>
[CreateAssetMenu(fileName = "Mission_New", menuName = "Mission/Room Mission")]
public class RoomMissionData : ScriptableObject
{
    [Header("場景設定")]
    [Tooltip("此任務要載入的場景名稱。空白時 fallback 至 TestMVP（普通戰鬥房）")]
    public string sceneName;

    [Header("任務類型（決定圖示語意與結算判斷）")]
    public MissionObjectiveType objectiveType = MissionObjectiveType.EliminateAll;

    [Tooltip("目標數值：ReachVotePercent 填百分比(0-100)；Survive 填秒數；EliminateAll 忽略此欄")]
    public int targetValue = 60;

    [Header("門口圖示")]
    [Tooltip("顯示在門口的任務類型圖示")]
    public Sprite icon;

    [Header("進場任務簡報")]
    [Tooltip("進入關卡後由競選助理說明的任務內容，復用 TutorialStepData")]
    public TutorialStepData briefingStep;

    [Header("敵人生成設定")]
    [Tooltip("此任務的敵人生成配置；留空時 EnemySpawner 使用場景 fallback 設定")]
    public EnemySpawnConfig spawnConfig;
}
