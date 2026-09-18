using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 出口指示器設置助手 - 在執行時自動創建第二個箭頭（如果需要）。
/// 
/// 功能：
/// 1. 在 Awake 時檢查是否已經有第二個箭頭
/// 2. 如果沒有，複製現有的 ExitIndicatorPanel 創建第二個
/// 3. 調整顏色區分（藍色/橘色）
/// 4. 自動設置 MultiExitIndicatorManager
/// 
/// 使用方式：
///   1. 將此腳本掛在 S0.unity 的 UIManager 上
///   2. 指定 exitIndicatorPrefab（現有的 ExitIndicatorPanel）
///   3. 按 Play，會自動創建第二個箭頭並設置完成
/// </summary>
[ExecuteInEditMode]
public class ExitIndicatorSetup : MonoBehaviour
{
    [Header("現有元件")]
    [Tooltip("現有的第一個出口指示器（ExitIndicatorPanel）")]
    [SerializeField] private ExitDirectionIndicator existingIndicator;

    [Header("設定")]
    [Tooltip("第一個箭頭的顏色（左門/藍色）")]
    [SerializeField] private Color firstArrowColor = new Color(0.2f, 0.8f, 1f, 1f);

    [Tooltip("第二個箭頭的顏色（右門/橘色）")]
    [SerializeField] private Color secondArrowColor = new Color(1f, 0.6f, 0.2f, 1f);

    [Tooltip("是否在 Awake 時自動設置")]
    [SerializeField] private bool autoSetup = true;

    [Header("狀態（唯讀）")]
    [SerializeField] private ExitDirectionIndicator secondIndicator;
    [SerializeField] private MultiExitIndicatorManager multiManager;
    [SerializeField] private bool isSetupComplete = false;

    private void Awake()
    {
        if (!Application.isPlaying || !autoSetup || isSetupComplete)
            return;

        SetupMultipleExitIndicators();
    }

    /// <summary>
    /// 設置多出口指示器系統。
    /// </summary>
    [ContextMenu("自動設置雙箭頭系統")]
    public void SetupMultipleExitIndicators()
    {
        Debug.Log("[ExitIndicatorSetup] 開始設置雙箭頭系統...");

        // 1. 驗證現有指示器
        if (existingIndicator == null)
        {
            existingIndicator = GetComponentInChildren<ExitDirectionIndicator>(true);
            if (existingIndicator == null)
            {
                Debug.LogError("[ExitIndicatorSetup] 找不到現有的 ExitDirectionIndicator！請在 Inspector 中指定。", this);
                return;
            }
        }

        // 2. 檢查是否已經有第二個指示器
        ExitDirectionIndicator[] indicators = GetComponentsInChildren<ExitDirectionIndicator>(true);
        if (indicators.Length >= 2)
        {
            Debug.Log($"[ExitIndicatorSetup] 已經有 {indicators.Length} 個指示器，跳過創建。");
            existingIndicator = indicators[0];
            secondIndicator = indicators[1];
            isSetupComplete = true;
            SetupColors();
            SetupMultiManager();
            return;
        }

        // 3. 複製現有指示器創建第二個
        GameObject firstPanel = existingIndicator.gameObject;
        GameObject secondPanel = Instantiate(firstPanel, firstPanel.transform.parent);
        secondPanel.name = "ExitIndicatorPanel_Second";

        secondIndicator = secondPanel.GetComponent<ExitDirectionIndicator>();
        if (secondIndicator == null)
        {
            Debug.LogError("[ExitIndicatorSetup] 複製的物件缺少 ExitDirectionIndicator 腳本！", secondPanel);
            Destroy(secondPanel);
            return;
        }

        Debug.Log("[ExitIndicatorSetup] ✓ 已創建第二個指示器");

        // 4. 設置顏色
        SetupColors();

        // 5. 設置 MultiExitIndicatorManager
        SetupMultiManager();

        // 6. 初始隱藏兩個指示器（等待 RoomExitController 調用）
        existingIndicator.Hide();
        secondIndicator.Hide();

        isSetupComplete = true;
        Debug.Log("[ExitIndicatorSetup] ✓ 雙箭頭系統設置完成！");
    }

    /// <summary>
    /// 設置箭頭顏色。
    /// </summary>
    private void SetupColors()
    {
        // 第一個箭頭
        Image firstArrowImage = existingIndicator.GetComponentInChildren<Image>();
        if (firstArrowImage != null)
        {
            firstArrowImage.color = firstArrowColor;
            Debug.Log($"[ExitIndicatorSetup] 第一個箭頭顏色: {firstArrowColor}");
        }

        // 第二個箭頭
        if (secondIndicator != null)
        {
            Image secondArrowImage = secondIndicator.GetComponentInChildren<Image>();
            if (secondArrowImage != null)
            {
                secondArrowImage.color = secondArrowColor;
                Debug.Log($"[ExitIndicatorSetup] 第二個箭頭顏色: {secondArrowColor}");
            }
        }
    }

    /// <summary>
    /// 設置 MultiExitIndicatorManager。
    /// </summary>
    private void SetupMultiManager()
    {
        // 檢查是否已經有 MultiExitIndicatorManager
        multiManager = GetComponent<MultiExitIndicatorManager>();
        if (multiManager == null)
        {
            multiManager = gameObject.AddComponent<MultiExitIndicatorManager>();
            Debug.Log("[ExitIndicatorSetup] ✓ 已添加 MultiExitIndicatorManager");
        }

        // 創建指示器列表供 MultiManager 使用
        Debug.Log("[ExitIndicatorSetup] ✓ MultiExitIndicatorManager 已設置");
    }

    /// <summary>
    /// 取得所有指示器。
    /// </summary>
    public ExitDirectionIndicator[] GetAllIndicators()
    {
        return GetComponentsInChildren<ExitDirectionIndicator>(true);
    }

    /// <summary>
    /// 重置設置（清除第二個指示器）。
    /// </summary>
    [ContextMenu("重置雙箭頭系統")]
    public void ResetSetup()
    {
        if (secondIndicator != null)
        {
            if (Application.isPlaying)
                Destroy(secondIndicator.gameObject);
            else
                DestroyImmediate(secondIndicator.gameObject);

            secondIndicator = null;
        }

        if (multiManager != null)
        {
            if (Application.isPlaying)
                Destroy(multiManager);
            else
                DestroyImmediate(multiManager);

            multiManager = null;
        }

        isSetupComplete = false;
        Debug.Log("[ExitIndicatorSetup] 已重置雙箭頭系統");
    }

    /// <summary>
    /// 顯示當前設置狀態。
    /// </summary>
    [ContextMenu("顯示設置狀態")]
    public void ShowStatus()
    {
        Debug.Log("========== ExitIndicatorSetup 狀態 ==========");
        Debug.Log($"現有指示器: {(existingIndicator != null ? existingIndicator.gameObject.name : "null")}");
        Debug.Log($"第二指示器: {(secondIndicator != null ? secondIndicator.gameObject.name : "null")}");
        Debug.Log($"MultiManager: {(multiManager != null ? "已設置" : "未設置")}");
        Debug.Log($"設置完成: {isSetupComplete}");
        
        ExitDirectionIndicator[] all = GetComponentsInChildren<ExitDirectionIndicator>(true);
        Debug.Log($"總共找到 {all.Length} 個 ExitDirectionIndicator");
        for (int i = 0; i < all.Length; i++)
        {
            Debug.Log($"  [{i}] {all[i].gameObject.name}");
        }
        Debug.Log("=============================================");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor 專用：創建 Prefab（儲存到 Assets/Prefabs/UI）。
    /// </summary>
    [ContextMenu("創建 ExitIndicator Prefab")]
    public void CreatePrefab()
    {
        if (existingIndicator == null)
        {
            Debug.LogError("[ExitIndicatorSetup] 請先指定 existingIndicator");
            return;
        }

        string prefabPath = "Assets/Prefabs/UI/ExitIndicatorPanel.prefab";
        
        // 確保目錄存在
        string directory = System.IO.Path.GetDirectoryName(prefabPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
            UnityEditor.AssetDatabase.Refresh();
        }

        // 創建 Prefab
        GameObject prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(
            existingIndicator.gameObject, 
            prefabPath
        );

        if (prefab != null)
        {
            Debug.Log($"[ExitIndicatorSetup] ✓ Prefab 已創建: {prefabPath}");
            UnityEditor.Selection.activeObject = prefab;
        }
        else
        {
            Debug.LogError("[ExitIndicatorSetup] Prefab 創建失敗！");
        }
    }
#endif
}
