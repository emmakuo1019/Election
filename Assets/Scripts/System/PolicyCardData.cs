using UnityEngine;

public enum PolicyCardType
{
    SpeechRangeUp,
    SpeechPowerUp,
    PartyCooldownDown,
    PolicyDebateRangeUp,
    EmotionalRangeUp,
    VoteBonus
}

[CreateAssetMenu(fileName = "PolicyCard", menuName = "Game/Policy Card")]
public class PolicyCardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public Sprite icon;

    public PolicyCardType cardType;
    public float value;
}