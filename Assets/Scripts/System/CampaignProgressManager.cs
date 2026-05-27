using UnityEngine;

public static class CampaignProgressManager
{
    private const string COMPLETED_BLOCKS_KEY = "CompletedBlocksCount";
    private const int StandardBlockCount = 3;
    private const int BossStageIndex = 4;

    public static void ResetCampaign()
    {
        PlayerPrefs.SetInt(COMPLETED_BLOCKS_KEY, 0);
        PlayerPrefs.Save();

        Debug.Log("已重置整體選區進度");
    }

    public static void AddCompletedBlock()
    {
        int current = PlayerPrefs.GetInt(COMPLETED_BLOCKS_KEY, 0);
        current++;
        PlayerPrefs.SetInt(COMPLETED_BLOCKS_KEY, current);
        PlayerPrefs.Save();

        Debug.Log("已完成區塊數：" + current);
    }

    public static int GetCompletedBlockCount()
    {
        return PlayerPrefs.GetInt(COMPLETED_BLOCKS_KEY, 0);
    }

    public static int GetTotalBlockCount()
    {
        return StandardBlockCount;
    }

    public static int GetNextBlockIndex()
    {
        return Mathf.Clamp(GetCompletedBlockCount() + 1, 1, StandardBlockCount);
    }

    public static bool IsBlockCompleted(int blockIndex)
    {
        int normalizedBlockIndex = NormalizeStandardBlockIndex(blockIndex);
        return GetCompletedBlockCount() >= normalizedBlockIndex;
    }

    public static bool CanEnterBlock(int blockIndex)
    {
        int normalizedBlockIndex = NormalizeStandardBlockIndex(blockIndex);
        return GetCompletedBlockCount() == normalizedBlockIndex - 1;
    }

    public static bool CanEnterBossStage()
    {
        return GetCompletedBlockCount() >= StandardBlockCount;
    }

    public static int GetBossStageIndex()
    {
        return BossStageIndex;
    }

    private static int NormalizeStandardBlockIndex(int blockIndex)
    {
        return Mathf.Clamp(blockIndex, 1, StandardBlockCount);
    }
}
