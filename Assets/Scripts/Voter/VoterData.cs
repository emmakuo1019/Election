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
    public event System.Action OnDataUpdated;
    public event System.Action<int> OnConversionSuccess;
    public event System.Action OnConversionLost;

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
    [Tooltip("單邊的最大值。例如 5 代表玩家滿值為 +5，敵人為 -5")]
    public int MaxSupportValue = 5;

    [Header("立場資料")]
    [Tooltip("-5 = 敵方完全支持，+5 = 玩家完全支持。")]
    [SerializeField] private int _currentPosition;
    public int CurrentPosition
    {
        get => _currentPosition;
        set
        {
            if (_currentPosition != value)
            {
                _currentPosition = value;
                OnDataUpdated?.Invoke();
            }
        }
    }

    [SerializeField] private int _convertedSide;
    public int ConvertedSide
    {
        get => _convertedSide;
        set
        {
            if (_convertedSide != value)
            {
                int oldSide = _convertedSide;
                _convertedSide = value;
                
                // 自動同步 isConverted
                isConverted = _convertedSide != NeutralSideSign;
                
                OnDataUpdated?.Invoke();

                // 觸發轉化與流失事件
                if (oldSide == NeutralSideSign && value != NeutralSideSign)
                {
                    OnConversionSuccess?.Invoke(value);
                }
                else if (oldSide != NeutralSideSign && value == NeutralSideSign)
                {
                    OnConversionLost?.Invoke();
                }
            }
        }
    }

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
            float multiplier = Application.isPlaying && PolicyManager.HasInstance
                ? PolicyManager.Instance.GlobalNpcSpeedMultiplier
                : 1f;

            return baseSpeed * multiplier;
        }
    }
    public bool IsPlayerAligned => ConvertedSide == PlayerSideSign;
    public bool IsEnemyAligned => ConvertedSide == EnemySideSign;
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
        else
        {
            // 允許在 Play Mode 時透過 Inspector 手動拉數值測試
            OnDataUpdated?.Invoke();
        }
    }

    public void InitializeData(int baseSupport)
    {
        MaxSupportValue = baseSupport;
        InitializeFromConfig();
    }

    public void InitializeFromConfig()
    {
        CurrentPosition = config != null
            ? Mathf.Clamp(config.startingPosition, -MaxSupportValue, MaxSupportValue)
            : 0;
        ConvertedSide = EvaluateSideFromPosition();
        isConverted = ConvertedSide != NeutralSideSign;
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
        if (CurrentPosition >= MaxSupportValue)
        {
            return PlayerSideSign;
        }

        if (CurrentPosition <= -MaxSupportValue)
        {
            return EnemySideSign;
        }

        return NeutralSideSign;
    }
}
