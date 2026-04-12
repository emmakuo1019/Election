using System;
using UnityEngine;

public class PolicyEffectRuntimeManager : MonoBehaviour
{
    public static bool HasInstance => instance != null;

    public static PolicyEffectRuntimeManager Instance
    {
        get
        {
            if (instance == null)
            {
                CreateRuntimeInstance();
            }

            return instance;
        }
    }

    public event Action OnEffectsChanged;

    public float AttackRadiusMultiplier { get; private set; } = 1f;
    public float ConvertChanceBonus { get; private set; }
    public float AttackCooldownBonus { get; private set; }
    public float LoseControlRate { get; private set; }
    public float SpreadRadius { get; private set; }
    public float GlobalNpcSpeedMultiplier { get; private set; } = 1f;

    private static PolicyEffectRuntimeManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ApplyCard(PolicyCardData card)
    {
        if (card == null)
        {
            Debug.LogWarning("⚠️ [PolicyEffectRuntimeManager] 嘗試套用空白政策卡");
            return;
        }

        AttackRadiusMultiplier *= Mathf.Max(0.1f, card.attackRadiusMultiplier);
        ConvertChanceBonus += card.convertChanceDelta;
        AttackCooldownBonus += card.attackCooldownDelta;
        LoseControlRate = Mathf.Max(0f, LoseControlRate + card.loseControlRateDelta);
        SpreadRadius = Mathf.Max(SpreadRadius, card.spreadRadius);
        GlobalNpcSpeedMultiplier *= Mathf.Max(0.1f, card.globalNpcSpeedMultiplier);

        if (card.socialClimateDelta != 0)
        {
            SocialAtmosphereManager.Instance?.ApplyClimateDelta(card.socialClimateDelta);
        }

        RefreshAllVoterMovement();
        OnEffectsChanged?.Invoke();

        Debug.Log(
            $"🎴 [PolicyEffect] 套用卡片 {card.cardName} | " +
            $"範圍 x{AttackRadiusMultiplier:F2}, 轉化率 {ConvertChanceBonus:+0.00;-0.00;0}, " +
            $"冷卻 {AttackCooldownBonus:+0.00;-0.00;0}, 流失 {LoseControlRate:F2}, " +
            $"擴散 {SpreadRadius:F1}, NPC 速度 x{GlobalNpcSpeedMultiplier:F2}");
    }

    public float GetModifiedAttackRange(float baseRange)
    {
        return baseRange * AttackRadiusMultiplier;
    }

    public float GetModifiedAttackCooldown(float baseCooldown)
    {
        return Mathf.Max(0f, baseCooldown + AttackCooldownBonus);
    }

    public float GetModifiedConvertChance(float baseChance)
    {
        return Mathf.Clamp01(baseChance + ConvertChanceBonus);
    }

    private static void CreateRuntimeInstance()
    {
        GameObject runtimeObject = new GameObject(nameof(PolicyEffectRuntimeManager));
        instance = runtimeObject.AddComponent<PolicyEffectRuntimeManager>();
    }

    private void RefreshAllVoterMovement()
    {
        VoterLogic[] voters = FindObjectsByType<VoterLogic>(FindObjectsSortMode.None);
        foreach (VoterLogic voter in voters)
        {
            voter.RefreshMovementSpeed();
        }
    }
}
