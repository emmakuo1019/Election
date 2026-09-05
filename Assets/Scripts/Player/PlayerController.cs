using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家主控制器，負責管理依賴項、輸入，並組合 StateMachine 來處理狀態邏輯。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("暈眩特效")]
    public GameObject stunVfxPrefab;         // 暈眩時生成的 VFX Prefab
    public Vector3 stunVfxOffset = new Vector3(0f, 2f, 0f); // VFX 相對玩家的偏移（預設頭頂）

    [Header("攻擊設定")]
    public float attackDuration = 0.2f;
    public float moveSpeed = 5f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("物理推擠設定")]
    [SerializeField, Tooltip("玩家擠開選民的推力大小")] 
    private float voterPushForce = 3.0f;

    [Header("Input")]
    public InputActionReference moveAction;
    public InputActionReference dashAction;
    public InputActionReference attackAction;
    public InputActionReference skillJAction;
    public InputActionReference skillKAction;
    public InputActionReference skillLAction;

    [Header("Animation")]
    public Animator characterAnimator;

    public float CurrentMoveSpeed
    {
        get
        {
            if (GameDB.Instance != null && GameDB.Instance.Run != null)
            {
                return GameDB.Instance.Run.Stats.ModifiedMoveSpeed;
            }
            return moveSpeed;
        }
    }

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
        BattleEventManager.OnEncounterPhaseChanged += HandleEncounterPhase;
    }

    private void OnDisable()
    {
        BattleEventManager.OnEncounterPhaseChanged -= HandleEncounterPhase;
    }

    private void Start()
    {
        // 初始化狀態機，給予起始狀態
        StateMachine.Initialize(new IdleState(this));
        
        // 場景換載後仍顯式啟用 action；是否接收輸入完全由 EncounterPhase 決定。
        EnableInputAction(moveAction);
        EnableInputAction(attackAction);
        EnableInputAction(dashAction);
        EnableInputAction(skillJAction);
        EnableInputAction(skillKAction);
        EnableInputAction(skillLAction);
        ApplyEncounterPhase(BattleEventManager.CurrentEncounterPhase);
    }

    private void Update()
    {
        MoveInput = _movementInputEnabled && moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        // 輪詢輸入，徹底避開 C# Event 殘留的坑
        DashInputThisFrame = _combatInputEnabled && dashAction != null && dashAction.action.WasPerformedThisFrame();
        AttackInputThisFrame = _combatInputEnabled && attackAction != null && attackAction.action.WasPerformedThisFrame();
        SkillJInputThisFrame = _combatInputEnabled && skillJAction != null && skillJAction.action.WasPerformedThisFrame();
        SkillKInputThisFrame = _combatInputEnabled && skillKAction != null && skillKAction.action.WasPerformedThisFrame();
        SkillLInputThisFrame = _combatInputEnabled && skillLAction != null && skillLAction.action.WasPerformedThisFrame();

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

    // 保留舊 API 的語意：非戰鬥階段不能施放攻擊／技能；但選卡與選門仍可走動。
    public bool IsTimeUp => !_combatInputEnabled;
    private bool _movementInputEnabled;
    private bool _combatInputEnabled;

    private void HandleEncounterPhase(BattleEventManager.EncounterPhase phase)
    {
        ApplyEncounterPhase(phase);
    }

    private void ApplyEncounterPhase(BattleEventManager.EncounterPhase phase)
    {
        bool movementAllowed = phase == BattleEventManager.EncounterPhase.Active ||
                               phase == BattleEventManager.EncounterPhase.RewardSelection ||
                               phase == BattleEventManager.EncounterPhase.RouteSelection;
        bool combatAllowed = phase == BattleEventManager.EncounterPhase.Active;
        SetInputAvailability(movementAllowed, combatAllowed);
    }

    private void SetInputAvailability(bool movementAllowed, bool combatAllowed)
    {
        _movementInputEnabled = movementAllowed;
        _combatInputEnabled = combatAllowed;
        if (!combatAllowed) StateMachine?.ChangeState(new IdleState(this));
    }

    private static void EnableInputAction(InputActionReference actionReference)
    {
        if (actionReference?.action != null) actionReference.action.Enable();
    }

    // 相容舊呼叫；計時器不再直接呼叫此方法。
    public void OnGameEnd()
    {
        SetInputAvailability(false, false);
    }

    // 房間結算後恢復玩家輸入
    public void ResumeIdle()
    {
        SetInputAvailability(true, true);
    }

    // 委派給當前狀態，讓狀態決定是否要被打斷
    public void ApplyStun(float duration)
    {
        StateMachine.ChangeState(new StunState(this, duration));
    }



    public string CurrentStateName => StateMachine.CurrentState?.GetType().Name ?? "None";

    /// <summary>
    /// 當 CharacterController 移動並撞擊到其他 Collider 時觸發。
    /// 用於處理玩家擠開帶有 NavMeshAgent 的選民，解決被卡死的問題。
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 檢查撞擊對象是否帶有 NavMeshAgent 組件 (如 VoterLogic)
        UnityEngine.AI.NavMeshAgent agent = hit.collider.GetComponentInParent<UnityEngine.AI.NavMeshAgent>();

        // 防呆機制：確保 agent 存在且啟用中，避免拋出錯誤
        if (agent != null && agent.isActiveAndEnabled)
        {
            // 計算從玩家中心推向選民的方向，並忽略 Y 軸保持平面推擠
            Vector3 pushDir = hit.point - transform.position;
            pushDir.y = 0;
            pushDir.Normalize();

            // 透過 agent.Move 讓選民被輕微推開，不破壞 NavMesh 尋路狀態
            agent.Move(pushDir * voterPushForce * Time.deltaTime);
        }
    }
}
