using UnityEngine;

public enum VoterLabel
{
    Rational,
    Emotion
}

public enum VoterAttribute
{
    None,
    Cold,
    Dark
}

public class VoterData : MonoBehaviour
{
    public const int PlayerSideSign = 1;
    public const int EnemySideSign = -1;
    public const int NeutralSideSign = 0;

    public event System.Action OnIdentityChanged;

    [Header("設定")]
    [SerializeField] private VoterConfig config;

    [Header("標籤")]
    [SerializeField] private VoterLabel primaryLabel = VoterLabel.Rational;
    [SerializeField] private VoterLabel secondaryLabel = VoterLabel.Rational;

    [Header("屬性")]
    [SerializeField] private VoterAttribute voterAttribute = VoterAttribute.None;

    [Header("基礎數值")]
    [SerializeField] private float normalMoveSpeed = 1.2f;
    [SerializeField] private float darkMoveSpeed = 2f;

    [Header("立場資料")]
    [Tooltip("-5 = 敵方完全支持，+5 = 玩家完全支持。")]
    public int currentPosition;
    public int convertedSide;

    [Header("狀態")]
    public bool isConverted = false;
    [Range(0f, 1f)] public float loyalty = 1f;

    public VoterConfig Config => config;
    public VoterLabel PrimaryLabel => primaryLabel;
    public VoterLabel SecondaryLabel => secondaryLabel;
    public VoterAttribute Attribute => voterAttribute;
    public int EmotionLabelCount => (primaryLabel == VoterLabel.Emotion ? 1 : 0) + (secondaryLabel == VoterLabel.Emotion ? 1 : 0);
    public int RationalLabelCount => 2 - EmotionLabelCount;
    public bool HasColdAttribute => voterAttribute == VoterAttribute.Cold;
    public bool HasDarkAttribute => voterAttribute == VoterAttribute.Dark;
    public float MoveSpeed
    {
        get
        {
            float baseSpeed = HasDarkAttribute ? darkMoveSpeed : normalMoveSpeed;
            float multiplier = Application.isPlaying && PolicyEffectRuntimeManager.HasInstance
                ? PolicyEffectRuntimeManager.Instance.GlobalNpcSpeedMultiplier
                : 1f;

            return baseSpeed * multiplier;
        }
    }
    public bool IsPlayerAligned => convertedSide == PlayerSideSign;
    public bool IsEnemyAligned => convertedSide == EnemySideSign;
    public bool ShouldFollowPlayer => HasDarkAttribute && IsPlayerAligned;

    private void Awake()
    {
        InitializeFromConfig();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            InitializeFromConfig();
        }
    }

    public void InitializeFromConfig()
    {
        currentPosition = config != null
            ? Mathf.Clamp(config.startingPosition, VoterConfig.MIN_POS, VoterConfig.MAX_POS)
            : 0;
        convertedSide = EvaluateSideFromPosition();
        isConverted = convertedSide != NeutralSideSign;
        loyalty = 1f;
    }

    public void AssignConfig(VoterConfig newConfig)
    {
        if (newConfig == null)
        {
            return;
        }

        config = newConfig;
        InitializeFromConfig();
    }

    public void ConfigureIdentity(VoterLabel firstLabel, VoterLabel secondLabel, VoterAttribute attribute)
    {
        primaryLabel = firstLabel;
        secondaryLabel = secondLabel;
        voterAttribute = attribute;
        OnIdentityChanged?.Invoke();
    }

    public void ConvertColdIdentityToEmotion()
    {
        if (voterAttribute != VoterAttribute.Cold)
        {
            return;
        }

        voterAttribute = VoterAttribute.None;

        if (primaryLabel != VoterLabel.Emotion)
        {
            primaryLabel = VoterLabel.Emotion;
        }
        else
        {
            secondaryLabel = VoterLabel.Emotion;
        }

        OnIdentityChanged?.Invoke();
    }

    public int EvaluateSideFromPosition()
    {
        if (currentPosition >= VoterConfig.MAX_POS)
        {
            return PlayerSideSign;
        }

        if (currentPosition <= VoterConfig.MIN_POS)
        {
            return EnemySideSign;
        }

        return NeutralSideSign;
    }
}
