using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillManager : MonoBehaviour
{
    [Header("輸入")]
    [SerializeField] private InputActionReference speechAction;
    [SerializeField] private InputActionReference partyAction;

    [Header("技能狀態")]
    [SerializeField] private bool speechUnlocked = true;
    [SerializeField] private bool partyUnlocked = false;

    [Header("技能引用")]
    [SerializeField] private PlayerAttack speechAttack;
    [SerializeField] private PartySkillAttack partySkillAttack;

    public bool SpeechUnlocked => speechUnlocked;
    public bool PartyUnlocked => partyUnlocked;

    private void OnEnable()
    {
        if (speechAction != null)
            speechAction.action.performed += OnSpeechInput;

        if (partyAction != null)
            partyAction.action.performed += OnPartyInput;
    }

    private void OnDisable()
    {
        if (speechAction != null)
            speechAction.action.performed -= OnSpeechInput;

        if (partyAction != null)
            partyAction.action.performed -= OnPartyInput;
    }

    private void OnSpeechInput(InputAction.CallbackContext context)
    {
        UseSpeech();
    }

    private void OnPartyInput(InputAction.CallbackContext context)
    {
        UseParty();
    }

    public void UnlockSpeech()
    {
        if (speechUnlocked) return;
        speechUnlocked = true;
        Debug.Log("已解鎖：演說攻擊");
    }

    public void UnlockParty()
    {
        if (partyUnlocked) return;
        partyUnlocked = true;
        Debug.Log("已解鎖：政黨技能");
    }

    public void UseSpeech()
    {
        if (!speechUnlocked) return;
        
        if (speechAttack == null)
        {
            Debug.LogWarning("PlayerSkillManager: speechAttack 未設定");
            return;
        }
        
        speechAttack.PerformSpeech();
    }

    public void UseParty()
    {
        if (!partyUnlocked) return;
        
        if (partySkillAttack == null)
        {
            Debug.LogWarning("PlayerSkillManager: partySkillAttack 未設定");
            return;
        }
        
        partySkillAttack.PerformPartySkill();
    }
}