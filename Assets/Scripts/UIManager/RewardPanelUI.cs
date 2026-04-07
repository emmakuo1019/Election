using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardPanelUI : MonoBehaviour
{
    [Header("三個按鈕")]
    public Button rewardBtn01;
    public Button rewardBtn02;
    public Button rewardBtn03;

    [Header("按鈕文字")]
    public Text rewardBtn01Text;
    public Text rewardBtn02Text;
    public Text rewardBtn03Text;

    [Header("系統引用")]
    public RewardPanelController rewardPanelController;
    public PolicyCardManager policyCardManager;
    public LevelFlowController levelFlowController;

    private List<PolicyCardData> currentCards = new List<PolicyCardData>();

    private void Start()
    {
        if (rewardBtn01 == null || rewardBtn02 == null || rewardBtn03 == null)
        {
            Debug.LogWarning("RewardPanelUI：按鈕引用未完整指定");
            return;
        }

        rewardBtn01.onClick.RemoveAllListeners();
        rewardBtn02.onClick.RemoveAllListeners();
        rewardBtn03.onClick.RemoveAllListeners();
        rewardBtn01.onClick.AddListener(() => ClickRewardBtn(0));
        rewardBtn02.onClick.AddListener(() => ClickRewardBtn(1));
        rewardBtn03.onClick.AddListener(() => ClickRewardBtn(2));
    }

    private void OnEnable()
    {
        RefreshRewardChoices();
    }

    public void RefreshRewardChoices()
    {
        if (policyCardManager == null)
        {
            Debug.LogWarning("PolicyCardManager 沒有指定");
            return;
        }

        if (rewardBtn01Text == null || rewardBtn02Text == null || rewardBtn03Text == null)
        {
            Debug.LogWarning("RewardPanelUI：按鈕文字引用未完整指定");
            return;
        }

        currentCards = policyCardManager.GetRandomCards(3);

        if (currentCards.Count < 3)
        {
            Debug.LogWarning("政策卡數量不足，至少需要 3 張卡");
            return;
        }

        rewardBtn01Text.text = currentCards[0].cardName;
        rewardBtn02Text.text = currentCards[1].cardName;
        rewardBtn03Text.text = currentCards[2].cardName;

        Debug.Log("已刷新三張政策卡");
    }

    private void ClickRewardBtn(int index)
    {
        if (index < 0 || index >= currentCards.Count)
        {
            Debug.LogWarning("選卡索引超出範圍");
            return;
        }

        PolicyCardData selectedCard = currentCards[index];

        Debug.Log("玩家選擇了政策卡：" + selectedCard.cardName);
        Debug.Log("卡片效果：" + selectedCard.upgradeType + " / 數值：" + selectedCard.value);

        BattleFlowController.Instance?.OnRewardSelected();

        if (rewardPanelController != null)
        {
            rewardPanelController.HideRewardPanel();
        }

        if (levelFlowController != null)
        {
            levelFlowController.GoToNextLevel();
        }
        else
        {
            Debug.LogWarning("LevelFlowController 沒有指定");
        }
    }
}
