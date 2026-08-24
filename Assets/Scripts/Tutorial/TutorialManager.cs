using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 教學系統的核心管理器。
/// 放在教學場景的 GameObject 上（不需要 DontDestroyOnLoad，只活在教學關卡內）。
///
/// 職責：
///   1. 接收 TutorialTrigger 的觸發，呼叫 TutorialDialogueUI 顯示對話
///   2. 對話確認後，呼叫 TutorialTipsUI 顯示常駐按鍵提示
///   3. 管理對話期間的玩家輸入鎖定（可選）
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI 元件參考")]
    [Tooltip("大對話框 UI 腳本")]
    [SerializeField] private TutorialDialogueUI dialogueUI;

    [Tooltip("常駐小提示框 UI 腳本")]
    [SerializeField] private TutorialTipsUI tipsUI;

    [Header("輸入設定")]
    [Tooltip("確認/推進對話的按鍵 (對應 InputSystem Action)")]
    [SerializeField] private InputActionReference confirmAction;

    [Header("選項")]
    [Tooltip("對話框顯示期間，是否暫停遊戲時間（Time.timeScale = 0）")]
    [SerializeField] private bool pauseWhileDialogue = false;

    // 目前正在顯示的步驟資料
    private TutorialStepData currentStep;
    private bool isDialogueOpen = false;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!isDialogueOpen) return;

        // 等待確認輸入（滑鼠左鍵 或 confirmAction）
        bool confirmPressed = Input.GetMouseButtonDown(0);

        if (confirmAction != null)
            confirmPressed |= confirmAction.action.WasPerformedThisFrame();

        if (confirmPressed)
        {
            // 先嘗試推進打字機效果，若已完成則關閉對話
            if (dialogueUI != null && dialogueUI.IsTyping)
                dialogueUI.SkipTypewriter();
            else
                CloseDialogue();
        }
    }

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>
    /// 由 TutorialTrigger 呼叫，顯示指定步驟的對話框。
    /// </summary>
    public void ShowDialogue(TutorialStepData step)
    {
        if (step == null) return;

        currentStep = step;
        isDialogueOpen = true;

        if (pauseWhileDialogue)
            Time.timeScale = 0f;

        dialogueUI?.Show(step);
    }

    /// <summary>
    /// 玩家確認後關閉對話框，並顯示對應的常駐 Tips。
    /// </summary>
    private void CloseDialogue()
    {
        isDialogueOpen = false;

        if (pauseWhileDialogue)
            Time.timeScale = 1f;

        dialogueUI?.Hide();

        // 對話結束後顯示/更新常駐 Tips
        if (currentStep != null && !string.IsNullOrEmpty(currentStep.tipsText))
            tipsUI?.Show(currentStep);

        currentStep = null;
    }

    /// <summary>
    /// 強制關閉所有教學 UI（場景切換或跳過教學時使用）。
    /// </summary>
    public void ForceCloseAll()
    {
        isDialogueOpen = false;
        Time.timeScale = 1f;
        dialogueUI?.Hide();
        tipsUI?.Hide();
        currentStep = null;
    }
}
