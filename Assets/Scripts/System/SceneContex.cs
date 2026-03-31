using UnityEngine;

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
        Headquarters    // 總部
    }

    public static SceneType CurrentScene { get; private set; } = SceneType.Menu;

    private void Awake()
    {
        string sceneName = gameObject.scene.name;
        
        CurrentScene = sceneName switch
        {
            "S0" => SceneType.Menu,
            "S1" => SceneType.Intro,
            "TestMVP" => SceneType.Level,
            "TestMVP02" => SceneType.Level,
            "headquarters" => SceneType.Headquarters,
            _ => SceneType.Menu
        };

        Debug.Log($"[SceneContext] 場景: {CurrentScene}");
    }

    public static bool IsLevelScene() => CurrentScene == SceneType.Level;
}