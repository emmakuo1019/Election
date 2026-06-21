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
    public bool SkillJInputThisFrame { get; private set; }
    public bool SkillKInputThisFrame { get; private set; }
    public bool SkillLInputThisFrame { get; private set; }

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

        SkillJInputThisFrame = !IsTimeUp && skillJAction != null && skillJAction.action.WasPerformedThisFrame();
        SkillKInputThisFrame = !IsTimeUp && skillKAction != null && skillKAction.action.WasPerformedThisFrame();
        SkillLInputThisFrame = !IsTimeUp && skillLAction != null && skillLAction.action.WasPerformedThisFrame();

        // 將輸入交由狀態機目前的狀態處理
        StateMachine.CurrentState?.HandleInput();

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

    // 委派給當前狀態，讓狀態決定是否要被打斷
    public void ApplyStun(float duration)
    {
        StateMachine.CurrentState?.OnStunned(duration);
    }



    public string CurrentStateName => StateMachine.CurrentState?.GetType().Name ?? "None";
}
