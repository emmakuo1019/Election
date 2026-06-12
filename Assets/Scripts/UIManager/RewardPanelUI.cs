using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardPanelUI : MonoBehaviour
{
    private const string FallbackFontName = "LegacyRuntime.ttf";

    [Header("結算資訊")]
    [SerializeField] private Text supportRateText;
    [SerializeField] private Text supporterCountText;
    [SerializeField] private Text rewardMPText;

    [Header("操作")]
    [SerializeField] private Button continueButton;

    [Header("區塊設定")]
    [Tooltip("請將包住結算資訊的父物件拖曳到這裡")]
    public GameObject settlementPanelContainer;
    [Tooltip("請將包住這3個獎勵按鈕的父物件拖曳到這裡")]
    public GameObject rewardPanelContainer;

    public Button rewardBtn01, rewardBtn02, rewardBtn03;
    public RewardPanelController rewardPanelController;
    public PolicyCardManager policyCardManager;

    private readonly List<PolicyCardData> currentCards = new List<PolicyCardData>();
    private Text rewardBtn01Text;
    private Text rewardBtn02Text;
    private Text rewardBtn03Text;
    private bool isProcessingSelection;
    private bool canClaimReward;
    private bool hasSelectedReward;
    private bool isProceeding;

    void Awake()
    {
        EnsureSettlementUIReferences();
        CacheTextReferences();
        BindButtons();
    }

    private void OnEnable()
    {
        isProcessingSelection = false;
        hasSelectedReward = false;
        isProceeding = false;
    }

    private void Update()
    {
        if (isProceeding) return;

        bool hasAnyContainer = rewardPanelContainer != null || settlementPanelContainer != null;
        if (!hasAnyContainer) return;

        bool rewardClosed = rewardPanelContainer == null || !rewardPanelContainer.activeSelf;
        bool settlementClosed = settlementPanelContainer == null || !settlementPanelContainer.activeSelf;

        if (rewardClosed && settlementClosed)
        {
            isProceeding = true;
            ProceedAutomatically();
        }
    }

    private void CacheTextReferences()
    {
        if (rewardBtn01 != null) rewardBtn01Text = rewardBtn01.GetComponentInChildren<Text>(true);
        if (rewardBtn02 != null) rewardBtn02Text = rewardBtn02.GetComponentInChildren<Text>(true);
        if (rewardBtn03 != null) rewardBtn03Text = rewardBtn03.GetComponentInChildren<Text>(true);
    }

    private void BindButtons()
    {
        if (rewardBtn01 != null)
        {
            rewardBtn01.onClick.RemoveAllListeners();
            rewardBtn01.onClick.AddListener(() => OnRewardClicked(0));
        }

        if (rewardBtn02 != null)
        {
            rewardBtn02.onClick.RemoveAllListeners();
            rewardBtn02.onClick.AddListener(() => OnRewardClicked(1));
        }

        if (rewardBtn03 != null)
        {
            rewardBtn03.onClick.RemoveAllListeners();
            rewardBtn03.onClick.AddListener(() => OnRewardClicked(2));
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    public void ConfigureSettlement(float supportRate, int supporterCount, int totalVoters, int rewardMP, bool canClaimReward)
    {
        StopAllCoroutines();
        EnsureSettlementUIReferences();
        CacheTextReferences();
        this.canClaimReward = canClaimReward;
        hasSelectedReward = false;
        isProcessingSelection = false;

        if (rewardPanelContainer != null)
        {
            rewardPanelContainer.SetActive(canClaimReward);
        }

        if (supportRateText != null)
        {
            supportRateText.text = $"支持率：{supportRate:P0}";
        }

        if (supporterCountText != null)
        {
            supporterCountText.text = $"支持者：{supporterCount}/{Mathf.Max(totalVoters, 0)}";
        }

        if (rewardMPText != null)
        {
            rewardMPText.text = $"回補 MP：+{rewardMP}";
        }

        RefreshRewardChoices();
        UpdateInteractionState();
    }

    public void RefreshRewardChoices()
    {
        if (policyCardManager == null)
        {
            policyCardManager = FindFirstObjectByType<PolicyCardManager>();
        }

        if (policyCardManager == null)
        {
            Debug.LogWarning("RewardPanelUI：找不到 PolicyCardManager，無法刷新獎勵卡");
            SetFallbackTexts("無卡片資料");
            SetButtonsInteractable(false);
            SetContinueInteractable(true);
            return;
        }

        currentCards.Clear();
        currentCards.AddRange(policyCardManager.GetRandomCards(3));

        if (currentCards.Count < 3)
        {
            Debug.LogWarning("RewardPanelUI：可用政策卡不足 3 張");
            SetFallbackTexts("卡片不足");
            SetButtonsInteractable(false);
            SetContinueInteractable(true);
            return;
        }

        SetButtonText(rewardBtn01Text, FormatCardLabel(currentCards[0]));
        SetButtonText(rewardBtn02Text, FormatCardLabel(currentCards[1]));
        SetButtonText(rewardBtn03Text, FormatCardLabel(currentCards[2]));

    }

    private void OnRewardClicked(int index)
    {
        if (isProcessingSelection || !canClaimReward || hasSelectedReward)
        {
            return;
        }

        if (index < 0 || index >= currentCards.Count)
        {
            Debug.LogWarning("RewardPanelUI：點擊的卡片索引超出範圍");
            return;
        }

        PolicyCardData selectedCard = currentCards[index];
        if (selectedCard == null)
        {
            Debug.LogWarning("RewardPanelUI：選到空白政策卡");
            return;
        }

        isProcessingSelection = true;

        PolicyEffectRuntimeManager.Instance?.ApplyCard(selectedCard);
        BattleFlowController.Instance?.OnRewardSelected();
        hasSelectedReward = true;
        isProcessingSelection = false;

        if (rewardPanelContainer != null)
        {
            rewardPanelContainer.SetActive(false);
        }

        UpdateInteractionState();
    }

    private void SetFallbackTexts(string fallbackText)
    {
        SetButtonText(rewardBtn01Text, fallbackText);
        SetButtonText(rewardBtn02Text, fallbackText);
        SetButtonText(rewardBtn03Text, fallbackText);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (rewardBtn01 != null) rewardBtn01.interactable = interactable;
        if (rewardBtn02 != null) rewardBtn02.interactable = interactable;
        if (rewardBtn03 != null) rewardBtn03.interactable = interactable;
    }

    private void SetContinueInteractable(bool interactable)
    {
        if (continueButton != null)
        {
            continueButton.interactable = interactable;
        }
    }

    private string FormatCardLabel(PolicyCardData card)
    {
        if (card == null)
        {
            return "無卡片資料";
        }

        if (string.IsNullOrWhiteSpace(card.description))
        {
            return card.cardName;
        }

        return $"{card.cardName}\n{card.description}";
    }

    private void SetButtonText(Text targetText, string value)
    {
        if (targetText != null)
        {
            targetText.text = value;
            return;
        }

        Debug.LogWarning("RewardPanelUI：按鈕文字元件未找到，無法更新卡片名稱");
    }

    private void UpdateInteractionState()
    {
        bool rewardButtonsEnabled = canClaimReward && !hasSelectedReward;
        bool continueEnabled = !canClaimReward || hasSelectedReward;

        SetButtonsInteractable(rewardButtonsEnabled);
        SetContinueInteractable(continueEnabled);
    }

    private void OnContinueClicked()
    {
        StopAllCoroutines();

        if (canClaimReward && !hasSelectedReward)
        {
            return;
        }

        ProceedAutomatically();
    }

    private void ProceedAutomatically()
    {
        rewardPanelController?.HideRewardPanel();

        RoomClearFlowController roomClearFlowController = FindFirstObjectByType<RoomClearFlowController>(FindObjectsInactive.Include);
        if (roomClearFlowController != null)
        {
            roomClearFlowController.OnContinuePressed();
            return;
        }

        Debug.LogWarning("RewardPanelUI：找不到 RoomClearFlowController，改用出口控制器收尾");
        RoomExitController roomExitController = FindFirstObjectByType<RoomExitController>(FindObjectsInactive.Include);
        roomExitController?.ProceedToNextScene();
    }

    private void EnsureSettlementUIReferences()
    {
        supportRateText ??= FindTextInChildren("SupportRateText");
        supporterCountText ??= FindTextInChildren("SupporterCountText");
        rewardMPText ??= FindTextInChildren("RewardMPText");
        continueButton ??= FindButtonInChildren("ContinueButton");

        if (supportRateText == null)
        {
            supportRateText = CreateAutoText("SupportRateText", new Vector2(0f, 150f));
        }

        if (supporterCountText == null)
        {
            supporterCountText = CreateAutoText("SupporterCountText", new Vector2(0f, 110f));
        }

        if (rewardMPText == null)
        {
            rewardMPText = CreateAutoText("RewardMPText", new Vector2(0f, 70f));
        }

        if (continueButton == null)
        {
            continueButton = CreateAutoContinueButton();
        }
    }

    private Text FindTextInChildren(string objectName)
    {
        Transform child = transform.Find(objectName);
        return child != null ? child.GetComponent<Text>() : null;
    }

    private Button FindButtonInChildren(string objectName)
    {
        Transform child = transform.Find(objectName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private Text CreateAutoText(string objectName, Vector2 anchoredPosition)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(transform, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(420f, 36f);

        Text text = textObject.GetComponent<Text>();
        text.font = LoadFallbackFont();
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        return text;
    }

    private Button CreateAutoContinueButton()
    {
        GameObject buttonObject = new GameObject(
            "ContinueButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(transform, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, -170f);
        rectTransform.sizeDelta = new Vector2(220f, 60f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.93f, 0.82f, 0.44f, 1f);

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.font = LoadFallbackFont();
        label.fontSize = 26;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.text = "繼續";

        return buttonObject.GetComponent<Button>();
    }

    private Font LoadFallbackFont()
    {
        return Resources.GetBuiltinResource<Font>(FallbackFontName);
    }
}
