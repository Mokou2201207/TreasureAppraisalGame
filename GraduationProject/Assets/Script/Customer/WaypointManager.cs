using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// NPCの移動ポイントを管理するクラス
/// </summary>
public class WaypointManager : MonoBehaviour
{
    [Header("お店を開けた後の移動ルート")]
    public Transform[] wayPoints;
    [Header("お店を閉めた後の移動ルート")]
    public Transform[] exitWayPoints;
    /// <summary>
    ///  子オブジェクトからWaypointを自動登録します
    /// </summary>
    void Awake()
    {
        // 子オブジェクト自動登録
        if (wayPoints == null || wayPoints.Length == 0)
        {
            List<Transform> list = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t != transform)
                {
                    list.Add(t);
                }
            }
            wayPoints = list.ToArray();
            Debug.Log("子オブジェクトから自動でWaypointsを登録しました。");
        }
        else
        {
            Debug.Log("インスペクターでWaypointsが手動設定されています。");
        }
    }
}
