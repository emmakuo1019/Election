using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 全局 UI 管理器，負責管理各個遊戲流程狀態對應的 UI 面板開關。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("全螢幕淡入淡出遮罩")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    public GameObject mainMenuPanel;
    public GameObject hqPanel;

    [Tooltip("總部 - 選角介面")]
    [SerializeField] private GameObject candidatePanel;
    
    [Tooltip("總部 - 選技能介面")]
    [SerializeField] private GameObject skillPanel;

    public GameObject gameplayHUDPanel;
    public GameObject gameEndPanel;

    [Header("HUD Sub-Objects")]
    [SerializeField] private GameObject exitPromptPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 強制解鎖並顯示游標，確保在執行檔中不會因為全螢幕或預設行為而消失
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 初始化漸變面板狀態
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    // Main Menu
    public void ShowMainMenu() { if (mainMenuPanel != null) mainMenuPanel.SetActive(true); }
    public void HideMainMenu() { if (mainMenuPanel != null) mainMenuPanel.SetActive(false); }

    // HQ
    public void HideHQPanel() 
    { 
        if (hqPanel != null) hqPanel.SetActive(false); 
        if (candidatePanel != null) candidatePanel.SetActive(false);
        if (skillPanel != null) skillPanel.SetActive(false);
    }

    /// <summary>
    /// Step 1：顯示候選人介紹面板（只有男候選人，直接展示，等玩家按確認）。
    /// candidatePanel 內放候選人名稱、背景介紹、操作提示（[Space] 確認）等文字即可。
    /// </summary>
    public void ShowHQCandidateStep()
    {
        if (hqPanel != null) hqPanel.SetActive(true);
        if (candidatePanel != null) candidatePanel.SetActive(true);
        if (skillPanel != null) skillPanel.SetActive(false);

        Debug.Log("[UIManager] 候選人介紹面板");
    }

    /// <summary>
    /// Step 2：顯示派系選擇面板。
    ///
    /// skillPanel 內需要有 N 個子物件，命名為 FactionOption0, FactionOption1...
    /// 每個 FactionOption 結構：
    ///   ├── Unselected   ← 游標停在此時顯示
    ///   ├── Selected     ← 確認後顯示（目前流程單次確認不使用，保留備用）
    ///   ├── factionName  ← 文字物件
    ///   └── description  ← 文字物件
    /// 游標指向的 option 顯示 Unselected，其他 option 整個隱藏。
    /// </summary>
    public void ShowHQFactionStep(int cursorIndex, FactionData[] factions)
    {
        if (hqPanel != null) hqPanel.SetActive(true);
        if (candidatePanel != null) candidatePanel.SetActive(false);
        if (skillPanel == null) return;

        skillPanel.SetActive(true);

        if (factions == null) return;

        for (int i = 0; i < factions.Length; i++)
        {
            Transform option = skillPanel.transform.Find($"FactionOption{i}");
            if (option == null) continue;

            bool isCursor = (i == cursorIndex);

            // FactionOption 本體開關
            option.gameObject.SetActive(isCursor);

            if (!isCursor) continue;

            // 游標所在的 option：確保 Unselected 開、Selected 關
            Transform unselected = option.Find("Unselected");
            Transform selected   = option.Find("Selected");
            if (unselected != null) unselected.gameObject.SetActive(true);
            if (selected   != null) selected.gameObject.SetActive(false);

            // 同步文字內容（如果有 TMP 或 Text 元件）
            FactionData data = factions[i];
            if (data != null)
            {
                SetChildText(option, "factionName",  data.factionName);
                SetChildText(option, "description",  data.description);
            }
        }

        string logName = (factions.Length > cursorIndex && factions[cursorIndex] != null)
            ? factions[cursorIndex].factionName : "?";
        Debug.Log($"[UIManager] 派系面板：cursor={cursorIndex} ({logName})");
    }

    /// <summary>
    /// 嘗試對子物件上的 TMP_Text 或 UnityEngine.UI.Text 設定文字
    /// </summary>
    private void SetChildText(Transform parent, string childName, string text)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;

        // 優先 TMP
        var tmp = child.GetComponent<TMPro.TMP_Text>();
        if (tmp != null) { tmp.text = text; return; }

        // 退回 Legacy Text
        var legacyText = child.GetComponent<UnityEngine.UI.Text>();
        if (legacyText != null) legacyText.text = text;
    }

    // Gameplay HUD
    public void ShowGameplayHUD() 
    { 
        if (gameplayHUDPanel != null) 
        {
            gameplayHUDPanel.SetActive(true); 
            
            // 重新綁定 HUD 到當前場景的實例
            var hpBar = gameplayHUDPanel.GetComponentInChildren<HPBarUI>(true);
            if (hpBar != null) hpBar.Rebind();

            var mpBar = gameplayHUDPanel.GetComponentInChildren<MPBarUI>(true);
            if (mpBar != null) mpBar.Rebind();

            var voteUI = gameplayHUDPanel.GetComponentInChildren<VoteDisplayUI>(true);
            if (voteUI != null) voteUI.Rebind();

            var timerUI = gameplayHUDPanel.GetComponentInChildren<LevelTimerUI>(true);
            if (timerUI != null) timerUI.Rebind();

            var enemyCounter = gameplayHUDPanel.GetComponentInChildren<EnemyCounterUI>(true);
            if (enemyCounter != null) enemyCounter.Rebind();

            // Rebind 之後重新套用 HUD 布局，確保 mission data 的 hudLayout 設定生效
            // MissionHUDController 掛在 gameplayHUDPanel 底下，用 GetComponentInChildren 取得
            var missionHUD = gameplayHUDPanel.GetComponentInChildren<MissionHUDController>(true);
            if (missionHUD != null) missionHUD.ApplyHUDLayout();
        }
    }
    public void HideGameplayHUD() { if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(false); }

    // Exit Prompt (子物件放在 gameplayHUDPanel 底下，Hide HUD 時自動帶走)
    public void ShowExitPrompt()
    {
        if (exitPromptPanel != null) exitPromptPanel.SetActive(true);
    }

    public void HideExitPrompt()
    {
        if (exitPromptPanel != null) exitPromptPanel.SetActive(false);
    }

    // Game End
    public void ShowGameEndPanel() { if (gameEndPanel != null) gameEndPanel.SetActive(true); }
    public void HideGameEndPanel() { if (gameEndPanel != null) gameEndPanel.SetActive(false); }

    // ==========================================
    // 漸變轉場與流程控制 (Fade & Flow Control)
    // ==========================================

    /// <summary>
    /// 執行畫面淡出，變黑後觸發回呼
    /// </summary>
    /// <param name="duration">漸變所需時間(秒)</param>
    /// <param name="onComplete">淡出完成後執行的動作</param>
    public void FadeOut(float duration, Action onComplete)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("[UIManager] FadeCanvasGroup 未設定！直接執行 onComplete。");
            onComplete?.Invoke();
            return;
        }
        
        StartCoroutine(FadeOutRoutine(duration, onComplete));
    }

    private IEnumerator FadeOutRoutine(float duration, Action onComplete)
    {
        // 防呆：在淡出期間擋住後面所有的 UI 點擊
        fadeCanvasGroup.blocksRaycasts = true;
        
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(timer / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
        
        // 畫面已全黑，執行回呼
        onComplete?.Invoke();
    }

    /// <summary>
    /// 執行畫面淡入 (從全黑變為透明)
    /// </summary>
    /// <param name="duration">漸變所需時間(秒)</param>
    /// <param name="onComplete">淡入完成後執行的動作(可選)</param>
    public void FadeIn(float duration, Action onComplete = null)
    {
        if (fadeCanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        StartCoroutine(FadeInRoutine(duration, onComplete));
    }

    private IEnumerator FadeInRoutine(float duration, Action onComplete)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        
        onComplete?.Invoke();
    }

    // ==========================================
    // 總部 UI 流程連動邏輯 (HQ UI Flow)
    // ==========================================

    /// <summary>
    /// 保留舊有呼叫介面（轉發）
    /// </summary>
    public void ShowCandidateHint() => ShowHQCandidateStep();

    /// <summary>
    /// 保留舊有呼叫介面（轉發）
    /// </summary>
    public void ShowSkillHint() => ShowHQFactionStep(0, null);

    // ==========================================
    // 教學 UI (Tutorial UI)
    // ==========================================

    [Header("Tutorial UI")]
    [Tooltip("教學大對話框 UI 腳本（掛在教學場景的 Canvas 下）")]
    [SerializeField] private TutorialDialogueUI tutorialDialogueUI;

    [Tooltip("教學常駐小提示框 UI 腳本（掛在教學場景的 Canvas 下）")]
    [SerializeField] private TutorialTipsUI tutorialTipsUI;

    [Header("Reward UI")]
    [Tooltip("場景內獎勵物件的說明面板（置中排版）")]
    [SerializeField] private RewardDescriptionUI rewardDescriptionUI;

    /// <summary>
    /// 顯示教學大對話框。
    /// 通常由 TutorialManager 呼叫，也可從外部直接驅動。
    /// </summary>
    public void ShowTutorialDialogue(TutorialStepData step)
    {
        if (tutorialDialogueUI == null)
        {
            Debug.LogWarning("[UIManager] tutorialDialogueUI 未設定，請在 Inspector 綁定。");
            return;
        }
        tutorialDialogueUI.Show(step);
    }

    /// <summary>
    /// 隱藏教學大對話框。
    /// </summary>
    public void HideTutorialDialogue()
    {
        tutorialDialogueUI?.Hide();
    }

    /// <summary>
    /// 顯示（或更新）常駐教學提示框。
    /// </summary>
    public void ShowTutorialTips(TutorialStepData step)
    {
        if (tutorialTipsUI == null)
        {
            Debug.LogWarning("[UIManager] tutorialTipsUI 未設定，請在 Inspector 綁定。");
            return;
        }
        tutorialTipsUI.Show(step);
    }

    /// <summary>
    /// 用純文字顯示常駐教學提示框（不需要 ScriptableObject）。
    /// </summary>
    public void ShowTutorialTipsText(string text, Sprite icon = null)
    {
        if (tutorialTipsUI == null)
        {
            Debug.LogWarning("[UIManager] tutorialTipsUI 未設定，請在 Inspector 綁定。");
            return;
        }
        tutorialTipsUI.ShowText(text, icon);
    }

    /// <summary>
    /// 隱藏常駐教學提示框。
    /// </summary>
    public void HideTutorialTips()
    {
        tutorialTipsUI?.Hide();
    }

    /// <summary>
    /// 強制關閉所有教學 UI（場景切換或跳過教學時使用）。
    /// </summary>
    public void HideAllTutorialUI()
    {
        tutorialDialogueUI?.Hide();
        tutorialTipsUI?.Hide();
    }

    /// <summary>
    /// 場景載入後重新綁定所有場景內的 Tutorial / Reward UI 元件。
    /// UIManager 是 DontDestroyOnLoad，但 TutorialDialogueUI / TutorialTipsUI / RewardDescriptionUI
    /// 都放在戰鬥場景的 Canvas 下，場景切換後舊引用會失效（MissingReference）。
    /// GameplayState.LoadBattleSceneRoutine 在場景載入完成後立即呼叫此方法。
    /// </summary>
    public void RebindTutorialUI()
    {
        tutorialDialogueUI  = FindFirstObjectByType<TutorialDialogueUI>(FindObjectsInactive.Include);
        tutorialTipsUI      = FindFirstObjectByType<TutorialTipsUI>(FindObjectsInactive.Include);
        rewardDescriptionUI = FindFirstObjectByType<RewardDescriptionUI>(FindObjectsInactive.Include);

        if (tutorialDialogueUI == null)
            Debug.LogWarning("[UIManager] RebindTutorialUI：場景中找不到 TutorialDialogueUI。");
        if (tutorialTipsUI == null)
            Debug.LogWarning("[UIManager] RebindTutorialUI：場景中找不到 TutorialTipsUI。");
        if (rewardDescriptionUI == null)
            Debug.LogWarning("[UIManager] RebindTutorialUI：場景中找不到 RewardDescriptionUI。");
    }

    /// <summary>
    /// 取得場景中的 TutorialDialogueUI 實例（供 TutorialManager 使用）。
    /// UIManager 持有的是 RebindTutorialUI 後的場景 instance，確保不會指向 Prefab asset。
    /// </summary>
    public TutorialDialogueUI GetTutorialDialogueUI() => tutorialDialogueUI;

    /// <summary>
    /// 取得場景中的 TutorialTipsUI 實例（供 TutorialManager 使用）。
    /// </summary>
    public TutorialTipsUI GetTutorialTipsUI() => tutorialTipsUI;

    // ── Reward Description UI ─────────────────────────────────────────

    public void ShowRewardDescription(PolicyCardData card)
    {
        rewardDescriptionUI?.Show(card);
    }

    public void HideRewardDescription()
    {
        rewardDescriptionUI?.Hide();
    }
}
