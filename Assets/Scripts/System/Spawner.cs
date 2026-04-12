using UnityEngine;

public class Spawner : MonoBehaviour
{
    
    public GameObject voterPrefab;
    public SocialAtmosphereManager climateManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SpawnVoter(Vector3 pos)
    {
        GameObject obj = Instantiate(voterPrefab, pos, Quaternion.identity);

        VoterData data = obj.GetComponent<VoterData>();
        if (data == null)
        {
            Debug.LogWarning("生成的選民缺少 VoterData，無法套用深色選民類型。");
            return;
        }

        float rate = climateManager.GetDarkVoterRate();
        VoterType type = Random.value < rate ? VoterType.Dark : VoterType.Normal;
        data.ApplyType(type);

        if (obj.TryGetComponent<VoterLogic>(out var voterLogic))
        {
            voterLogic.RefreshMovementSpeed();
        }
    }
    
}
