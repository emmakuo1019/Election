using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 多出口指示器管理器 - 處理雙門或多門場景的方向指引。
/// 
/// 解決問題：
/// - 雙門場景只顯示最近的一個門，導致玩家不知道有兩個選項
/// - 玩家可能錯過重要的路線選擇
/// 
/// 使用方式：
///   1. 在 UIManager 中引用此腳本
///   2. 調用 ShowMultipleExits(List<Transform> exits) 顯示多個出口
///   3. 調用 HideAll() 隱藏所有指示器
/// </summary>
public class MultiExitIndicatorManager : MonoBehaviour
{
    [Header("指示器 Prefab")]
    [Tooltip("出口指示器的 Prefab（包含 ExitDirectionIndicator 腳本）")]
    [SerializeField] private GameObject indicatorPrefab;

    [Header("設定")]
    [Tooltip("是否使用場景中已存在的指示器（由 ExitIndicatorSetup 創建）")]
    [SerializeField] private bool useExistingIndicators = true;

    [Tooltip("最多同時顯示的指示器數量")]
    [SerializeField] private int maxIndicators = 3;

    [Tooltip("不同出口的箭頭顏色")]
    [SerializeField] private Color[] exitColors = new Color[]
    {
        new Color(0.2f, 0.8f, 1f, 1f),  // 藍色 - 左側出口
        new Color(1f, 0.6f, 0.2f, 1f),  // 橘色 - 右側出口
        new Color(0.3f, 1f, 0.3f, 1f)   // 綠色 - 其他出口
    };

    // ── 內部狀態 ──────────────────────────────────────────────────────

    private List<ExitDirectionIndicator> activeIndicators = new List<ExitDirectionIndicator>();
    private Queue<ExitDirectionIndicator> indicatorPool = new Queue<ExitDirectionIndicator>();

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        if (indicatorPrefab == null && !useExistingIndicators)
        {
            Debug.LogError("[MultiExitIndicatorManager] indicatorPrefab 未設定且 useExistingIndicators=false！請在 Inspector 中指定指示器 Prefab。", this);
        }

        // 如果使用場景中已存在的指示器，將它們加入池中
        if (useExistingIndicators)
        {
            InitializeExistingIndicators();
        }
    }

    /// <summary>
    /// 初始化場景中已存在的指示器。
    /// </summary>
    private void InitializeExistingIndicators()
    {
        ExitDirectionIndicator[] existing = GetComponentsInChildren<ExitDirectionIndicator>(true);
        
        if (existing.Length == 0)
        {
            Debug.LogWarning("[MultiExitIndicatorManager] 場景中找不到現有的 ExitDirectionIndicator。");
            return;
        }

        foreach (var indicator in existing)
        {
            if (indicator != null)
            {
                indicator.Hide(); // 初始隱藏
                indicatorPool.Enqueue(indicator);
                Debug.Log($"[MultiExitIndicatorManager] 已加入現有指示器: {indicator.gameObject.name}");
            }
        }

        Debug.Log($"[MultiExitIndicatorManager] 初始化完成，找到 {existing.Length} 個現有指示器");
    }

    // ── 公開 API ──────────────────────────────────────────────────────

    /// <summary>
    /// 顯示多個出口的方向指示器。
    /// </summary>
    /// <param name="exits">出口門的 Transform 列表</param>
    public void ShowMultipleExits(List<Transform> exits)
    {
        if (exits == null || exits.Count == 0)
        {
            Debug.LogWarning("[MultiExitIndicatorManager] 沒有出口需要顯示。");
            HideAll();
            return;
        }

        // 如果只有一個出口，使用原本的單一指示器邏輯
        if (exits.Count == 1)
        {
            UIManager.Instance?.ShowExitDirectionIndicator(exits[0]);
            return;
        }

        // 隱藏原本的單一指示器
        UIManager.Instance?.HideExitDirectionIndicator();

        // 確保有足夠的指示器
        int needCount = Mathf.Min(exits.Count, maxIndicators);
        while (activeIndicators.Count < needCount)
        {
            ExitDirectionIndicator indicator = GetOrCreateIndicator();
            if (indicator != null)
                activeIndicators.Add(indicator);
            else
                break;
        }

        // 設定每個指示器的目標
        for (int i = 0; i < needCount && i < activeIndicators.Count; i++)
        {
            ExitDirectionIndicator indicator = activeIndicators[i];
            indicator.SetTarget(exits[i]);

            // 只在使用 Prefab 創建時設定顏色（現有指示器保留預設顏色）
            if (!useExistingIndicators && i < exitColors.Length)
            {
                Image arrowImage = indicator.GetComponentInChildren<Image>();
                if (arrowImage != null)
                    arrowImage.color = exitColors[i];
            }

            indicator.Show();
            Debug.Log($"[MultiExitIndicatorManager] 指示器 {i} 指向：{exits[i].name}");
        }
        
        // 如果指示器不夠，警告
        if (activeIndicators.Count < needCount)
        {
            Debug.LogWarning($"[MultiExitIndicatorManager] 只有 {activeIndicators.Count} 個指示器，但需要 {needCount} 個！請確認 ExitIndicatorSetup 已創建第二個箭頭。");
        }

        // 隱藏多餘的指示器
        for (int i = needCount; i < activeIndicators.Count; i++)
        {
            activeIndicators[i].Hide();
        }
    }

    /// <summary>
    /// 顯示單一出口（回退到 UIManager 的原始行為）。
    /// </summary>
    public void ShowSingleExit(Transform exit)
    {
        HideAll();
        UIManager.Instance?.ShowExitDirectionIndicator(exit);
    }

    /// <summary>
    /// 隱藏所有指示器。
    /// </summary>
    public void HideAll()
    {
        foreach (var indicator in activeIndicators)
        {
            if (indicator != null)
            {
                indicator.Hide();
                indicatorPool.Enqueue(indicator);
            }
        }
        activeIndicators.Clear();

        // 同時隱藏原本的單一指示器
        UIManager.Instance?.HideExitDirectionIndicator();
    }

    // ── 內部邏輯 ──────────────────────────────────────────────────────

    private ExitDirectionIndicator GetOrCreateIndicator()
    {
        // 從物件池取得
        if (indicatorPool.Count > 0)
        {
            ExitDirectionIndicator pooledIndicator = indicatorPool.Dequeue();
            if (pooledIndicator != null)
            {
                return pooledIndicator;
            }
        }

        // 如果使用現有指示器但池已空，不創建新的
        if (useExistingIndicators)
        {
            Debug.LogWarning("[MultiExitIndicatorManager] 現有指示器已用完，無法顯示更多出口。");
            return null;
        }

        // 創建新指示器（從 Prefab）
        if (indicatorPrefab == null)
        {
            Debug.LogError("[MultiExitIndicatorManager] 無法創建指示器：Prefab 未設定。", this);
            return null;
        }

        GameObject obj = Instantiate(indicatorPrefab, transform);
        ExitDirectionIndicator newIndicator = obj.GetComponent<ExitDirectionIndicator>();

        if (newIndicator == null)
        {
            Debug.LogError($"[MultiExitIndicatorManager] Prefab 缺少 ExitDirectionIndicator 腳本：{indicatorPrefab.name}", this);
            Destroy(obj);
            return null;
        }

        return newIndicator;
    }

    // ── 輔助方法 ──────────────────────────────────────────────────────

    /// <summary>
    /// 取得當前啟用的指示器數量。
    /// </summary>
    public int GetActiveIndicatorCount()
    {
        return activeIndicators.Count;
    }

    /// <summary>
    /// 檢查是否有任何指示器正在顯示。
    /// </summary>
    public bool HasActiveIndicators()
    {
        foreach (var indicator in activeIndicators)
        {
            if (indicator != null && indicator.HasTarget())
                return true;
        }
        return false;
    }
}
