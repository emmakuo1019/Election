using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教學大對話框的顯示邏輯。
///
/// 對應 Prefab 結構（仿參考圖片排版）：
///
///   TutorialDialoguePanel  (CanvasGroup, 根節點)
///   ├── AdvisorPortrait    (Image) — 左下角立繪，超出框外
///   ├── Background         (Image) — 深色半透明主框
///   │   ├── TitleBar       (Image) — 橘色標題欄
///   │   │   └── AdvisorNameText  (TMP_Text)
///   │   ├── LinesContainer (VerticalLayoutGroup)
///   │   │   └── LineText   (TMP_Text, 顯示所有 bullet lines)
///   │   └── ConfirmHint    (TMP_Text) — 「▶ 繼續」
///   └── (可選) BlockerPanel (全螢幕透明 Image, Raycast Target=true)
///
/// 使用方式：
///   dialogueUI.Show(stepData);   // 顯示並開始打字機效果
///   dialogueUI.SkipTypewriter(); // 立即顯示全部文字（玩家第一次點擊）
///   dialogueUI.Hide();           // 淡出隱藏
/// </summary>
public class TutorialDialogueUI : MonoBehaviour
{
    [Header("根節點")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("立繪")]
    [SerializeField] private Image advisorPortraitImage;

    [Header("對話框內容")]
    [SerializeField] private TMP_Text advisorNameText;
    [SerializeField] private TMP_Text linesText;
    [SerializeField] private TMP_Text confirmHintText;

    [Header("打字機設定")]
    [Tooltip("每個字元顯示間隔（秒），0 = 關閉打字機效果")]
    [SerializeField] private float typewriterSpeed = 0.03f;

    [Header("淡入淡出")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.15f;

    // ── 狀態 ──────────────────────────────────────────────────────────

    /// <summary>打字機動畫是否正在播放中</summary>
    public bool IsTyping => typewriterCoroutine != null;

    /// <summary>是否還有下一頁（尚未顯示完所有 dialogueLines）</summary>
    public bool HasNextPage => _currentLineIndex < _lines.Length - 1;

    private Coroutine typewriterCoroutine;
    private Coroutine fadeCoroutine;
    private string fullText = "";

    private string[] _lines = System.Array.Empty<string>();
    private int _currentLineIndex = 0;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        // 初始隱藏
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>
    /// 顯示對話框並填入步驟資料，從第一頁開始打字機效果。
    /// </summary>
    public void Show(TutorialStepData step)
    {
        if (step == null) return;

        // 填入靜態欄位
        if (advisorNameText != null)
            advisorNameText.text = step.advisorName;

        if (advisorPortraitImage != null)
        {
            advisorPortraitImage.sprite = step.advisorPortrait;
            advisorPortraitImage.gameObject.SetActive(step.advisorPortrait != null);
        }

        if (confirmHintText != null)
            confirmHintText.text = step.confirmHintText;

        // 儲存所有頁面，從第一頁開始
        _lines = (step.dialogueLines != null && step.dialogueLines.Length > 0)
            ? step.dialogueLines
            : new string[] { "" };
        _currentLineIndex = 0;

        ShowPage(_currentLineIndex);
        SetVisible(true);
    }

    /// <summary>
    /// 立即顯示當前頁全部文字，停止打字機動畫。
    /// 通常在玩家第一次點擊時呼叫。
    /// </summary>
    public void SkipTypewriter()
    {
        StopTypewriter();
        if (linesText != null)
            linesText.text = fullText;
    }

    /// <summary>
    /// 推進到下一頁。由 TutorialManager 在打字機結束且還有下一頁時呼叫。
    /// </summary>
    public void ShowNextPage()
    {
        if (!HasNextPage) return;
        _currentLineIndex++;
        ShowPage(_currentLineIndex);
    }

    /// <summary>
    /// 隱藏對話框（淡出）。
    /// </summary>
    public void Hide()
    {
        StopTypewriter();
        SetVisible(false);
    }

    // ── 內部方法 ──────────────────────────────────────────────────────

    private void ShowPage(int index)
    {
        fullText = _lines[index] != null ? "• " + _lines[index] : "";

        StopTypewriter();
        if (typewriterSpeed > 0f)
        {
            if (linesText != null) linesText.text = "";
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(fullText));
        }
        else
        {
            if (linesText != null) linesText.text = fullText;
        }
    }

    /// <summary>
    /// 打字機 Coroutine：逐字顯示 text 內容。
    /// </summary>
    private IEnumerator TypewriterRoutine(string text)
    {
        if (linesText == null) yield break;

        linesText.text = "";
        foreach (char c in text)
        {
            linesText.text += c;
            // TMP 支援 richtext 標籤，跳過標籤字元不等待
            if (c != '<' && c != '>')
                yield return new WaitForSecondsRealtime(typewriterSpeed);
        }
        typewriterCoroutine = null;
    }

    private void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(
            visible ? 1f : 0f,
            visible ? fadeInDuration : fadeOutDuration,
            visible
        ));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, bool interactive)
    {
        // 若要顯示，先設為可互動再淡入
        if (interactive)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // 使用 Unscaled，暫停遊戲時也能正常顯示
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        // 淡出完成後關閉互動
        if (!interactive)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        fadeCoroutine = null;
    }
}
