using UnityEngine;

public class MapProgressManager : MonoBehaviour
{
    public static MapProgressManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsNodeAvailable(string nodeID)
    {
        return true;
    }

    public bool IsNodeCompleted(string nodeID)
    {
        return false;
    }

    public bool CanEnterNode(string nodeID)
    {
        return true;
    }

    public void CompleteNode(MapNodeData completedNode)
    {
        Debug.Log("MapProgressManager：單向節點功能目前停用，略過節點完成記錄");
    }

    public void SetCurrentNode(MapNodeData currentNode)
    {
        Debug.Log("MapProgressManager：單向節點功能目前停用，略過目前節點記錄");
    }

    public bool CompleteCurrentNodeSelection()
    {
        Debug.Log("MapProgressManager：單向節點功能目前停用，略過目前節點完成");
        return false;
    }

    public void ResetMapProgress()
    {
        Debug.Log("🔄 MapProgressManager：單向節點功能目前停用");
    }
}
