using UnityEngine;

[CreateAssetMenu(fileName = "PolicyCard", menuName = "Game/Policy Card")]
public class PolicyCardData : ScriptableObject
{
    [Header("基本資料")]
    public string cardName;

    [TextArea(2, 5)]
    public string description;

    [Header("普通攻擊")]
    public float attackRadiusMultiplier = 1f;
    public float convertChanceDelta = 0f;
    public float attackCooldownDelta = 0f;

    [Header("社會風氣 / 深色選民")]
    [Tooltip("正值代表更偏情緒動員，負值代表更偏理性。")]
    public int socialClimateDelta = 0;

    [Header("風險 / 報酬")]
    public float loseControlRateDelta = 0f;
    public float spreadRadius = 0f;
    public float globalNpcSpeedMultiplier = 1f;
}
