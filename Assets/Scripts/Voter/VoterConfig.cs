// /Assets/Scripts/NPCScripts/VoterConfig.cs
using UnityEngine;

/// <summary>
/// 選民靜態設定，在 Assets 裡以 ScriptableObject 共享。
/// 建立：Assets 右鍵 → Create → Election/Voter Config
/// </summary>
[CreateAssetMenu(fileName = "VoterConfig", menuName = "Election/Voter Config")]
public class VoterConfig : ScriptableObject
{
    [Header("立場光譜")]
    public int startingPosition = 0;    // -5 (玩家陣營) ↔ +5 (對手陣營)
    public const int MIN_POS = -5;
    public const int MAX_POS = 5;

    [Header("屬性與標籤")]
    public VoterTag tag = VoterTag.Normal;
    public bool isDieHard = false;
}