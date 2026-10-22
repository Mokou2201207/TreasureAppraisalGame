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

    [Header("まだカードがない時に出すテキスト")]
    [SerializeField] private GameObject emptyText;

    [Header("カードを並べる場所（Scroll View の Content）")]
    [SerializeField] private Transform cardParent;
    [Header("カードのプレハブ")]
    [SerializeField] private ItemCard cardPrefab;

    [SerializeField] private AudioSource audioSource;

    public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

    private void Start()
    {
        // インベントリパネルを非表示にする
        inventoryPanel.SetActive(false);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // 鑑定中と交渉中はインベントリを開けない
            if (InspectionManager.Instance != null && InspectionManager.Instance.IsInspecting) return;
            if (NegotiationManager.Instance != null && NegotiationManager.Instance.IsNegotiating) return;

            // プレイヤーはあとからスポーンするので、まだ見つけていなければ探す
            if (mouseLookScript == null) mouseLookScript = FindObjectOfType<MouseLook>();
            if (playerMoveScript == null) playerMoveScript = FindObjectOfType<PlayerMove>();

            audioSource.Play();

            bool open = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(open);

            if(open) RefreshCards();

            // 開いている間はカーソルを出して、視点と移動を止める
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
            if (mouseLookScript != null) mouseLookScript.enabled = !open;
            if (playerMoveScript != null) playerMoveScript.enabled = !open;
        }
    }

    /// <summary>
    /// 持ち物一覧からカードを生成して表示するメソッド
    /// </summary>
    private void RefreshCards()
    {
        if (InventoryManager.Instance == null) return;

        //前に作ったカードを消す※そうしないと開くたびカードが増えるから
        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }

        //一覧の数だけカードを作る
        List<PurchasedItem> items = InventoryManager.Instance.items;
        for (int i = 0; i < items.Count; i++)
        {
            ItemCard card = Instantiate(cardPrefab, cardParent);
            card.Setup(items[i], i + 1);
        }

        //カードがない場合のテキストを出す
        if (emptyText!=null)
        {
            emptyText.SetActive(items.Count == 0);
        }
    }
}
