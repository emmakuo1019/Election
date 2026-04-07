using UnityEngine;

public static class BlockProgressManager
{
    private const string ROOM_COUNT_KEY = "CurrentBlockRoomCount";
    private const string MAX_ROOM_KEY = "CurrentBlockMaxRooms";

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

    public static int GetMaxRooms()
    {
        return PlayerPrefs.GetInt(MAX_ROOM_KEY, 3);
    }

    public static bool IsLastRoomInBlock()
    {
        return GetCurrentRoomCount() >= GetMaxRooms();
    }

    public static void ClearBlockProgress()
    {
        PlayerPrefs.DeleteKey(ROOM_COUNT_KEY);
        PlayerPrefs.DeleteKey(MAX_ROOM_KEY);
        PlayerPrefs.Save();

        Debug.Log("已清除區塊進度");
    }
}