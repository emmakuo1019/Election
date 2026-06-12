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
    VoteManager.Instance?.ResetToDefaultVotes();
    backBtn.onClick.AddListener(backBtnOnClick);

    void backBtnOnClick()
    {
        SceneManager.LoadScene("S0");
    }
}
}
