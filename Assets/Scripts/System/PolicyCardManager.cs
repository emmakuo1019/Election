using System.Collections.Generic;
using UnityEngine;

public class PolicyCardManager : MonoBehaviour
{
    public static PolicyCardManager Instance { get; private set; }

    [SerializeField] private List<PolicyCardData> allCards = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public List<PolicyCardData> GetRandomCards(int count)
    {
        List<PolicyCardData> pool = new List<PolicyCardData>(allCards);
        List<PolicyCardData> result = new List<PolicyCardData>();

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
}