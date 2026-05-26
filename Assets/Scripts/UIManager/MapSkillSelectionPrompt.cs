using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSkillSelectionPrompt : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "MapScene")
        {
            return;
        }

        if (!PlayerSkillManager.HasPendingMapSkillSelection())
        {
            return;
        }

        GameObject promptObject = new GameObject(nameof(MapSkillSelectionPrompt));
        promptObject.AddComponent<MapSkillSelectionPrompt>();
    }

    private void Start()
    {
        UpgradePanelUI upgradePanelUI = FindFirstObjectByType<UpgradePanelUI>(FindObjectsInactive.Include);
        if (upgradePanelUI == null)
        {
            Debug.LogWarning("⚠️ MapScene 找不到 UpgradePanelUI，無法開啟技能二選一");
            Destroy(gameObject);
            return;
        }

        upgradePanelUI.OpenPanel();
        Destroy(gameObject);
    }
}
