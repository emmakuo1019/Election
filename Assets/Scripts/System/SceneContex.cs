using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 標記目前所在的場景環境
/// 用來控制玩家技能是否可用
/// </summary>
public class SceneContext : MonoBehaviour
{
    public enum SceneType
    {
        Menu,           // S0 - 主菜單
        Intro,          // S1 - 劇情介紹
        Level,          // 真正的關卡
        Headquarters,   // 總部
        Tutorial        // 教學關卡
    }

    // 保留 static 屬性供外部讀取（向後相容），但實際值改為動態查詢
    public static SceneType CurrentScene => GetSceneType(SceneManager.GetActiveScene().name);

    private void Awake()
    {
        // MonoBehaviour 保留在場景中供向後相容，但邏輯已移至靜態方法
    }

    public static SceneType GetSceneType(string sceneName)
    {
        // 特定場景映射
        switch (sceneName)
        {
            case "S0": return SceneType.Menu;
            case "S1": return SceneType.Intro;
            case "headquarters": return SceneType.Headquarters;
            case "TeachScenes": return SceneType.Tutorial;
        }
        
        // [FIX] 所有以 Test 開頭的場景都視為關卡場景
        if (sceneName.StartsWith("Test") || 
            sceneName.StartsWith("Combat") || 
            sceneName.StartsWith("Vote") ||
            sceneName.StartsWith("Elite") ||
            sceneName.StartsWith("Boss"))
        {
            return SceneType.Level;
        }
        
        // 預設為主選單
        return SceneType.Menu;
    }

    public static bool IsLevelScene() => CurrentScene == SceneType.Level || CurrentScene == SceneType.Tutorial;
}
