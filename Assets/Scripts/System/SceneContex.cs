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

    public static SceneType GetSceneType(string sceneName) => sceneName switch
    {
        "S0"           => SceneType.Menu,
        "S1"           => SceneType.Intro,
        "TestMVP"      => SceneType.Level,
        "TestSpecial"  => SceneType.Level,
        "TestSmallBoss"=> SceneType.Level,
        "headquarters" => SceneType.Headquarters,
        "TeachScenes"  => SceneType.Tutorial,
        _              => SceneType.Menu
    };

    public static bool IsLevelScene() => CurrentScene == SceneType.Level || CurrentScene == SceneType.Tutorial;
}
