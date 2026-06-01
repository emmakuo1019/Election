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
    [SerializeField] private int defaultPlayerVotes = 50;
    [SerializeField] private int defaultOpponentVotes = 50;

    private int playerVotes;      // 玩家得票
    private int opponentVotes;    // 敵手得票

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
        ResetToDefaultVotes(notifyListeners: false);
    }

    public void ApplyAlignmentChange(int oldSideSign, int newSideSign)
    {
        if (oldSideSign == newSideSign)
        {
            return;
        }

        RemoveVoteFromSide(oldSideSign);
        AddVoteToSide(newSideSign);

        OnVotesChanged?.Invoke(playerVotes, opponentVotes);
    }

    public void SetVotes(int newPlayerVotes, int newOpponentVotes, bool notifyListeners = true)
    {
        playerVotes = Mathf.Max(0, newPlayerVotes);
        opponentVotes = Mathf.Max(0, newOpponentVotes);

        if (notifyListeners)
        {
            OnVotesChanged?.Invoke(playerVotes, opponentVotes);
        }
    }

    public void SetDefaultVotes(int newDefaultPlayerVotes, int newDefaultOpponentVotes, bool applyImmediately = true)
    {
        defaultPlayerVotes = Mathf.Max(0, newDefaultPlayerVotes);
        defaultOpponentVotes = Mathf.Max(0, newDefaultOpponentVotes);

        if (applyImmediately)
        {
            ResetToDefaultVotes();
        }
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
        ResetToDefaultVotes();
    }

    public void ResetToDefaultVotes(bool notifyListeners = true)
    {
        SetVotes(defaultPlayerVotes, defaultOpponentVotes, notifyListeners);
    }
}
