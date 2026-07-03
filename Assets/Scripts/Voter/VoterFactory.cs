using UnityEngine;

public class VoterFactory : MonoBehaviour, IVoterFactory
{
    [Header("選民預製體")]
    [SerializeField] private VoterLogic normalVoterPrefab;
    [SerializeField] private VoterLogic coldVoterPrefab;
    [SerializeField] private VoterLogic darkVoterPrefab;

    public VoterLogic CreateVoter(VoterAttribute type, Vector3 position)
    {
        // TODO: 未來請將此處替換為「物件池管理 (Object Pool)」，避免頻繁 Instantiate
        VoterLogic prefabToInstantiate = normalVoterPrefab;
        
        switch (type)
        {
            case VoterAttribute.Cold:
                prefabToInstantiate = coldVoterPrefab;
                break;
            case VoterAttribute.Dark:
                prefabToInstantiate = darkVoterPrefab;
                break;
            case VoterAttribute.None:
            default:
                prefabToInstantiate = normalVoterPrefab;
                break;
        }

        if (prefabToInstantiate == null)
        {
            Debug.LogWarning("[VoterFactory] 找不到對應屬性的選民 Prefab！將退回使用預設");
            prefabToInstantiate = normalVoterPrefab;
        }
        
        // 若連 normal 都沒有，直接回傳 null (防呆)
        if (prefabToInstantiate == null) return null;

        VoterLogic newVoter = Instantiate(prefabToInstantiate, position, Quaternion.identity);
        return newVoter;
    }
}
