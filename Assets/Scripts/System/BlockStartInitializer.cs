using UnityEngine;

public class BlockStartInitializer : MonoBehaviour
{
    [Header("這個區塊總房數")]
    [SerializeField] private int maxRoomsInBlock = 5;

    [Header("是否在進場時初始化")]
    [SerializeField] private bool initializeOnStart = true;

    private void Start()
    {
        if (initializeOnStart && !BlockProgressManager.HasBlockProgress())
        {
            BlockProgressManager.InitBlock(maxRoomsInBlock);
            BlockProgressManager.EnterNextRoom();
        }
    }
}
