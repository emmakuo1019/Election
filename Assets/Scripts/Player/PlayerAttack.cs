using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour, IAttackSource
{
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
    
    private Animator characterAnimator;

    void Awake()
    {
        
        impulseSource = GetComponent<CinemachineImpulseSource>();
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
        attackRangeMesh?.Show();

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
                
                    voter.OnInfluence(attackInfluence, false, transform.position);
                    hitAny = true;
                
            }
        }

        if (hitAny)
        {
            impulseSource?.GenerateImpulse();
        }
        
    }
}