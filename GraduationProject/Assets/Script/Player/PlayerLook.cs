using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤー視点インタラクション（Raycast判定＆テキスト表示）
/// </summary>
public class PlayerLook : MonoBehaviour
{
    [Header("カメラ設定")]
    [SerializeField] private Transform playerCamera;

    [Header("Playerから出すRayの距離")]
    [SerializeField] private float rayDistance = 100.0f;

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }


    void Update()
    {
        // カメラ位置から前方へRayを射出
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            //お客さん判定
            int customerLayer = LayerMask.NameToLayer("Customer");
            if (customerLayer != -1 && hit.collider.gameObject.layer == customerLayer)
            {
                MoveWaypoint hitCustomer = hit.collider.GetComponentInParent<MoveWaypoint>();
                if (hitCustomer != null && !hitCustomer.isMoving && hitCustomer.queuePositionNumber == 0)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        CustomerItem customerItemData = hit.collider.GetComponentInParent<CustomerItem>();
                        if (customerItemData != null && customerItemData.currentItem != null && !customerItemData.isItemDropped)
                        {
                            customerItemData.isItemDropped = true;

                            // DropItemManager 経由でお宝をドロップ
                            if (DropItemManager.Instance != null)
                            {
                                DropItemManager.Instance.DropItem(customerItemData.currentItem);
                            }
                        }
                    }
                    return;
                }
            }

            //掲示板に触れたら掲示板のUIを開く
            int CarLayer = LayerMask.NameToLayer("AuctionBoard");
            if (hit.collider.gameObject.layer == CarLayer)
            {
                if (Input.GetMouseButtonDown(0)&&AuctionBoardUI.Instance!=null)
                {
                    AuctionBoardUI.Instance.Open();
                }
                return;
            }



            // お宝判定の検索
            ItemData treasure = hit.collider.GetComponent<ItemData>();
            if (treasure == null) treasure = hit.collider.GetComponentInParent<ItemData>();
            if (treasure == null) treasure = hit.collider.GetComponentInChildren<ItemData>();

            // ItemDataコンポーネントが存在するか、またはオブジェクト名にお宝関連キーワードが含まれる場合
            string objName = hit.collider.gameObject.name.ToLower();
            bool isTreasureItem = (treasure != null) ||
                                  objName.Contains("treasure") ||
                                  objName.Contains("item") ||
                                  objName.Contains("otakara");

            if (isTreasureItem)
            {
                bool isCurrentlyInspecting = InspectionManager.Instance != null && InspectionManager.Instance.IsInspecting;

                if (Input.GetMouseButtonDown(0) && !isCurrentlyInspecting)
                {
                    if (InspectionManager.Instance != null)
                    {
                        GameObject inspectTarget = (treasure != null) ? treasure.gameObject : hit.collider.gameObject;
                        InspectionManager.Instance.StartInspection(inspectTarget);
                    }
                    else
                    {
                        Debug.LogError("[PlayerLook] InspectionManager.Instance が見つかりません。シーン内に InspectionManager を配置してください。");
                    }
                }
            }
        }
    }
}


