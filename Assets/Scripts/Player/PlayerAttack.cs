using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour, IAttackSource
{
    private PlayerController playerController;

    [Header("Input")]
    public InputActionReference attackAction;

    [Header("攻擊設定")]
    public float attackRange = 3f;
    public float attackAngle = 60f;
    public int attackInfluence = 1;

    [Header("顯示")]
    public AttackRangeMesh attackRangeMesh;
    private CinemachineImpulseSource impulseSource;


    public event Action<float, float> OnAttackShapeChanged;
    public event Action OnAttackPerformed;

    private static readonly int HashAttack = Animator.StringToHash("attack");

    [Header("Layer")]
    public LayerMask voterLayer;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    void OnEnable()
    {
        if (playerController != null)
            playerController.OnDirectionChanged += OnDirectionChanged;

        if (attackAction != null)
            attackAction.action.performed += OnAttackInput;

        // 初始化推送
        OnAttackShapeChanged?.Invoke(attackRange, attackAngle);
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
        if (attackRangeMesh != null)
            attackRangeMesh.ShowIdle();
    }

    private void OnDirectionChanged(Vector3 dir)
    {
        if (attackRangeMesh != null)
            attackRangeMesh.transform.rotation = Quaternion.LookRotation(dir);
    }

    public float AttackRange => attackRange;
    public float AttackAngle => attackAngle;

    public void UpdateAttackShape(float range, float angle)
    {
        attackRange = range;
        attackAngle = angle;
        OnAttackShapeChanged?.Invoke(range, angle);
    }

    public void PerformSpeech()
    {
        OnAttackPerformed?.Invoke();

        playerController?.characterAnimator?.SetTrigger(HashAttack);

        attackRangeMesh?.Show();

        Vector3 attackDir = playerController != null
            ? playerController.LastMoveDirection
            : transform.forward;

        bool hitAny = false;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            attackRange,
            voterLayer
        );

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<VoterLogic>(out var voter))
            {
                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;

                if (Vector3.Angle(attackDir, dirToTarget) < attackAngle / 2f)
                {
                    voter.OnInfluence(attackInfluence, false, transform.position);
                    hitAny = true;
                }
            }
        }

        if (hitAny)
        {
            impulseSource?.GenerateImpulse();
        }
        
    }  
}