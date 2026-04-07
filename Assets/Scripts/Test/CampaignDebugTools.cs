using UnityEngine;

public class CampaignDebugTools : MonoBehaviour
{
    public void ResetCampaignProgress()
    {
        CampaignProgressManager.ResetCampaign();
        BlockProgressManager.ClearBlockProgress();

        Debug.Log("已重置本輪所有進度");
    }
}