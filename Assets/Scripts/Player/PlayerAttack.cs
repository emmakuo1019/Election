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
    private Animator characterAnimator;

    public event Action<float, float> OnAttackShapeChanged;
    public event Action OnAttackPerformed;

    private static readonly int HashAttack = Animator.StringToHash("attack");
    private bool isGameActive = true;

    [Header("Layer")]
    public LayerMask voterLayer;

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        characterAnimator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (attackAction != null)
            attackAction.action.performed += OnAttackInput;

        OnAttackShapeChanged?.Invoke(attackRange, attackAngle);
        
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd += OnGameEnd;
        }
    }

    void OnDisable()
    {
        if (attackAction != null)
            attackAction.action.performed -= OnAttackInput;
        
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd -= OnGameEnd;
        }
    }
    
    private void OnGameEnd()
    {
        isGameActive = false;
        Debug.Log("🛑 [PlayerAttack] 遊戲結束，攻擊禁用");
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
        
        if (!isGameActive)
            return;
        
        if (!SceneContext.IsLevelScene())
        {
            Debug.LogWarning("⚠️ 只能在關卡中進行攻擊！");
            return;
        }

        OnAttackPerformed?.Invoke();
        characterAnimator?.SetTrigger(HashAttack);
        attackRangeMesh?.Show();

        Vector3 attackDir = transform.forward;
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