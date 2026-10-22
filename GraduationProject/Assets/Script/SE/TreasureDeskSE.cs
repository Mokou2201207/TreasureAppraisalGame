using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// お宝が机の上に置かれたときのSE
/// </summary>
public class TreasureDeskSE : MonoBehaviour
{
    [Header("流したいSE"),SerializeField]
    private AudioClip seClip;

    private AudioSource audioSource;

    //机に置かれたかどうかのフラグ
    private bool isOnDesk = false;
    /// <summary>
    /// 開始
    /// </summary>
    private void Start()
    {
        //AudioSourceを取得
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (seClip == null)
        {
            Debug.LogError("SEがアタッチされていません");
        }

        isOnDesk = false;
    }

    /// <summary>
    /// 机に触れたらSEを流す
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (audioSource == null && seClip == null) return;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Desk") && !isOnDesk)
        {
            isOnDesk = true;
            audioSource.PlayOneShot(seClip);
        }
    }
}

