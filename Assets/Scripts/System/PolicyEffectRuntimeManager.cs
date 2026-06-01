using System;
using System.Collections.Generic;
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
    public float IntegrityHp { get; private set; } = 70f;
    public float MaxIntegrityHp { get; private set; } = 100f;
    public float IntegrityHpRatio => MaxIntegrityHp <= 0f ? 0f : IntegrityHp / MaxIntegrityHp;

    private static PolicyEffectRuntimeManager instance;
    private readonly List<string> appliedCardSummaries = new List<string>();

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstanceExists()
    {
        _ = Instance;
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            normal = { textColor = Color.black }
        };

        float lineHeight = 40f;
        float lineCount = appliedCardSummaries.Count == 0 ? 9f : 8f + appliedCardSummaries.Count;
        float startY = Screen.height - 10f - (lineCount * lineHeight);

        float y = startY;
        GUI.Label(new Rect(10f, y, 700f, lineHeight), "政策卡數據", labelStyle);
        y += lineHeight;

        if (appliedCardSummaries.Count == 0)
        {
            GUI.Label(new Rect(10f, y, 700f, lineHeight), "名稱: 尚未選擇政策卡", labelStyle);
            y += lineHeight;
        }
        else
        {
            for (int i = 0; i < appliedCardSummaries.Count; i++)
            {
                GUI.Label(new Rect(10f, y, 700f, lineHeight), $"名稱 {i + 1}: {appliedCardSummaries[i]}", labelStyle);
                y += lineHeight;
            }
        }

        GUI.Label(new Rect(10f, y, 700f, lineHeight), $"攻擊範圍倍率: {AttackRadiusMultiplier:F2}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(10f, y, 700f, lineHeight), $"轉化率加成: {ConvertChanceBonus:+0.00;-0.00;0}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(10f, y, 700f, lineHeight), $"攻擊冷卻增量: {AttackCooldownBonus:+0.00;-0.00;0.00}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(10f, y, 700f, lineHeight), $"流失率: {LoseControlRate:F2}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(10f, y, 700f, lineHeight), $"擴散半徑: {SpreadRadius:F1}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(10f, y, 700f, lineHeight), $"NPC 速度倍率: {GlobalNpcSpeedMultiplier:F2}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(10f, y, 700f, lineHeight), $"誠信值 HP: {IntegrityHp:F0}/{MaxIntegrityHp:F0}", labelStyle);
        y += lineHeight;

        if (SocialAtmosphereManager.Instance != null)
        {
            GUI.Label(new Rect(10f, y, 700f, lineHeight), $"社會風氣值: {SocialAtmosphereManager.Instance.SocialAtmosphere}", labelStyle);
        }
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

        appliedCardSummaries.Add(card.cardName);

        RefreshAllVoterMovement();
        OnEffectsChanged?.Invoke();

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

    public float GetModifiedVoterBaseHp(float baseHp)
    {
        return Mathf.Max(0f, baseHp * IntegrityHpRatio);
    }

    public void SetIntegrityHp(float currentValue)
    {
        IntegrityHp = Mathf.Clamp(currentValue, 0f, MaxIntegrityHp);
        OnEffectsChanged?.Invoke();
    }

    public void AddIntegrityHp(float delta)
    {
        SetIntegrityHp(IntegrityHp + delta);
    }

    public void SetMaxIntegrityHp(float maxValue, bool refillCurrentHp = false)
    {
        MaxIntegrityHp = Mathf.Max(1f, maxValue);

        if (refillCurrentHp)
        {
            IntegrityHp = MaxIntegrityHp;
        }
        else
        {
            IntegrityHp = Mathf.Clamp(IntegrityHp, 0f, MaxIntegrityHp);
        }

        OnEffectsChanged?.Invoke();
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
