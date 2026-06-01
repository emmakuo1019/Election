using UnityEngine;

public class RoomResultCalculator : MonoBehaviour
{
    [Header("房間結算 MP 設定")]
    [SerializeField] private int baseRoomMPReward = 20;

    [Header("是否輸出詳細 Debug")]
    [SerializeField] private bool showDebugLog = true;

    public int CalculateAndRewardMP()
    {
        int totalVoters = GetTotalVoters();
        int playerSupporters = GetPlayerSupporters();

        if (totalVoters <= 0)
        {
            if (showDebugLog)
                Debug.LogWarning("⚠️ 本房間找不到任何有效選民資料，無法計算支持率");

            return 0;
        }

        float supportRate = (float)playerSupporters / totalVoters;
        int rewardMP = Mathf.RoundToInt(baseRoomMPReward * supportRate);

        PlayerMPSystem.Instance?.RecoverMP(rewardMP);

        if (showDebugLog)
        {
        }

        return rewardMP;
    }

    public float GetSupportRate()
    {
        int totalVoters = GetTotalVoters();
        int playerSupporters = GetPlayerSupporters();

        if (totalVoters <= 0) return 0f;

        return (float)playerSupporters / totalVoters;
    }

    public float GetGlobalSupportRate()
    {
        if (VoteManager.Instance == null)
        {
            return 0.5f;
        }

        return VoteManager.Instance.PlayerVotePercentage;
    }

    public int GetTotalVoters()
    {
        VoterLogic[] allVoters = FindObjectsByType<VoterLogic>(FindObjectsSortMode.None);

        if (allVoters == null || allVoters.Length == 0) return 0;

        int total = 0;
        foreach (var voter in allVoters)
        {
            if (TryGetVoterData(voter, out _))
                total++;
        }

        return total;
    }

    public int GetPlayerSupporters()
    {
        VoterLogic[] allVoters = FindObjectsByType<VoterLogic>(FindObjectsSortMode.None);

        if (allVoters == null || allVoters.Length == 0) return 0;

        int count = 0;
        foreach (var voter in allVoters)
        {
            if (TryGetVoterData(voter, out VoterData voterData) && voterData.IsPlayerAligned)
                count++;
        }

        return count;
    }

    private bool TryGetVoterData(VoterLogic voter, out VoterData voterData)
    {
        voterData = null;

        if (voter == null)
        {
            return false;
        }

        voterData = voter.GetComponent<VoterData>();
        return voterData != null;
    }
}
