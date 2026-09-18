using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 出口指示器測試工具 - 快速測試雙箭頭功能。
/// 
/// 功能：
/// - F11: 測試單一出口（一個箭頭）
/// - F12: 測試雙出口（兩個箭頭）
/// - F9: 顯示診斷資訊（原有功能）
/// 
/// 使用方式：
///   1. 將此腳本掛在場景中任何物件上
///   2. 按 Play 後按對應快捷鍵測試
/// </summary>
public class ExitIndicatorTester : MonoBehaviour
{
    [Header("快捷鍵")]
    [SerializeField] private KeyCode testSingleExitKey = KeyCode.F11;
    [SerializeField] private KeyCode testDualExitKey = KeyCode.F12;

    [Header("測試設定")]
    [SerializeField] private float testDistance = 10f;
    [SerializeField] private float testSeparation = 5f; // 兩個目標的間距

    private GameObject testTargetLeft;
    private GameObject testTargetRight;

    private void Update()
    {
        if (Input.GetKeyDown(testSingleExitKey))
        {
            TestSingleExit();
        }

        if (Input.GetKeyDown(testDualExitKey))
        {
            TestDualExit();
        }
    }

    /// <summary>
    /// 測試單一出口（一個箭頭）。
    /// </summary>
    [ContextMenu("測試單一出口")]
    public void TestSingleExit()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[ExitIndicatorTester] UIManager.Instance 為 null！");
            return;
        }

        // 隱藏多出口指示器
        MultiExitIndicatorManager multiManager = UIManager.Instance.GetComponent<MultiExitIndicatorManager>();
        multiManager?.HideAll();

        // 創建單一測試目標
        Vector3 targetPos = GetTestPosition(0);
        if (testTargetLeft == null)
        {
            testTargetLeft = CreateTestTarget("TestExit_Single", targetPos, Color.cyan);
        }
        else
        {
            testTargetLeft.transform.position = targetPos;
            testTargetLeft.SetActive(true);
        }

        // 隱藏右側目標
        if (testTargetRight != null)
        {
            testTargetRight.SetActive(false);
        }

        // 使用單一指示器
        UIManager.Instance.ShowExitDirectionIndicator(testTargetLeft.transform);

        Debug.Log("[ExitIndicatorTester] ✓ 單一出口測試：應該看到 1 個箭頭指向青色球體");
    }

    /// <summary>
    /// 測試雙出口（兩個箭頭）。
    /// </summary>
    [ContextMenu("測試雙出口")]
    public void TestDualExit()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[ExitIndicatorTester] UIManager.Instance 為 null！");
            return;
        }

        MultiExitIndicatorManager multiManager = UIManager.Instance.GetComponent<MultiExitIndicatorManager>();
        if (multiManager == null)
        {
            Debug.LogError("[ExitIndicatorTester] MultiExitIndicatorManager 未找到！請先執行 ExitIndicatorSetup。");
            return;
        }

        // 隱藏單一指示器
        UIManager.Instance.HideExitDirectionIndicator();

        // 創建兩個測試目標
        Vector3 leftPos = GetTestPosition(-testSeparation / 2);
        Vector3 rightPos = GetTestPosition(testSeparation / 2);

        if (testTargetLeft == null)
        {
            testTargetLeft = CreateTestTarget("TestExit_Left", leftPos, Color.blue);
        }
        else
        {
            testTargetLeft.transform.position = leftPos;
            testTargetLeft.SetActive(true);
        }

        if (testTargetRight == null)
        {
            testTargetRight = CreateTestTarget("TestExit_Right", rightPos, Color.red);
        }
        else
        {
            testTargetRight.transform.position = rightPos;
            testTargetRight.SetActive(true);
        }

        // 使用多指示器
        List<Transform> exits = new List<Transform> 
        { 
            testTargetLeft.transform, 
            testTargetRight.transform 
        };

        multiManager.ShowMultipleExits(exits);

        Debug.Log("[ExitIndicatorTester] ✓ 雙出口測試：應該看到 2 個箭頭分別指向藍色和紅色球體");
        Debug.Log("[ExitIndicatorTester] 藍色球體在左側，紅色球體在右側");
    }

    /// <summary>
    /// 隱藏所有測試目標。
    /// </summary>
    [ContextMenu("隱藏測試目標")]
    public void HideTestTargets()
    {
        if (testTargetLeft != null) testTargetLeft.SetActive(false);
        if (testTargetRight != null) testTargetRight.SetActive(false);

        // 隱藏所有指示器
        UIManager.Instance?.HideExitDirectionIndicator();
        MultiExitIndicatorManager multiManager = UIManager.Instance?.GetComponent<MultiExitIndicatorManager>();
        multiManager?.HideAll();

        Debug.Log("[ExitIndicatorTester] 已隱藏所有測試目標和箭頭");
    }

    // ── 輔助方法 ──────────────────────────────────────────────────────

    private Vector3 GetTestPosition(float offset)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 forward = player.transform.forward;
            Vector3 right = player.transform.right;
            return player.transform.position + forward * testDistance + right * offset;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 forward = cam.transform.forward;
            Vector3 right = cam.transform.right;
            return cam.transform.position + forward * testDistance + right * offset;
        }

        return new Vector3(offset, 0, testDistance);
    }

    private GameObject CreateTestTarget(string name, Vector3 position, Color color)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.name = name;
        target.transform.position = position;
        target.transform.localScale = Vector3.one * 0.5f;

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        return target;
    }

    private void OnDestroy()
    {
        if (testTargetLeft != null) Destroy(testTargetLeft);
        if (testTargetRight != null) Destroy(testTargetRight);
    }

    private void OnDisable()
    {
        HideTestTargets();
    }
}
