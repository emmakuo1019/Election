using System.Collections.Generic;
using UnityEngine;

/// <summary>在既有出口附近建立兩扇選路門；門本身不寫入戰役資料。</summary>
public static class RouteDoorSpawner
{
    private static readonly List<GameObject> SpawnedDoors = new List<GameObject>();

    public static bool ShowRouteChoices()
    {
        Clear();
        GameObject prefab = GameDB.Instance?.CampaignDefinition?.RouteDoorPrefab;
        if (prefab == null)
        {
            Debug.LogError("[RouteDoorSpawner] RouteDoorPrefab 為 null，無法生成選路門。");
            return false;
        }

        // 找場景中現有的門作為參考位置（通常是出口門）
        DoorController[] existingDoors = Object.FindObjectsByType<DoorController>(
            FindObjectsInactive.Include, 
            FindObjectsSortMode.None);
        
        if (existingDoors == null || existingDoors.Length == 0)
        {
            Debug.LogError("[RouteDoorSpawner] 場景中找不到任何 DoorController，無法生成選路門。");
            return false;
        }

        // 使用第一個找到的門作為參考點
        Transform referenceTransform = existingDoors[0].transform;
        Vector3 origin = referenceTransform.position;
        Vector3 right = referenceTransform.right;
        Vector3 forward = referenceTransform.forward;
        Quaternion rotation = referenceTransform.rotation;

        Debug.Log($"[RouteDoorSpawner] 使用場景中的門作為參考：{existingDoors[0].name}，位置：{origin}");

        for (int optionIndex = 0; optionIndex < 2; optionIndex++)
        {
            float side = optionIndex == 0 ? -1f : 1f;
            Vector3 spawnPos = origin + right * side * 3f + forward * 2f;
            
            // 使用非泛型版本，讓 Unity 自動處理類型
            UnityEngine.Object instantiated = Object.Instantiate(prefab, spawnPos, rotation);
            GameObject doorObject = instantiated as GameObject;
            
            if (doorObject == null)
            {
                Debug.LogError($"[RouteDoorSpawner] 無法將 prefab 轉換為 GameObject，prefab 類型：{prefab.GetType().Name}");
                if (instantiated != null) Object.Destroy(instantiated);
                Clear();
                return false;
            }
            
            DoorController door = doorObject.GetComponentInChildren<DoorController>();
            if (door == null)
            {
                Debug.LogError("[RouteDoorSpawner] prefab 中找不到 DoorController 組件。");
                Object.Destroy(doorObject);
                Clear();
                return false;
            }

            door.ConfigureRoute(optionIndex);
            SpawnedDoors.Add(doorObject);
            
            Debug.Log($"[RouteDoorSpawner] 生成選路門 {optionIndex}，位置：{spawnPos}");
        }

        return true;
    }

    public static void Clear()
    {
        foreach (GameObject door in SpawnedDoors)
            if (door != null) Object.Destroy(door);
        SpawnedDoors.Clear();
    }
}
