using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 場景內獎勵物件的說明面板（置中排版）。
/// 玩家靠近獎勵物件時由 RewardItem 呼叫 Show()，離開時呼叫 Hide()。
///
/// 掛在 UIManager 的 DontDestroyOnLoad Canvas 下，全局共用一個實例。
///
/// 建議 Prefab 結構：
///   RewardDescriptionPanel  (CanvasGroup, 螢幕置中)
///   └── Background          (Image) — 半透明深色框
///       ├── CardName        (TMP_Text, 置中, 較大字)
///       ├── Description     (TMP_Text, 置中, 較小字)
///       └── InteractHint    (TMP_Text) — 例如「按 E 選擇」
/// </summary>
public class RewardDescriptionUI : MonoBehaviour
{
    [Header("內容元件")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text interactHintText;

    [Header("根節點")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("動畫設定")]
    [SerializeField] private float fadeInDuration  = 0.15f;
    [SerializeField] private float fadeOutDuration = 0.1f;

    [Header("互動提示文字")]
    [SerializeField] private string interactHint = "按 E 選擇";

    private Coroutine _fadeCoroutine;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (interactHintText != null)
            interactHintText.text = interactHint;
    }

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>顯示指定政策卡的說明。</summary>
    public void Show(PolicyCardData card)
    {
        if (card == null) { Hide(); return; }

        if (cardNameText    != null) cardNameText.text    = card.cardName;
        if (descriptionText != null) descriptionText.text = card.description;

        SetVisible(true);
    }

    /// <summary>隱藏說明面板。</summary>
    public void Hide()
    {
        SetVisible(false);
    }

    // ── 內部 ─────────────────────────────────────────────────────────

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(
            visible ? 1f : 0f,
            visible ? fadeInDuration : fadeOutDuration,
            visible
        ));
    }

    private IEnumerator FadeRoutine(float target, float duration, bool interactive)
    {
        float start   = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha          = target;
        canvasGroup.interactable   = interactive;
        canvasGroup.blocksRaycasts = false;  // 說明面板不擋點擊
        _fadeCoroutine = null;
    }
}
