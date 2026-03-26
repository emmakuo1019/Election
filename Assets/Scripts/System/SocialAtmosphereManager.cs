using System;
using UnityEngine;

/// <summary>
/// 社會風氣管理系統
/// 追蹤全球的理性 / 情感傾向
/// 範圍: -100 (情緒動員) ↔ +100 (理性政策)
/// </summary>
public class SocialAtmosphereManager : MonoBehaviour
{
    public static SocialAtmosphereManager Instance { get; private set; }

    [Header("社會風氣範圍")]
    [SerializeField] private int socialAtmosphere = 0;
    [SerializeField] private int minAtmosphere = -100;
    [SerializeField] private int maxAtmosphere = 100;

    [Header("影響效應")]
    [SerializeField] private int policyDebateGain = 3;
    [SerializeField] private int emotionalStirringGain = 3;

    public delegate void AtmosphereChangeDelegate(int oldValue, int newValue);
    public event AtmosphereChangeDelegate OnAtmosphereChanged;

    public int SocialAtmosphere => socialAtmosphere;
    public int MinAtmosphere => minAtmosphere;
    public int MaxAtmosphere => maxAtmosphere;

    public float AtmosphereNormalized =>
        (float)(socialAtmosphere - minAtmosphere) / (maxAtmosphere - minAtmosphere);

    public string GetAtmosphereDescription()
    {
        return socialAtmosphere switch
        {
            < -70 => "🔴 強情緒動員",
            < -30 => "🟠 傾向情感",
            <= 30 => "⚪ 理性與情感平衡",
            <= 70 => "🟢 傾向理性",
            _ => "🟢 強理性政策"
        };
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[SocialAtmosphereManager] 初始化完成");
    }

    public void AddRationalism(int amount)
    {
        ModifyAtmosphere(amount);
    }

    public void AddEmotion(int amount)
    {
        ModifyAtmosphere(-amount);
    }

    private void ModifyAtmosphere(int delta)
    {
        int oldValue = socialAtmosphere;
        socialAtmosphere = Mathf.Clamp(socialAtmosphere + delta, minAtmosphere, maxAtmosphere);

        if (oldValue != socialAtmosphere)
        {
            Debug.Log($"🌍 社會風氣: {oldValue} → {socialAtmosphere} | {GetAtmosphereDescription()}");
            OnAtmosphereChanged?.Invoke(oldValue, socialAtmosphere);
        }
    }

    public void OnSkillUsed(bool isPolicyDebate, int voterCount)
    {
        if (isPolicyDebate)
        {
            AddRationalism(policyDebateGain * voterCount);
        }
        else
        {
            AddEmotion(emotionalStirringGain * voterCount);
        }
    }

    public void ResetAtmosphere()
    {
        int oldValue = socialAtmosphere;
        socialAtmosphere = 0;
        Debug.Log("🔄 社會風氣已重置為中立");
        OnAtmosphereChanged?.Invoke(oldValue, socialAtmosphere);
    }

    public int GetNetTendency() => socialAtmosphere;
    public bool IsRationalTendency() => socialAtmosphere > 0;
    public bool IsEmotionalTendency() => socialAtmosphere < 0;
    public bool IsNeutral() => socialAtmosphere == 0;
}