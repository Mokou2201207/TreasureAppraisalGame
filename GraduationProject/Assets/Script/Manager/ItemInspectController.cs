using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 鑑定中にお宝モデルを特定位置に固定したまま、マウスドラッグ回転・ホイール拡大縮小を行うスクリプト
/// </summary>
public class ItemInspectController : MonoBehaviour
{
    [Header("回転速度")]
    [SerializeField] private float rotationSpeed = 5.0f;

    [Header("マウスホイールによる拡大縮小を有効にするか")]
    [SerializeField] private bool enableZoom = true;

    [Header("マウスホイール回転時の拡大縮小スピード")]
    [SerializeField] private float zoomSpeed = 2.0f;

    [Header("最小スケール倍率")]
    [SerializeField] private float minScaleMultiplier = 0.3f;

    [Header("最大スケール倍率")]
    [SerializeField] private float maxScaleMultiplier = 3.0f;

    private bool isInspecting = true;
    private Vector3 initialLocalPos;
    private Vector3 initialScale;
    private Quaternion initialRotation;

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
        initialScale = transform.localScale;
        initialRotation = transform.localRotation;
    }

    private void Update()
    {
        if (!isInspecting) return;

        // 【位置固定】重力や物理計算で落ちないよう位置を完全固定
        transform.localPosition = initialLocalPos;

        // 【マウス左ドラッグで向き（回転）変更】
        if (Input.GetMouseButton(0))
        {
            float xDelta = Input.GetAxis("Mouse X") * rotationSpeed;
            float yDelta = Input.GetAxis("Mouse Y") * rotationSpeed;

            // カメラ・世界軸に合わせて360度スムーズ回転
            transform.Rotate(Vector3.up, -xDelta, Space.World);
            transform.Rotate(Vector3.right, yDelta, Space.World);
        }

        // 【マウスホイールで拡大・縮小（ズーム）】
        if (enableZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 newScale = transform.localScale + Vector3.one * scroll * zoomSpeed;

                // 最小・最大スケール範囲制限
                float clampedX = Mathf.Clamp(newScale.x, initialScale.x * minScaleMultiplier, initialScale.x * maxScaleMultiplier);
                float clampedY = Mathf.Clamp(newScale.y, initialScale.y * minScaleMultiplier, initialScale.y * maxScaleMultiplier);
                float clampedZ = Mathf.Clamp(newScale.z, initialScale.z * minScaleMultiplier, initialScale.z * maxScaleMultiplier);

                transform.localScale = new Vector3(clampedX, clampedY, clampedZ);
            }
        }

        // Rキーで初期向き・サイズへリセット
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTransform();
        }
    }

    /// <summary>
    /// インスペクターからズーム設定を外部変更・適用するメソッド
    /// </summary>
    public void SetZoomSettings(float speed, float minMult, float maxMult, bool enabled = true)
    {
        zoomSpeed = speed;
        minScaleMultiplier = minMult;
        maxScaleMultiplier = maxMult;
        enableZoom = enabled;
    }

    /// <summary>
    /// 鑑定操作を開始
    /// </summary>
    public void StartInspect()
    {
        isInspecting = true;
        initialLocalPos = transform.localPosition;
    }

    /// <summary>
    /// 鑑定操作を停止
    /// </summary>
    public void StopInspect()
    {
        isInspecting = false;
    }

    /// <summary>
    /// 初期回転・サイズにリセット
    /// </summary>
    public void ResetTransform()
    {
        transform.localRotation = initialRotation;
        transform.localScale = initialScale;
        transform.localPosition = initialLocalPos;
    }
}