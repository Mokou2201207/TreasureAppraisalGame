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

    [Header("売れる値段の倍率（X:最小〜Y:最大。最高金額にかける）")]
    [Header("チェックが全部合っているとき"), SerializeField]
    private Vector2 rateWrong0 = new Vector2(1.20f, 1.30f);
    [Header("チェックの間違いが1つのとき"), SerializeField]
    private Vector2 rateWrong1 = new Vector2(1.05f, 1.10f);
    [Header("チェックの間違いが2つのとき"), SerializeField]
    private Vector2 rateWrong2 = new Vector2(0.80f, 0.90f);
    [Header("チェックの間違いが3つのとき"), SerializeField]
    private Vector2 rateWrong3 = new Vector2(0.40f, 0.60f);
    [Header("チェックが全部違うとき"), SerializeField]
    private Vector2 rateWrong4 = new Vector2(0.20f, 0.20f);

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
        //間違いの数で、Inspectorで決めた倍率の範囲を選ぶ（AuctionManagerがなければ最初の値を使う）
        Vector2 range;
        AuctionManager m = Instance;
        switch (wrongCount)
        {
            case 0: range = m != null ? m.rateWrong0 : new Vector2(1.20f, 1.30f); break;
            case 1: range = m != null ? m.rateWrong1 : new Vector2(1.05f, 1.10f); break;
            case 2: range = m != null ? m.rateWrong2 : new Vector2(0.80f, 0.90f); break;
            case 3: range = m != null ? m.rateWrong3 : new Vector2(0.40f, 0.60f); break;
            default: range = m != null ? m.rateWrong4 : new Vector2(0.20f, 0.20f); break;
        }

        //範囲の中でランダムに倍率を決める
        float rate = Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
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
