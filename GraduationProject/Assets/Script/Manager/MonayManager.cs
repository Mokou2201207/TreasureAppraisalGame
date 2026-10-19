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
    [SerializeField] private int targetMoney = 0;

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

        switch (DayManager.Instance.currentQuotaIndex) 
        {
            case 0:
                targetMoney = 3000;
                monayText.text = $"{currentMonay}$/{targetMoney}$;";
                break;

            case 1:
                targetMoney = 6000;
                monayText.text = $"{currentMonay}$/{targetMoney}$;";
                break;

        }
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
            currentMonay=currentMonay-targetMoney;
            DayManager.Instance.currentQuotaIndex++;
        }
        else
        {
            // 失敗：ゲームオーバー
            Debug.Log("ゲームオーバー");
        }
    }

}
