using UnityEngine;
using UnityEngine.Serialization;
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
    [FormerlySerializedAs("emotionalStirringButton")]
    [SerializeField] private Button dogezaButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Start()
    {
        ValidateReferences();

        panel.SetActive(false);

        closeButton.onClick.AddListener(ClosePanel);
        policyDebateButton.onClick.AddListener(() => OnSelectPartySkill(PlayerSkillManager.PartySkillType.PolicyDebate));
        dogezaButton.onClick.AddListener(() => OnSelectPartySkill(PlayerSkillManager.PartySkillType.Dogeza));

        SetButtonLabel(policyDebateButton, "暈眩對手");
        SetButtonLabel(dogezaButton, "悲情土下座");

        playerSkillManager.OnPartySkillSelectionRequested += OnPartySkillSelectionRequested;
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);

        if (policyDebateButton != null)
            policyDebateButton.onClick.RemoveListener(() => OnSelectPartySkill(PlayerSkillManager.PartySkillType.PolicyDebate));

        if (dogezaButton != null)
            dogezaButton.onClick.RemoveListener(() => OnSelectPartySkill(PlayerSkillManager.PartySkillType.Dogeza));

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
            policyDebateButton == null || dogezaButton == null)
        {
            Debug.LogError("❌ UpgradePanelUI 的 UI 元件有未指定項目！");
            enabled = false;
            return;
        }
    }

    public void OpenPanel()
    {
        Time.timeScale = 0f;
        panel.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (PlayerSkillManager.HasPendingMapSkillSelection() && !PlayerSkillManager.HasSavedPartySkill())
        {
            return;
        }

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
            : "請從下方二選一：暈眩對手 / 悲情土下座";

        policyDebateButton.interactable = !hasPartySkill;
        dogezaButton.interactable = !hasPartySkill;
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
            PlayerSkillManager.PartySkillType.Dogeza => "悲情土下座",
            _ => "未選擇"
        };
    }
}
