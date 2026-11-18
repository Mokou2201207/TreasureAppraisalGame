using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// お金に関する処理を管理するクラス
/// </summary>
public class MonayManager : MonoBehaviour
{
    //インスタンス化
    public static MonayManager Instance { get; private set; }

    //今持ってるお金
    private const int StartMonay = 1000;
    [SerializeField]private int currentMonay = StartMonay;

    // 今のミッションの目標金額
    [SerializeField] private int[] targetMoneys = {3000,6000,10000};

    //今のミッションの目標金額
    private int targetMoney => targetMoneys[DayManager.Instance.currentQuotaIndex] + borrowedDebt;

    //借りたお金の分、次の返済に上乗せされる額
    private int borrowedDebt = 0;
    public int BorrowedDebt => borrowedDebt;

    //次に貸してもらえる段階（返済できたら最初に戻る）
    private int borrowStep = 0;
    public int BorrowStep => borrowStep;

    //今持ってるお金を取得するプロパティ
    public int CurrentMonay => currentMonay;

    //今の返済の目標額（全部返し終えていたら0）
    public int CurrentTargetMoney => IsAllCleared ? 0 : targetMoney;

    //全部の返済を終えたか
    public bool IsAllCleared => DayManager.Instance != null && DayManager.Instance.currentQuotaIndex >= targetMoneys.Length;

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
    /// お金の更新
    /// </summary>
    private void Update()
    {
        if (DayManager.Instance == null) return;

        int index = DayManager.Instance.currentQuotaIndex;

        // 全部クリアしたあとは、配列の外を読まないように止める
        if (index >= targetMoneys.Length) return;
    }

    /// <summary>
    /// 返済するときのマネー
    /// </summary>
    public bool TryRepay()
    {
        if (IsAllCleared) return true;
        if (currentMonay < targetMoney) return false;

        //目標の額を払って、次の返済の目標へ進める
        currentMonay -= targetMoney;
        borrowedDebt = 0;
        borrowStep = 0;
        DayManager.Instance.currentQuotaIndex++;

        //残り日数を3日に戻して、次の返済まで数え直す
        DayManager.Instance.ResetDay();
        return true;
    }

    /// <summary>
    /// 最初のお金に戻す（やり直し用）
    /// </summary>
    public void ResetMonay()
    {
        currentMonay = StartMonay;
        borrowedDebt = 0;
        borrowStep = 0;
    }

    /// <summary>
    /// お金を借りる処理（借りたお金はすぐ手に入り、次の返済に「借りた額×倍率」が上乗せされる）
    /// </summary>
    public void Borrow(int amount, int rate)
    {
        currentMonay += amount;
        borrowedDebt += amount * rate;
    }

    /// <summary>
    /// 貸してもらえる段階を1つ進める処理（次に借りるときは、次の額になる）
    /// </summary>
    public void AdvanceBorrowStep()
    {
        borrowStep++;
    }

    /// <summary>
    /// お金を払う処理
    /// </summary>
    public void SpendMonay(int amount)
    {
        currentMonay -= amount;
    }

    /// <summary>
    /// お金を受け取る処理
    /// </summary>
    public void AddMonay(int amount)
    {
        currentMonay += amount;
    }

    /// <summary>
    /// 「今のお金/目標金額」の文字を作る
    /// </summary>
    public string GetMoneyText()
    {
        if (DayManager.Instance == null) return $"{currentMonay}$";

        int index = DayManager.Instance.currentQuotaIndex;
        if (index >= targetMoneys.Length) return $"{currentMonay}$";

        return $"{currentMonay}$/{targetMoneys[index] + borrowedDebt}$";
    }
}
