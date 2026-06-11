using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillManager : MonoBehaviour
{
    private const string PendingMapSkillSelectionKey = "PendingMapSkillSelection";
    private static PartySkillData equippedPartySkill;

    [Header("技能")]
    [SerializeField] private PlayerAttack speechAttack;
    [SerializeField] private PartySkillData currentPartySkill;

    [Header("戰鬥技能裝備 (Data-Driven)")]
    public SkillData baseSkillJ;
    public SkillData skillK;
    public SkillData skillL;

    // 儲存各個技能的上次施放時間，用來計算冷卻
    private System.Collections.Generic.Dictionary<SkillData, float> skillLastUseTime = new System.Collections.Generic.Dictionary<SkillData, float>();

    [Header("技能選單（長按）")]
    [SerializeField] private InputActionReference partySelectAction;
    [SerializeField] private float holdDuration = 1f;

    private float holdTimer;
    private float lastPartySkillUseTime = float.NegativeInfinity;
    private bool isHolding;

    public delegate void PartySkillSelectionDelegate();
    public event PartySkillSelectionDelegate OnPartySkillSelectionRequested;

    public bool HasPartySkill => currentPartySkill != null;
    public PartySkillData CurrentPartySkill => currentPartySkill;
    public static bool HasEquippedPartySkill => equippedPartySkill != null;
    public static PartySkillData EquippedPartySkill => equippedPartySkill;
    public float HoldProgress => isHolding ? Mathf.Clamp01(holdTimer / holdDuration) : 0f;

    private void Awake()
    {
        if (currentPartySkill == null && equippedPartySkill != null)
            currentPartySkill = equippedPartySkill;
    }

    private void OnEnable()
    {
        if (partySelectAction != null)
        {
            partySelectAction.action.started  += OnPartySelectStarted;
            partySelectAction.action.canceled += OnPartySelectCanceled;
        }
    }

    private void OnDisable()
    {
        if (partySelectAction != null)
        {
            partySelectAction.action.started  -= OnPartySelectStarted;
            partySelectAction.action.canceled -= OnPartySelectCanceled;
        }
    }

    private void Update()
    {
        if (isHolding) holdTimer += Time.deltaTime;
    }

    private void OnPartySelectStarted(InputAction.CallbackContext context)
    {
        isHolding = true;
        holdTimer = 0f;
    }

    private void OnPartySelectCanceled(InputAction.CallbackContext context)
    {
        if (holdTimer >= holdDuration && !HasPartySkill)
            OnPartySkillSelectionRequested?.Invoke();

        isHolding = false;
        holdTimer = 0f;
    }

    public void UseSpeech()
    {
        if (speechAttack == null)
        {
            Debug.LogWarning("⚠️ speechAttack 未設定");
            return;
        }
        speechAttack.PerformAttack(Vector3.zero);
    }

    public void UsePartySkill()
    {
        if (currentPartySkill == null)
        {
            Debug.LogWarning("⚠️ 尚未裝備政黨技能");
            return;
        }

        if (Time.time < lastPartySkillUseTime + currentPartySkill.baseCooldown)
        {
            Debug.LogWarning($"⏳ 技能冷卻中... {lastPartySkillUseTime + currentPartySkill.baseCooldown - Time.time:F1} 秒");
            return;
        }

        if (!currentPartySkill.CanExecute(gameObject, out string executeFailureReason))
        {
            if (!string.IsNullOrWhiteSpace(executeFailureReason)) Debug.LogWarning(executeFailureReason);
            return;
        }

        if (!currentPartySkill.TryConsumeResources(gameObject, out string resourceFailureReason))
        {
            if (!string.IsNullOrWhiteSpace(resourceFailureReason)) Debug.LogWarning(resourceFailureReason);
            return;
        }

        currentPartySkill.Execute(gameObject);
        lastPartySkillUseTime = Time.time;
    }

    // ============================================
    // 新增：戰鬥技能 (J, K, L) 管理邏輯 (SRP & OCP)
    // ============================================

    /// <summary>
    /// 檢查傳入的戰鬥技能是否可以施放（冷卻時間是否結束）
    /// </summary>
    public bool CanCastSkill(SkillData skillData)
    {
        if (skillData == null) return false;

        if (skillLastUseTime.TryGetValue(skillData, out float lastTime))
        {
            // 如果當前時間已經大於等於 上次施放時間 + 冷卻時間，代表 CD 完畢
            return Time.time >= lastTime + skillData.cooldown;
        }

        // 字典裡沒有紀錄，代表從沒施放過，直接回傳 true
        return true;
    }

    /// <summary>
    /// 執行技能：生成特效、觸發邏輯，並記錄施放時間以進入 CD
    /// </summary>
    public void PerformSkill(SkillData skillData)
    {
        if (skillData == null) return;

        // 1. 紀錄施放時間，進入 CD
        skillLastUseTime[skillData] = Time.time;

        // 2. 處理技能的具體邏輯（例如生成特效）
        // 假設 SkillData 有 prefab 欄位
        // if (skillData.prefab != null)
        // {
        //     Instantiate(skillData.prefab, transform.position, transform.rotation);
        // }
        Debug.Log($"[PlayerSkillManager] 施放技能：{skillData.name}");
    }

    public void EquipPartySkill(PartySkillData skillData)
    {
        SetEquippedPartySkill(skillData);
        currentPartySkill = equippedPartySkill;
        lastPartySkillUseTime = float.NegativeInfinity;
    }

    public void ClearPartySkill()
    {
        currentPartySkill = null;
        equippedPartySkill = null;
        lastPartySkillUseTime = float.NegativeInfinity;
    }

    public void UnlockSpeech() { }

    public static void SetEquippedPartySkill(PartySkillData skillData)
    {
        equippedPartySkill = skillData;
        ClearPendingMapSkillSelection();
    }

    public static void ResetSavedPartySkill()
    {
        equippedPartySkill = null;
        PlayerPrefs.DeleteKey(PendingMapSkillSelectionKey);
        PlayerPrefs.Save();
    }

    public static void MarkPendingMapSkillSelection()
    {
        PlayerPrefs.SetInt(PendingMapSkillSelectionKey, 1);
        PlayerPrefs.Save();
    }

    public static bool HasPendingMapSkillSelection() =>
        PlayerPrefs.GetInt(PendingMapSkillSelectionKey, 0) == 1;

    public static void ClearPendingMapSkillSelection()
    {
        PlayerPrefs.DeleteKey(PendingMapSkillSelectionKey);
        PlayerPrefs.Save();
    }
}
