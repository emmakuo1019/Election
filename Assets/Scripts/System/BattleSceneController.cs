using UnityEngine;

public class BattleSceneController : MonoBehaviour
{
    [Header("場景初始化")]
    [SerializeField] private bool initializeVoterIdentityOnStart = true;
    [SerializeField] private BattleFlowController battleFlowController;
    [SerializeField, Range(0f, 1f)] private float coldAttributeChance = 0.25f;

    private void Start()
    {
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

        int addedPlayerVotes = 0;
        int addedOpponentVotes = 0;

        foreach (VoterData voter in voters)
        {
            voter.InitializeFromConfig();
            VoterLabel label = GetRandomLabel();
            VoterAttribute attribute = GetRandomAttribute();
            int stance = VoterData.NeutralSideSign;

            if (attribute == VoterAttribute.Dark)
            {
                float playerRatio = GameDB.Instance != null ? GameDB.Instance.Run.PlayerVotePercentage : 0.5f;
                stance = UnityEngine.Random.value < playerRatio ? VoterData.PlayerSideSign : VoterData.EnemySideSign;
            }

            if (voter.TryGetComponent<VoterLogic>(out var logic))
            {
                logic.SetIdentity(label, attribute, stance);
            }
            else
            {
                voter.ConfigureIdentity(label, attribute, stance);
                if (voter.TryGetComponent<VoterVisuals>(out var visuals))
                    visuals.ApplyCurrentVisualState();
            }

            if (voter.ConvertedSide == VoterData.PlayerSideSign) addedPlayerVotes++;
            else if (voter.ConvertedSide == VoterData.EnemySideSign) addedOpponentVotes++;
        }

        // 將本場選民的初始票數加入跨場景累計
        if (GameDB.Instance != null && (addedPlayerVotes > 0 || addedOpponentVotes > 0))
        {
            GameDB.Instance.Run.AddVote(addedPlayerVotes, addedOpponentVotes);
        }
    }

    private VoterLabel GetRandomLabel()
    {
        return Random.value < 0.5f ? VoterLabel.Rational : VoterLabel.Emotion;
    }

    private VoterAttribute GetRandomAttribute()
    {
        float darkChance = GameDB.Instance != null ? GameDB.Instance.Run.GetDarkVoterRate() : 0.1f;
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
