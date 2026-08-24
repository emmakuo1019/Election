using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教學常駐小提示框的顯示邏輯。
/// 對話框關閉後出現，持續顯示在畫面角落直到下一段觸發更新內容。
///
/// 對應 Prefab 結構：
///
///   TutorialTipsPanel  (CanvasGroup, 根節點)
///   └── Background     (Image) — 半透明深色小框
///       ├── Icon       (Image) — 按鍵圖示（可選）
///       └── TipsText   (TMP_Text) — 提示文字
///
/// 建議放在畫面右下角或右上角，不遮擋主要遊戲區域。
/// </summary>
public class TutorialTipsUI : MonoBehaviour
{
    [Header("根節點")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("內容元件")]
    [SerializeField] private TMP_Text tipsText;
    [SerializeField] private Image tipsIcon;

    [Header("動畫設定")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;

    [Tooltip("顯示時輕微向上彈入的位移量（px），0 = 關閉）")]
    [SerializeField] private float slideInOffset = 12f;

    // ── 內部狀態 ──────────────────────────────────────────────────────

    private Coroutine fadeCoroutine;
    private RectTransform rectTransform;
    private Vector2 anchoredPosBase;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            anchoredPosBase = rectTransform.anchoredPosition;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>
    /// 顯示（或更新）常駐提示內容。
    /// 若已顯示中，會直接更新文字而不重播淡入動畫。
    /// </summary>
    public void Show(TutorialStepData step)
    {
        if (step == null) return;

        // 更新文字
        if (tipsText != null)
            tipsText.text = step.tipsText;

        // 更新圖示
        if (tipsIcon != null)
        {
            tipsIcon.sprite = step.tipsIcon;
            tipsIcon.gameObject.SetActive(step.tipsIcon != null);
        }

        // 若已經可見（alpha 接近 1），只更新文字不重播動畫
        if (canvasGroup != null && canvasGroup.alpha > 0.9f) return;

        SetVisible(true);
    }

    /// <summary>
    /// 直接用文字字串顯示提示（不需要 ScriptableObject）。
    /// </summary>
    public void ShowText(string text, Sprite icon = null)
    {
        if (tipsText != null)
            tipsText.text = text;

        if (tipsIcon != null)
        {
            tipsIcon.sprite = icon;
            tipsIcon.gameObject.SetActive(icon != null);
        }

        if (canvasGroup != null && canvasGroup.alpha > 0.9f) return;

        SetVisible(true);
    }

    /// <summary>
    /// 淡出隱藏提示框。
    /// </summary>
    public void Hide()
    {
        SetVisible(false);
    }

    // ── 內部方法 ──────────────────────────────────────────────────────

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(
            targetAlpha: visible ? 1f : 0f,
            duration: visible ? fadeInDuration : fadeOutDuration,
            interactive: visible
        ));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, bool interactive)
    {
        if (interactive)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false; // Tips 不擋點擊
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        // 滑入起始位置
        Vector2 startPos = anchoredPosBase + (interactive ? Vector2.down * slideInOffset : Vector2.zero);
        Vector2 endPos   = anchoredPosBase;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = EaseOutQuad(t);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedT);

            if (rectTransform != null && slideInOffset > 0f)
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        if (rectTransform != null)
            rectTransform.anchoredPosition = anchoredPosBase;

        if (!interactive)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        fadeCoroutine = null;
    }

    /// <summary>緩出二次曲線，讓滑入動畫更自然</summary>
    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
