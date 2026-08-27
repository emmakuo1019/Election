using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Election/SkillData", order = 1)]
public class SkillData : ScriptableObject, ISkillData
{
    [Header("基礎設定")]
    public string skillName;
    [UnityEngine.Serialization.FormerlySerializedAs("baseCooldown")]
    public float cooldown;
    public float duration = 0.5f;

    [Header("資源消耗")]
    [Tooltip("施放消耗資金（MP），0 = 免費")]
    public int mpCost = 0;
    [Tooltip("施放消耗誠信（HP），0 = 免費，通常用於高風險技能")]
    public float hpCost = 0f;

    [Header("社會風氣影響")]
    [Tooltip("正值偏情緒動員，負值偏理性。0 = 無影響")]
    public int socialClimateDelta = 0;

    [Header("派系歸屬")]
    [Tooltip("所屬派系（null = 通用）")]
    public FactionData faction;

    [Header("稀有度")]
    public CardRarity Rarity = CardRarity.Common;

    [Header("動畫與表現")]
    public string animationTriggerName;
    [UnityEngine.Serialization.FormerlySerializedAs("skillEffectPrefab")]
    public GameObject vfxPrefab;
    public float vfxDuration = 0f;

    // 實作 ISkillData 介面屬性
    public string AnimationTriggerName => animationTriggerName;
    public float Cooldown => cooldown;
    public float Duration => duration;

    public virtual void ExecuteSkill(GameObject caster)
    {
        if (vfxPrefab != null && caster != null)
        {
            GameObject effectInstance = null;
            if (PoolManager.HasInstance)
            {
                effectInstance = PoolManager.Instance.Get(vfxPrefab, caster.transform.position, caster.transform.rotation);
            }
            else
            {
                effectInstance = Instantiate(vfxPrefab, caster.transform.position, caster.transform.rotation);
            }

            if (effectInstance != null && effectInstance.TryGetComponent<PooledVFXInstance>(out var vfxInstance))
            {
                vfxInstance.duration = vfxDuration;
            }
        }
        Debug.Log($"[SkillData] 執行技能邏輯：{skillName}");
    }

    public virtual void UpdateSkill(GameObject caster, float deltaTime)
    {
    }
}
