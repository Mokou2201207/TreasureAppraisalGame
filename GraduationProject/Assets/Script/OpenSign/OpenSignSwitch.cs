using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// お店の看板をOpenにするかCloseにするかのスイッチ
/// </summary>
public class OpenSignSwitch : MonoBehaviour
{
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
    }
}
