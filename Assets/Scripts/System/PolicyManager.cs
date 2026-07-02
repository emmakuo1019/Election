using System;
using System.Collections.Generic;
using UnityEngine;

public class PolicyManager : MonoBehaviour
{
    public static bool HasInstance => Instance != null;
    public static PolicyManager Instance { get; private set; }

    public float AttackRadiusMultiplier { get; private set; } = 1f;
    public float ConvertChanceBonus { get; private set; }
    public float AttackCooldownBonus { get; private set; }
    public float LoseControlRate { get; private set; }
    public float SpreadRadius { get; private set; }
    public float GlobalNpcSpeedMultiplier { get; private set; } = 1f;

    private readonly List<string> appliedCardSummaries = new List<string>();

    public event Action OnEffectsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ApplyCard(PolicyCardData card)
    {
        if (card == null)
        {
            Debug.LogWarning("⚠️ [PolicyManager] 嘗試套用空白政策卡");
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
            GameDB.Instance?.Run.ModifyAtmosphere(card.socialClimateDelta);
        }

        appliedCardSummaries.Add(card.cardName);

        RefreshAllVoterMovement();
        OnEffectsChanged?.Invoke();
    }

    public float GetModifiedAttackRange(float baseRange) => baseRange * AttackRadiusMultiplier;
    public float GetModifiedAttackCooldown(float baseCooldown) => Mathf.Max(0f, baseCooldown + AttackCooldownBonus);
    public float GetModifiedConvertChance(float baseChance) => Mathf.Clamp01(baseChance + ConvertChanceBonus);

    private void RefreshAllVoterMovement()
    {
        VoterLogic[] voters = FindObjectsByType<VoterLogic>(FindObjectsSortMode.None);
        foreach (VoterLogic voter in voters)
        {
            voter.RefreshMovementSpeed();
        }
    }

    public void ResetPolicies()
    {
        AttackRadiusMultiplier = 1f;
        ConvertChanceBonus = 0f;
        AttackCooldownBonus = 0f;
        LoseControlRate = 0f;
        SpreadRadius = 0f;
        GlobalNpcSpeedMultiplier = 1f;
        appliedCardSummaries.Clear();
        OnEffectsChanged?.Invoke();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            normal = { textColor = Color.black },
            alignment = TextAnchor.MiddleRight
        };

        float lineHeight = 40f;
        float lineCount = appliedCardSummaries.Count == 0 ? 9f : 8f + appliedCardSummaries.Count;
        float startY = Screen.height - 10f - (lineCount * lineHeight);
        float rectWidth = 700f;
        float startX = Screen.width - rectWidth - 20f;
        float y = startY;

        GUI.Label(new Rect(startX, y, rectWidth, lineHeight), "政策卡數據", labelStyle);
        y += lineHeight;

        if (appliedCardSummaries.Count == 0)
        {
            GUI.Label(new Rect(startX, y, rectWidth, lineHeight), "名稱: 尚未選擇政策卡", labelStyle);
            y += lineHeight;
        }
        else
        {
            for (int i = 0; i < appliedCardSummaries.Count; i++)
            {
                GUI.Label(new Rect(startX, y, rectWidth, lineHeight), $"名稱 {i + 1}: {appliedCardSummaries[i]}", labelStyle);
                y += lineHeight;
            }
        }

        GUI.Label(new Rect(startX, y, rectWidth, lineHeight), $"攻擊範圍倍率: {AttackRadiusMultiplier:F2}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(startX, y, rectWidth, lineHeight), $"轉化率加成: {ConvertChanceBonus:+0.00;-0.00;0}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(startX, y, rectWidth, lineHeight), $"攻擊冷卻增量: {AttackCooldownBonus:+0.00;-0.00;0.00}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(startX, y, rectWidth, lineHeight), $"流失率: {LoseControlRate:F2}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(startX, y, rectWidth, lineHeight), $"擴散半徑: {SpreadRadius:F1}", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(startX, y, rectWidth, lineHeight), $"NPC 速度倍率: {GlobalNpcSpeedMultiplier:F2}", labelStyle);
        y += lineHeight;

        if (GameDB.Instance != null)
        {
            GUI.Label(new Rect(startX, y, rectWidth, lineHeight), $"誠信值 HP: {GameDB.Instance.Run.IntegrityHp:F0}/{GameDB.Instance.Run.MaxIntegrityHp:F0}", labelStyle);
            y += lineHeight;
            GUI.Label(new Rect(startX, y, rectWidth, lineHeight), $"社會風氣值: {GameDB.Instance.Run.SocialAtmosphere}", labelStyle);
        }
    }
}
