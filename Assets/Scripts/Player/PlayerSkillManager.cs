using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillManager : MonoBehaviour
{
    public enum PartySkillType
    {
        None,              // 未選擇
        PolicyDebate,      // 政策論述
        EmotionalStirring  // 煽動情緒
    }

    [Header("輸入")]
    [SerializeField] private InputActionReference speechAction;
    [SerializeField] private InputActionReference partyAction;
    [SerializeField] private InputActionReference partySelectAction; // 長按E

    [Header("技能狀態")]
    [SerializeField] private PartySkillType selectedPartySkill = PartySkillType.None;

    [Header("技能引用")]
    [SerializeField] private PlayerAttack speechAttack;
    [SerializeField] private PartySkillAttack partySkillAttack;

    // 長按時間
    private float holdDuration = 1f;
    private float holdTimer = 0f;
    private bool isHolding = false;

    public bool SpeechUnlocked => true;
    public PartySkillType SelectedPartySkill => selectedPartySkill;
    public bool HasPartySkill => selectedPartySkill != PartySkillType.None;
    private bool isGameActive = true;

    private void OnEnable()
    {
        if (speechAction != null)
            speechAction.action.performed += OnSpeechInput;

        if (partyAction != null)
            partyAction.action.performed += OnPartyInput;

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
            speechAction.action.performed -= OnSpeechInput;

        if (partyAction != null)
            partyAction.action.performed -= OnPartyInput;

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
    
    private void OnGameEnd()
    {
        isGameActive = false;
        isHolding = false;
        holdTimer = 0f;
        Debug.Log("🛑 [PlayerSkillManager] 遊戲結束，技能禁用");
    }

    private void Update()
    {
        // 長按計時
        if (isHolding)
        {
            holdTimer += Time.deltaTime;
        }
    }

    private void OnSpeechInput(InputAction.CallbackContext context)
    {
        UseSpeech();
    }

    private void OnPartyInput(InputAction.CallbackContext context)
    {
        UsePartySkill();
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
            Debug.Log("[PlayerSkillManager] 長按E達到時間！打開技能選擇面板");
            // 觸發事件，通知 UpgradePanelUI 打開選擇面板
            OnPartySkillSelectionRequested?.Invoke();
        }

        isHolding = false;
        holdTimer = 0f;
    }

    public void UnlockSpeech() { }

    public void SelectPartySkill(PartySkillType skillType)
    {
        if (selectedPartySkill != PartySkillType.None)
        {
            Debug.LogWarning("⚠️ 已選擇其他政黨技能，無法更換");
            return;
        }

        selectedPartySkill = skillType;
        Debug.Log($"✅ 已解鎖政黨技能: {skillType}");

        if (partySkillAttack != null)
        {
            partySkillAttack.Initialize(skillType);
        }
    }

    public void UseSpeech()
    {
        if (!isGameActive)
            return;
        // 檢查場景
        if (!SceneContext.IsLevelScene())
        {
            Debug.LogWarning("⚠️ 只能在關卡中使用技能！");
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
            return;
        // 檢查場景
        if (!SceneContext.IsLevelScene())
        {
            Debug.LogWarning("⚠️ 只能在關卡中使用技能！");
            return;
        }

        if (!HasPartySkill)
        {
            Debug.LogWarning("⚠️ 政黨技能未選擇");
            return;
        }

        if (partySkillAttack == null)
        {
            Debug.LogWarning("⚠️ partySkillAttack 未設定");
            return;
        }
        partySkillAttack.PerformPartySkill();
    }
    // 事件：通知 UI 打開技能選擇
    public delegate void PartySkillSelectionDelegate();
    public event PartySkillSelectionDelegate OnPartySkillSelectionRequested;
    public float HoldProgress => isHolding ? Mathf.Clamp01(holdTimer / holdDuration) : 0f;
}
