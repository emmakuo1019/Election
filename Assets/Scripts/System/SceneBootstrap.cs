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


        if (autoLoadS0IfMissingUIManager)
        {
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

        
        if (autoCreateMainCamera)
        {
            CreateMainCamera();
            return true;
        }
        else
        {
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

        return false;
    }

    // ── 修復方法 ──────────────────────────────────────────────────────

    private void CreateMainCamera()
    {

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

    }

    // ── 輔助方法 ──────────────────────────────────────────────────────

    private void LogVerbose(string message)
    {
        if (verboseLogging)
        {
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
    }
#endif
}
