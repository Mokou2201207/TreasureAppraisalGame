using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// お客さんが持ってるお宝
/// </summary>
public class CustomerItem : MonoBehaviour
{
    [Header("お宝一覧"), SerializeField]
    private GameObject[] itemList;

    [Header("今持ってるお宝※自動でランダムなお宝になります。"), SerializeField]
    public GameObject currentItem;

    // お宝が落とされたかどうかのフラグ
    public bool isItemDropped = false; 

    private void Start()
    {
        if (itemList == null || itemList.Length == 0) return;

        //今のお店の評価で出てくるお宝だけを集める
        int reputation = ReputationManager.Instance != null ? ReputationManager.Instance.Reputation : int.MaxValue;
        List<GameObject> candidates = new List<GameObject>();
        foreach (GameObject item in itemList)
        {
            if (item == null) continue;
            ItemData data = item.GetComponentInChildren<ItemData>(true);
            if (data == null || data.requiredReputation <= reputation) candidates.Add(item);
        }

        //出せるお宝がなければ、全部の中から選ぶ
        if (candidates.Count == 0) candidates.AddRange(itemList);

        //ランダムにお宝を選択する
        currentItem = candidates[Random.Range(0, candidates.Count)];
    }

}
