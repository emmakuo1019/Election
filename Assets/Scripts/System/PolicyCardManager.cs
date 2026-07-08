using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class PolicyCardManager
{
    public static List<PolicyCardData> GetRandomCards(int count)
    {
        var allCards = Resources.LoadAll<PolicyCardData>("PolicyCards");
        List<PolicyCardData> tempPool = new List<PolicyCardData>();

        foreach (PolicyCardData card in allCards)
        {
            if (card != null)
            {
                // 過濾掉已經擁有的政策卡
                bool isAcquired = GameDB.Instance != null && GameDB.Instance.Run.AcquiredPolicyCards.Contains(card.cardName);
                if (!isAcquired)
                {
                    tempPool.Add(card);
                }
            }
        }

        List<PolicyCardData> result = new List<PolicyCardData>();
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
