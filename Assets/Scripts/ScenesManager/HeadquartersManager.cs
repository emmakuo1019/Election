using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeadquartersManager : MonoBehaviour

{
    public Button backBtn;
void Start()
{
    Time.timeScale = 1f;
    CampaignProgressManager.ResetCampaign();
    BlockProgressManager.ClearBlockProgress();
    PlayerSkillManager.ResetSavedPartySkill();
    backBtn.onClick.AddListener(backBtnOnClick);

    MapProgressManager mapProgressManager = FindFirstObjectByType<MapProgressManager>();
    if (mapProgressManager != null)
    {
        mapProgressManager.ResetMapProgress();
    }

    void backBtnOnClick()
    {
        SceneManager.LoadScene("S0");
    }
}
}
