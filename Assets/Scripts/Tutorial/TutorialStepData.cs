using UnityEngine;

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
}
