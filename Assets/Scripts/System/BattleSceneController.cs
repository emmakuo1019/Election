using UnityEngine;

public class BattleSceneController : MonoBehaviour
{
    [Header("場景初始化")]
    [SerializeField] private bool initializeVoterTypesOnStart = true;
    [SerializeField] private SocialAtmosphereManager socialAtmosphereManager;
    [SerializeField] private BattleFlowController battleFlowController;

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

        if (initializeVoterTypesOnStart)
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
        float darkRate = socialAtmosphereManager != null ? socialAtmosphereManager.GetDarkVoterRate() : 0.1f;
        VoterData[] voters = FindObjectsByType<VoterData>(FindObjectsSortMode.None);

        foreach (VoterData voter in voters)
        {
            VoterType type = Random.value < darkRate ? VoterType.Dark : VoterType.Normal;
            voter.ApplyType(type);

            if (voter.TryGetComponent<VoterLogic>(out var logic))
            {
                logic.RefreshMovementSpeed();
            }

            if (voter.TryGetComponent<VoterVisuals>(out var visuals))
            {
                visuals.ApplyCurrentVisualState();
            }
        }

        Debug.Log($"🗳️ [BattleSceneController] 已初始化 {voters.Length} 位選民，深色選民機率 {darkRate:P0}");
    }
}
