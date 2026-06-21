using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 技能狀態：負責管理戰鬥技能 (J, K, L) 的播放流程與時間
/// </summary>
public class SkillState : IState
{
    private readonly PlayerController _ctx;
    private readonly SkillData _skillData;
    private float _timer;

    // --- DogezaSkill 專屬狀態 (State-based Dash) ---
    private Vector3 _dashDirection;
    private HashSet<VoterLogic> _hitVoters;
    private bool _hasHitVoter;
    private float _dashTimer;

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

        // 如果是 DogezaSkill，初始化專用的衝刺狀態
        if (_skillData is DogezaSkill dogeza)
        {
            _hitVoters = new HashSet<VoterLogic>();
            _hasHitVoter = false;
            _dashTimer = 0f;

            if (_ctx.LastMoveDirection.sqrMagnitude > 0.01f)
            {
                _dashDirection = _ctx.LastMoveDirection.normalized;
            }
            else
            {
                _dashDirection = _ctx.transform.forward;
            }
        }

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
        // 處理 DogezaSkill 專屬的衝刺與物理判定
        if (_skillData is DogezaSkill dogeza)
        {
            if (_dashTimer < dogeza.DashDuration)
            {
                // 執行衝刺位移
                _ctx.CharCon.Move(_dashDirection * dogeza.DashSpeed * Time.fixedDeltaTime);
                
                // 範圍偵測與判定
                DetectVotersDuringDash(dogeza);
                
                _dashTimer += Time.fixedDeltaTime;

                // 衝刺剛結束時，如果撞到人則扣除 Integrity HP
                if (_dashTimer >= dogeza.DashDuration && _hasHitVoter && PolicyEffectRuntimeManager.HasInstance)
                {
                    PolicyEffectRuntimeManager.Instance.AddIntegrityHp(-dogeza.HpCost);
                }
            }
        }
    }

    private void DetectVotersDuringDash(DogezaSkill dogeza)
    {
        Collider[] hitColliders = Physics.OverlapSphere(_ctx.transform.position, dogeza.VoterDetectionRadius);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            Collider hitCollider = hitColliders[i];
            if (hitCollider == null) continue;

            GameObject hitObject = hitCollider.attachedRigidbody != null
                ? hitCollider.attachedRigidbody.gameObject
                : hitCollider.gameObject;
                
            if (!hitObject.CompareTag("Voter")) continue;

            VoterLogic voterLogic = hitCollider.GetComponentInParent<VoterLogic>();
            if (voterLogic == null || _hitVoters.Contains(voterLogic)) continue;

            _hitVoters.Add(voterLogic);
            if (voterLogic.ApplySkillEffect(new DogezaVoterEffect(dogeza.StunTime, dogeza.ConvertChance)))
            {
                _hasHitVoter = true;
            }
        }
    }

    public void Exit()
    {
        Debug.Log("[SkillState] Exit");
    }

    public void OnStunned(float duration)
    {
        Debug.Log("[SkillState] 技能期間遭到擊暈！由於已經拔除 Coroutine，衝刺會安全中斷，切換至 StunState。");
        _ctx.StateMachine.ChangeState(new StunState(_ctx, duration));
    }
}
