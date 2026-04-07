using UnityEngine;
using UnityEngine.UI;

public class UpgradePanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("資料來源")]
    [SerializeField] private PlayerSkillManager playerSkillManager;

    [Header("UI - 主面板")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button speechButton;
    [SerializeField] private Text speechStatusText;
    [SerializeField] private Text partyStatusText;

    [Header("UI - 政黨技能選擇")]
    [SerializeField] private Button policyDebateButton;
    [SerializeField] private Button emotionalStirringButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Start()
    {
        ValidateReferences();

        panel.SetActive(false);

        // 主面板按鈕
        closeButton.onClick.AddListener(ClosePanel);
        speechButton.onClick.AddListener(OnClickUnlockSpeech);

        // 政黨技能選擇按鈕
        policyDebateButton.onClick.AddListener(() => OnSelectPartySkill(PlayerSkillManager.PartySkillType.PolicyDebate));
        emotionalStirringButton.onClick.AddListener(() => OnSelectPartySkill(PlayerSkillManager.PartySkillType.EmotionalStirring));

        // 訂閱 PlayerSkillManager 事件
        playerSkillManager.OnPartySkillSelectionRequested += OnPartySkillSelectionRequested;

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);

        if (speechButton != null)
            speechButton.onClick.RemoveListener(OnClickUnlockSpeech);

        if (policyDebateButton != null)
            policyDebateButton.onClick.RemoveListener(() => OnSelectPartySkill(PlayerSkillManager.PartySkillType.PolicyDebate));

        if (emotionalStirringButton != null)
            emotionalStirringButton.onClick.RemoveListener(() => OnSelectPartySkill(PlayerSkillManager.PartySkillType.EmotionalStirring));

        if (playerSkillManager != null)
            playerSkillManager.OnPartySkillSelectionRequested -= OnPartySkillSelectionRequested;
    }

    private void ValidateReferences()
    {
        if (panel == null)
        {
            Debug.LogError("❌ panel 未指定！");
            enabled = false;
            return;
        }

        if (playerSkillManager == null)
        {
            Debug.LogError("❌ playerSkillManager 未指定！");
            enabled = false;
            return;
        }

        if (closeButton == null || speechButton == null ||
            speechStatusText == null || partyStatusText == null ||
            policyDebateButton == null || emotionalStirringButton == null)
        {
            Debug.LogError("❌ UpgradePanelUI 的 UI 元件有未指定項目！");
            enabled = false;
            return;
        }
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

    private void OnClickUnlockSpeech()
    {
        playerSkillManager.UnlockSpeech();
        RefreshUI();
        Debug.Log("✅ 演說攻擊已解鎖");
    }

    private void OnClickOpenPartySelection()
    {
        if (!playerSkillManager.SpeechUnlocked)
        {
            Debug.LogWarning("⚠️ 需要先解鎖演說攻擊才能選擇政黨技能");
            return;
        }
    }

    private void OnPartySkillSelectionRequested()
    {
        // 長按E時自動打開面板
        if (!IsOpen)
        {
            OpenPanel();
        }
        OnClickOpenPartySelection();
    }

    private void OnSelectPartySkill(PlayerSkillManager.PartySkillType skillType)
    {
        playerSkillManager.SelectPartySkill(skillType);
        RefreshUI();
        Debug.Log($"✅ 政黨技能已選擇: {skillType}");
    }

    public void RefreshUI()
    {
        bool speechUnlocked = playerSkillManager.SpeechUnlocked;
        bool hasPartySkill = playerSkillManager.HasPartySkill;

        speechStatusText.text = speechUnlocked ? "✅ 已解鎖" : "❌ 未解鎖";
        partyStatusText.text = hasPartySkill ? $"✅ 已選擇: {playerSkillManager.SelectedPartySkill}" : "❌ 未選擇";

        speechButton.interactable = !speechUnlocked;
    }
}
