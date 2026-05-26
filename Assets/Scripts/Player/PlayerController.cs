using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("基礎移動")]
    public float moveSpeed = 5f;

    [Header("衝刺/閃避 (Dash)")]
    public float dashSpeed    = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Input System")]
    public InputActionReference moveAction;
    public InputActionReference dashAction;
    public InputActionReference partySkillAction;

    [Header("Animation")]
    public Animator characterAnimator;

    public event Action<Vector3> OnDirectionChanged;
    public Vector3 LastMoveDirection { get; private set; } = Vector3.forward;
    public bool    IsDashing         { get; private set; }

    private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
    private static readonly int HashDash     = Animator.StringToHash("dash");

    private CharacterController charCon;
    private PlayerSkillManager skillManager;
    private Vector3 movement;
    private bool    canDash = true;
    private bool    isGameplayActive = true;
    private Coroutine dashCoroutine;

    private void Awake()
    {
        charCon = GetComponent<CharacterController>();
        skillManager = GetComponent<PlayerSkillManager>();
    }

    private void OnEnable()
    {
        if (dashAction != null)
        {
            dashAction.action.performed += OnDashInput;
        }

        if (partySkillAction != null)
        {
            partySkillAction.action.performed += OnPartySkillInput;
        }

        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd += OnGameEnd;
        }
    }

    private void OnDisable()
    {
        if (dashAction != null)
        {
            dashAction.action.performed -= OnDashInput;
        }

        if (partySkillAction != null)
        {
            partySkillAction.action.performed -= OnPartySkillInput;
        }

        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd -= OnGameEnd;
        }
    }

    private void Update()
    {
        if (!isGameplayActive)
        {
            characterAnimator?.SetBool(HashIsMoving, false);
            return;
        }

        if (partySkillAction == null && Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        {
            Debug.Log("[PlayerController] 透過備援路徑偵測到 J 鍵輸入。");
            OnPartySkillInput(default);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("[PlayerController] 透過 Input.GetKeyDown(KeyCode.J) 偵測到 J 鍵輸入。");
            OnPartySkillInput(default);
        }

        if (IsDashing) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (moveAction == null)
        {
            characterAnimator?.SetBool(HashIsMoving, false);
            return;
        }

        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        movement = new Vector3(moveInput.x, 0f, moveInput.y);

        bool isMoving = movement.sqrMagnitude > 0.01f;
        characterAnimator?.SetBool(HashIsMoving, isMoving);

        if (isMoving)
        {
            Vector3 newDir = movement.normalized;
            if (newDir != LastMoveDirection)
            {
                LastMoveDirection = newDir;
                OnDirectionChanged?.Invoke(LastMoveDirection);
            }
        }

        charCon.Move(movement * moveSpeed * Time.deltaTime);
    }

    private void OnDashInput(InputAction.CallbackContext context)
    {
        if (!isGameplayActive) return;
        if (canDash && !IsDashing)
        {
            dashCoroutine = StartCoroutine(DashRoutine());
        }
    }

    private void OnPartySkillInput(InputAction.CallbackContext context)
    {
        if (!isGameplayActive)
        {
            Debug.Log("[PlayerController] 偵測到政黨技能輸入，但玩家目前不可操作。");
            return;
        }

        Debug.Log("[PlayerController] 偵測到政黨技能輸入，準備呼叫 PlayerSkillManager.UsePartySkill()");

        if (skillManager == null)
        {
            Debug.LogWarning("⚠️ PlayerSkillManager 未設定，無法施放政黨技能。");
            return;
        }

        skillManager.UsePartySkill();
    }

    private void OnGameEnd()
    {
        isGameplayActive = false;
        movement = Vector3.zero;
        canDash = false;

        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }

        IsDashing = false;
        characterAnimator?.SetBool(HashIsMoving, false);
    }

    public void EnableMovementOnly()
    {
        isGameplayActive = true;
        movement = Vector3.zero;
        IsDashing = false;
        dashCoroutine = null;
        characterAnimator?.SetBool(HashIsMoving, false);
    }

    private IEnumerator DashRoutine()
    {
        if (movement.sqrMagnitude <= 0.01f) yield break;

        IsDashing = true;
        canDash   = false;
        characterAnimator?.SetTrigger(HashDash);

        Vector3 dashDir   = movement.normalized;
        float   startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            charCon.Move(dashDir * dashSpeed * Time.deltaTime);
            yield return null;
        }

        IsDashing = false;
        dashCoroutine = null;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
