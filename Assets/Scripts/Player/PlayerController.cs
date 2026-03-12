using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("組件引用")]
    public CharacterController charCon;
    private PlayerAttack attackScript; // 引用戰鬥腳本

    [Header("基礎移動")]
    public float moveSpeed = 5f;
    private float activeSpeed;
    private Vector3 movement;

    [Header("衝刺/閃避 (Dash)")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private bool canDash = true;

    [Header("Input System")]
    public InputActionReference moveAction;
    public InputActionReference dashAction;   // 取代原本的 Shift 邏輯
    public InputActionReference attackAction; // 攻擊輸入
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        charCon = GetComponent<CharacterController>();
        attackScript = GetComponent<PlayerAttack>();
        activeSpeed = moveSpeed;
        
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // 訂閱事件：新版 Input System 的最佳實作
        dashAction.action.performed += OnDashInput;
        attackAction.action.performed += OnAttackInput;
    }

    private void OnDisable()
    {
        dashAction.action.performed -= OnDashInput;
        attackAction.action.performed -= OnAttackInput;
    }

    void Update()
    {
        if (isDashing) return; // 衝刺時不允許玩家手動控制移動

        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        movement = new Vector3(moveInput.x, 0f, moveInput.y);

        Debug.Log(moveInput);

        charCon.Move(movement * moveSpeed);

        //Vector3 moveForward = transform.forward * moveInput.y;
        //Vector3 moveSideways = transform.right * moveInput.x; spriteRenderer.flipX = input.x < 0f;
    }

    // --- 事件處理 ---

    private void OnDashInput(InputAction.CallbackContext context)
    {
        if (canDash && !isDashing)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        // 呼叫 PlayerCombat 腳本執行演說
        if (attackScript != null)
        {
            attackScript.PerformSpeech();
        }
    }

    // --- 協程邏輯 ---

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;

        // 衝刺方向：只依照玩家當前的移動方向，不再自動往前
        if (movement.magnitude <= 0.1f)
        {
            // 沒有移動輸入時不進行衝刺，直接結束並還原狀態
            isDashing = false;
            canDash = true;
            yield break;
        }

        Vector3 dashDir = movement.normalized;
        
        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            // 使用 CharacterController.Move 確保物理碰撞依然有效
            charCon.Move(dashDir * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}