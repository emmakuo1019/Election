using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UpgradePanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("資料來源")]
    [SerializeField] private PlayerSkillManager playerSkillManager;

    [Header("技能資料")]
    [SerializeField] private PartySkillData policyDebateSkill;
    [SerializeField] private PartySkillData dogezaSkill;

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
        ResolveReferences();
        ValidateReferences();
        EnsureDefaultSkills();

        panel.SetActive(false);

        closeButton.onClick.AddListener(ClosePanel);
        policyDebateButton.onClick.AddListener(() => OnSelectPartySkill(policyDebateSkill));
        dogezaButton.onClick.AddListener(() => OnSelectPartySkill(dogezaSkill));

        SetButtonLabel(policyDebateButton, "暈眩對手");
        SetButtonLabel(dogezaButton, "悲情土下座");

        if (playerSkillManager != null)
        {
            playerSkillManager.OnPartySkillSelectionRequested += OnPartySkillSelectionRequested;
        }
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);

        if (policyDebateButton != null)
            policyDebateButton.onClick.RemoveListener(() => OnSelectPartySkill(policyDebateSkill));

        if (dogezaButton != null)
            dogezaButton.onClick.RemoveListener(() => OnSelectPartySkill(dogezaSkill));

        if (playerSkillManager != null)
            playerSkillManager.OnPartySkillSelectionRequested -= OnPartySkillSelectionRequested;
    }

    private void ResolveReferences()
    {
        if (playerSkillManager == null)
        {
            playerSkillManager = FindFirstObjectByType<PlayerSkillManager>(FindObjectsInactive.Include);
        }
    }

    private void EnsureDefaultSkills()
    {
        if (dogezaSkill == null)
        {
            DogezaSkill runtimeDogezaSkill = ScriptableObject.CreateInstance<DogezaSkill>();
            runtimeDogezaSkill.skillName = "悲情土下座";
            runtimeDogezaSkill.baseCooldown = 5f;
            runtimeDogezaSkill.animationTriggerName = "Begging";
            dogezaSkill = runtimeDogezaSkill;
        }
    }

    private void ValidateReferences()
    {
        if (panel == null)
        {
            Debug.LogError("❌ panel 未指定！");
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
        bool hasPartySkill = playerSkillManager != null
            ? playerSkillManager.HasPartySkill
            : PlayerSkillManager.HasEquippedPartySkill;

        if (PlayerSkillManager.HasPendingMapSkillSelection() && !hasPartySkill)
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

    private void OnSelectPartySkill(PartySkillData skillData)
    {
        if (skillData == null)
        {
            Debug.LogWarning("⚠️ 尚未指定技能資料，無法裝備。");
            return;
        }

        if (playerSkillManager != null)
        {
            playerSkillManager.EquipPartySkill(skillData);
        }
        else
        {
            PlayerSkillManager.SetEquippedPartySkill(skillData);
        }

        ClosePanel();
        RefreshUI();
    }

    public void RefreshUI()
    {
        bool hasPartySkill = playerSkillManager != null
            ? playerSkillManager.HasPartySkill
            : PlayerSkillManager.HasEquippedPartySkill;
        PartySkillData equippedSkill = playerSkillManager != null
            ? playerSkillManager.CurrentPartySkill
            : PlayerSkillManager.EquippedPartySkill;

        partyStatusText.text = hasPartySkill
            ? $"✅ 已選擇技能: {GetSkillDisplayName(equippedSkill)}"
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

    private string GetSkillDisplayName(PartySkillData skillData)
    {
        return skillData != null && !string.IsNullOrWhiteSpace(skillData.skillName)
            ? skillData.skillName
            : "未選擇";
    }
}
