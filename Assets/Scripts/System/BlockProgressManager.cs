using UnityEngine;

public static class BlockProgressManager
{
    private const string ROOM_COUNT_KEY = "CurrentBlockRoomCount";
    private const string MAX_ROOM_KEY = "CurrentBlockMaxRooms";
    private const string ROOM_SEQUENCE_KEY = "CurrentBlockRoomSequence";
    private const string NEXT_SCENE_OVERRIDE_KEY = "CurrentBlockNextSceneOverride";
    private const int DefaultBlockRoomCount = 5;
    private const string NormalRoomSceneName = "TestMVP";
    private const string SpecialRoomSceneName = "TestSpecial";
    private const string MapSceneName = "MapScene";
    private const float SpecialRoomChance = 0.2f;

    public static void InitBlock(int maxRooms)
    {
        PlayerPrefs.SetInt(ROOM_COUNT_KEY, 0);
        PlayerPrefs.SetInt(MAX_ROOM_KEY, maxRooms);
        PlayerPrefs.Save();

        Debug.Log("初始化區塊，總房數：" + maxRooms);
    }

    public static void EnterNextRoom()
    {
        int current = PlayerPrefs.GetInt(ROOM_COUNT_KEY, 0);
        current++;
        PlayerPrefs.SetInt(ROOM_COUNT_KEY, current);
        PlayerPrefs.Save();

        Debug.Log("目前區塊進度房數：" + current);
    }

    public static int GetCurrentRoomCount()
    {
        return PlayerPrefs.GetInt(ROOM_COUNT_KEY, 0);
    }

    public static bool HasBlockProgress()
    {
        return PlayerPrefs.HasKey(MAX_ROOM_KEY);
    }

    public static int GetMaxRooms()
    {
        return PlayerPrefs.GetInt(MAX_ROOM_KEY, DefaultBlockRoomCount);
    }

    public static string StartRandomBlock(int maxRooms = DefaultBlockRoomCount)
    {
        int safeMaxRooms = Mathf.Max(1, maxRooms);

        InitBlock(safeMaxRooms);
        SaveRoomSequence(GenerateRoomSequence(safeMaxRooms));
        EnterNextRoom();

        return GetCurrentRoomSceneName();
    }

    public static string GetCurrentRoomSceneName()
    {
        string[] roomSequence = LoadRoomSequence();
        if (roomSequence.Length == 0)
        {
            return NormalRoomSceneName;
        }

        int roomIndex = Mathf.Clamp(GetCurrentRoomCount() - 1, 0, roomSequence.Length - 1);
        return roomSequence[roomIndex];
    }

    public static string AdvanceToNextRoom()
    {
        if (!HasBlockProgress() || IsLastRoomInBlock())
        {
            return null;
        }

        EnterNextRoom();
        return GetCurrentRoomSceneName();
    }

    public static bool IsLastRoomInBlock()
    {
        return GetCurrentRoomCount() >= GetMaxRooms();
    }

    public static bool TryCompleteCurrentBlock()
    {
        if (!HasBlockProgress() || !IsLastRoomInBlock())
        {
            return false;
        }

        CampaignProgressManager.AddCompletedBlock();
        PlayerSkillManager.MarkPendingMapSkillSelection();
        ClearBlockProgress();
        return true;
    }

    public static void FailCurrentBlock()
    {
        ClearBlockProgress();
    }

    public static string GetSceneAfterRoomExit()
    {
        string nextSceneOverride = ConsumeNextSceneOverride();
        if (!string.IsNullOrWhiteSpace(nextSceneOverride))
        {
            return nextSceneOverride;
        }

        if (!HasBlockProgress())
        {
            return null;
        }

        if (IsLastRoomInBlock())
        {
            bool completed = TryCompleteCurrentBlock();
            return completed ? MapSceneName : null;
        }

        return AdvanceToNextRoom();
    }

    public static void ClearBlockProgress()
    {
        PlayerPrefs.DeleteKey(ROOM_COUNT_KEY);
        PlayerPrefs.DeleteKey(MAX_ROOM_KEY);
        PlayerPrefs.DeleteKey(ROOM_SEQUENCE_KEY);
        PlayerPrefs.DeleteKey(NEXT_SCENE_OVERRIDE_KEY);
        PlayerPrefs.Save();

        Debug.Log("已清除區塊進度");
    }

    public static void SetNextSceneOverride(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            PlayerPrefs.DeleteKey(NEXT_SCENE_OVERRIDE_KEY);
        }
        else
        {
            PlayerPrefs.SetString(NEXT_SCENE_OVERRIDE_KEY, sceneName);
        }

        PlayerPrefs.Save();
    }

    private static string[] GenerateRoomSequence(int maxRooms)
    {
        string[] roomSequence = new string[maxRooms];

        for (int i = 0; i < maxRooms; i++)
        {
            roomSequence[i] = Random.value < SpecialRoomChance
                ? SpecialRoomSceneName
                : NormalRoomSceneName;
        }

        return roomSequence;
    }

    private static void SaveRoomSequence(string[] roomSequence)
    {
        PlayerPrefs.SetString(ROOM_SEQUENCE_KEY, string.Join("|", roomSequence));
        PlayerPrefs.Save();
    }

    private static string[] LoadRoomSequence()
    {
        string serialized = PlayerPrefs.GetString(ROOM_SEQUENCE_KEY, string.Empty);
        return string.IsNullOrWhiteSpace(serialized)
            ? System.Array.Empty<string>()
            : serialized.Split('|');
    }

    private static string ConsumeNextSceneOverride()
    {
        string sceneName = PlayerPrefs.GetString(NEXT_SCENE_OVERRIDE_KEY, string.Empty);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return string.Empty;
        }

        PlayerPrefs.DeleteKey(NEXT_SCENE_OVERRIDE_KEY);
        PlayerPrefs.Save();
        return sceneName;
    }
}
