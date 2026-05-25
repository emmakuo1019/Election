using UnityEngine;

public abstract class PartySkillData : ScriptableObject
{
    public string skillName;
    public float baseCooldown;
    public string animationTriggerName;

    public abstract void Execute(GameObject caster);
}
