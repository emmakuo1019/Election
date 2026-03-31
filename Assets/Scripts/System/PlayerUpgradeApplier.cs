using UnityEngine;

public class PlayerUpgradeApplier : MonoBehaviour
{
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PartySkillAttack partySkillAttack;

    public void ApplyCard(PolicyCardData card)
    {
        switch (card.cardType)
        {
            case PolicyCardType.SpeechRangeUp:
                playerAttack.attackRange += card.value;
                playerAttack.UpdateAttackShape(playerAttack.attackRange, playerAttack.attackAngle);
                break;

            case PolicyCardType.SpeechPowerUp:
                playerAttack.attackInfluence += Mathf.RoundToInt(card.value);
                break;

            case PolicyCardType.PartyCooldownDown:
                // 你要在 PartySkillAttack 裡另外做方法
                break;

            case PolicyCardType.PolicyDebateRangeUp:
                // 你要在 PartySkillAttack 裡另外做 Upgrade 方法
                break;

            case PolicyCardType.EmotionalRangeUp:
                // 同上
                break;
        }

        Debug.Log($"📜 已套用政策卡：{card.cardName}");
    }
}