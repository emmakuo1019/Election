using UnityEngine;

public sealed class DogezaVoterEffect : IVoterSkillEffect
{
    private readonly float stunTime;
    private readonly float convertChance;
    private readonly int attackPower;
    private readonly float knockbackDistance;
    private readonly float knockbackDuration;
    private readonly Vector3 attackerPosition;

    public DogezaVoterEffect(float stunTime, float convertChance, int attackPower, float knockbackDistance, float knockbackDuration, Vector3 attackerPosition)
    {
        this.stunTime = stunTime;
        this.convertChance = Mathf.Clamp01(convertChance);
        this.attackPower = attackPower;
        this.knockbackDistance = knockbackDistance;
        this.knockbackDuration = knockbackDuration;
        this.attackerPosition = attackerPosition;
    }

    public bool ApplyTo(VoterLogic voter)
    {
        if (voter == null || !voter.CanReceiveSkillEffect)
        {
            return false;
        }

        // 1. 套用傷害/說服進度 (傳入 default 避免 OnInfluence 內部覆蓋我們後續指定的狀態)
        if (attackPower > 0)
        {
            voter.OnInfluence(attackPower, true, default, true);
        }

        bool appliedEffect = false;

        // 2. 舊有的屬性判定 (冷感解鎖 or 情緒轉化)
        if (voter.Data.HasColdAttribute)
        {
            voter.ConvertColdIdentityToEmotion();
            // 注意：若原先有 StunState，這裡可選擇保留，但為了表現擊退，
            // 稍後的擊退狀態會覆蓋此 StunState，如果希望被擊退，這符合預期。
            voter.ApplyTimedStun(stunTime);
            appliedEffect = true;
        }
        else if (voter.Data.EmotionLabelCount > 0 && Random.value < convertChance)
        {
            voter.ForceConvertToPlayer();
            appliedEffect = true;
        }

        // 3. 處理擊退狀態
        // 確保選民能切換至受擊狀態，並接收指定的擊退距離與時間
        if (knockbackDistance > 0f)
        {
            voter.StateMachine.ChangeState(new VoterHitState(voter, attackerPosition, knockbackDistance, knockbackDuration));
            appliedEffect = true;
        }

        return appliedEffect || (attackPower > 0);
    }
}
