using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 鑑定システムおよび購入・お客さん返却処理を管理するマネージャー
/// </summary>
public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("プレイヤー制御スクリプト")]
    [SerializeField] private PlayerMove playerMoveScript;   // プレイヤー移動スクリプト (PlayerMove)
    [SerializeField] private MouseLook mouseLookScript;     // 視点移動スクリプト (MouseLook)
    [SerializeField] private PlayerLook playerLookScript;   // インタラクションRay判定 (PlayerLook)

    [Header("背景暗転・UI用パネル")]
    [SerializeField] private GameObject blurOrDarkPanel;

    [Header("クロスヘア用テキスト")]
    [SerializeField] private Text crosshairText;

    [Header("鑑定情報表示用UIテキスト")]
    [SerializeField] private Text itemDescriptionText;
    [SerializeField] private Text itemCustomerDescriptionText;
    [SerializeField] private Text appraisalMission1Text;
    [SerializeField] private Text appraisalMission2Text;

    [Header("鑑定金額表示用UIテキスト")]
    [SerializeField] private Text highestPriceText;
    [SerializeField] private Text damagedPriceText;
    [SerializeField] private Text customerDescriptionPriceText;
    [SerializeField] private Text mission1PriceText;
    [SerializeField] private Text mission2PriceText;
    [SerializeField] private Text finalPriceText;

    [Header("減額判定用丸ボタン (Button + 丸表示)")]
    [SerializeField] private Button damagedButton;
    [SerializeField] private GameObject damagedCircleMark;
    [SerializeField] private Button customerDescriptionButton;
    [SerializeField] private GameObject customerDescriptionCircleMark;
    [SerializeField] private Button mission1Button;
    [SerializeField] private GameObject mission1CircleMark;
    [SerializeField] private Button mission2Button;
    [SerializeField] private GameObject mission2CircleMark;

    private ItemData currentItemData;
    public ItemData CurrentItemData => currentItemData;
    private float currentCalculatedPrice;
    public float CurrentCalculatedPrice => currentCalculatedPrice;

    [Header("鑑定アイテム配置位置（鑑定台）")]
    [SerializeField] private Transform inspectionTablePos;
    [SerializeField] private bool centerItemToBounds = true; // お宝モデルの中心（バウンディングボックス中心）に合わせるか
    [SerializeField] private Vector3 inspectionOffset = Vector3.zero; // 配置の微調整用オフセット

    [Header("鑑定中のカメラ特定位置")]
    [SerializeField] private Transform inspectionCameraPos;

    [Header("カメラ回転・注視設定")]
    [SerializeField] private bool lookAtInspectionTable = true; // 移動時に自動でお宝（鑑定台）の方向を向くか
    [SerializeField] private bool smoothCameraMove = true;      // スムーズな補間移動を行うか
    [SerializeField] private float cameraMoveSpeed = 6.0f;       // カメラ移動スピード

    [Header("交渉時のカメラ設定")]
    [Tooltip("お客さんの顔の高さオフセット")]
    [SerializeField] private float customerFaceHeightOffset = 1.6f;

    [Header("お宝の回転・拡大縮小（ズーム）インスペクター設定")]
    [Tooltip("マウスドラッグ時の回転感度")]
    [SerializeField] private float rotationSpeed = 5.0f;
    [Tooltip("拡大縮小（ズーム）機能を有効にするか")]
    [SerializeField] private bool enableZoom = true;
    [Tooltip("マウスホイールのズーム倍率スピード")]
    [SerializeField] private float zoomSpeed = 2.0f;
    [Tooltip("最小縮小倍率（初期サイズの何倍まで縮小可能か）")]
    [SerializeField] private float minScaleMultiplier = 0.3f;
    [Tooltip("最大拡大倍率（初期サイズの何倍まで拡大可能か）")]
    [SerializeField] private float maxScaleMultiplier = 3.0f;

    private GameObject currentInspectingItem;
    public bool isInspecting = false;

    // カメラ元の位置・回転の保持用
    private Camera mainCamera;
    private Vector3 savedCameraPos;
    private Quaternion savedCameraRot;
    private Coroutine cameraMoveCoroutine;

    public bool IsInspecting => isInspecting;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (blurOrDarkPanel != null)
        {
            blurOrDarkPanel.SetActive(false);
        }
    }

    private void Start()
    {
        AutoFindPlayerComponents();
        SetupButtonListeners();
    }

    /// <summary>
    /// プレイヤーの移動・視点移動スクリプトとカメラを自動検出
    /// </summary>
    private void AutoFindPlayerComponents()
    {
        if (playerMoveScript == null) playerMoveScript = FindObjectOfType<PlayerMove>();
        if (mouseLookScript == null) mouseLookScript = FindObjectOfType<MouseLook>();
        if (playerLookScript == null) playerLookScript = FindObjectOfType<PlayerLook>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    /// <summary>
    /// 鑑定を開始する
    /// </summary>
    /// <param name="itemPrefab">鑑定対象のお宝Prefabまたはゲームオブジェクト</param>
    public void StartInspection(GameObject itemPrefab)
    {
        if (isInspecting) return;
        isInspecting = true;

        AutoFindPlayerComponents();

        // クロスヘアテキストをクリア
        if (crosshairText != null) crosshairText.text = "";

        // 鑑定開始時に交渉パネルを非表示
        if (NegotiationManager.Instance != null)
        {
            NegotiationManager.Instance.HideAppraisalAcceptancePanel();
        }

        // 1. プレイヤーの移動・視点移動を停止
        SetPlayerControlsActive(false);

        // マウスカーソルを開放
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. カメラをお宝の方向（特定位置）へ移動
        MoveCameraToInspectionPosition();

        // 3. 背景パネル表示
        if (blurOrDarkPanel != null)
        {
            blurOrDarkPanel.SetActive(true);
        }

        // 4. 鑑定台にお宝モデルを生成・完全に位置固定
        if (itemPrefab != null)
        {
            if (inspectionTablePos != null)
            {
                currentInspectingItem = Instantiate(itemPrefab, inspectionTablePos.position, inspectionTablePos.rotation);
                currentInspectingItem.transform.SetParent(inspectionTablePos);
                currentInspectingItem.transform.localPosition = Vector3.zero; // 鑑定台の中心にピタッと固定

                // お宝の見た目の中心（Z軸含むバウンディングボックス中心）に合わせる補正
                if (centerItemToBounds)
                {
                    Renderer[] renderers = currentInspectingItem.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds bounds = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; i++)
                        {
                            bounds.Encapsulate(renderers[i].bounds);
                        }
                        Vector3 localCenter = inspectionTablePos.InverseTransformPoint(bounds.center);
                        currentInspectingItem.transform.localPosition = -localCenter + inspectionOffset;
                    }
                    else
                    {
                        currentInspectingItem.transform.localPosition = inspectionOffset;
                    }
                }
                else
                {
                    currentInspectingItem.transform.localPosition = inspectionOffset;
                }
            }
            else
            {
                currentInspectingItem = itemPrefab;
            }

            // 【落下防止】物理挙動 (Rigidbody) を無効化・固定化して一切落ちないようにする
            Rigidbody[] rbs = currentInspectingItem.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rbs)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 【判定調整】Colliderのめり込み等で跳ね飛ぶのを防ぐ
            Collider[] cols = currentInspectingItem.GetComponentsInChildren<Collider>();
            foreach (var col in cols)
            {
                col.isTrigger = true;
            }

            // 回転・ズーム操作スクリプト（ItemInspectController）のアタッチ＆インスペクター設定反映
            ItemInspectController inspectController = currentInspectingItem.GetComponent<ItemInspectController>();
            if (inspectController == null)
            {
                inspectController = currentInspectingItem.AddComponent<ItemInspectController>();
            }
            inspectController.SetZoomSettings(zoomSpeed, minScaleMultiplier, maxScaleMultiplier, enableZoom);
            inspectController.StartInspect();

            // ItemDataのテキスト情報をUIに表示
            ItemData itemData = itemPrefab != null ? itemPrefab.GetComponent<ItemData>() : null;
            if (itemData == null && itemPrefab != null) itemData = itemPrefab.GetComponentInParent<ItemData>();
            if (itemData == null && itemPrefab != null) itemData = itemPrefab.GetComponentInChildren<ItemData>();
            UpdateInspectionUI(itemData);
        }
    }

    /// <summary>
    /// 鑑定を終了する
    /// </summary>
    public void EndInspection()
    {
        if (!isInspecting) return;
        isInspecting = false;

        // 鑑定中モデルの破棄
        if (currentInspectingItem != null)
        {
            Destroy(currentInspectingItem);
            currentInspectingItem = null;
        }

        // 背景パネル非表示
        if (blurOrDarkPanel != null)
        {
            blurOrDarkPanel.SetActive(false);
        }

        // UIテキストの表示クリア
        ClearInspectionUI();

        // カメラをお客さんの顔付近に向ける
        LookAtCurrentCustomerFace();

        // 交渉中はプレイヤーの移動・視点移動を無効のままにする

        // 交渉用パネルを表示
        if (NegotiationManager.Instance != null)
        {
            NegotiationManager.Instance.ShowAppraisalAcceptancePanel();
        }
    }

    /// <summary>
    /// Enterキー押下で「購入・買い取り」を決定し、お宝を消去・回収してお客さんを帰す処理
    /// </summary>
    public void BuyItemAndFinishCustomer()
    {
        // 1. 鑑定モードを終了（カメラ復帰・鑑定表示用モデル破棄）
        if (isInspecting)
        {
            EndInspection();
        }

        // 2. カウンターの上にお客さんが出したお宝オブジェクトを消去・回収
        if (DropItemManager.Instance != null)
        {
            DropItemManager.Instance.ClearDroppedItem();
        }

        // 3. レジ最前列（queuePositionNumber == 0）のお客さんを取得して退場（FinishCheckout）
        MoveWaypoint currentCustomer = FindCurrentCustomer();

        if (currentCustomer != null)
        {
            Debug.Log("[InspectionManager] お宝を購入・回収しました！お客さんが帰宅します。");
            currentCustomer.FinishCheckout();
        }
        else
        {
            Debug.Log("[InspectionManager] 購入処理を実行しましたが、レジ前のお客さんが見つかりませんでした。");
        }

        // 取引完了時に交渉パネルを非表示
        if (NegotiationManager.Instance != null)
        {
            NegotiationManager.Instance.HideAppraisalAcceptancePanel();
        }

        // カメラを元の位置へ戻す
        ReturnCameraToOriginalPosition();

        // プレイヤーの移動と視点操作を再開
        SetPlayerControlsActive(true);
    }

    /// <summary>
    /// レジ前（最前列）のお客さんを検索
    /// </summary>
    private MoveWaypoint FindCurrentCustomer()
    {
        MoveWaypoint[] customers = FindObjectsOfType<MoveWaypoint>();
        foreach (var c in customers)
        {
            if (c.queuePositionNumber == 0 && c.isStopped)
            {
                return c;
            }
        }
        foreach (var c in customers)
        {
            if (c.queuePositionNumber == 0)
            {
                return c;
            }
        }
        return null;
    }

    /// <summary>
    /// カメラをお宝（鑑定台）の方向へ向けて特定位置へ移動
    /// </summary>
    private void MoveCameraToInspectionPosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 元の位置・回転を記憶
        savedCameraPos = mainCamera.transform.position;
        savedCameraRot = mainCamera.transform.rotation;

        Vector3 targetPos = (inspectionCameraPos != null) ? inspectionCameraPos.position : mainCamera.transform.position;
        Quaternion targetRot = (inspectionCameraPos != null) ? inspectionCameraPos.rotation : mainCamera.transform.rotation;

        // お宝（鑑定台）の方向を自動的に向かせる
        if (lookAtInspectionTable && inspectionTablePos != null)
        {
            Vector3 dir = (inspectionTablePos.position - targetPos).normalized;
            if (dir != Vector3.zero)
            {
                targetRot = Quaternion.LookRotation(dir);
            }
        }

        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);

        if (smoothCameraMove)
        {
            cameraMoveCoroutine = StartCoroutine(AnimateCamera(targetPos, targetRot));
        }
        else
        {
            mainCamera.transform.position = targetPos;
            mainCamera.transform.rotation = targetRot;
        }
    }

    /// <summary>
    /// カメラをお客さんの顔付近に向ける
    /// </summary>
    private void LookAtCurrentCustomerFace()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        MoveWaypoint customer = FindCurrentCustomer();
        Vector3 targetPos = savedCameraPos;
        Quaternion targetRot = savedCameraRot;

        if (customer != null)
        {
            Vector3 customerFacePos = customer.transform.position + Vector3.up * customerFaceHeightOffset;
            Vector3 dir = (customerFacePos - targetPos).normalized;
            if (dir != Vector3.zero)
            {
                targetRot = Quaternion.LookRotation(dir);
            }
        }

        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);

        if (smoothCameraMove)
        {
            cameraMoveCoroutine = StartCoroutine(AnimateCamera(targetPos, targetRot));
        }
        else
        {
            mainCamera.transform.position = targetPos;
            mainCamera.transform.rotation = targetRot;
        }
    }

    /// <summary>
    /// カメラを元の視点位置へ復帰
    /// </summary>
    private void ReturnCameraToOriginalPosition()
    {
        if (mainCamera == null) return;

        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);

        if (smoothCameraMove)
        {
            cameraMoveCoroutine = StartCoroutine(AnimateCamera(savedCameraPos, savedCameraRot));
        }
        else
        {
            mainCamera.transform.position = savedCameraPos;
            mainCamera.transform.rotation = savedCameraRot;
        }
    }

    /// <summary>
    /// カメラの位置・回転をスムーズに補間移動するコルーチン
    /// </summary>
    private IEnumerator AnimateCamera(Vector3 targetPos, Quaternion targetRot)
    {
        if (mainCamera == null) yield break;

        float t = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        while (t < 1.0f)
        {
            t += Time.deltaTime * cameraMoveSpeed;
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;
    }

    /// <summary>
    /// プレイヤーコントロールの一括切り替え
    /// </summary>
    private void SetPlayerControlsActive(bool isActive)
    {
        if (playerMoveScript != null) playerMoveScript.enabled = isActive;
        if (mouseLookScript != null) mouseLookScript.enabled = isActive;
        if (playerLookScript != null) playerLookScript.enabled = isActive;
    }

    private void Update()
    {
        // 1. Enterキー押下で「購入・買い取り決定＆お宝回収＆お客さん帰宅」処理
        // 2. 右クリックまたはESCでキャンセル/鑑定終了
        if (isInspecting && Input.GetMouseButtonDown(1))
        {
            EndInspection();
        }
    }

    /// <summary>
    /// ItemDataのテキスト情報をレガシーUI Textに反映
    /// </summary>
    private void UpdateInspectionUI(ItemData itemData)
    {
        if (itemData == null) return;

        currentItemData = itemData;

        if (itemDescriptionText != null) itemDescriptionText.text = itemData.itemDescription;
        if (itemCustomerDescriptionText != null) itemCustomerDescriptionText.text = itemData.itemCustomerDescription;
        if (appraisalMission1Text != null) appraisalMission1Text.text = itemData.appraisalMission1;
        if (appraisalMission2Text != null) appraisalMission2Text.text = itemData.appraisalMission2;

        if (highestPriceText != null) highestPriceText.text = itemData.highestPrice.ToString("N0") + "$";
        if (damagedPriceText != null) damagedPriceText.text = itemData.damagedPrice.ToString("N0") + "$";
        if (customerDescriptionPriceText != null) customerDescriptionPriceText.text = itemData.customerDescriptionPrice.ToString("N0") + "$";
        if (mission1PriceText != null) mission1PriceText.text = itemData.isCustomerDescriptionPrice1.ToString("N0") + "$";
        if (mission2PriceText != null) mission2PriceText.text = itemData.isCustomerDescriptionPrice2.ToString("N0") + "$";

        if (damagedCircleMark != null) damagedCircleMark.SetActive(false);
        if (customerDescriptionCircleMark != null) customerDescriptionCircleMark.SetActive(false);
        if (mission1CircleMark != null) mission1CircleMark.SetActive(false);
        if (mission2CircleMark != null) mission2CircleMark.SetActive(false);

        CalculateTotalPrice();
    }

    /// <summary>
    /// 鑑定UIテキストのクリア
    /// </summary>
    private void ClearInspectionUI()
    {
        currentItemData = null;

        if (itemDescriptionText != null) itemDescriptionText.text = string.Empty;
        if (itemCustomerDescriptionText != null) itemCustomerDescriptionText.text = string.Empty;
        if (appraisalMission1Text != null) appraisalMission1Text.text = string.Empty;
        if (appraisalMission2Text != null) appraisalMission2Text.text = string.Empty;

        if (highestPriceText != null) highestPriceText.text = string.Empty;
        if (damagedPriceText != null) damagedPriceText.text = string.Empty;
        if (customerDescriptionPriceText != null) customerDescriptionPriceText.text = string.Empty;
        if (mission1PriceText != null) mission1PriceText.text = string.Empty;
        if (mission2PriceText != null) mission2PriceText.text = string.Empty;
        if (finalPriceText != null) finalPriceText.text = string.Empty;

        if (damagedCircleMark != null) damagedCircleMark.SetActive(false);
        if (customerDescriptionCircleMark != null) customerDescriptionCircleMark.SetActive(false);
        if (mission1CircleMark != null) mission1CircleMark.SetActive(false);
        if (mission2CircleMark != null) mission2CircleMark.SetActive(false);
    }

    /// <summary>
    /// ボタンのイベント設定
    /// </summary>
    private void SetupButtonListeners()
    {
        if (damagedButton != null)
        {
            damagedButton.onClick.AddListener(() =>
            {
                if (damagedCircleMark != null) damagedCircleMark.SetActive(!damagedCircleMark.activeSelf);
                CalculateTotalPrice();
            });
        }
        if (customerDescriptionButton != null)
        {
            customerDescriptionButton.onClick.AddListener(() =>
            {
                if (customerDescriptionCircleMark != null) customerDescriptionCircleMark.SetActive(!customerDescriptionCircleMark.activeSelf);
                CalculateTotalPrice();
            });
        }
        if (mission1Button != null)
        {
            mission1Button.onClick.AddListener(() =>
            {
                if (mission1CircleMark != null) mission1CircleMark.SetActive(!mission1CircleMark.activeSelf);
                CalculateTotalPrice();
            });
        }
        if (mission2Button != null)
        {
            mission2Button.onClick.AddListener(() =>
            {
                if (mission2CircleMark != null) mission2CircleMark.SetActive(!mission2CircleMark.activeSelf);
                CalculateTotalPrice();
            });
        }
    }

    /// <summary>
    /// 予測買取価格の自動計算およびUI反映
    /// </summary>
    public void CalculateTotalPrice()
    {
        if (currentItemData == null) return;

        float total = currentItemData.highestPrice;

        bool isDamagedChecked = damagedCircleMark != null && damagedCircleMark.activeSelf;
        bool isCustomerDescChecked = customerDescriptionCircleMark != null && customerDescriptionCircleMark.activeSelf;
        bool isMission1Checked = mission1CircleMark != null && mission1CircleMark.activeSelf;
        bool isMission2Checked = mission2CircleMark != null && mission2CircleMark.activeSelf;

        if (isDamagedChecked) total += currentItemData.damagedPrice;
        if (isCustomerDescChecked) total += currentItemData.customerDescriptionPrice;
        if (isMission1Checked) total += currentItemData.isCustomerDescriptionPrice1;
        if (isMission2Checked) total += currentItemData.isCustomerDescriptionPrice2;

        currentCalculatedPrice = total;

        if (finalPriceText != null)
        {
            finalPriceText.text = total.ToString("N0") + "$";
        }
    }
}