using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillManager : MonoBehaviour
{
    private const string PendingMapSkillSelectionKey = "PendingMapSkillSelection";
    private static PartySkillData equippedPartySkill;

    [Header("輸入")]
    [SerializeField] private InputActionReference speechAction;
    [SerializeField] private InputActionReference partySelectAction;

    [Header("技能")]
    [SerializeField] private PlayerAttack speechAttack;
    [SerializeField] private PartySkillData currentPartySkill;

    [Header("選單")]
    [SerializeField] private float holdDuration = 1f;

    private float holdTimer;
    private float lastPartySkillUseTime = float.NegativeInfinity;
    private bool isHolding;
    private bool isGameActive = true;

    public delegate void PartySkillSelectionDelegate();
    public event PartySkillSelectionDelegate OnPartySkillSelectionRequested;

    public bool SpeechUnlocked => true;
    public bool HasPartySkill => currentPartySkill != null;
    public PartySkillData CurrentPartySkill => currentPartySkill;
    public static bool HasEquippedPartySkill => equippedPartySkill != null;
    public static PartySkillData EquippedPartySkill => equippedPartySkill;
    public float HoldProgress => isHolding ? Mathf.Clamp01(holdTimer / holdDuration) : 0f;

    private void Awake()
    {
        if (currentPartySkill == null && equippedPartySkill != null)
        {
            currentPartySkill = equippedPartySkill;
        }
    }

    private void OnEnable()
    {
        if (speechAction != null)
        {
            speechAction.action.performed += OnSpeechInput;
        }

        if (partySelectAction != null)
        {
            partySelectAction.action.started += OnPartySelectStarted;
            partySelectAction.action.canceled += OnPartySelectCanceled;
        }

        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd += OnGameEnd;
        }
    }

    private void OnDisable()
    {
        if (speechAction != null)
        {
            speechAction.action.performed -= OnSpeechInput;
        }

        if (partySelectAction != null)
        {
            partySelectAction.action.started -= OnPartySelectStarted;
            partySelectAction.action.canceled -= OnPartySelectCanceled;
        }

        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.OnTimerEnd -= OnGameEnd;
        }
    }

    private void Update()
    {
        if (isHolding)
        {
            holdTimer += Time.deltaTime;
        }
    }

    private void OnSpeechInput(InputAction.CallbackContext context)
    {
        UseSpeech();
    }

    private void OnPartySelectStarted(InputAction.CallbackContext context)
    {
        isHolding = true;
        holdTimer = 0f;
    }

    private void OnPartySelectCanceled(InputAction.CallbackContext context)
    {
        if (holdTimer >= holdDuration && !HasPartySkill)
        {
            OnPartySkillSelectionRequested?.Invoke();
        }

        isHolding = false;
        holdTimer = 0f;
    }

    private void OnGameEnd()
    {
        isGameActive = false;
        isHolding = false;
        holdTimer = 0f;
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

    public void UnlockSpeech()
    {
    }

    public void UseSpeech()
    {
        if (!isGameActive)
        {
            return;
        }

        if (speechAttack == null)
        {
            Debug.LogWarning("⚠️ speechAttack 未設定");
            return;
        }

        speechAttack.PerformSpeech();
    }

    public void UsePartySkill()
    {
        if (!isGameActive)
        {
            return;
        }

        if (currentPartySkill == null)
        {
            Debug.LogWarning("⚠️ 尚未裝備政黨技能");
            return;
        }

        if (Time.time < lastPartySkillUseTime + currentPartySkill.baseCooldown)
        {
            float remainingCooldown = lastPartySkillUseTime + currentPartySkill.baseCooldown - Time.time;
            Debug.LogWarning($"⏳ 技能冷卻中... {remainingCooldown:F1} 秒");
            return;
        }

        if (!currentPartySkill.CanExecute(gameObject, out string executeFailureReason))
        {
            if (!string.IsNullOrWhiteSpace(executeFailureReason))
            {
                Debug.LogWarning(executeFailureReason);
            }
            return;
        }

        if (!currentPartySkill.TryConsumeResources(gameObject, out string resourceFailureReason))
        {
            if (!string.IsNullOrWhiteSpace(resourceFailureReason))
            {
                Debug.LogWarning(resourceFailureReason);
            }
            return;
        }
        currentPartySkill.Execute(gameObject);
        lastPartySkillUseTime = Time.time;
    }

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

    public static bool HasPendingMapSkillSelection()
    {
        return PlayerPrefs.GetInt(PendingMapSkillSelectionKey, 0) == 1;
    }

    public static void ClearPendingMapSkillSelection()
    {
        PlayerPrefs.DeleteKey(PendingMapSkillSelectionKey);
        PlayerPrefs.Save();
    }
}
