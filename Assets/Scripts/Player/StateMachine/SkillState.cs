using UnityEngine;

/// <summary>
/// 技能狀態：負責管理戰鬥技能 (J, K, L) 的播放流程與時間
/// </summary>
public class SkillState : IState
{
    private readonly PlayerController _ctx;
    private readonly SkillData _skillData;
    private float _timer;

    // 將建構子改為接收統一的 SkillData
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
            // 2. 呼叫 Animator 播放對應的技能動畫 (硬控，不依賴 Trigger 與箭頭)
            if (_ctx.AnimController != null && !string.IsNullOrEmpty(_skillData.AnimationTriggerName))
            {
                _ctx.AnimController.PlaySkillAnimation(_skillData.AnimationTriggerName);
                Debug.Log($"[SkillState] 正在強制播放動畫 State: {_skillData.AnimationTriggerName}");
            }
            else
            {
                if (_ctx.AnimController == null)
                    Debug.LogWarning("[SkillState] ⚠️ _ctx.AnimController 為空！");
                if (string.IsNullOrEmpty(_skillData.AnimationTriggerName))
                    Debug.LogWarning($"[SkillState] ⚠️ {_skillData.skillName} 的 AnimationTriggerName 為空！");
            }

            // 3. 呼叫多型介面執行真正的技能邏輯（生成特效、扣資源等）
            _skillData.ExecuteSkill(_ctx.gameObject);
            
            // 4. 紀錄施放時間並正式進入 CD
            if (_ctx.SkillManager != null)
            {
                _ctx.SkillManager.RecordSkillUse(_skillData);
            }
            
            Debug.Log($"[SkillState] Enter — 施放技能");
        }
    }

    public void Update()
    {
        _timer += Time.deltaTime;

        // 呼叫多型介面每幀更新技能邏輯 (例如：位移、追蹤)
        _skillData?.UpdateSkill(_ctx.gameObject, Time.deltaTime);

        // 透過介面取得該技能的持續時間
        float duration = _skillData != null ? _skillData.Duration : 0.5f;

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
