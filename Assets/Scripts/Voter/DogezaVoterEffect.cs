using UnityEngine;

public sealed class DogezaVoterEffect : IVoterSkillEffect
{
    private readonly float stunTime;
    private readonly float convertChance;

    public DogezaVoterEffect(float stunTime, float convertChance)
    {
        this.stunTime = stunTime;
        this.convertChance = Mathf.Clamp01(convertChance);
    }

    public bool ApplyTo(VoterLogic voter)
    {
        if (voter == null || !voter.CanReceiveSkillEffect)
        {
            return false;
        }

        if (voter.Data.HasColdAttribute)
        {
            voter.ConvertColdIdentityToEmotion();
            voter.ApplyTimedStun(stunTime);
            return true;
        }

        if (voter.Data.EmotionLabelCount <= 0)
        {
            return false;
        }

        if (Random.value < convertChance)
        {
            voter.ForceConvertToPlayer();
            return true;
        }
        return false;
    }
}
