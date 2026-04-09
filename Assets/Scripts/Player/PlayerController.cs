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

    [Header("Animation")]
    public Animator characterAnimator;

    public event Action<Vector3> OnDirectionChanged;
    public Vector3 LastMoveDirection { get; private set; } = Vector3.forward;
    public bool    IsDashing         { get; private set; }

    private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
    private static readonly int HashDash     = Animator.StringToHash("dash");

    private CharacterController charCon;
    private Vector3 movement;
    private bool    canDash = true;
    private bool    isGameplayActive = true;
    private Coroutine dashCoroutine;

    private void Awake()
    {
        charCon = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        dashAction.action.performed += OnDashInput;

        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd += OnGameEnd;
        }
    }

    private void OnDisable()
    {
        dashAction.action.performed -= OnDashInput;

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

        if (IsDashing) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
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
