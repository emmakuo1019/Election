using UnityEngine;

public class BattleSceneController : MonoBehaviour
{
    [Header("場景初始化")]
    [SerializeField] private bool initializeVoterIdentityOnStart = true;
    [SerializeField] private SocialAtmosphereManager socialAtmosphereManager;
    [SerializeField] private BattleFlowController battleFlowController;
    [SerializeField, Range(0f, 1f)] private float coldAttributeChance = 0.25f;

    private void Start()
    {
        if (socialAtmosphereManager == null)
        {
            socialAtmosphereManager = SocialAtmosphereManager.Instance ?? FindFirstObjectByType<SocialAtmosphereManager>();
        }

        if (battleFlowController == null)
        {
            battleFlowController = FindFirstObjectByType<BattleFlowController>();
        }

        if (initializeVoterIdentityOnStart)
        {
            InitializeSceneVoters();
        }

        if (battleFlowController != null)
        {
            battleFlowController.StartBattle();
        }
        else if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.StartTimer();
        }
        else
        {
            Debug.LogError("❌ 場景中找不到 LevelTimer，無法開始關卡計時");
        }
    }

    private void InitializeSceneVoters()
    {
        VoterData[] voters = FindObjectsByType<VoterData>(FindObjectsSortMode.None);

        foreach (VoterData voter in voters)
        {
            voter.InitializeFromConfig();
            voter.ConfigureIdentity(GetRandomLabel(), GetRandomLabel(), GetRandomAttribute());

            if (voter.TryGetComponent<VoterLogic>(out var logic))
            {
                logic.RefreshMovementSpeed();
            }

            if (voter.TryGetComponent<VoterVisuals>(out var visuals))
            {
                visuals.ApplyCurrentVisualState();
            }
        }

        Debug.Log($"🗳️ [BattleSceneController] 已初始化 {voters.Length} 位選民的新標籤與屬性。");
    }

    private VoterLabel GetRandomLabel()
    {
        return Random.value < 0.5f ? VoterLabel.Rational : VoterLabel.Emotion;
    }

    private VoterAttribute GetRandomAttribute()
    {
        float darkChance = socialAtmosphereManager != null ? socialAtmosphereManager.GetDarkVoterRate() : 0.1f;
        float roll = Random.value;

        if (roll < darkChance)
        {
            return VoterAttribute.Dark;
        }

        if (roll < darkChance + coldAttributeChance)
        {
            return VoterAttribute.Cold;
        }

        return VoterAttribute.None;
    }
}
