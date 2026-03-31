using System.Collections.Generic;
using UnityEngine;

public class UpgradeRewardUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private RewardCardUI[] cardSlots;
    [SerializeField] private PlayerUpgradeApplier upgradeApplier;

    private List<PolicyCardData> currentCards = new();

    public void OpenRewardPanel()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;

        currentCards = PolicyCardManager.Instance.GetRandomCards(3);

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (i < currentCards.Count)
                cardSlots[i].Setup(currentCards[i], OnCardSelected);
        }
    }

    private void OnCardSelected(PolicyCardData card)
    {
        upgradeApplier.ApplyCard(card);

        panel.SetActive(false);
        Time.timeScale = 1f;

        BattleFlowController.Instance?.OnRewardSelected();
    }
}