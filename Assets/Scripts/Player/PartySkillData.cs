using UnityEngine;

public abstract class PartySkillData : ScriptableObject
{
    public string skillName;
    public float baseCooldown;
    public float duration = 1f;
    public string animationTriggerName;

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
}
