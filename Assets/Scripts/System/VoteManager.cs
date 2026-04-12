using System;
using UnityEngine;

/// <summary>
/// 投票管理系統
/// 追蹤玩家陣營 vs 敵方陣營的選票數
/// </summary>
public class VoteManager : MonoBehaviour
{
    public static VoteManager Instance { get; private set; }

    [Header("投票統計")]
    private int playerVotes = 0;      // 玩家得票
    private int opponentVotes = 0;    // 敵手得票

    public delegate void VoteChangeDelegate(int playerVotes, int opponentVotes);
    public event VoteChangeDelegate OnVotesChanged;

    public int PlayerVotes => playerVotes;
    public int OpponentVotes => opponentVotes;
    public int TotalVotes => playerVotes + opponentVotes;
    
    public float PlayerVotePercentage => TotalVotes > 0 ? (float)playerVotes / TotalVotes : 0f;
    public float OpponentVotePercentage => TotalVotes > 0 ? (float)opponentVotes / TotalVotes : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[VoteManager] 初始化完成");
    }

    public void ApplyAlignmentChange(int oldSideSign, int newSideSign)
    {
        if (oldSideSign == newSideSign)
        {
            return;
        }

        RemoveVoteFromSide(oldSideSign);
        AddVoteToSide(newSideSign);

        Debug.Log($"📊 得票數更新 | 你: {playerVotes} | 對手: {opponentVotes}");
        OnVotesChanged?.Invoke(playerVotes, opponentVotes);
    }

    private void AddVoteToSide(int sideSign)
    {
        if (sideSign == VoterData.PlayerSideSign)
        {
            playerVotes++;
        }
        else if (sideSign == VoterData.EnemySideSign)
        {
            opponentVotes++;
        }
    }

    private void RemoveVoteFromSide(int sideSign)
    {
        if (sideSign == VoterData.PlayerSideSign)
        {
            playerVotes = Mathf.Max(0, playerVotes - 1);
        }
        else if (sideSign == VoterData.EnemySideSign)
        {
            opponentVotes = Mathf.Max(0, opponentVotes - 1);
        }
    }

    public void ResetVotes()
    {
        playerVotes = 0;
        opponentVotes = 0;
        Debug.Log("🔄 得票數已重置");
        OnVotesChanged?.Invoke(playerVotes, opponentVotes);
    }
}
