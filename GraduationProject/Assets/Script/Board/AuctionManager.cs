using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 出品中のお宝を、シーンや日をまたいで覚えておく処理※AI担当
/// </summary>
public class AuctionManager : MonoBehaviour
{
    public static AuctionManager Instance { get; private set; }

    [Header("出品中のお宝（次の日の朝に結果が出る）")]
    public List<PurchasedItem> onAuction = new List<PurchasedItem>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 親（GameData）ごと、シーンを移動しても消えないようにする
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// お宝を出品中に入れる
    /// </summary>
    public void AddListings(IEnumerable<PurchasedItem> items)
    {
        foreach (PurchasedItem item in items)
        {
            if (item != null && !onAuction.Contains(item)) onAuction.Add(item);
        }
    }
}
