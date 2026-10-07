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

    [Header("鑑定情報表示用UIテキスト")]
    [SerializeField] private Text itemDescriptionText;
    [SerializeField] private Text itemCustomerDescriptionText;
    [SerializeField] private Text appraisalMission1Text;
    [SerializeField] private Text appraisalMission2Text;

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
    private bool isInspecting = false;

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

        // カメラを元のプレイヤー位置へ復帰
        ReturnCameraToOriginalPosition();

        // プレイヤーの移動・視点移動を再開
        SetPlayerControlsActive(true);

        // マウスカーソルを固定
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            BuyItemAndFinishCustomer();
        }
        // 2. 右クリックまたはESCでキャンセル/鑑定終了
        else if (isInspecting && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
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

        if (itemDescriptionText != null) itemDescriptionText.text = itemData.itemDescription;
        if (itemCustomerDescriptionText != null) itemCustomerDescriptionText.text = itemData.itemCustomerDescription;
        if (appraisalMission1Text != null) appraisalMission1Text.text = itemData.appraisalMission1;
        if (appraisalMission2Text != null) appraisalMission2Text.text = itemData.appraisalMission2;
    }

    /// <summary>
    /// 鑑定UIテキストのクリア
    /// </summary>
    private void ClearInspectionUI()
    {
        if (itemDescriptionText != null) itemDescriptionText.text = string.Empty;
        if (itemCustomerDescriptionText != null) itemCustomerDescriptionText.text = string.Empty;
        if (appraisalMission1Text != null) appraisalMission1Text.text = string.Empty;
        if (appraisalMission2Text != null) appraisalMission2Text.text = string.Empty;
    }
}