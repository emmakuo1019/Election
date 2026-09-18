using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 場景啟動檢查器 - 確保場景擁有所有必要的系統元件。
/// 
/// 解決問題：
/// - 直接開啟 TeachScenes.unity 按 Play 時缺少 UIManager
/// - 教學場景沒有 MainCamera 導致 ExitDirectionIndicator 失效
/// - 避免運行時 NullReferenceException
/// 
/// 使用方式：
///   1. 將此腳本掛在每個場景的根物件上（或創建 _SceneBootstrap 空物件）
///   2. 勾選需要驗證的元件選項
///   3. 按 Play 時會自動檢查並報告缺失
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    [Header("必要系統檢查")]
    [Tooltip("是否檢查 UIManager 是否存在")]
    [SerializeField] private bool requireUIManager = true;

    [Tooltip("是否檢查 MainCamera 是否存在")]
    [SerializeField] private bool requireMainCamera = true;

    [Tooltip("是否檢查 GameDB 是否存在")]
    [SerializeField] private bool requireGameDB = false;

    [Header("修復選項")]
    [Tooltip("如果缺少 UIManager，是否自動載入 S0 場景")]
    [SerializeField] private bool autoLoadS0IfMissingUIManager = false;

    [Tooltip("如果缺少 MainCamera，是否自動創建")]
    [SerializeField] private bool autoCreateMainCamera = true;

    [Header("除錯")]
    [Tooltip("顯示詳細的檢查日誌")]
    [SerializeField] private bool verboseLogging = true;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        LogVerbose($"[SceneBootstrap] 場景 '{sceneName}' 啟動檢查開始...");

        bool allChecksPass = true;

        // 檢查 UIManager
        if (requireUIManager)
        {
            allChecksPass &= CheckUIManager();
        }

        // 檢查 MainCamera
        if (requireMainCamera)
        {
            allChecksPass &= CheckMainCamera();
        }

        // 檢查 GameDB
        if (requireGameDB)
        {
            allChecksPass &= CheckGameDB();
        }

        // 總結
        if (allChecksPass)
        {
            LogVerbose($"[SceneBootstrap] 場景 '{sceneName}' 所有檢查通過 ✓");
        }
        else
        {
            Debug.LogWarning($"[SceneBootstrap] 場景 '{sceneName}' 部分檢查失敗，請查看上方日誌。");
        }
    }

    // ── 檢查方法 ──────────────────────────────────────────────────────

    private bool CheckUIManager()
    {
        if (UIManager.Instance != null)
        {
            LogVerbose("[SceneBootstrap] ✓ UIManager 已存在。");
            return true;
        }

        Debug.LogError($"[SceneBootstrap] ✗ UIManager 不存在！這會導致 UI 功能失效。");
        Debug.LogError("[SceneBootstrap] 原因：此場景可能是直接開啟並按 Play，而不是從 S0.unity 啟動。");
        Debug.LogError("[SceneBootstrap] 解決方法：請從 S0.unity 開始遊戲，或在 Build Settings 中將 S0.unity 設為第一個場景。");

        if (autoLoadS0IfMissingUIManager)
        {
            Debug.LogWarning("[SceneBootstrap] 正在自動載入 S0.unity...");
            SceneManager.LoadScene("S0");
        }

        return false;
    }

    private bool CheckMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            LogVerbose($"[SceneBootstrap] ✓ MainCamera 已存在：{mainCamera.gameObject.name}");
            return true;
        }

        Debug.LogError("[SceneBootstrap] ✗ MainCamera 不存在！這會導致 ExitDirectionIndicator 等 UI 元件失效。");
        Debug.LogError("[SceneBootstrap] 原因：場景中沒有標記為 'MainCamera' 的攝影機。");
        
        if (autoCreateMainCamera)
        {
            CreateMainCamera();
            return true;
        }
        else
        {
            Debug.LogError("[SceneBootstrap] 解決方法：在場景中新增 Camera 並設定 Tag 為 'MainCamera'，或啟用 autoCreateMainCamera 選項。");
            return false;
        }
    }

    private bool CheckGameDB()
    {
        if (GameDB.Instance != null)
        {
            LogVerbose("[SceneBootstrap] ✓ GameDB 已存在。");
            return true;
        }

        Debug.LogWarning("[SceneBootstrap] ✗ GameDB 不存在！這可能導致任務系統無法運作。");
        return false;
    }

    // ── 修復方法 ──────────────────────────────────────────────────────

    private void CreateMainCamera()
    {
        Debug.LogWarning("[SceneBootstrap] 正在自動創建 MainCamera...");

        GameObject cameraObj = new GameObject("Main Camera (Auto-Created)");
        Camera camera = cameraObj.AddComponent<Camera>();
        cameraObj.tag = "MainCamera";

        // 設定基本參數（根據專案需求調整）
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.backgroundColor = Color.black;
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;

        // 添加 AudioListener（如果需要）
        if (cameraObj.GetComponent<AudioListener>() == null)
        {
            cameraObj.AddComponent<AudioListener>();
        }

        // 設定位置（根據專案需求調整）
        cameraObj.transform.position = new Vector3(0f, 1.5f, -10f);
        cameraObj.transform.rotation = Quaternion.identity;

        Debug.LogWarning($"[SceneBootstrap] MainCamera 已創建：{cameraObj.name}");
        Debug.LogWarning("[SceneBootstrap] 注意：這是臨時解決方案，建議在場景中手動設置攝影機。");
    }

    // ── 輔助方法 ──────────────────────────────────────────────────────

    private void LogVerbose(string message)
    {
        if (verboseLogging)
        {
            Debug.Log(message);
        }
    }

    // ── Editor 輔助 ───────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("執行場景檢查")]
    private void ManualCheck()
    {
        Awake();
    }

    [ContextMenu("顯示當前場景資訊")]
    private void ShowSceneInfo()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"=== 場景資訊：{sceneName} ===");
        Debug.Log($"UIManager: {(UIManager.Instance != null ? "存在" : "不存在")}");
        Debug.Log($"MainCamera: {(Camera.main != null ? Camera.main.gameObject.name : "不存在")}");
        Debug.Log($"GameDB: {(GameDB.Instance != null ? "存在" : "不存在")}");
        Debug.Log($"總 Camera 數量: {FindObjectsOfType<Camera>().Length}");
        Debug.Log($"總 Canvas 數量: {FindObjectsOfType<Canvas>().Length}");
        Debug.Log("================================");
    }
#endif
}
