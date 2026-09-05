using UnityEngine;
using UnityEngine.UI;

public class MapProgressUI : MonoBehaviour
{
    [Header("進度文字")]
    [SerializeField] private Text progressText;

    private GameDB _gameDB;
    private CampaignData _campaign;

    private void OnEnable()
    {
        Rebind();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (_gameDB != null) _gameDB.OnCampaignChanged -= HandleCampaignChanged;
        if (_campaign != null) _campaign.OnProgressChanged -= RefreshUI;
        _gameDB = null;
        _campaign = null;
    }

    public void RefreshUI()
    {
        if (progressText == null)
        {
            Debug.LogWarning("MapProgressUI：progressText 沒有指定");
            return;
        }

        Rebind();
        int currentNode = _campaign != null
            ? _campaign.CurrentNodeNumber
            : 0;
        int daysRemaining = currentNode <= 0
            ? CampaignDefinition.FormalNodeCount
            : CampaignDefinition.FormalNodeCount - currentNode + 1;
        progressText.text = "選戰倒數：" + daysRemaining + " / " + CampaignDefinition.FormalNodeCount + " 天";
    }

    private void Rebind()
    {
        GameDB currentGameDB = GameDB.Instance;
        CampaignData currentCampaign = currentGameDB != null ? currentGameDB.Campaign : null;
        if (_gameDB == currentGameDB && _campaign == currentCampaign) return;

        if (_gameDB != null) _gameDB.OnCampaignChanged -= HandleCampaignChanged;
        if (_campaign != null) _campaign.OnProgressChanged -= RefreshUI;

        _gameDB = currentGameDB;
        _campaign = currentCampaign;

        if (_gameDB != null) _gameDB.OnCampaignChanged += HandleCampaignChanged;
        if (_campaign != null) _campaign.OnProgressChanged += RefreshUI;
    }

    private void HandleCampaignChanged(CampaignData _)
    {
        Rebind();
        RefreshUI();
    }
}
