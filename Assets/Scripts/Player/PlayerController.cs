using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家主控制器，負責管理依賴項、輸入，並組合 StateMachine 來處理狀態邏輯。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("攻擊設定")]
    public float attackDuration = 0.2f;
    public float moveSpeed = 5f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Input")]
    public InputActionReference moveAction;
    public InputActionReference dashAction;
    public InputActionReference attackAction;
    public InputActionReference skillJAction;
    public InputActionReference skillKAction;
    public InputActionReference skillLAction;

    [Header("Animation")]
    public Animator characterAnimator;

    public CharacterController CharCon { get; private set; }
    public PlayerAttack PlayerAttack { get; private set; }
    public PlayerAnimationController AnimController { get; private set; }
    public Vector2 MoveInput { get; private set; }
    public Vector2 lastFacingDirection => new Vector2(LastMoveDirection.x, LastMoveDirection.z);

    public event Action<Vector3> OnDirectionChanged;
    private Vector3 _lastMoveDirection = Vector3.forward;
    public Vector3 LastMoveDirection
    {
        get => _lastMoveDirection;
        set
        {
            if (value != _lastMoveDirection)
            {
                _lastMoveDirection = value;
                OnDirectionChanged?.Invoke(value);
            }
        }
    }

    public bool DashInputThisFrame { get; private set; }
    public bool AttackInputThisFrame { get; private set; }
    public bool CanDash => Time.time >= _dashReadyTime;
    private float _dashReadyTime;

    public PlayerSkillManager SkillManager { get; private set; }
    
    /// <summary>
    /// 獨立的狀態機實例。
    /// </summary>
    public StateMachine StateMachine { get; private set; }

    // 預先宣告並初始化狀態
    public AttackState AttackState { get; private set; }

    private void Awake()
    {
        CharCon = GetComponent<CharacterController>();
        PlayerAttack = GetComponentInChildren<PlayerAttack>();
        SkillManager = GetComponent<PlayerSkillManager>();
        
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
        }
        
        // 改為 GetComponentInChildren，允許使用者將腳本掛在父物件或子物件(如 PlayerSprite)上
        AnimController = GetComponentInChildren<PlayerAnimationController>();
        if (AnimController == null)
        {
            Debug.LogWarning("[PlayerController] 尚未掛載 PlayerAnimationController 腳本，動畫解耦將暫時失效。");
        }

        // 實例化狀態機與各狀態
        StateMachine = new StateMachine();
        AttackState = new AttackState(this);
    }

    private void OnEnable()
    {
        if (LevelTimer.Instance != null)
            LevelTimer.Instance.OnTimerEnd += OnGameEnd;
    }

    private void OnDisable()
    {
        if (LevelTimer.Instance != null)
            LevelTimer.Instance.OnTimerEnd -= OnGameEnd;
    }

    private void OnDestroy()
    {
    }

    private void OnDash(InputAction.CallbackContext _)
    {
        // 留空，改為在 Update 輪詢
    }
    
    private void OnAttack(InputAction.CallbackContext _)
    {
        // 留空，改為在 Update 輪詢
    }
    
    // 確認當前狀態是否允許施放技能
    private bool IsInActionableState()
    {
        return StateMachine.CurrentState is IdleState || StateMachine.CurrentState is MoveState;
    }

    private void OnSkillJ(InputAction.CallbackContext _)
    {
        if (this == null) return;
        Debug.Log("[PlayerController] 收到技能 J 按鍵輸入！");
        if (!IsInActionableState())
        {
            Debug.LogWarning($"[PlayerController] 當前狀態無法施放技能！當前狀態：{CurrentStateName}");
            return;
        }
        if (SkillManager == null)
        {
            Debug.LogWarning("[PlayerController] SkillManager 為空！");
            return;
        }
        if (SkillManager.baseSkillJ == null)
        {
            Debug.LogWarning("[PlayerController] SkillManager.baseSkillJ 為空！尚未裝備技能。");
            return;
        }
        if (!SkillManager.CanCastSkill(SkillManager.baseSkillJ))
        {
            Debug.LogWarning("[PlayerController] 技能無法施放 (可能在 CD 中或資源不足)！");
            return;
        }
        
        Debug.Log("[PlayerController] 條件達成，準備切換至 SkillState...");
        StateMachine.ChangeState(new SkillState(this, SkillManager.baseSkillJ));
    }

    private void OnSkillK(InputAction.CallbackContext _)
    {
        if (this == null) return;
        if (!IsInActionableState()) return;
        if (SkillManager != null && SkillManager.skillK != null && SkillManager.CanCastSkill(SkillManager.skillK))
            StateMachine.ChangeState(new SkillState(this, SkillManager.skillK));
    }

    private void OnSkillL(InputAction.CallbackContext _)
    {
        if (this == null) return;
        if (!IsInActionableState()) return;
        // L 鍵對應大招 (CurrentPartySkill)
        if (SkillManager != null && SkillManager.CurrentPartySkill != null && SkillManager.CanCastSkill(SkillManager.CurrentPartySkill))
            StateMachine.ChangeState(new SkillState(this, SkillManager.CurrentPartySkill));
    }

    private void Start()
    {
        // 初始化狀態機，給予起始狀態
        StateMachine.Initialize(new IdleState(this));
    }

    private void Update()
    {
        MoveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        // 輪詢輸入，徹底避開 C# Event 殘留的坑
        DashInputThisFrame = !IsTimeUp && dashAction != null && dashAction.action.WasPerformedThisFrame();
        AttackInputThisFrame = !IsTimeUp && attackAction != null && attackAction.action.WasPerformedThisFrame();

        if (skillJAction != null && skillJAction.action.WasPerformedThisFrame() && !IsTimeUp) OnSkillJ(default);
        if (skillKAction != null && skillKAction.action.WasPerformedThisFrame() && !IsTimeUp) OnSkillK(default);
        if (skillLAction != null && skillLAction.action.WasPerformedThisFrame() && !IsTimeUp) OnSkillL(default);

        // 呼叫當前狀態的 Update
        StateMachine.CurrentState?.Update();
    }

    private void FixedUpdate()
    {
        // 呼叫當前狀態的 PhysicsUpdate
        StateMachine.CurrentState?.PhysicsUpdate();
    }

    public void SetDashCooldown() => _dashReadyTime = Time.time + dashCooldown;

    public bool IsTimeUp { get; private set; } = false;

    // 時間結束：停用攻擊/技能，但保留移動讓玩家能走到出口
    public void OnGameEnd()
    {
        IsTimeUp = true;
        StateMachine.ChangeState(new IdleState(this));
    }

    // 房間結算後恢復玩家輸入
    public void ResumeIdle() => StateMachine.ChangeState(new IdleState(this));

    // 由外部（敵人、技能）呼叫，SkillState 期間有霸體不被打斷
    public void ApplyStun(float duration)
    {
        if (StateMachine.CurrentState is SkillState) return;
        StateMachine.ChangeState(new StunState(this, duration));
    }



    public string CurrentStateName => StateMachine.CurrentState?.GetType().Name ?? "None";
}
