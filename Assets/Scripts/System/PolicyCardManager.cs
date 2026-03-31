using System.Collections.Generic;
using UnityEngine;

public class PolicyCardManager : MonoBehaviour
{
    [Header("所有可抽政策卡")]
    public List<PolicyCardData> allCards = new List<PolicyCardData>();

    public List<PolicyCardData> GetRandomCards(int count)
    {
        List<PolicyCardData> result = new List<PolicyCardData>();
        List<PolicyCardData> tempPool = new List<PolicyCardData>(allCards);

        int drawCount = Mathf.Min(count, tempPool.Count);

        for (int i = 0; i < drawCount; i++)
        {
            int randomIndex = Random.Range(0, tempPool.Count);
            result.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }

        return result;
    }
}