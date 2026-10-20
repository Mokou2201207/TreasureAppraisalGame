using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// インベントリの処理
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("インベントリのパネル")]
    [SerializeField] private GameObject inventoryPanel;
    [Header("視点を動かすスクリプト")]
    [SerializeField] private MouseLook mouseLookScript;
    [Header("移動のスクリプト")]
    [SerializeField] private PlayerMove playerMoveScript;

    private void Start()
    {
        // インベントリパネルを非表示にする
        inventoryPanel.SetActive(false);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // プレイヤーはあとからスポーンするので、まだ見つけていなければ探す
            if (mouseLookScript == null) mouseLookScript = FindObjectOfType<MouseLook>();
            if (playerMoveScript == null) playerMoveScript = FindObjectOfType<PlayerMove>();

            bool open = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(open);

            // 開いている間はカーソルを出して、視点と移動を止める
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
            if (mouseLookScript != null) mouseLookScript.enabled = !open;
            if (playerMoveScript != null) playerMoveScript.enabled = !open;
        }
    }
}
