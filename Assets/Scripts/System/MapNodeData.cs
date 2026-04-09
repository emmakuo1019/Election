using System.Collections.Generic;
using UnityEngine;

public class MapNodeData : MonoBehaviour
{
    [Header("這個節點的唯一 ID（不可重複）")]
    public string nodeID;

    [Header("這個節點對應要進入的場景名稱")]
    public string targetSceneName;

    [Header("這個節點連到哪些下一節點")]
    public List<MapNodeData> nextNodes = new List<MapNodeData>();
}