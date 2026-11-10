using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// お店の看板をOpenにするかCloseにするかのスイッチ
/// </summary>
public class OpenSignSwitch : MonoBehaviour
{
    private AudioSource audioSource;
    [Header("持ち上げる音"),SerializeField]
    private AudioClip liftSE;
    [Header("落とす音"), SerializeField]
    private AudioClip dropSE;

    private Animator animator;
    //看板をOpenにするかCloseにするかのフラグ
    public bool isOpen = false;
    //看板が動き終わるまでの時間
    [SerializeField] private float moveTime = 0.6f;
    //次に押せるようになる時間
    private float nextToggleTime = 0f;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (audioSource==null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// 看板のOpenとCloseを切りかえる
    /// </summary>
    public void ToggleSign()
    {
        // 動いている途中は押しても受け付けない
        if (Time.time < nextToggleTime) return;

        nextToggleTime = Time.time + moveTime;
        isOpen = !isOpen;
        animator.SetBool("isOpen", isOpen);

        // 閉店：お客さん全員に帰ってもらう
        foreach (MoveWaypoint customer in FindObjectsOfType<MoveWaypoint>())
        {
            customer.GoHome();
        }
    }

    /// <summary>
    /// animationイベントで呼ばれるSE再生用の関数（持ち上げる）
    /// </summary>
    public void LiftSE()
    {
        audioSource.PlayOneShot(liftSE);
    }

    /// <summary>
    /// animationイベントで呼ばれるSE再生用の関数（落とす）
    /// </summary>
    public void DropSE()
    {
        audioSource.PlayOneShot(dropSE);
    }
}
