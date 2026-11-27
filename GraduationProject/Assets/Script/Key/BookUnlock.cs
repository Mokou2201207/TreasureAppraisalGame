using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BookUnlock : MonoBehaviour
{
    [Header("ロックしているブロック"), SerializeField]
    private GameObject lockedBlock;

    [Header("MonayManagerをアタッチ"), SerializeField]
    private MonayManager monayManager;

    [Header("ロックしている値段"),SerializeField]
    private int unlockPrice=10000;

    /// <summary>
    /// 図鑑のロックを解除する（お金が足りれば払って、ロックのブロックを消す）
    /// 呼ばれたときだけ動く
    /// </summary>
    public void TryUnlock()
    {
        if (MonayManager.Instance == null || lockedBlock == null) return;

        //お金が足りているか確認する
        if (MonayManager.Instance.CurrentMonay >= unlockPrice)
        {
            MonayManager.Instance.currentMonay -= unlockPrice;
            lockedBlock.SetActive(false);
        }
        else
        {
            Debug.Log("お金が足りません");
        }
    }
}
