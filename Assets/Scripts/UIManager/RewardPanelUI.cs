using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardPanelUI : MonoBehaviour
{
    public Button rewardBtn01, rewardBtn02, rewardBtn03;
    public RewardPanelController rewardPanelController;
    public LevelFlowController levelFlowController;
    public PolicyCardManager policyCardManager;

    private readonly List<PolicyCardData> currentCards = new List<PolicyCardData>();
    private Text rewardBtn01Text;
    private Text rewardBtn02Text;
    private Text rewardBtn03Text;

    void Awake()
    {
        CacheTextReferences();
        BindButtons();
    }

    private void OnEnable()
    {
        RefreshRewardChoices();
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
    }

    public void RefreshRewardChoices()
    {
        CacheTextReferences();

        if (policyCardManager == null)
        {
            policyCardManager = FindFirstObjectByType<PolicyCardManager>();
        }

        if (policyCardManager == null)
        {
            Debug.LogWarning("RewardPanelUI：找不到 PolicyCardManager，無法刷新獎勵卡");
            SetFallbackTexts("無卡片資料");
            SetButtonsInteractable(false);
            return;
        }

        currentCards.Clear();
        currentCards.AddRange(policyCardManager.GetRandomCards(3));

        if (currentCards.Count < 3)
        {
            Debug.LogWarning("RewardPanelUI：可用政策卡不足 3 張");
            SetFallbackTexts("卡片不足");
            SetButtonsInteractable(false);
            return;
        }

        SetButtonText(rewardBtn01Text, currentCards[0].cardName);
        SetButtonText(rewardBtn02Text, currentCards[1].cardName);
        SetButtonText(rewardBtn03Text, currentCards[2].cardName);
        SetButtonsInteractable(true);

        Debug.Log("RewardPanelUI：已刷新三張政策卡");
    }

    void OnRewardClicked(int index)
    {
        if (index < 0 || index >= currentCards.Count)
        {
            Debug.LogWarning("RewardPanelUI：點擊的卡片索引超出範圍");
            return;
        }

        PolicyCardData selectedCard = currentCards[index];
        Debug.Log("玩家選擇了政策卡：" + selectedCard.cardName);

        if (rewardPanelController != null)
        {
            rewardPanelController.HideRewardPanel();
        }

        BattleFlowController.Instance?.OnRewardSelected();

        if (levelFlowController != null)
        {
            levelFlowController.GoToNextLevel();
        }
        else
        {
            Debug.LogWarning("RewardPanelUI：levelFlowController 沒有指定");
        }
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

    private void SetButtonText(Text targetText, string value)
    {
        if (targetText != null)
        {
            targetText.text = value;
            return;
        }

        Debug.LogWarning("RewardPanelUI：按鈕文字元件未找到，無法更新卡片名稱");
    }
}
