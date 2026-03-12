using UnityEngine;

public enum VoterTag { Normal, HatePolitics, DontKnow }
public enum CampaignRoute { Rational, Party }

public class VoterData : MonoBehaviour
{
    [Header("立場光譜 (Position Spectrum)")]
    public int currentPosition = 0; // -5 (玩家) <-> 0 (中立) <-> +5 (對手)
    public const int MIN_POS = -5;
    public const int MAX_POS = 5;

    [Header("屬性與標籤")]
    public VoterTag tag = VoterTag.Normal;
    public bool isDieHard = false; // 是否為深色選民
    public bool isConverted = false;

    [Header("動態狀態")]
    public float attentionBar = 0f; // 注意力條 (0-100)
    public int influenceCount = 0; // 當前正在對其演說的人數
}