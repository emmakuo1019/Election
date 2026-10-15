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

        if (!(GameDB.Instance != null && GameDB.Instance.Run != null && GameDB.Instance.Run.HasPendingSkillSelection))
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
            Destroy(gameObject);
            return;
        }

        upgradePanelUI.OpenPanel();
        Destroy(gameObject);
    }
}
