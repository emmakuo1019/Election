using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SocketCategory
{
    [Tooltip("用來比對的後綴，例如 '_AC' 或 '_Sign'。\n" +
             "【招牌規範】直式與橫式招牌已統一使用 '_Sign' 後綴，\n" +
             "請勿再新增 '_SignH' 或 '_SignV' 等細分後綴，以避免混搭問題。")]
    public string suffix;

    [Tooltip("專屬此類別的裝飾物 Prefab 清單（橫式與直式招牌 Prefab 請放在同一個 _Sign 分類中）")]
    public List<GameObject> prefabs;

    [Tooltip("專屬此類別的生成機率 (0 = 不生成, 1 = 必定生成)")]
    [Range(0f, 1f)]
    public float spawnProbability = 0.7f;

    [Tooltip("生成後隨機套用的材質候選清單（留空則保持 Prefab 原本材質）")]
    public List<Material> randomMaterials;
}

/// <summary>
/// 建築物配件隨機自動生成器，對接專案的 PoolManager 以高效管理實體。
/// 同時支援隨機替換建築主體的材質球。
/// </summary>
public class SocketBuilder : MonoBehaviour
{
    [Header("生成設定")]
    [Tooltip("定義不同的掛點後綴與對應的生成設定")]
    public List<SocketCategory> categories;

    [Header("建築材質隨機化")]
    [Tooltip("建築主體的 MeshRenderer（整棟共用一張材質）")]
    public Renderer buildingRenderer;

    [Tooltip("隨機候選材質清單，生成時從中隨機挑一個套用")]
    public List<Material> buildingMaterials;

    // 用於暫存找到的掛點 (Sockets)
    private List<Transform> sockets = new List<Transform>();

    // 用於追蹤當前已生成的配件實體，以便後續精確回收
    private List<GameObject> spawnedProps = new List<GameObject>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (categories == null) return;

        foreach (var category in categories)
        {
            if (string.IsNullOrEmpty(category.suffix)) continue;

            string lower = category.suffix.ToLower();
            if (lower == "_signh" || lower == "_signv")
            {
                Debug.LogWarning(
                    $"[SocketBuilder] 偵測到已廢棄的招牌後綴 '{category.suffix}'。\n" +
                    "直式與橫式招牌已統一合併為 '_Sign'，請將此 category 的後綴改為 '_Sign'，" +
                    "並將所有招牌 Prefab 集中放入該分類。",
                    this
                );
            }
        }
    }
#endif

    /// <summary>
    /// 自動搜尋建築主體底下所有名稱包含 "Socket" 的掛點
    /// </summary>
    private void FindAllSockets()
    {
        sockets.Clear();

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
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
    /// 從候選清單隨機挑一個材質套用，並備份原始材質以便還原。
    /// </summary>
    private void RandomizeMaterial()
    {
        if (buildingRenderer == null || buildingMaterials == null || buildingMaterials.Count == 0) return;

        // 備份原始材質
        originalMaterial = buildingRenderer.sharedMaterial;

        // 隨機挑選並套用
        Material picked = buildingMaterials[Random.Range(0, buildingMaterials.Count)];
        if (picked == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RecordObject(buildingRenderer, "Randomize Building Material");
#endif
        buildingRenderer.sharedMaterial = picked;

        Debug.Log($"[SocketBuilder] 材質替換：{buildingRenderer.name} → {picked.name}");
    }

    /// <summary>
    /// 還原建築材質為替換前的原始材質。
    /// </summary>
    private void RestoreMaterial()
    {
        if (buildingRenderer == null || originalMaterial == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.RecordObject(buildingRenderer, "Restore Building Material");
#endif
        buildingRenderer.sharedMaterial = originalMaterial;
        originalMaterial = null;
    }

    /// <summary>
    /// 隨機生成配件並掛載至 Socket 上，同時隨機替換建築材質。
    /// </summary>
    [ContextMenu("Generate Props")]
    public void GenerateProps()
    {
        // 1. 生成前先清空已生成的配件並還原材質
        ClearAllProps();

        // 2. 重新搜尋掛點
        FindAllSockets();

        // 3. 隨機替換建築主體材質
        RandomizeMaterial();

        if (categories == null || categories.Count == 0)
        {
            Debug.LogError("[SocketBuilder] categories 分類清單為空，無法生成物件。");
            return;
        }

        // 4. 遍歷所有掛點進行判定
        foreach (Transform socket in sockets)
        {
            string socketName = socket.name.ToLower();
            SocketCategory matchedCategory = null;

            foreach (var category in categories)
            {
                if (!string.IsNullOrEmpty(category.suffix) && socketName.Contains(category.suffix.ToLower()))
                {
                    matchedCategory = category;
                    break;
                }
            }

            if (matchedCategory == null) continue;
            if (Random.value > matchedCategory.spawnProbability) continue;
            if (matchedCategory.prefabs == null || matchedCategory.prefabs.Count == 0) continue;

            GameObject selectedPrefab = matchedCategory.prefabs[Random.Range(0, matchedCategory.prefabs.Count)];
            if (selectedPrefab == null) continue;

            GameObject propInstance = null;

            if (Application.isPlaying)
            {
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
#if UNITY_EDITOR
                propInstance = PrefabUtility.InstantiatePrefab(selectedPrefab, socket) as GameObject;
#endif
            }

            if (propInstance != null)
            {
                propInstance.transform.SetParent(socket);
                propInstance.transform.localPosition = Vector3.zero;
                propInstance.transform.localRotation = Quaternion.identity;

                // 若該 category 有設定隨機材質，套用到 instance 底下所有 Renderer
                if (matchedCategory.randomMaterials != null && matchedCategory.randomMaterials.Count > 0)
                {
                    Material picked = matchedCategory.randomMaterials[Random.Range(0, matchedCategory.randomMaterials.Count)];
                    if (picked != null)
                    {
                        foreach (Renderer r in propInstance.GetComponentsInChildren<Renderer>(true))
                            r.sharedMaterial = picked;
                    }
                }

                spawnedProps.Add(propInstance);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.RegisterCreatedObjectUndo(propInstance, "Generate Props");
#endif

                Debug.Log($"[SocketBuilder] 成功在 {socket.name} 上掛載了 {selectedPrefab.name}。");
            }
        }

        Debug.Log($"[SocketBuilder] 生成完畢！掛點 {sockets.Count} 個，配件 {spawnedProps.Count} 個。");
    }

    /// <summary>
    /// 移除並回收所有由本腳本生成的配件，同時還原建築材質。
    /// </summary>
    [ContextMenu("Clear All Props")]
    public void ClearAllProps()
    {
        foreach (GameObject prop in spawnedProps)
        {
            if (prop == null) continue;

            if (Application.isPlaying)
            {
                if (PoolManager.HasInstance)
                    PoolManager.Instance.Release(prop);
                else
                    Destroy(prop);
            }
            else
            {
                DestroyImmediate(prop);
            }
        }

        spawnedProps.Clear();
        sockets.Clear();

        // 還原建築材質
        RestoreMaterial();

        Debug.Log("[SocketBuilder] 已清除所有生成的配件並還原材質。");
    }
}
