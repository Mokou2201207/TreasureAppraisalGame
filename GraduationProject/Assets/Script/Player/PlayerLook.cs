using System.Collections;
using System.Collections.Generic;
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

    [Header("クロスヘア用のText")]
    [SerializeField] private Text crosshairText;

    private void Awake()
    {
        FindCrosshairText();
    }

    void Start()
    {
        FindCrosshairText();

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    /// <summary>
    /// UIのCrosshairTextを自動検索
    /// </summary>
    private void FindCrosshairText()
    {
        if (crosshairText != null) return;

        // CrosshairTextで検索
        GameObject textObj = GameObject.Find("CrosshairText");
        if (textObj != null)
        {
            crosshairText = textObj.GetComponent<Text>();
        }

        //見つからなかったら、名前に "crosshair" や "interact" を含むTextを検索
        if (crosshairText == null)
        {
            Text[] allTexts = FindObjectsOfType<Text>(true);
            foreach (var t in allTexts)
            {
                if (t.name.ToLower().Contains("crosshair") || t.name.ToLower().Contains("interact") || t.name.ToLower().Contains("aim") || t.name.ToLower().Contains("text"))
                {
                    crosshairText = t;
                    break;
                }
            }
            if (crosshairText == null && allTexts.Length > 0)
            {
                crosshairText = allTexts[0];
            }
        }

        if (crosshairText == null)
        {
            Debug.LogWarning("[PlayerLook] クロスヘア用のTextが見つかりません。Canvas内にTextを作成し名前を 'CrosshairText' にするか、Inspectorでアタッチしてください。");
        }
    }

    void Update()
    {
        // 鑑定中ならRayキャスト処理およびテキスト表示をスキップ
        if (InspectionManager.Instance != null && InspectionManager.Instance.IsInspecting)
        {
            if (crosshairText != null) crosshairText.text = "";
            return;
        }

        // カメラが未設定の場合、MainCameraを自動取得
        if (playerCamera == null)
        {
            if (Camera.main != null) playerCamera = Camera.main.transform;
            else return;
        }

        // 毎フレームのテキスト表示を初期化
        if (crosshairText != null)
        {
            crosshairText.text = "";
        }
        else
        {
            FindCrosshairText();
        }

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
                if (hitCustomer != null && hitCustomer.isStopped && hitCustomer.queuePositionNumber == 0)
                {
                    if (crosshairText != null)
                    {
                        crosshairText.text = "お宝を見せてもらう(左クリック)";
                    }

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
                if (crosshairText != null)
                {
                    crosshairText.text = "鑑定(左クリック)";
                }

                if (Input.GetMouseButtonDown(0))
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
