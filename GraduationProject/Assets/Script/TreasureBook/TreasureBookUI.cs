using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureBookUI : MonoBehaviour
{
    [Header("BookUIのパネル"), SerializeField]
    private GameObject bookUI;

    [Header("本のImage"), SerializeField]
    private GameObject bookImage;

    [Header("InspectionManagerをアタッチ"), SerializeField]
    private InspectionManager inspectionManager;

    private AudioSource audioSource;

    [Header("ページを開くSE"), SerializeField]
    private AudioClip openSE;
    [Header("ページを閉じるSE"), SerializeField]
    private AudioClip closeSE;

    //ページを開いているかどうか
    public bool isBookOpen = false;

    void Start()
    {
        //格納
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        //画像を非表示にする
        bookUI.SetActive(false);

        //フラグを最初に決める
        isBookOpen = false;
    }

    private void Update()
    {
        //BKeyを押したら本を開くか閉じる
        if (Input.GetKeyDown(KeyCode.B)&&inspectionManager.IsInspecting)
        {
            if (!isBookOpen)
            {
                //カーソル表示
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                bookUI.SetActive(true);
                isBookOpen = true;
                //図鑑を開いている間は、プレイヤーの操作と視点を止める
                SetPlayerControl(false);
                //SEを鳴らす
                if (audioSource != null && openSE != null)
                {
                    audioSource.PlayOneShot(openSE);
                }
            }
            else
            {
                //カーソル非表示
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                bookUI.SetActive(false);
                isBookOpen = false;
                //図鑑を閉じたら、プレイヤーの操作と視点を戻す
                SetPlayerControl(true);
                //SEを鳴らす
                if (audioSource != null && closeSE != null)
                {
                    audioSource.PlayOneShot(closeSE);
                }
            }
        }
    }

    /// <summary>
    /// プレイヤーの移動と視点を止めたり、動けるようにしたりする
    /// </summary>
    private void SetPlayerControl(bool isActive)
    {
        //プレイヤーはあとからスポーンするので、そのときに探す
        MouseLook mouseLook = FindObjectOfType<MouseLook>();
        PlayerMove playerMove = FindObjectOfType<PlayerMove>();
        PlayerLook playerLook = FindObjectOfType<PlayerLook>();

        if (mouseLook != null) mouseLook.enabled = isActive;
        if (playerMove != null) playerMove.enabled = isActive;
        if (playerLook != null) playerLook.enabled = isActive;
    }
}
