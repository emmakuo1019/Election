using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button selectButton;

    private PolicyCardData currentCard;
    private Action<PolicyCardData> onSelected;

    public void Setup(PolicyCardData card, Action<PolicyCardData> callback)
    {
        currentCard = card;
        onSelected = callback;

        titleText.text = card.cardName;
        descriptionText.text = card.description;
        iconImage.sprite = card.icon;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onSelected?.Invoke(currentCard));
    }
}