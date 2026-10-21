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
    [SerializeField]private int currentMonay = 1000;

    // 今のミッションの目標金額
    [SerializeField] private int[] targetMoneys = {3000,6000,10000};

    //今のミッションの目標金額
    private int targetMoney => targetMoneys[DayManager.Instance.currentQuotaIndex];

    //今持ってるお金を取得するプロパティ
    public int CurrentMonay => currentMonay;

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
    public void RepaymentMonay()
    {
        if (currentMonay >= targetMoney)
        {
            // 返済して次のノルマへ
            Debug.Log("返済成功");
            currentMonay-=targetMoney;
            DayManager.Instance.currentQuotaIndex++;

            //すべてのミッションをクリアした場合はゲームクリア
            if (DayManager.Instance.currentQuotaIndex>=targetMoneys.Length)
            {
                //ゲームクリアの関数を入れる予定
                return;
            }

            //残り日数をリセット
            DayManager.Instance.ResetDay();
        }
        else
        {
            // 失敗：ゲームオーバー
            Debug.Log("ゲームオーバー");
        }
    }

    /// <summary>
    /// お金を払う処理
    /// </summary>
    public void SpendMonay(int amount)
    {
        currentMonay -= amount;
    }

    /// <summary>
    /// 「今のお金/目標金額」の文字を作る
    /// </summary>
    public string GetMoneyText()
    {
        if (DayManager.Instance == null) return $"{currentMonay}$";

        int index = DayManager.Instance.currentQuotaIndex;
        if (index >= targetMoneys.Length) return $"{currentMonay}$";

        return $"{currentMonay}$/{targetMoneys[index]}$";
    }
}
