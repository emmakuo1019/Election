using UnityEngine;

public abstract class PartySkillData : ScriptableObject, ISkillData
{
    public string skillName;
    public float baseCooldown;
    public float duration = 1f;
    public string animationTriggerName;

    // 實作 ISkillData 介面屬性
    public string AnimationTriggerName => animationTriggerName;
    public float Cooldown => baseCooldown;
    public float Duration => duration;

    public virtual bool CanExecute(GameObject caster, out string failureReason)
    {
        failureReason = string.Empty;
        return true;
    }

    public virtual bool TryConsumeResources(GameObject caster, out string failureReason)
    {
        failureReason = string.Empty;
        return true;
    }

    public abstract void Execute(GameObject caster);

    public virtual void UpdateSkill(GameObject caster, float deltaTime) { }

    // 實作統一的 ExecuteSkill，讓 SkillState 可以呼叫
    public void ExecuteSkill(GameObject caster)
    {
        Execute(caster);
    }
}
