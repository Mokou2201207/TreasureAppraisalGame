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

    [Header("お金のText")]
    [SerializeField] private Text monayText;

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
        // まだ誰もいなければ、自分が担当になる
        if (Instance == null)
        {
            Instance = this;
            // シーンを移動しても消えないようにする
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // すでにいる場合は、あとから来た自分を消す
            Destroy(gameObject);
        }
    }   

    /// <summary>
    /// お金の更新
    /// </summary>
    private void Update()
    {
        if (monayText == null) return;
        if (DayManager.Instance == null) return;

        int index = DayManager.Instance.currentQuotaIndex;

        // 全部クリアしたあとは、配列の外を読まないように止める
        if (index >= targetMoneys.Length) return;

        monayText.text = $"{currentMonay}$/{targetMoneys[index]}$";
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

}
