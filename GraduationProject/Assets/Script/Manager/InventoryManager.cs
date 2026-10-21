using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 買い取ったお宝の一覧を管理するクラス
/// </summary>
public class InventoryManager : MonoBehaviour
{
    //インスタンス化
    public static InventoryManager Instance { get; private set; }

    [Header("買い取ったお宝を見れる一覧")]
    public List<PurchasedItem> items = new List<PurchasedItem>();

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
            // すでにいる場合は、あとから来た GameData ごと消す
            Destroy(transform.root.gameObject);
        }
    }

    /// <summary>
    /// 買い取ったお宝を一覧に追加するメソッド
    /// </summary>
    /// <param name="item"></param>
    public void AddItem(PurchasedItem item)
    {
        if (item == null) return;
        items.Add(item);
        Debug.Log("アイテムが入りました");
    }
}
