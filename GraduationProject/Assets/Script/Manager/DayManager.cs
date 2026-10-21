using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DayManager : MonoBehaviour
{
    //インスタンス化
    public static DayManager Instance { get; private set; }

    //残り日数
    public int dayCount = 3;

    // 今何個目のミッションか
    public int currentQuotaIndex = 0;

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
    }

}
