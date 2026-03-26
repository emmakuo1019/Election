using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradePanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("資料來源")]
    [SerializeField] private PlayerSkillManager playerSkillManager;

    [Header("UI")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button speechButton;
    [SerializeField] private Button partyButton;
    [SerializeField] private TMP_Text speechStatusText;
    [SerializeField] private TMP_Text partyStatusText;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Start()
    {
        if (panel == null)
        {
            Debug.LogError("panel 未指定！");
            enabled = false;
            return;
        }

        if (playerSkillManager == null)
        {
            Debug.LogError("playerSkillManager 未指定！");
            enabled = false;
            return;
        }

        if (closeButton == null || speechButton == null || partyButton == null ||
            speechStatusText == null || partyStatusText == null)
        {
            Debug.LogError("UpgradePanelUI 的 UI 元件有未指定項目！");
            enabled = false;
            return;
        }

        panel.SetActive(false);

        closeButton.onClick.AddListener(ClosePanel);
        speechButton.onClick.AddListener(OnClickUnlockSpeech);
        partyButton.onClick.AddListener(OnClickUnlockParty);

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);

        if (speechButton != null)
            speechButton.onClick.RemoveListener(OnClickUnlockSpeech);

        if (partyButton != null)
            partyButton.onClick.RemoveListener(OnClickUnlockParty);
    }

    public void OpenPanel()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
        RefreshUI();
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnClickUnlockSpeech()
    {
        playerSkillManager.UnlockSpeech();
        RefreshUI();
    }

    public void OnClickUnlockParty()
    {
        playerSkillManager.UnlockParty();
        RefreshUI();
    }

    public void RefreshUI()
    {
        bool speechUnlocked = playerSkillManager.SpeechUnlocked;
        bool partyUnlocked = playerSkillManager.PartyUnlocked;

        speechStatusText.text = speechUnlocked ? "已解鎖" : "未解鎖";
        partyStatusText.text = partyUnlocked ? "已解鎖" : "未解鎖";

        speechButton.interactable = !speechUnlocked;
        partyButton.interactable = !partyUnlocked;
    }
}