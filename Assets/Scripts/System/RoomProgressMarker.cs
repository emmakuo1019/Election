using UnityEngine;

public class RoomProgressMarker : MonoBehaviour
{
    private void Start()
    {
        BlockProgressManager.EnterNextRoom();
    }
}