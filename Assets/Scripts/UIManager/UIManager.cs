using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 全局 UI 管理器，負責管理各個遊戲流程狀態對應的 UI 面板開關。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject characterSelectPanel;
    public GameObject hqPanel;
    public GameObject gameplayHUDPanel;
    public GameObject stageClearPanel;
    public GameObject gameEndPanel;

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
    }

    // Main Menu
    public void ShowMainMenu() { if (mainMenuPanel != null) mainMenuPanel.SetActive(true); }
    public void HideMainMenu() { if (mainMenuPanel != null) mainMenuPanel.SetActive(false); }

    // Character Select
    public void ShowCharacterSelect() { if (characterSelectPanel != null) characterSelectPanel.SetActive(true); }
    public void HideCharacterSelect() { if (characterSelectPanel != null) characterSelectPanel.SetActive(false); }

    // HQ
    public void ShowHQPanel() { if (hqPanel != null) hqPanel.SetActive(true); }
    public void HideHQPanel() { if (hqPanel != null) hqPanel.SetActive(false); }

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
        if (gameplayHUDPanel == null) return;
        var prompt = gameplayHUDPanel.transform.Find("ExitPrompt");
        if (prompt != null) prompt.gameObject.SetActive(true);
    }

    public void HideExitPrompt()
    {
        if (gameplayHUDPanel == null) return;
        var prompt = gameplayHUDPanel.transform.Find("ExitPrompt");
        if (prompt != null) prompt.gameObject.SetActive(false);
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

        PolicyCardManager cardManager = FindAnyObjectByType<PolicyCardManager>();
        if (cardManager != null)
        {
            var cards = cardManager.GetRandomCards(3);
            foreach (var card in cards)
            {
                var ui = Instantiate(rewardCardPrefab, rewardCardContainer);
                ui.Setup(card, OnRewardCardClicked);
                generatedRewardCards.Add(ui);
            }
        }
        else
        {
            Debug.LogWarning("[UIManager] 找不到 PolicyCardManager！");
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

        // 確認選擇，發送事件給 StageClearState 套用卡片效果
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
}
