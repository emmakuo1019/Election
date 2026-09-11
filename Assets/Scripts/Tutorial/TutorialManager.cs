using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 教學系統的核心管理器。
/// 放在教學場景或一般戰鬥場景的 GameObject 上。
///
/// 職責：
///   1. 接收 TutorialTrigger 的觸發，透過 UIManager 顯示對話框
///   2. 對話確認後，透過 UIManager 顯示常駐按鍵提示
///   3. 若步驟有 completionCondition（非 AnyConfirm），等待玩家完成指定遊戲動作
///   4. 管理對話期間的玩家輸入鎖定（可選）
///
/// 注意：UI 元件（TutorialDialogueUI / TutorialTipsUI）一律透過 UIManager.Instance 存取，
/// 確保使用的是場景中的 instance 而非 Prefab asset 引用。
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("輸入設定")]
    [Tooltip("確認/推進對話的按鍵 (對應 InputSystem Action)")]
    [SerializeField] private InputActionReference confirmAction;

    [Header("選項")]
    [Tooltip("對話框顯示期間，是否暫停遊戲時間（Time.timeScale = 0）")]
    [SerializeField] private bool pauseWhileDialogue = false;

    // 目前正在顯示的步驟資料
    private TutorialStepData currentStep;
    private bool isDialogueOpen = false;

    // Tips 顯示後，是否正在等待玩家完成指定動作
    private bool _isWaitingForAction = false;

    // 動作完成後要顯示的 Tips 文字（在等待期間暫存，因為 currentStep 會被清空）
    private string _pendingCompletedText;

    // ── UI 存取捷徑（一律走 UIManager，確保拿到場景 instance）────────

    private TutorialDialogueUI DialogueUI => UIManager.Instance != null
        ? UIManager.Instance.GetTutorialDialogueUI()
        : null;

    private TutorialTipsUI TipsUI => UIManager.Instance != null
        ? UIManager.Instance.GetTutorialTipsUI()
        : null;

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
        if (Instance == this)
        {
            StopWaitingForAction();
            Instance = null;
        }
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
            var dlg = DialogueUI;
            if (dlg != null && dlg.IsTyping)
            {
                // 打字機進行中 → 跳至當前頁末尾
                dlg.SkipTypewriter();
            }
            else if (dlg != null && dlg.HasNextPage)
            {
                // 當前頁已完成，還有下一頁 → 翻頁
                dlg.ShowNextPage();
            }
            else
            {
                // 最後一頁也完成 → 關閉對話框
                CloseDialogue();
            }
        }
    }

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>
    /// 由 TutorialTrigger 或 GameplayState 呼叫，顯示指定步驟的對話框。
    /// </summary>
    public void ShowDialogue(TutorialStepData step)
    {
        if (step == null) return;

        // 若前一步驟還在等待動作，先取消訂閱（避免殘留）
        StopWaitingForAction();

        currentStep = step;
        isDialogueOpen = true;

        if (pauseWhileDialogue)
            Time.timeScale = 0f;

        DialogueUI?.Show(step);
    }

    /// <summary>
    /// 強制關閉所有教學 UI（場景切換或跳過教學時使用）。
    /// </summary>
    public void ForceCloseAll()
    {
        StopWaitingForAction();
        isDialogueOpen = false;
        Time.timeScale = 1f;
        UIManager.Instance?.HideAllTutorialUI();
        currentStep = null;
    }

    // ── 內部：對話流程 ────────────────────────────────────────────────

    /// <summary>
    /// 玩家確認後關閉對話框，並顯示對應的常駐 Tips。
    /// 若步驟有 completionCondition，Tips 顯示後開始監聽對應事件。
    /// </summary>
    private void CloseDialogue()
    {
        isDialogueOpen = false;

        if (pauseWhileDialogue)
            Time.timeScale = 1f;

        DialogueUI?.Hide();

        // 對話結束後顯示/更新常駐 Tips
        if (currentStep != null && !string.IsNullOrEmpty(currentStep.tipsText))
        {
            TipsUI?.Show(currentStep);

            // 若完成條件不是 AnyConfirm，開始等待玩家執行指定動作
            if (currentStep.completionCondition != TutorialCompletionCondition.AnyConfirm)
                StartWaitingForAction(currentStep.completionCondition, currentStep.completedTipsText);
        }

        currentStep = null;
    }

    // ── 內部：動作監聽 ────────────────────────────────────────────────

    /// <summary>
    /// 根據 condition 訂閱對應的 BattleEventManager 事件，並暫存完成後的提示文字。
    /// </summary>
    private void StartWaitingForAction(TutorialCompletionCondition condition, string completedText)
    {
        _isWaitingForAction = true;
        _pendingCompletedText = completedText;

        switch (condition)
        {
            case TutorialCompletionCondition.UseAnySkill:
                BattleEventManager.OnAnySkillUsed += HandleSkillUsed;
                Debug.Log("[TutorialManager] 等待玩家使用技能...");
                break;

            case TutorialCompletionCondition.ConvertVoter:
                BattleEventManager.OnVoterConverted += HandleVoterConverted;
                Debug.Log("[TutorialManager] 等待玩家轉化選民...");
                break;

            case TutorialCompletionCondition.ConvertDarkVoter:
                BattleEventManager.OnDarkVoterConverted += HandleDarkVoterConverted;
                Debug.Log("[TutorialManager] 等待玩家轉化深色選民...");
                break;

            case TutorialCompletionCondition.ReachExit:
                BattleEventManager.OnRoomCleared += HandleReachExit;
                Debug.Log("[TutorialManager] 等待玩家走進出口...");
                break;
        }
    }

    /// <summary>
    /// 取消所有動作監聽訂閱。
    /// </summary>
    private void StopWaitingForAction()
    {
        if (!_isWaitingForAction) return;

        BattleEventManager.OnAnySkillUsed       -= HandleSkillUsed;
        BattleEventManager.OnVoterConverted     -= HandleVoterConverted;
        BattleEventManager.OnDarkVoterConverted -= HandleDarkVoterConverted;
        BattleEventManager.OnRoomCleared        -= HandleReachExit;

        _isWaitingForAction = false;
        _pendingCompletedText = null;
    }

    /// <summary>
    /// 玩家完成指定動作後的通用處理：取消訂閱，並更新 Tips 文字。
    /// </summary>
    private void OnActionCompleted()
    {
        string completedText = _pendingCompletedText;
        StopWaitingForAction();

        Debug.Log("[TutorialManager] 玩家完成教學動作！");

        // 若有設定完成後的提示文字，更新 Tips
        if (!string.IsNullOrEmpty(completedText))
            TipsUI?.ShowText(completedText);
    }

    // ── 事件回調 ──────────────────────────────────────────────────────

    private void HandleSkillUsed(SkillData skill) => OnActionCompleted();

    private void HandleVoterConverted(int side) => OnActionCompleted();

    private void HandleDarkVoterConverted() => OnActionCompleted();

    private void HandleReachExit()
    {
        OnActionCompleted();
        
        // 教學最後一步完成，觸發房間過關事件（讓 TutorialState 接手顯示選路門）
        Debug.Log("[TutorialManager] 教學最後一步完成，觸發 TriggerRoomCleared");
        BattleEventManager.TriggerRoomCleared();
    }
}
