using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerStateMachine : MonoBehaviour
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
    public Vector2 MoveInput { get; private set; }

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
    public SkillState.SkillSlot? SkillInputThisFrame { get; private set; }
    public bool CanDash => Time.time >= _dashReadyTime;
    private float _dashReadyTime;

    private IPlayerState _currentState;
    private string _currentStateName;
    private PlayerSkillManager _skillManager;

    private void Awake()
    {
        CharCon = GetComponent<CharacterController>();
        PlayerAttack = GetComponent<PlayerAttack>();
        _skillManager = GetComponent<PlayerSkillManager>();
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
    private void OnSkillJ(InputAction.CallbackContext _)  => SkillInputThisFrame  = SkillState.SkillSlot.J;
    private void OnSkillK(InputAction.CallbackContext _)  => SkillInputThisFrame  = SkillState.SkillSlot.K;
    private void OnSkillL(InputAction.CallbackContext _)  => SkillInputThisFrame  = SkillState.SkillSlot.L;

    private void Start()
    {
        ChangeState(new IdleState(this));
    }

    private void Update()
    {
        MoveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        _currentState?.Update();

        DashInputThisFrame   = false;
        AttackInputThisFrame = false;
        SkillInputThisFrame  = null;
    }

    public void ChangeState(IPlayerState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentStateName = newState.GetType().Name;
        Debug.Log($"[PlayerStateMachine] → {_currentStateName}");
        _currentState.Enter();
    }

    public void SetDashCooldown() => _dashReadyTime = Time.time + dashCooldown;

    // 遊戲結束，停用所有輸入（進入無法轉換的空狀態）
    public void OnGameEnd()
    {
        _currentState?.Exit();
        _currentState = null;
        _currentStateName = "GameEnd";
    }

    // 房間結算後恢復玩家輸入
    public void ResumeIdle() => ChangeState(new IdleState(this));

    // 由外部（敵人、技能）呼叫，SkillState 期間有霸體不被打斷
    public void ApplyStun(float duration)
    {
        if (_currentState is SkillState) return;
        ChangeState(new StunState(this, duration));
    }

    public float UseSkill(SkillState.SkillSlot slot)
    {
        if (_skillManager == null) return 0f;

        switch (slot)
        {
            case SkillState.SkillSlot.J:
                _skillManager.UseSpeech();
                return 0.5f;
            case SkillState.SkillSlot.K:
            case SkillState.SkillSlot.L:
                var skill = _skillManager.CurrentPartySkill;
                if (skill != null)
                {
                    _skillManager.UsePartySkill();
                    return skill.duration;
                }
                return 0f;
        }
        return 0f;
    }

    public string CurrentStateName => _currentStateName;
}
