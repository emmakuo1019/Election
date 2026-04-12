using UnityEngine;

public enum VoterTag { Normal, HatePolitics, DontKnow }
public enum CampaignRoute { Rational, Party }

public enum VoterType 
{ 
    Normal, 
    Dark 
}

public class VoterData : MonoBehaviour
{
    public const int PlayerSideSign = 1;
    public const int EnemySideSign = -1;
    public const int NeutralSideSign = 0;

    public event System.Action<VoterType> OnTypeChanged;

    [Header("設定")]
    [SerializeField] private VoterConfig config;

    [Header("類型")]
    public VoterType voterType = VoterType.Normal;

    [Header("基礎數值")]
    [SerializeField] private float normalMoveSpeed = 1.2f;
    [SerializeField] private float darkMoveSpeed = 2f;

    [Header("立場資料")]
    [Tooltip("-5 = 敵方完全支持，+5 = 玩家完全支持。")]
    [SerializeField] private VoterTag fallbackTag = VoterTag.Normal;
    public int currentPosition;
    public int convertedSide;

    [Header("狀態")]
    public bool isConverted = false;
    [Range(0f, 1f)] public float loyalty = 1f;

    public VoterTag Tag => config != null ? config.tag : fallbackTag;
    public float MoveSpeed
    {
        get
        {
            float baseSpeed = voterType == VoterType.Dark ? darkMoveSpeed : normalMoveSpeed;
            float multiplier = Application.isPlaying && PolicyEffectRuntimeManager.HasInstance
                ? PolicyEffectRuntimeManager.Instance.GlobalNpcSpeedMultiplier
                : 1f;

            return baseSpeed * multiplier;
        }
    }
    public bool IsPlayerAligned => convertedSide == PlayerSideSign;
    public bool IsEnemyAligned => convertedSide == EnemySideSign;
    public bool ShouldFollowPlayer => voterType == VoterType.Dark && IsPlayerAligned;

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
        if (config == null)
        {
            return;
        }

        currentPosition = Mathf.Clamp(config.startingPosition, VoterConfig.MIN_POS, VoterConfig.MAX_POS);
        fallbackTag = config.tag;
    }

    public void ApplyType(VoterType type)
    {
        if (voterType == type)
        {
            return;
        }

        voterType = type;
        OnTypeChanged?.Invoke(voterType);
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
