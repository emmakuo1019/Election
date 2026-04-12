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
    [SerializeField] private Text partyStatusText;

    [Header("UI - 技能選擇")]
    [SerializeField] private Button policyDebateButton;
    [SerializeField] private Button emotionalStirringButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Start()
    {
        ValidateReferences();

        panel.SetActive(false);

        closeButton.onClick.AddListener(ClosePanel);
        policyDebateButton.onClick.AddListener(() => OnSelectPartySkill(PlayerSkillManager.PartySkillType.PolicyDebate));
        emotionalStirringButton.onClick.AddListener(() => OnSelectPartySkill(PlayerSkillManager.PartySkillType.EmotionalStirring));

        playerSkillManager.OnPartySkillSelectionRequested += OnPartySkillSelectionRequested;

        UpdateSkillButtonLabels();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);

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

        if (closeButton == null || partyStatusText == null ||
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
        UpdateSkillButtonLabels();
        RefreshUI();
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnPartySkillSelectionRequested()
    {
        if (!IsOpen)
        {
            OpenPanel();
        }
    }

    private void OnSelectPartySkill(PlayerSkillManager.PartySkillType skillType)
    {
        playerSkillManager.SelectPartySkill(skillType);
        RefreshUI();
        Debug.Log($"✅ 政黨技能已選擇: {skillType}");
    }

    public void RefreshUI()
    {
        bool hasPartySkill = playerSkillManager.HasPartySkill;

        partyStatusText.text = hasPartySkill
            ? $"✅ 已選擇技能: {GetSkillDisplayName(playerSkillManager.SelectedPartySkill)}"
            : "請從下方二選一：暈眩對手 / 增加攻擊範圍";

        policyDebateButton.interactable = !hasPartySkill;
        emotionalStirringButton.interactable = !hasPartySkill;
    }

    private void UpdateSkillButtonLabels()
    {
        SetButtonLabel(policyDebateButton, "暈眩對手\n讓範圍內敵人短暫停下");
        SetButtonLabel(emotionalStirringButton, "增加攻擊範圍\n暫時放大普通攻擊範圍");
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }

    private string GetSkillDisplayName(PlayerSkillManager.PartySkillType skillType)
    {
        return skillType switch
        {
            PlayerSkillManager.PartySkillType.PolicyDebate => "暈眩對手",
            PlayerSkillManager.PartySkillType.EmotionalStirring => "增加攻擊範圍",
            _ => "未選擇"
        };
    }
}
