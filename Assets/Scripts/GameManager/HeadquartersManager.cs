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
    GameDB.Instance?.ResetRunData();
    backBtn.onClick.AddListener(backBtnOnClick);

    void backBtnOnClick()
    {
        SceneManager.LoadScene("S0");
    }
}
}
