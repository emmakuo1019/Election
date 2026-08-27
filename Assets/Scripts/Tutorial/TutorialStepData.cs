using UnityEngine;

/// <summary>
/// 教學步驟的完成條件。
/// Tips 顯示後，玩家必須完成對應動作才算此步驟結束。
/// </summary>
public enum TutorialCompletionCondition
{
    /// <summary>任意確認鍵即可（預設，維持原有行為）</summary>
    AnyConfirm,

    /// <summary>玩家使用了任意技能（J / K / L）</summary>
    UseAnySkill,

    /// <summary>玩家成功轉化了一個選民（任意陣營）</summary>
    ConvertVoter,

    /// <summary>玩家轉化了一個深色（Dark）選民</summary>
    ConvertDarkVoter,

    /// <summary>玩家走進出口（DoorController 過關）</summary>
    ReachExit,
}

/// <summary>
/// 單一教學步驟的資料定義。
/// 在 Assets/Data/Tutorial/ 資料夾用 CreateAssetMenu 建立。
/// </summary>
[CreateAssetMenu(fileName = "TutorialStep_New", menuName = "Tutorial/Tutorial Step")]
public class TutorialStepData : ScriptableObject
{
    [Header("對話框設定")]
    [Tooltip("競選助理的名稱，顯示於橘色標題欄")]
    public string advisorName = "競選助理";

    [Tooltip("競選助理的立繪，顯示於對話框左下角")]
    public Sprite advisorPortrait;

    [Tooltip("對話內容，每個元素為一行 bullet point")]
    [TextArea(2, 6)]
    public string[] dialogueLines;

    [Header("常駐 Tips 設定")]
    [Tooltip("對話結束後顯示在畫面上的按鍵提示文字，例如：「按 滑鼠左鍵 進行攻擊」")]
    [TextArea(1, 3)]
    public string tipsText;

    [Tooltip("Tips 圖示（可選，例如按鍵圖示）")]
    public Sprite tipsIcon;

    [Header("確認提示文字")]
    [Tooltip("對話框底部的確認提示，預設為「▶ 繼續」")]
    public string confirmHintText = "▶ 繼續";

    [Header("完成條件（Tips 顯示後生效）")]
    [Tooltip("Tips 出現後，玩家需完成此動作才算此教學步驟通過。\n" +
             "AnyConfirm = 維持原本點擊即可，其他選項需要玩家執行對應遊戲動作。")]
    public TutorialCompletionCondition completionCondition = TutorialCompletionCondition.AnyConfirm;

    [Tooltip("完成後更新 Tips 文字（可選，例如「✓ 完成！」），留空則保持原文字")]
    [TextArea(1, 2)]
    public string completedTipsText;
}
