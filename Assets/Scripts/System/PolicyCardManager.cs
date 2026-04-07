using System.Collections.Generic;
using UnityEngine;

public class PolicyCardManager : MonoBehaviour
{
    [Header("所有可抽政策卡")]
    public List<PolicyCardData> allCards = new List<PolicyCardData>();

    public List<PolicyCardData> GetRandomCards(int count)
    {
        List<PolicyCardData> result = new List<PolicyCardData>();
        List<PolicyCardData> tempPool = new List<PolicyCardData>();

        foreach (PolicyCardData card in allCards)
        {
            if (card != null)
            {
                tempPool.Add(card);
            }
        }

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
