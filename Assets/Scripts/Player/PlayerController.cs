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

    private void Awake()
    {
        charCon = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        dashAction.action.performed += OnDashInput;
    }

    private void OnDisable()
    {
        dashAction.action.performed -= OnDashInput;
    }

    private void Update()
    {
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
        else if (movement.sqrMagnitude > 0.01f)  // 即使不移動也更新方向
        {
            Vector3 newDir = movement.normalized;
            LastMoveDirection = newDir;
            OnDirectionChanged?.Invoke(LastMoveDirection);
        }

        charCon.Move(movement * moveSpeed * Time.deltaTime);
    }

    private void OnDashInput(InputAction.CallbackContext context)
    {
        if (canDash && !IsDashing) StartCoroutine(DashRoutine());
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
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}