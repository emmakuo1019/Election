using UnityEngine;

public class SkillState : IPlayerState
{
    public enum SkillSlot { J, K, L }

    private readonly PlayerStateMachine _ctx;
    private readonly SkillSlot _slot;
    private float _endTime;

    public SkillState(PlayerStateMachine ctx, SkillSlot slot)
    {
        _ctx = ctx;
        _slot = slot;
    }

    public void Enter()
    {
        float duration = _ctx.UseSkill(_slot);
        _endTime = Time.time + duration;
        Debug.Log($"[SkillState] Enter — {_slot}, duration: {duration}");
    }

    public void Update()
    {
        if (Time.time >= _endTime)
            _ctx.ChangeState(new IdleState(_ctx));
    }

    public void Exit()
    {
        Debug.Log("[SkillState] Exit");
    }
}
