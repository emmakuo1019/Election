using UnityEngine;

public enum VoterTag { Normal, HatePolitics, DontKnow }
public enum CampaignRoute { Rational, Party }

public class VoterData : MonoBehaviour
{
    [Header("設定檔")]
    public VoterConfig config;

    public VoterTag Tag       => config != null ? config.tag       : VoterTag.Normal;
    public bool     IsDieHard => config != null ? config.isDieHard : false;

    [HideInInspector] public int  currentPosition;
    [HideInInspector] public bool isConverted;

    // 1 = player, -1 = opponent, 0 = none
    [HideInInspector] public int convertedSide;

    void Awake()
    {
        currentPosition = config != null ? config.startingPosition : 0;
        isConverted     = false;
        convertedSide   = 0;
    }
}