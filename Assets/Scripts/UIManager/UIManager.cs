using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

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
    public GameObject stageClearPanel;
    public GameObject gameEndPanel;

    [Header("HUD Sub-Objects")]
    [SerializeField] private GameObject exitPromptPanel;

    [Header("Stage Clear Sub-Panels")]
    public GameObject stageClearDataPanel;
    public GameObject stageClearRewardPanel;
    public GameObject stageClearSkillPanel;

    [Header("Policy Card Reward")]
    public RewardCardUI rewardCardPrefab;
    public Transform rewardCardContainer;

    public Action<PolicyCardData> OnPolicyCardSelected;

    // 本次結算的獎勵卡牌類型過濾器（null = 不限制）
    // 由 StageClearState 在啟動結算序列前設定，抽完卡後自動清除
    private CardType[] _rewardCardTypeFilter = null;

    private PolicyCardData selectedRewardCard;
    private List<RewardCardUI> generatedRewardCards = new List<RewardCardUI>();

    private int currentRoomNumber;
    private Action onStageClearSequenceComplete;

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
            
            // 重新綁定 HUD 到當前場景的實例 (Bug 2 Fix)
            var hpBar = gameplayHUDPanel.GetComponentInChildren<HPBarUI>(true);
            if (hpBar != null) hpBar.Rebind();

            var mpBar = gameplayHUDPanel.GetComponentInChildren<MPBarUI>(true);
            if (mpBar != null) mpBar.Rebind();

            var voteUI = gameplayHUDPanel.GetComponentInChildren<VoteDisplayUI>(true);
            if (voteUI != null) voteUI.Rebind();

            var timerUI = gameplayHUDPanel.GetComponentInChildren<LevelTimerUI>(true);
            if (timerUI != null) timerUI.Rebind();
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

    // Stage Clear
    public void ShowStageClearPanel() { if (stageClearPanel != null) stageClearPanel.SetActive(true); }
    public void HideStageClearPanel() { if (stageClearPanel != null) stageClearPanel.SetActive(false); }

    // Game End
    public void ShowGameEndPanel() { if (gameEndPanel != null) gameEndPanel.SetActive(true); }
    public void HideGameEndPanel() { if (gameEndPanel != null) gameEndPanel.SetActive(false); }

    // --- Stage Clear Sequence Control ---

    public void StartStageClearSequence(int roomNumber, Action onComplete)
    {
        currentRoomNumber = roomNumber;
        onStageClearSequenceComplete = onComplete;
        
        ShowStageClearPanel();

        // 1. 打開小結算面板(Data)，確保其他先關閉
        if (stageClearDataPanel != null) stageClearDataPanel.SetActive(true);
        if (stageClearRewardPanel != null) stageClearRewardPanel.SetActive(false);
        if (stageClearSkillPanel != null) stageClearSkillPanel.SetActive(false);
    }

    /// <summary>
    /// 設定本次結算獎勵的 CardType 過濾器。
    /// 需在 StartStageClearSequence 之前呼叫；傳 null 代表不限制（走原本隨機邏輯）。
    /// </summary>
    public void SetRewardFilter(CardType[] allowedTypes)
    {
        _rewardCardTypeFilter = allowedTypes;
    }

    public void OnDataPanelContinueClicked()
    {
        // 2. 玩家點擊繼續後，關閉 Data，打開「獎勵面板 (Reward)」
        if (stageClearDataPanel != null) stageClearDataPanel.SetActive(false);
        if (stageClearRewardPanel != null) 
        {
            stageClearRewardPanel.SetActive(true);
            GenerateRewardCards(); // Bug 1 Fix
        }
    }

    private void GenerateRewardCards()
    {
        if (rewardCardContainer == null || rewardCardPrefab == null)
        {
            Debug.LogWarning("[UIManager] RewardCardPrefab 或 RewardCardContainer 未設定！請在 Inspector 綁定。");
            return;
        }

        selectedRewardCard = null;
        generatedRewardCards.Clear();

        // 清除舊卡片
        foreach (Transform child in rewardCardContainer)
        {
            Destroy(child.gameObject);
        }

        // 套用任務獎勵過濾器（null = 不限制）
        List<PolicyCardData> cards;
        if (_rewardCardTypeFilter != null && _rewardCardTypeFilter.Length > 0)
            cards = DrawFilteredPolicyCards(3, _rewardCardTypeFilter);
        else
            cards = GameDB.Instance.Run.DrawRandomPolicyCards(3);

        // 過濾器用完即清，避免影響下一次結算
        _rewardCardTypeFilter = null;

        if (cards.Count > 0)
        {
            foreach (var card in cards)
            {
                var ui = Instantiate(rewardCardPrefab, rewardCardContainer);
                ui.Setup(card, OnRewardCardClicked);
                generatedRewardCards.Add(ui);
            }
        }
        else
        {
            Debug.LogWarning("[UIManager] 可選政策卡不足！");
        }
    }

    /// <summary>
    /// 從牌池中篩出符合 allowedTypes 的卡，再隨機抽 count 張。
    /// 若篩後數量不足，fallback 至不限制的隨機抽牌。
    /// </summary>
    private List<PolicyCardData> DrawFilteredPolicyCards(int count, CardType[] allowedTypes)
    {
        var allCards = GameDB.Instance.Run.DrawRandomPolicyCards(count * 3); // 多抽幾張再篩
        var filtered = new List<PolicyCardData>();
        foreach (var card in allCards)
        {
            foreach (var type in allowedTypes)
            {
                if (card.Type == type) { filtered.Add(card); break; }
            }
            if (filtered.Count >= count) break;
        }

        // 篩後不足 → fallback 至原本不限制的邏輯
        if (filtered.Count < count)
        {
            Debug.Log("[UIManager] 篩選後牌量不足，fallback 至全牌池隨機抽牌");
            return GameDB.Instance.Run.DrawRandomPolicyCards(count);
        }

        return filtered;
    }

    private void OnRewardCardClicked(PolicyCardData card)
    {
        selectedRewardCard = card;

        foreach (var ui in generatedRewardCards)
        {
            if (ui != null)
            {
                ui.SetSelected(ui.GetCard() == card);
            }
        }
    }

    public void OnRewardPanelContinueClicked()
    {
        if (selectedRewardCard == null)
        {
            Debug.LogWarning("[UIManager] 尚未選擇任何政策卡！");
            return;
        }

        // 確認選擇，發送事件給 StageClearState 套用卡片效果（AddPolicyCard 由 StageClearState 負責）
        OnPolicyCardSelected?.Invoke(selectedRewardCard);

        // 3. 玩家點擊繼續後，關閉 Reward
        if (stageClearRewardPanel != null) stageClearRewardPanel.SetActive(false);

        // 4. 檢查 roomNumber，如果是 5 或 10 關，打開「技能選擇面板 (Skill)」
        if (currentRoomNumber == 5 || currentRoomNumber == 10) // 也可以用 currentRoomNumber % 5 == 0 視你的需求而定
        {
            if (stageClearSkillPanel != null) stageClearSkillPanel.SetActive(true);
        }
        else
        {
            // 5. 如果不是，觸發 onComplete
            FinishStageClearSequence();
        }
    }

    public void OnSkillPanelContinueClicked()
    {
        // 玩家技能選擇完畢後
        if (stageClearSkillPanel != null) stageClearSkillPanel.SetActive(false);
        FinishStageClearSequence();
    }

    private void FinishStageClearSequence()
    {
        HideStageClearPanel();
        onStageClearSequenceComplete?.Invoke();
        onStageClearSequenceComplete = null;
    }

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
}
