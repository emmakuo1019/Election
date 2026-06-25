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

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => 
        {
            onSelected?.Invoke(currentCard);
        });
    }

    public PolicyCardData GetCard() => currentCard;

    public void SetSelected(bool isSelected)
    {
        // 變更顏色以表示選中狀態（可依需求修改為啟用 Outline 等其他效果）
        selectButton.image.color = isSelected ? new Color(0.8f, 1f, 0.8f) : Color.white;
    }
}