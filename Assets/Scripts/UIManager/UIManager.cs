using UnityEngine;

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
}
