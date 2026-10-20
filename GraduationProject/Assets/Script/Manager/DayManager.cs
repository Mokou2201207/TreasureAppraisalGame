using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DayManager : MonoBehaviour
{
    //インスタンス化
    public static DayManager Instance { get; private set; }

    [Header("現在の日のText")]
    [SerializeField] private Text currentDay;

    //残り日数
    public int dayCount = 3;

    // 今何個目のミッションか
    public int currentQuotaIndex = 0;

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
    /// 開始処理
    /// </summary>
    private void Start()
    {
        //設定した日数に変更
        if (currentDay == null) return;
        currentDay.text = $"{dayCount}日";
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    private void Update()
    {
        if (DayManager.Instance == null) return;

        /// <summary>
        /// テスト用
        /// <summary>
        if (Input.GetKeyDown(KeyCode.L))
        {
            NextDay();
        }
    }

    /// <summary>
    /// 残り日数を減らす処理
    /// </summary>
    private void NextDay()
    {
        //残り日数が０以下になったら処理をしない
        if (dayCount <= 0) return;

        dayCount--;
        if(currentDay!=null)currentDay.text = $"{dayCount}日";

        //残り日数が０になったら金額が足りているかの関数へ
        if (dayCount==0)
        {
            MonayManager.Instance.RepaymentMonay();
        }
    }

    /// <summary>
    /// リセット用の関数
    /// </summary>
    public void ResetDay()
    {
        dayCount = 3;
        if (currentDay != null) currentDay.text = $"{dayCount}日";
    }

}
