using System;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 舊政黨技能執行器的相容包裝。
/// 新系統主流程已由 PlayerSkillManager 直接呼叫 PartySkillData.Execute(...)。
/// 這個元件保留給既有 prefab / 介面引用使用，避免編譯錯誤與 Inspector 遺失欄位。
/// </summary>
public class PartySkillAttack : MonoBehaviour, IAttackSource
{
    [Header("技能資料")]
    [SerializeField] private PartySkillData partySkillData;

    [Header("顯示")]
    [SerializeField] private AttackRangeMesh attackRangeMesh;
    [SerializeField] private float displayRange = 1.5f;
    [SerializeField] private float displayAngle = 360f;

    private CinemachineImpulseSource impulseSource;
    private Animator characterAnimator;
    private float lastSkillTime = float.NegativeInfinity;

    public event Action<float, float> OnAttackShapeChanged;

    public float AttackRange => displayRange;
    public float AttackAngle => displayAngle;

    private static readonly int HashPartyAttack = Animator.StringToHash("partyAttack");

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        characterAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        attackRangeMesh?.ShowIdle();
        OnAttackShapeChanged?.Invoke(AttackRange, AttackAngle);
    }

    public void SetSkill(PartySkillData skillData)
    {
        partySkillData = skillData;
        OnAttackShapeChanged?.Invoke(AttackRange, AttackAngle);
    }

    public void PerformPartySkill()
    {
        if (!SceneContext.IsLevelScene())
        {
            Debug.LogWarning("⚠️ 只能在關卡中使用政黨技能！");
            return;
        }

        if (partySkillData == null)
        {
            Debug.LogWarning("⚠️ PartySkillData 未指定");
            return;
        }

        if (Time.time < lastSkillTime + partySkillData.baseCooldown)
        {
            float remainingCooldown = lastSkillTime + partySkillData.baseCooldown - Time.time;
            Debug.LogWarning($"⏳ 技能冷卻中... {remainingCooldown:F1} 秒");
            return;
        }

        characterAnimator?.SetTrigger(HashPartyAttack);
        attackRangeMesh?.Show();
        partySkillData.Execute(gameObject);
        impulseSource?.GenerateImpulse();
        lastSkillTime = Time.time;
    }
}
