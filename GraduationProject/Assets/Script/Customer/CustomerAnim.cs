using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// お客さん専用のアニメーション管理スクリプト
/// </summary>
public class CustomerAnim : MonoBehaviour
{
    [Header("MoveWaypointのスクリプトをアタッチ"), SerializeField]
    private MoveWaypoint moveWaypointScript;

    [Header("アニメーターをアタッチ"), SerializeField]
    private Animator animator;

    private void Start()
    {
        if (moveWaypointScript == null)
        {
            moveWaypointScript = GetComponent<MoveWaypoint>();
        }

        if (animator == null)
        {
            // 親または子オブジェクトからアニメーターを自動取得
            animator = GetComponentInChildren<Animator>();
        }

        if (moveWaypointScript == null)
        {
            Debug.LogWarning("MoveWaypointスクリプトが見つかりませんでした。");
        }

        if (animator == null)
        {
            Debug.LogWarning("Animatorコンポーネントが見つかりませんでした。");
        }
    }

    private void Update()
    {
        if (animator == null) return;

        bool moving = false;
        if (moveWaypointScript != null)
        {
            moving = moveWaypointScript.isMoving;
        }

        animator.SetBool("isMoving", moving);
    }
}
