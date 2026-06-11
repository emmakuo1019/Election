using UnityEngine;

/// <summary>
/// 技能狀態：負責管理戰鬥技能 (J, K, L) 的播放流程與時間
/// </summary>
public class SkillState : IState
{
    private readonly PlayerController _ctx;
    private readonly SkillData _skillData;
    private float _timer;

    // 將建構子改為接收 PlayerController 與具體的 SkillData
    public SkillState(PlayerController ctx, SkillData skillData)
    {
        _ctx = ctx;
        _skillData = skillData;
    }

    public void Enter()
    {
        _timer = 0f;

        // 1. 鎖定玩家移動（避免滑步）
        _ctx.CharCon.Move(Vector3.zero);

        if (_skillData != null)
        {
            // 2. 呼叫 Animator 播放對應的技能動畫
            if (_ctx.characterAnimator != null && !string.IsNullOrEmpty(_skillData.animationTriggerName))
            {
                _ctx.characterAnimator.SetTrigger(_skillData.animationTriggerName);
            }

            // 3. 通知 Manager 執行真正的技能邏輯（生成特效、進入 CD 等）
            // 假設您的 _ctx 有公開 _skillManager，或者您可以透過 GetComponent 取得
            var skillManager = _ctx.GetComponent<PlayerSkillManager>();
            if (skillManager != null)
            {
                skillManager.PerformSkill(_skillData);
            }
            
            Debug.Log($"[SkillState] Enter — 施放技能: {_skillData.name}");
        }
    }

    public void Update()
    {
        _timer += Time.deltaTime;

        // 假設 SkillData 也有定義動畫的播放長度 (duration)，如果沒有，可以給一個預設值
        float duration = _skillData != null ? _skillData.cooldown : 0.5f; // 這裡暫用 cooldown 或預設值，建議在 SkillData 新增 duration 欄位

        if (_timer >= duration)
        {
            _ctx.StateMachine.ChangeState(new IdleState(_ctx));
        }
    }

    public void PhysicsUpdate()
    {
    }

    public void Exit()
    {
        Debug.Log("[SkillState] Exit");
    }
}
