using UnityEngine;

public class MapProgressManager : MonoBehaviour
{
    public static MapProgressManager Instance { get; private set; }

    private static readonly string[] LinearRouteNodeIDs = { "A", "B", "C" };
    private const string BossNodeID = "D";

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
        return CanEnterNode(nodeID);
    }

    public bool IsNodeCompleted(string nodeID)
    {
        int routeIndex = GetRouteIndex(nodeID);
        if (routeIndex < 0)
        {
            return false;
        }

        int completedBlocks = CampaignProgressManager.GetCompletedBlockCount();
        return completedBlocks > routeIndex;
    }

    public bool CanEnterNode(string nodeID)
    {
        if (string.IsNullOrWhiteSpace(nodeID))
        {
            return false;
        }

        if (nodeID == BossNodeID)
        {
            return CampaignProgressManager.IsBossUnlocked();
        }

        int routeIndex = GetRouteIndex(nodeID);
        if (routeIndex < 0)
        {
            return false;
        }

        int completedBlocks = CampaignProgressManager.GetCompletedBlockCount();
        return completedBlocks == routeIndex;
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
        Debug.Log("🔄 MapProgressManager：單一路線模式不需額外重置節點資料");
    }

    private int GetRouteIndex(string nodeID)
    {
        for (int i = 0; i < LinearRouteNodeIDs.Length; i++)
        {
            if (LinearRouteNodeIDs[i] == nodeID)
            {
                return i;
            }
        }

        return -1;
    }
}
