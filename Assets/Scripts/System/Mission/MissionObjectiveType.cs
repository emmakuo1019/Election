/// <summary>
/// 關卡任務類型。
/// 每種類型對應固定的獎勵池與門口圖示，玩家看圖示即可判斷任務內容與可得獎勵。
/// </summary>
public enum MissionObjectiveType
{
    /// <summary>消滅場上所有對手（預設）</summary>
    EliminateAll,

    /// <summary>達到指定的玩家得票率 %（targetValue = 目標百分比，例如 60）</summary>
    ReachVotePercent,

    /// <summary>生存指定秒數（targetValue = 秒數）</summary>
    Survive,
}
