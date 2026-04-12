using System;
using System.Collections;
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
    [SerializeField] private float attackCooldown = 0f;
    [SerializeField] private float convertChance = 0.3f;
    [SerializeField] private float darkVoterConvertChance = 0.8f;
    
    [Header("MP 消耗")]
    [SerializeField] private int speechMPCost = 1;

    [Header("顯示")]
    public AttackRangeMesh attackRangeMesh;
    private CinemachineImpulseSource impulseSource;
    private Animator characterAnimator;

    public event Action<float, float> OnAttackShapeChanged;
    public event Action OnAttackPerformed;

    private static readonly int HashAttack = Animator.StringToHash("attack");
    private bool isGameActive = true;
    private const int HitBufferSize = 64;
    private readonly Collider[] hitBuffer = new Collider[HitBufferSize];
    private float lastAttackTime = -999f;
    private float baseAttackRange;
    private float currentAttackRange;
    private float currentAttackCooldown;
    private float temporaryAttackRangeMultiplier = 1f;
    private Coroutine rangeBoostCoroutine;

    [Header("Layer")]
    public LayerMask voterLayer;

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        characterAnimator = GetComponent<Animator>();
        baseAttackRange = attackRange;
        currentAttackRange = attackRange;
        currentAttackCooldown = attackCooldown;
    }

    void OnEnable()
    {
        if (attackAction != null)
            attackAction.action.performed += OnAttackInput;

        RefreshAttackStats();

        if (PolicyEffectRuntimeManager.HasInstance)
        {
            PolicyEffectRuntimeManager.Instance.OnEffectsChanged += RefreshAttackStats;
        }

        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd += OnGameEnd;
        }
    }

    void OnDisable()
    {
        if (attackAction != null)
            attackAction.action.performed -= OnAttackInput;

        if (PolicyEffectRuntimeManager.HasInstance)
        {
            PolicyEffectRuntimeManager.Instance.OnEffectsChanged -= RefreshAttackStats;
        }

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
        if (!isGameActive) return;
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
        baseAttackRange = range;
        attackRange = range;
        attackAngle = angle;
        RefreshAttackStats();
    }

    private void RefreshAttackStats()
    {
        PolicyEffectRuntimeManager effects = PolicyEffectRuntimeManager.Instance;
        float policyAdjustedRange = effects != null ? effects.GetModifiedAttackRange(baseAttackRange) : baseAttackRange;
        currentAttackRange = policyAdjustedRange * temporaryAttackRangeMultiplier;
        currentAttackCooldown = effects != null ? effects.GetModifiedAttackCooldown(attackCooldown) : attackCooldown;
        attackRange = currentAttackRange;
        OnAttackShapeChanged?.Invoke(currentAttackRange, attackAngle);
    }

    public void ApplyTemporaryRangeBoost(float multiplier, float duration)
    {
        if (multiplier <= 1f || duration <= 0f)
        {
            return;
        }

        if (rangeBoostCoroutine != null)
        {
            StopCoroutine(rangeBoostCoroutine);
        }

        rangeBoostCoroutine = StartCoroutine(TemporaryRangeBoostRoutine(multiplier, duration));
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

        if (PlayerMPSystem.Instance == null)
        {
            Debug.LogWarning("找不到 PlayerMPSystem，無法施放演說");
            return;
        }

        if (Time.time < lastAttackTime + currentAttackCooldown)
        {
            Debug.Log($"⏳ 普通攻擊冷卻中... {(lastAttackTime + currentAttackCooldown - Time.time):F1} 秒");
            return;
        }

        if (!PlayerMPSystem.Instance.UseMP(speechMPCost))
        {
            Debug.Log("⚠️ MP 不足，無法施放演說");
            return;
        }

        lastAttackTime = Time.time;

        OnAttackPerformed?.Invoke();
        characterAnimator?.SetTrigger(HashAttack);
        attackRangeMesh?.Show();

        Vector3 attackDir = transform.forward;
        bool hitAny = false;

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            currentAttackRange,
            hitBuffer,
            voterLayer
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null) continue;

            if (hit.TryGetComponent<VoterLogic>(out var voter))
            {
                VoterData voterData = voter.Data;
                if (voterData != null && voterData.voterType == VoterType.Dark)
                {
                    continue;
                }

                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;

                if (Vector3.Angle(attackDir, dirToTarget) < attackAngle / 2f)
                {
                    voter.OnInfluence(attackInfluence, false, transform.position);
                    TryConvert(voterData);
                    hitAny = true;
                }
            }
        }

        if (hitAny)
        {
            impulseSource?.GenerateImpulse();
        }
    }
    
    public void TryConvert(VoterData voter)
    {
        if (voter == null)
            return;

        PolicyEffectRuntimeManager effects = PolicyEffectRuntimeManager.Instance;
        float chance = effects != null
            ? effects.GetModifiedConvertChance(convertChance)
            : convertChance;

        if (voter.voterType == VoterType.Dark)
            chance = effects != null
                ? effects.GetModifiedConvertChance(darkVoterConvertChance)
                : darkVoterConvertChance;

        if (UnityEngine.Random.value < chance)
        {
            ForceConvert(voter);
        }
    }

    private void ForceConvert(VoterData voter)
    {
        if (voter.TryGetComponent<VoterLogic>(out var logic))
        {
            int requiredInfluence = VoterConfig.MAX_POS - voter.currentPosition;
            if (requiredInfluence > 0)
            {
                logic.OnInfluence(requiredInfluence, true, transform.position);
            }
        }
    }

    private IEnumerator TemporaryRangeBoostRoutine(float multiplier, float duration)
    {
        temporaryAttackRangeMultiplier = multiplier;
        RefreshAttackStats();

        yield return new WaitForSeconds(duration);

        temporaryAttackRangeMultiplier = 1f;
        RefreshAttackStats();
        rangeBoostCoroutine = null;
    }
}
