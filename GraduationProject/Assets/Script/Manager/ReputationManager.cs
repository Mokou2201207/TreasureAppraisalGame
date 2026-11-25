using UnityEngine;

/// <summary>
/// お店の評価（0〜100）を管理する処理（買い取りとオークションの結果で上下し、交渉やお宝の種類に影響する）
/// </summary>
public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance { get; private set; }

    [Header("最初の評価"), SerializeField]
    private int startReputation = 30;

    [Header("評価の最大"), SerializeField]
    private int maxReputation = 100;

    [Header("買い取りが成功したとき・失敗したときに変わる量"), SerializeField]
    private int purchaseSuccessDelta = 2;
    [SerializeField]
    private int purchaseFailDelta = -2;

    [Header("オークションで×がこの数以上なら評価が下がる（未満なら上がる）"), SerializeField]
    private int badWrongCount = 2;

    [Header("オークションの結果で上がる量・下がる量（お宝1つごと）"), SerializeField]
    private int auctionGoodDelta = 3;
    [SerializeField]
    private int auctionBadDelta = -3;

    [Header("交渉に影響しない、基準の評価"), SerializeField]
    private int neutralReputation = 30;

    [Header("評価0のとき、交渉の失敗確率に足す分（パーセント）"), SerializeField]
    private float lowReputationPenalty = 10f;

    [Header("評価が最大のとき、交渉の失敗確率から引く分（パーセント）"), SerializeField]
    private float highReputationRelief = 10f;

    [Header("今の評価（確認用）"), SerializeField]
    private int reputation;

    //今の評価
    public int Reputation => reputation;
    public int MaxReputation => maxReputation;

    /// <summary>
    /// 開始前：1つだけにして、シーンを移っても残るようにする
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        reputation = startReputation;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 評価を増やす（減らす）処理（0〜最大の間におさめる）
    /// </summary>
    public void Add(int amount)
    {
        reputation = Mathf.Clamp(reputation + amount, 0, maxReputation);
    }

    /// <summary>
    /// 買い取りの結果で評価を変える処理
    /// </summary>
    public void OnPurchase(bool isSuccess)
    {
        Add(isSuccess ? purchaseSuccessDelta : purchaseFailDelta);
    }

    /// <summary>
    /// オークションの結果（チェックの間違いの数）で評価を変える処理
    /// </summary>
    public void OnAuctionResult(int wrongCount)
    {
        Add(wrongCount >= badWrongCount ? auctionBadDelta : auctionGoodDelta);
    }

    /// <summary>
    /// 交渉の失敗確率を上下させる分（基準の評価より低いと少しずつ足し、高いと少しずつ引く）
    /// </summary>
    public float NegotiationOffset
    {
        get
        {
            if (reputation < neutralReputation)
            {
                //低いほど上がる（評価0で最大）
                return lowReputationPenalty * (neutralReputation - reputation) / Mathf.Max(1, neutralReputation);
            }

            //高いほど下がる（評価が最大で最大まで下がる）
            float k = (float)(reputation - neutralReputation) / Mathf.Max(1, maxReputation - neutralReputation);
            return -highReputationRelief * Mathf.Clamp01(k);
        }
    }

    /// <summary>
    /// 最初の評価に戻す処理（やり直し用）
    /// </summary>
    public void ResetReputation()
    {
        reputation = startReputation;
    }
}
