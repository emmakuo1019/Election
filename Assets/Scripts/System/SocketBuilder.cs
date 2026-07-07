using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SocketCategory
{
    [Tooltip("用來比對的後綴，例如 '_AC' 或 '_Sign'")]
    public string suffix;

    [Tooltip("專屬此類別的裝飾物 Prefab 清單")]
    public List<GameObject> prefabs;

    [Tooltip("專屬此類別的生成機率 (0 = 不生成, 1 = 必定生成)")]
    [Range(0f, 1f)] 
    public float spawnProbability = 0.7f;
}

/// <summary>
/// 建築物配件隨機自動生成器，對接專案的 PoolManager 以高效管理實體。
/// </summary>
public class SocketBuilder : MonoBehaviour
{
    [Header("生成設定")]
    [Tooltip("定義不同的掛點後綴與對應的生成設定")]
    public List<SocketCategory> categories;

    // 用於暫存找到的掛點 (Sockets)
    private List<Transform> sockets = new List<Transform>();

    // 用於追蹤當前已生成的配件實體，以便後續精確回收
    private List<GameObject> spawnedProps = new List<GameObject>();

    /// <summary>
    /// 自動搜尋建築主體底下所有名稱包含 "Socket" 的掛點
    /// </summary>
    private void FindAllSockets()
    {
        sockets.Clear();
        
        // GetComponentsInChildren(true) 會尋找包含自身及所有隱藏子節點的 Transform
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        
        foreach (Transform child in allChildren)
        {
            // 排除自身，並檢查名稱是否包含 "Socket" (不區分大小寫)
            if (child != this.transform && child.name.ToLower().Contains("socket"))
            {
                sockets.Add(child);
            }
        }

        Debug.Log($"[SocketBuilder] 總共找到 {sockets.Count} 個掛點。");
        if (sockets.Count == 0)
        {
            Debug.LogWarning("[SocketBuilder] 找不到任何名稱包含 'Socket' 的子物件，請檢查模型命名或階層。");
        }
    }

    /// <summary>
    /// 隨機生成配件並掛載至 Socket 上
    /// </summary>
    [ContextMenu("Generate Props")]
    public void GenerateProps()
    {
        // 1. 生成前先清空場景中已生成的配件
        ClearAllProps();
        
        // 2. 重新搜尋並更新掛點清單
        FindAllSockets();

        if (categories == null || categories.Count == 0)
        {
            Debug.LogError("[SocketBuilder] categories 分類清單為空，無法生成物件。");
            return;
        }

        // 3. 遍歷所有的掛點進行判定
        foreach (Transform socket in sockets)
        {
            string socketName = socket.name.ToLower();
            SocketCategory matchedCategory = null;

            // 遍歷 categories 清單，尋找符合後綴的分類
            foreach (var category in categories)
            {
                // 使用 Contains 進行比對（不區分大小寫）
                if (!string.IsNullOrEmpty(category.suffix) && socketName.Contains(category.suffix.ToLower()))
                {
                    matchedCategory = category;
                    break; // 找到第一個符合的分類就停止
                }
            }

            // 如果成功配對到分類
            if (matchedCategory != null)
            {
                // 根據該分類的生成機率進行隨機判定
                if (Random.value <= matchedCategory.spawnProbability)
                {
                    // 若該分類的 prefabs 有防呆不為空
                    if (matchedCategory.prefabs == null || matchedCategory.prefabs.Count == 0)
                    {
                        continue;
                    }

                    // 隨機挑選其中一個 Prefab
                    GameObject selectedPrefab = matchedCategory.prefabs[Random.Range(0, matchedCategory.prefabs.Count)];
                    if (selectedPrefab == null) continue;

                    GameObject propInstance = null;

                    // 【Play Mode 與 Edit Mode 雙軌分離】
                    if (Application.isPlaying)
                    {
                        // 在 Play Mode (遊戲執行中)
                        if (PoolManager.HasInstance)
                        {
                            propInstance = PoolManager.Instance.Get(selectedPrefab, socket.position, socket.rotation);
                        }
                        else
                        {
                            Debug.LogError("[SocketBuilder] 場景中缺少 PoolManager，無法在 Play Mode 生成配件。");
                            continue;
                        }
                    }
                    else
                    {
                        // 在 Edit Mode (編輯模式預覽)
#if UNITY_EDITOR
                        // 使用 PrefabUtility 實體化以保持 Prefab 的藍色連結，並直接指定父節點
                        propInstance = PrefabUtility.InstantiatePrefab(selectedPrefab, socket) as GameObject;
#else
                        Debug.LogWarning("[SocketBuilder] 抱歉，不在 Editor 環境下無法使用 PrefabUtility。");
#endif
                    }

                    if (propInstance != null)
                    {
                        // 將生成的物件掛在該 socket 底下成為子物件
                        propInstance.transform.SetParent(socket);
                        
                        // 【美術防變形規範】：強制重設本地座標與旋轉，防止因為父節點形變導致美術模型變形
                        propInstance.transform.localPosition = Vector3.zero;
                        propInstance.transform.localRotation = Quaternion.identity;

                        // 加入 spawnedProps 清單以便後續精確回收
                        spawnedProps.Add(propInstance);
                        
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            Undo.RegisterCreatedObjectUndo(propInstance, "Generate Props");
                        }
#endif
                        
                        Debug.Log($"[SocketBuilder] 成功在 {socket.name} 上根據分類 '{matchedCategory.suffix}' 掛載了 {selectedPrefab.name}。");
                    }
                }
            }
        }
        
        Debug.Log($"[SocketBuilder] 生成完畢！共找到 {sockets.Count} 個掛點，成功生成 {spawnedProps.Count} 個配件。");
    }

    /// <summary>
    /// 移除並回收所有由本腳本生成的配件
    /// </summary>
    [ContextMenu("Clear All Props")]
    public void ClearAllProps()
    {
        // 1. 遍歷 spawnedProps 清單
        foreach (GameObject prop in spawnedProps)
        {
            // 防呆：如果 prop 已經是 null（可能被手動刪除），直接跳過
            if (prop == null) continue;

            // 【Play Mode 與 Edit Mode 雙軌分離】
            if (Application.isPlaying)
            {
                if (PoolManager.HasInstance)
                {
                    PoolManager.Instance.Release(prop);
                }
                else
                {
                    Destroy(prop);
                }
            }
            else
            {
                // 在 Editor 編輯模式下直接銷毀
                DestroyImmediate(prop);
            }
        }

        // 2. 清空清單，重置狀態
        spawnedProps.Clear();
        sockets.Clear();
        
        Debug.Log("[SocketBuilder] 已清除所有生成的配件。");
    }
}
