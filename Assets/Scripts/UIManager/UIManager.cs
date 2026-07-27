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
    /// Step 1：顯示選角資訊面板。
    /// 你需要在 candidatePanel 內放兩個子物件：maleIndicator（男選中）、femaleIndicator（女選中）。
    /// 根據 isMaleSelected 顯示對應的指示器。
    /// </summary>
    public void ShowHQCandidateStep(bool isMaleSelected)
    {
        if (hqPanel != null) hqPanel.SetActive(true);
        if (candidatePanel != null)
        {
            candidatePanel.SetActive(true);

            // 找子物件 maleIndicator 和 femaleIndicator，控制顯示
            Transform maleIndicator = candidatePanel.transform.Find("MaleIndicator");
            Transform femaleIndicator = candidatePanel.transform.Find("FemaleIndicator");

            if (maleIndicator != null) maleIndicator.gameObject.SetActive(isMaleSelected);
            if (femaleIndicator != null) femaleIndicator.gameObject.SetActive(!isMaleSelected);

            Debug.Log($"[UIManager] 選角面板：{(isMaleSelected ? "男" : "女")} 被選中");
        }

        if (skillPanel != null) skillPanel.SetActive(false);
    }

    /// <summary>
    /// Step 2：顯示技能選擇面板。
    /// skillPanel 內需要有 N 個子物件對應 availableSkills（名稱建議 SkillOption0, SkillOption1...）。
    /// 每個 SkillOption 有兩個子物件：Unselected（未選中）和 Selected（已確認選中）。
    /// </summary>
    public void ShowHQSkillStep(int cursorIndex, bool skillConfirmed, SkillData[] availableSkills)
    {
        if (candidatePanel != null) candidatePanel.SetActive(false);
        if (skillPanel != null)
        {
            skillPanel.SetActive(true);

            // 遍歷所有技能選項 UI
            for (int i = 0; i < availableSkills.Length; i++)
            {
                Transform optionTransform = skillPanel.transform.Find($"SkillOption{i}");
                if (optionTransform == null) continue;

                bool isCursor = (i == cursorIndex);

                Transform unselected = optionTransform.Find("Unselected");
                Transform selected = optionTransform.Find("Selected");

                // 當前游標指向這個選項
                if (isCursor)
                {
                    if (!skillConfirmed)
                    {
                        // 游標高亮但未確認
                        if (unselected != null) unselected.gameObject.SetActive(true);
                        if (selected != null) selected.gameObject.SetActive(false);
                    }
                    else
                    {
                        // 確認選中
                        if (unselected != null) unselected.gameObject.SetActive(false);
                        if (selected != null) selected.gameObject.SetActive(true);
                    }
                }
                else
                {
                    // 非游標選項全部變暗或隱藏
                    if (unselected != null) unselected.gameObject.SetActive(false);
                    if (selected != null) selected.gameObject.SetActive(false);
                }
            }

            Debug.Log($"[UIManager] 技能面板：cursor={cursorIndex}, confirmed={skillConfirmed}");
        }
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

        var cards = GameDB.Instance.Run.DrawRandomPolicyCards(3);
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
    public void ShowCandidateHint() => ShowHQCandidateStep(true);

    /// <summary>
    /// 保留舊有呼叫介面（轉發，無技能資料時的 fallback）
    /// </summary>
    public void ShowSkillHint() => ShowHQSkillStep(0, false, new SkillData[0]);
}
