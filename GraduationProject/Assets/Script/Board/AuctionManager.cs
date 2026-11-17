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

    [Header("まだ受け取っていない結果（ポストで受け取る）")]
    public List<AuctionResult> results = new List<AuctionResult>();

    //まだ受け取っていない結果があるか
    public bool HasResults => results.Count > 0;

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

    /// <summary>
    /// 出品中のお宝を全部売って、結果を作る処理（寝て次の日になったときに呼ぶ）
    /// </summary>
    public void ResolveAuctions()
    {
        foreach (PurchasedItem item in onAuction)
        {
            if (item == null) continue;

            //チェックが合っていなかった数で売値を決めて、結果に入れる
            int wrongCount = CountWrongChecks(item);
            int soldPrice = CalculateSoldPrice(item.highestPrice, wrongCount);
            results.Add(new AuctionResult { item = item, soldPrice = soldPrice, wrongCount = wrongCount });

            Debug.Log($"[AuctionManager] {item.itemName} が {soldPrice:N0}$ で売れました（チェックの間違い {wrongCount}個）");
        }

        //全部売れたので出品中から消す
        onAuction.Clear();
    }

    /// <summary>
    /// 鑑定リストのチェックと、お宝の本当の状態が合っていない数を数える処理
    /// </summary>
    public static int CountWrongChecks(PurchasedItem item)
    {
        int wrong = 0;
        if (item.checkedDamaged != item.isDamaged) wrong++;
        if (item.checkedDescriptionWrong != item.isDescriptionWrong) wrong++;
        if (item.checkedMission1 != item.isAppraisalMission1) wrong++;
        if (item.checkedMission2 != item.isAppraisalMission2) wrong++;
        return wrong;
    }

    /// <summary>
    /// 間違いの数から売値を決める処理（最高金額をもとに上げ下げする）
    /// </summary>
    public static int CalculateSoldPrice(float highestPrice, int wrongCount)
    {
        float rate;
        switch (wrongCount)
        {
            //全部合っていたら20〜30%上がる
            case 0: rate = Random.Range(1.20f, 1.30f); break;
            //1つ違いは5〜10%上がる
            case 1: rate = Random.Range(1.05f, 1.10f); break;
            //2つ違いは10〜20%下がる
            case 2: rate = Random.Range(0.80f, 0.90f); break;
            //3つ違いは40〜60%下がる
            case 3: rate = Random.Range(0.40f, 0.60f); break;
            //4つ全部違いは80%下がる
            default: rate = 0.20f; break;
        }
        return Mathf.RoundToInt(highestPrice * rate);
    }

    /// <summary>
    /// まだ受け取っていない結果を全部取り出す処理（ポストで受け取るときに呼ぶ）
    /// </summary>
    public List<AuctionResult> TakeResults()
    {
        List<AuctionResult> taken = new List<AuctionResult>(results);
        results.Clear();
        return taken;
    }
}

/// <summary>
/// オークションで売れた結果1つ分
/// </summary>
[System.Serializable]
public class AuctionResult
{
    public PurchasedItem item;   // 売れたお宝
    public int soldPrice;        // 売れた値段
    public int wrongCount;       // チェックの間違いの数
}
