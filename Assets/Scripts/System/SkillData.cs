using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Election/SkillData", order = 1)]
public class SkillData : ScriptableObject, ISkillData
{
    [Header("基礎設定")]
    public string skillName;
    [UnityEngine.Serialization.FormerlySerializedAs("baseCooldown")]
    public float cooldown; // 用於冷卻計算
    public float duration = 0.5f; // 狀態鎖定時間
    
    [Header("動畫與表現")]
    public string animationTriggerName;
    public GameObject skillEffectPrefab;
    
    // 實作 ISkillData 介面屬性
    public string AnimationTriggerName => animationTriggerName;
    public float Cooldown => cooldown;
    public float Duration => duration;
    
    // 實作執行邏輯（將原本在 Manager 的實體化邏輯移過來，封裝在自身）
    public virtual void ExecuteSkill(GameObject caster)
    {
        if (skillEffectPrefab != null && caster != null)
        {
            if (PoolManager.HasInstance)
            {
                PoolManager.Instance.Get(skillEffectPrefab, caster.transform.position, caster.transform.rotation);
            }
            else
            {
                Instantiate(skillEffectPrefab, caster.transform.position, caster.transform.rotation);
            }
        }
        Debug.Log($"[SkillData] 執行技能邏輯：{skillName}");
    }

    // 預設為空實作，讓有持續性邏輯（地位、追蹤）的技能覆寫
    public virtual void UpdateSkill(GameObject caster, float deltaTime)
    {
    }
}
