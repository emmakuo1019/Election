using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeadquartersManager : MonoBehaviour

{
void Start()
{
    Time.timeScale = 1f;
    CampaignProgressManager.ResetCampaign();
    BlockProgressManager.ClearBlockProgress();

    MapProgressManager mapProgressManager = FindFirstObjectByType<MapProgressManager>();
    if (mapProgressManager != null)
    {
        mapProgressManager.ResetMapProgress();
    }
}
}
