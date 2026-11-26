using UnityEngine;

/// <summary>
/// 背後カメラの土台：位置は体にぴったりついて行き、向き（左右）だけゆっくり追いかける※AI担当
/// 歩くときの体の細かい揺れをカメラに伝えないための物
/// </summary>
[DefaultExecutionOrder(-100)]
public class SmoothYawFollow : MonoBehaviour
{
    [Tooltip("ついて行く体（ClearCutamar）")]
    [SerializeField] private Transform target;

    [Tooltip("向きを追いかける速さ（大きいほどゆっくり）")]
    [SerializeField] private float yawSmoothTime = 0.6f;

    private float yaw;
    private float yawVelocity;

    private void OnEnable()
    {
        Snap();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position;
        yaw = Mathf.SmoothDampAngle(yaw, target.eulerAngles.y, ref yawVelocity, yawSmoothTime);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void Snap()
    {
        if (target == null) return;
        yaw = target.eulerAngles.y;
        yawVelocity = 0f;
        transform.position = target.position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
