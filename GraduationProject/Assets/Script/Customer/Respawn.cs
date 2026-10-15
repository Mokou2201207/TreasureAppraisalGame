using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawn : MonoBehaviour
{
    [Header("お客さんの種類"), SerializeField]
    private GameObject[] customers;

    [Header("スポーンする位置"), SerializeField]
    private Transform[] respawnPositions;

    [Header("最低限のリスポーン時間"), SerializeField]
    private float respawnTime = 20;

    [Header("最大限のリスポーン時間"), SerializeField]
    private float respawnMaxTime = 40;

    [Header("お店に並べる最大人数"), SerializeField]
    private int maxCustomer = 5;
    /// <summary>
    /// 開始
    /// </summary>
    private void Start()
    {
        Spawn();
    }

    /// <summary>
    /// キャラクターの種類、スポーン位置、時間をランダムに設定してスポーン
    /// </summary>
    [System.Obsolete]
    void Spawn()
    {
        //このシーンにいるお客さんの数を数える
        MoveWaypoint[] activeCustomers = FindObjectsOfType<MoveWaypoint>();
        if (activeCustomers.Length >= maxCustomer)
        {
            Debug.Log("お客さんがいっぱいで行列が満員のため、新しいお客さんは来ませんでした。");
        }
        else
        {
            //ランダムのキャラを選ぶ
            int randCustomer = Random.Range(0, customers.Length);
            GameObject customer = customers[randCustomer];

            //ランダムのスポーンの位置選ぶ
            int randPos = Random.Range(0, respawnPositions.Length);
            Transform spawnPos = respawnPositions[randPos];

            // その位置にスポーン
            GameObject spawnedCustomer = Instantiate(customer, spawnPos.position, spawnPos.rotation);

            // 生成したお客さんに「現在の列の何番目か」を教えてあげる
            MoveWaypoint moveScript = spawnedCustomer.GetComponent<MoveWaypoint>();
            if (moveScript != null)
            {
                int queueCount = 0;
                foreach (var c in activeCustomers)
                {
                    if (c.queuePositionNumber >= 0) queueCount++;
                }
                moveScript.SetQueuePosition(queueCount);
            }
        }

        ////次のスポーンをランダムで時間を決める
        float nextspown = Random.Range(respawnTime, respawnMaxTime);
        Invoke(nameof(Spawn), nextspown);
    }

}

