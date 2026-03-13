using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private PlayerController playerController;
    
    [Header("Input System")]
    public InputActionReference attackAction;


    [Header("演說設定")]
    public float attackRange    = 3.0f;
    public float attackAngle    = 60.0f;
    public int   attackInfluence = 1;
    public int   fundingCost    = 10;

    [Header("攻擊對話框特效")]
    public GameObject attackBubblePrefab;
    public float      bubbleDisplayDuration = 1.0f;

    [Header("範圍指示器")]
    public AttackRangeIndicator rangeIndicator;

    private GameObject activeBubble;
    public event Action OnAttackPerformed;
    private static readonly int HashAttack = Animator.StringToHash("attack");


    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    void OnEnable()
    {
        if (playerController != null)
            playerController.OnDirectionChanged += OnDirectionChanged;

        if (attackAction != null)
            attackAction.action.performed += OnAttackInput;
    }

    void OnDisable()
    {
        if (playerController != null)
            playerController.OnDirectionChanged -= OnDirectionChanged;

        if (attackAction != null)
            attackAction.action.performed -= OnAttackInput;
    }

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        PerformSpeech();
    }


    void Start()
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.SetShape(attackRange, attackAngle);
            rangeIndicator.ShowIdle();
        }
    }

    private void OnDirectionChanged(Vector3 dir)
    {
        if (rangeIndicator != null)
            rangeIndicator.transform.rotation = Quaternion.LookRotation(dir);
    }
    
    /// 發動演說攻擊
    public void PerformSpeech()
    {
        OnAttackPerformed?.Invoke();

        // 取 PlayerController 上的 Animator 觸發攻擊動畫
        playerController
            ?.GetComponent<PlayerController>()
            .characterAnimator
            ?.SetTrigger(HashAttack);

        ShowAttackBubble();
        rangeIndicator?.Show();

        Vector3 attackDir = playerController != null
            ? playerController.LastMoveDirection
            : transform.forward;

        bool hitAny = false;
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position, attackRange,
            Physics.AllLayers, QueryTriggerInteraction.Collide);

        foreach (var hit in hitColliders)
        {
            if (hit.TryGetComponent<VoterLogic>(out var voterLogic))
            {
                Vector3 dirToVoter = (hit.transform.position - transform.position).normalized;
                if (Vector3.Angle(attackDir, dirToVoter) < attackAngle / 2f)
                {
                    voterLogic.OnInfluence(attackInfluence, isSkill: false, transform.position);
                    hitAny = true;
                }
            }
        }

        // 有命中才震動，強調打擊感
        if (hitAny)
            CameraShake.Instance?.Shake(0.12f, 0.18f);

        if (!hitAny) Debug.Log("演說範圍內沒有選民。");
    }


    private void ShowAttackBubble()
    {
        if (attackBubblePrefab == null) return;

        if (activeBubble != null)
        {
            StopAllCoroutines();
            Destroy(activeBubble);
        }

        activeBubble = Instantiate(
            attackBubblePrefab,
            transform.position + Vector3.up * 2f,
            Quaternion.identity,
            transform);

        StartCoroutine(HideBubbleAfterDelay());
    }

    private IEnumerator HideBubbleAfterDelay()
    {
        yield return new WaitForSeconds(bubbleDisplayDuration);
        if (activeBubble != null)
        {
            Destroy(activeBubble);
            activeBubble = null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 dir = (playerController != null && Application.isPlaying)
            ? playerController.LastMoveDirection
            : transform.forward;

        float halfAngle  = attackAngle / 2f;
        Vector3 leftDir  = Quaternion.Euler(0f, -halfAngle, 0f) * dir;
        Vector3 rightDir = Quaternion.Euler(0f,  halfAngle, 0f) * dir;

        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.15f);
        UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up, leftDir, attackAngle, attackRange);
        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.9f);
        UnityEditor.Handles.DrawWireArc(transform.position, Vector3.up, leftDir, attackAngle, attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
        Gizmos.DrawRay(transform.position, leftDir  * attackRange);
        Gizmos.DrawRay(transform.position, rightDir * attackRange);
    }
#endif
}
