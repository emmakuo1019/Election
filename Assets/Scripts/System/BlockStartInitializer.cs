using UnityEngine;

public class BlockStartInitializer : MonoBehaviour
{
    [Header("這個區塊總房數")]
    [SerializeField] private int maxRoomsInBlock = 3;

    [Header("是否在進場時初始化")]
    [SerializeField] private bool initializeOnStart = true;

    private void Start()
    {
        if (initializeOnStart)
        {
            BlockProgressManager.InitBlock(maxRoomsInBlock);
            BlockProgressManager.EnterNextRoom();
        }
    }
}