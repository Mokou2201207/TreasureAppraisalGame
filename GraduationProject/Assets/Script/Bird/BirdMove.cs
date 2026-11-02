using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 鳥が移動する処理
/// </summary>
public class BirdMove : MonoBehaviour
{
    private Animator animator;
    private AudioSource audioSource;
    [Header("飛ぶ際の音")]
    [SerializeField] private AudioClip flySound; 
    //飛ぶ向き
    private Vector3 flyDir;

    private float timer = 0f; // タイマー

    private bool isFlyingSE = false; // 飛ぶSEが再生されたかどうか
    private bool isFlying = false; // 鳥が飛んでいる途中

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
    /// プレイヤーが鳥のエリアに入ったときの処理
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // プレイヤーが鳥に触れたときの処理
            Debug.Log("プレイヤーが鳥のエリアに入りました。");

            // まだ飛んでいなければ飛び立つ（2回目以降は何もしない）
            if (!isFlying) Fly();
        }
    }

    private void Update()
    {
        // 決めた向きに飛び続ける
        if (isFlying)
        {
            transform.Translate(flyDir * Time.deltaTime * 2.5f, Space.World);
        }

        //飛んだ後に１０秒経過したら消す
        if (isFlying)
        {
            timer+=Time.deltaTime;
            if (timer >= 10f)
            {
                GameObject.Destroy(gameObject);
            }
        }

        //animatorとSEの切り替え
        if (isFlying)
        {
            animator.SetBool("isFlying", true);

            if (!isFlyingSE)
            {
                audioSource.PlayOneShot(flySound);
                isFlyingSE = true;
            }
        }
    }



    /// <summary>
    /// 鳥が飛ぶ処理
    /// </summary>
    private void Fly()
    {
        // Rigidbodyを取得して、isKinematicをtrueに設定することで物理演算の影響を受けないようにする
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        // どの方角へ飛ぶか（0〜360度）と、真上からどれだけ前に傾けるか
        float yaw = Random.Range(0f, 360f);
        float tilt = Random.Range(20f, 40f);
        flyDir = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(tilt, 0f, 0f) * Vector3.up;

        // 飛ぶ方角に体を向ける
        transform.rotation = Quaternion.LookRotation(new Vector3(flyDir.x, 0f, flyDir.z));

        isFlying = true;
    }

}

