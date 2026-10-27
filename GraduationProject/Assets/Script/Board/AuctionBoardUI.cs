using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// オークションの掲示板を開け閉めできる処理※AI担当
/// </summary>
public class AuctionBoardUI : MonoBehaviour
{
    //インスタンス化
  public static AuctionBoardUI Instance { get; private set; }

    [Header("掲示板のパネル")]
    [SerializeField] private GameObject boardPanel;
    [Header("閉じるボタン（なくてもEscで閉じられる）")]
    [SerializeField] private Button closeButton;

    [Header("カメラが止まる位置（掲示板の前）")]
    [SerializeField] private Transform cameraPoint;
    [Header("カメラが動く時間（秒）")]
    [SerializeField] private float moveTime = 0.6f;
    [Header("パネルをふわっと出す時間（秒）")]
    [SerializeField] private float fadeTime = 0.2f;

    // プレイヤーはあとからスポーンするので、開くときに探す
    private MouseLook mouseLook;
    private PlayerMove playerMove;
    private PlayerLook playerLook;

    // 閉じたフレーム（同じEscで設定画面まで開かないようにする）
    private int closedFrame = -1;

    // カメラが動いている途中かどうか
    private bool isMoving = false;
    // 開く前のカメラの位置と向き（プレイヤーから見た位置）
    private Vector3 savedLocalPos;
    private Quaternion savedLocalRot;
    // パネルの透明度を変える部品
    private CanvasGroup panelGroup;

    // 掲示板が開いているか（カメラが動いている途中も開いている扱いにする）
    public bool IsOpen => isMoving || (boardPanel != null && boardPanel.activeSelf);
    // Escをほかのスクリプトに使わせたくないとき
    public bool BlockEscape => IsOpen || closedFrame == Time.frameCount;

    private void Awake()
    {
            Instance = this;
    }

    /// <summary>
    /// 最初は掲示板パネルを非表示にする
    /// </summary>
    private void Start()
    {
        // 掲示板パネルを非表示にする
        boardPanel.SetActive(false);

        // フェード用のCanvasGroupがなければ付ける
        panelGroup = boardPanel.GetComponent<CanvasGroup>();
        if (panelGroup == null) panelGroup = boardPanel.AddComponent<CanvasGroup>();

        // 閉じるボタンがあればクリック時に閉じる
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    /// <summary>
    /// Escで閉じる
    /// </summary>
    private void Update()
    {
        // Escで閉じる
        if (Input.GetKeyDown(KeyCode.Escape) && IsOpen && !isMoving)
        {
            Close();
        }
    }

    /// <summary>
    /// 掲示板を開く処理
    /// </summary>
    public void Open()
    {
        if (IsOpen) return;
        StartCoroutine(OpenRoutine());
    }

    /// <summary>
    /// 閉じる処理
    /// </summary>
    public void Close()
    {
        if (!IsOpen || isMoving) return;
        StartCoroutine(CloseRoutine());
    }

    /// <summary>
    /// カメラを掲示板の前まで動かしてから、パネルを出す
    /// </summary>
    private IEnumerator OpenRoutine()
    {
        isMoving = true;

        //パネルの表示とPlayerの制御を不可能にする
        SetPlayerActive(false);

        Transform cam = Camera.main.transform;
        savedLocalPos = cam.localPosition;
        savedLocalRot = cam.localRotation;

        if (cameraPoint != null)
        {
            yield return MoveCamera(cam, cameraPoint.position, cameraPoint.rotation);
        }

        boardPanel.SetActive(true);
        yield return Fade(0f, 1f);

        //カーソルのロックを解除して表示させる
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isMoving = false;
    }

    /// <summary>
    /// パネルを消してから、カメラを元の位置に戻す
    /// </summary>
    private IEnumerator CloseRoutine()
    {
        isMoving = true;

        //カーソルのロックを解除して表示させる
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yield return Fade(1f, 0f);

        //パネルの非表示とPlayerの制御を可能にする
        boardPanel.SetActive(false);

        // 元の位置は「プレイヤーから見た位置」で覚えているので、今の世界の位置に直して戻す
        Transform cam = Camera.main.transform;
        Transform parent = cam.parent;
        Vector3 backPos = parent != null ? parent.TransformPoint(savedLocalPos) : savedLocalPos;
        Quaternion backRot = parent != null ? parent.rotation * savedLocalRot : savedLocalRot;
        yield return MoveCamera(cam, backPos, backRot);

        cam.localPosition = savedLocalPos;
        cam.localRotation = savedLocalRot;

        SetPlayerActive(true);

        //同じキーの設定画面まで開かないようにするため、閉じたフレームを記録する
        closedFrame = Time.frameCount;

        isMoving = false;
    }

    /// <summary>
    /// カメラを目的の位置と向きまで、なめらかに動かす
    /// </summary>
    private IEnumerator MoveCamera(Transform cam, Vector3 targetPos, Quaternion targetRot)
    {
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, moveTime);
            // 最初と最後がゆっくりになる動き
            float k = Mathf.SmoothStep(0f, 1f, t);
            cam.position = Vector3.Lerp(startPos, targetPos, k);
            cam.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }
        cam.position = targetPos;
        cam.rotation = targetRot;
    }

    /// <summary>
    /// パネルの透明度を変える
    /// </summary>
    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, fadeTime);
            panelGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        panelGroup.alpha = to;
    }

    /// <summary>
    /// プレイヤーの移動、視点、クリックを止めたり、動かしたりする処理
    /// </summary>
    /// <param name="active"></param>
    private void SetPlayerActive(bool active)
    {
        if (mouseLook == null) mouseLook = FindObjectOfType<MouseLook>();
        if (playerMove == null) playerMove = FindObjectOfType<PlayerMove>();
        if (playerLook == null) playerLook = FindObjectOfType<PlayerLook>();

        if (mouseLook != null) mouseLook.enabled = active;
        if (playerMove != null) playerMove.enabled = active;
        if (playerLook != null) playerLook.enabled = active;
    }

}

