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
    if (GameDB.Instance?.Run != null)
        GameDB.Instance.Run.HasPendingSkillSelection = false;
    if (GameDB.Instance != null && GameDB.Instance.Player != null)
    {
        GameDB.Instance.Player.EquipPartySkill(null);
        GameDB.Instance.Player.EquipBaseSkillJ(null);
    }
    GameDB.Instance?.ResetRunData();
    
    if (backBtn != null)
    {
        backBtn.onClick.AddListener(backBtnOnClick);
    }
    else
    {
        Debug.LogWarning("[HeadquartersManager] backBtn 未在 Inspector 中綁定！(可能是因為已被 HQSceneController 取代)");
    }

    void backBtnOnClick()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ChangeState(new MainMenuState());
        }
    }
}
}
