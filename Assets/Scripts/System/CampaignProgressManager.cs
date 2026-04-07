using UnityEngine;

public static class CampaignProgressManager
{
    private const string COMPLETED_BLOCKS_KEY = "CompletedBlocksCount";

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

    public static bool IsBossUnlocked()
    {
        return GetCompletedBlockCount() >= 3;
    }
}