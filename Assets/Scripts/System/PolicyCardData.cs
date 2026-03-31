using UnityEngine;

[CreateAssetMenu(fileName = "PolicyCard", menuName = "Game/Policy Card")]
public class PolicyCardData : ScriptableObject
{
    [Header("基本資料")]
    public string cardName;

    [TextArea(2, 5)]
    public string description;

    [Header("效果資料")]
    public PolicyUpgradeType upgradeType;
    public float value = 1f;
}