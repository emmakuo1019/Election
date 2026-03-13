// /Assets/Scripts/NPCScripts/VoterData.cs
using UnityEngine;

public enum VoterTag { Normal, HatePolitics, DontKnow }
public enum CampaignRoute { Rational, Party }

/// <summary>
/// 選民 Runtime 動態狀態，掛在 GameObject 上。
/// 靜態設定從 VoterConfig (ScriptableObject) 讀取。
/// </summary>
public class VoterData : MonoBehaviour
{
    [Header("設定檔")]
    public VoterConfig config;

    // --- 唯讀代理，方便外部存取 config 值 ---
    public VoterTag Tag       => config != null ? config.tag       : VoterTag.Normal;
    public bool     IsDieHard => config != null ? config.isDieHard : false;

    // --- Runtime 動態狀態 ---
    [HideInInspector] public int   currentPosition;
    [HideInInspector] public bool  isConverted;
    [HideInInspector] public float attentionBar;
    [HideInInspector] public int   influenceCount;

    void Awake()
    {
        // 從 config 初始化 runtime 狀態
        currentPosition = config != null ? config.startingPosition : 0;
        isConverted     = false;
        attentionBar    = 0f;
        influenceCount  = 0;
    }
}