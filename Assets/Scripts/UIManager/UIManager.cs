using UnityEngine;
using System;

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
    public void ShowGameplayHUD() { if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(true); }
    public void HideGameplayHUD() { if (gameplayHUDPanel != null) gameplayHUDPanel.SetActive(false); }

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
        if (stageClearRewardPanel != null) stageClearRewardPanel.SetActive(true);
    }

    public void OnRewardPanelContinueClicked()
    {
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
