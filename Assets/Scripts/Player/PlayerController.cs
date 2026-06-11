using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家主控制器，負責管理依賴項、輸入，並組合 StateMachine 來處理狀態邏輯。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
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
        PlayerAttack = GetComponent<PlayerAttack>();
        SkillManager = GetComponent<PlayerSkillManager>();
        
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
        if (dashAction != null)   dashAction.action.performed   += OnDash;
        if (attackAction != null) attackAction.action.performed += OnAttack;
        if (skillJAction != null) skillJAction.action.performed += OnSkillJ;
        if (skillKAction != null) skillKAction.action.performed += OnSkillK;
        if (skillLAction != null) skillLAction.action.performed += OnSkillL;

        if (LevelTimer.Instance != null)
            LevelTimer.Instance.OnTimerEnd += OnGameEnd;
    }

    private void OnDisable()
    {
        if (dashAction != null)   dashAction.action.performed   -= OnDash;
        if (attackAction != null) attackAction.action.performed -= OnAttack;
        if (skillJAction != null) skillJAction.action.performed -= OnSkillJ;
        if (skillKAction != null) skillKAction.action.performed -= OnSkillK;
        if (skillLAction != null) skillLAction.action.performed -= OnSkillL;

        if (LevelTimer.Instance != null)
            LevelTimer.Instance.OnTimerEnd -= OnGameEnd;
    }

    private void OnDash(InputAction.CallbackContext _)    => DashInputThisFrame   = true;
    private void OnAttack(InputAction.CallbackContext _)  => AttackInputThisFrame = true;
    
    private void OnSkillJ(InputAction.CallbackContext _)
    {
        if (SkillManager != null && SkillManager.baseSkillJ != null && SkillManager.CanCastSkill(SkillManager.baseSkillJ))
            StateMachine.ChangeState(new SkillState(this, SkillManager.baseSkillJ));
    }

    private void OnSkillK(InputAction.CallbackContext _)
    {
        if (SkillManager != null && SkillManager.skillK != null && SkillManager.CanCastSkill(SkillManager.skillK))
            StateMachine.ChangeState(new SkillState(this, SkillManager.skillK));
    }

    private void OnSkillL(InputAction.CallbackContext _)
    {
        if (SkillManager != null && SkillManager.skillL != null && SkillManager.CanCastSkill(SkillManager.skillL))
            StateMachine.ChangeState(new SkillState(this, SkillManager.skillL));
    }

    private void Start()
    {
        // 初始化狀態機，給予起始狀態
        StateMachine.Initialize(new IdleState(this));
    }

    private void Update()
    {
        MoveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        // 呼叫當前狀態的 Update
        StateMachine.CurrentState?.Update();

        DashInputThisFrame   = false;
        AttackInputThisFrame = false;
    }

    private void FixedUpdate()
    {
        // 呼叫當前狀態的 PhysicsUpdate
        StateMachine.CurrentState?.PhysicsUpdate();
    }

    public void SetDashCooldown() => _dashReadyTime = Time.time + dashCooldown;

    // 遊戲結束，停用所有輸入（進入無法轉換的空狀態）
    public void OnGameEnd()
    {
        StateMachine.ChangeState(null);
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
