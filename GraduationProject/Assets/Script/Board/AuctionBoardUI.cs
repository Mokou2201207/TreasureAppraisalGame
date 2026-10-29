using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// オークションの掲示板を開け閉めできる処理※AI担当
/// </summary>
public class AuctionBoardUI : MonoBehaviour
{
    //インスタンス化
  public static AuctionBoardUI Instance { get; private set; }

    [Header("掲示板のパネル")]
    [SerializeField] private GameObject boardPanel;
    [Header("閉じるボタン（なくてもEscで閉じられる）")]
    [SerializeField] private Button closeButton;

    [Header("カメラが止まる位置（掲示板の前）")]
    [SerializeField] private Transform cameraPoint;
    [Header("カメラが動く時間（秒）")]
    [SerializeField] private float moveTime = 0.6f;
    [Header("パネルをふわっと出す時間（秒）")]
    [SerializeField] private float fadeTime = 0.2f;

    [Header("お宝のカード")]
    [Tooltip("カードを並べる場所（Scroll View の Content）")]
    [SerializeField] private Transform cardParent;
    [SerializeField] private BoardCard cardPrefab;
    [Tooltip("出品できるお宝がないときに出すもの")]
    [SerializeField] private GameObject emptyText;
    [Tooltip("お宝の種類ごとの絵")]
    [SerializeField] private ItemIcon[] itemIcons;

    [Header("上のバー")]
    [SerializeField] private Text moneyText;
    [SerializeField] private Text listCountText;

    [Header("右の詳しい情報")]
    [SerializeField] private GameObject detailArea;
    [SerializeField] private Image detailIcon;
    [SerializeField] private Text detailNameText;
    [SerializeField] private Text detailMemoText;
    [SerializeField] private Text detailPriceText;
    [SerializeField] private Text detailFeeText;
    [SerializeField] private Button detailAddButton;
    [SerializeField] private Text detailAddButtonText;

    [Header("出品手数料（買値に対する割合）")]
    [SerializeField, Range(0f, 1f)] private float feeRate = 0.05f;

    [Header("出品申込書")]
    [Tooltip("上のバーの「出品リスト」ボタン（押すと申込書が出る）")]
    [SerializeField] private Button listButton;
    [SerializeField] private GameObject confirmPanel;
    [Tooltip("申込書の行を並べる場所")]
    [SerializeField] private Transform confirmRowParent;
    [SerializeField] private ConfirmRow confirmRowPrefab;
    [SerializeField] private Text confirmTotalText;
    [SerializeField] private Text confirmFeeText;
    [Tooltip("「結果は次の日の朝に…」やお金が足りないときの文")]
    [SerializeField] private Text confirmMessageText;
    [Tooltip("「この内容で貼り出す」ボタン")]
    [SerializeField] private Button submitButton;
    [Tooltip("「もどる」ボタン")]
    [SerializeField] private Button backButton;
    [Tooltip("貼り出したときのSE（紙の音など）")]
    [SerializeField] private AudioClip submitSE;
    [SerializeField, Range(0f, 1f)] private float submitSEVolume = 1f;

    /// <summary>
    /// お宝の種類と絵の組み合わせ
    /// </summary>
    [System.Serializable]
    public class ItemIcon
    {
        public ItemData.ItemCategory category;
        public Sprite icon;
    }

    // プレイヤーはあとからスポーンするので、開くときに探す
    private MouseLook mouseLook;
    private PlayerMove playerMove;
    private PlayerLook playerLook;

    // 閉じたフレーム（同じEscで設定画面まで開かないようにする）
    private int closedFrame = -1;

    // カメラが動いている途中かどうか
    private bool isMoving = false;
    // 開く前のカメラの位置と向き（プレイヤーから見た位置）
    private Vector3 savedLocalPos;
    private Quaternion savedLocalRot;
    // パネルの透明度を変える部品
    private CanvasGroup panelGroup;

    // 出品リストに入れたお宝（まだ貼り出す前）
    private readonly List<PurchasedItem> listedItems = new List<PurchasedItem>();
    // 並べたカードと、今選んでいるカード
    private readonly List<BoardCard> cards = new List<BoardCard>();
    private BoardCard selectedCard;

    // 出品リストに入れたお宝の一覧（次の手順の申込書で使う）
    public IReadOnlyList<PurchasedItem> ListedItems => listedItems;

    // 申込書が開いているかどうか
    private bool IsConfirmOpen => confirmPanel != null && confirmPanel.activeSelf;
    private AudioSource seSource;

    // 掲示板が開いているか（カメラが動いている途中も開いている扱いにする）
    public bool IsOpen => isMoving || (boardPanel != null && boardPanel.activeSelf);
    // Escをほかのスクリプトに使わせたくないとき
    public bool BlockEscape => IsOpen || closedFrame == Time.frameCount;

    private void Awake()
    {
            Instance = this;
    }

    /// <summary>
    /// 最初は掲示板パネルを非表示にする
    /// </summary>
    private void Start()
    {
        // 掲示板パネルを非表示にする
        boardPanel.SetActive(false);

        // フェード用のCanvasGroupがなければ付ける
        panelGroup = boardPanel.GetComponent<CanvasGroup>();
        if (panelGroup == null) panelGroup = boardPanel.AddComponent<CanvasGroup>();

        // 閉じるボタンがあればクリック時に閉じる
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        // 申込書は最初は閉じておく
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (listButton != null) listButton.onClick.AddListener(OpenConfirm);
        if (submitButton != null) submitButton.onClick.AddListener(Submit);
        if (backButton != null) backButton.onClick.AddListener(CloseConfirm);

        // 右の紙のボタンで、選んでいるお宝を出品リストに入れたり外したりする
        if (detailAddButton != null)
        {
            detailAddButton.onClick.AddListener(() =>
            {
                if (selectedCard != null) ToggleListed(selectedCard);
            });
        }
    }

    /// <summary>
    /// Escで閉じる
    /// </summary>
    private void Update()
    {
        // Escで閉じる
        if (Input.GetKeyDown(KeyCode.Escape) && IsOpen && !isMoving)
        {
            // 申込書が開いているときは、申込書だけ閉じる
            if (IsConfirmOpen) CloseConfirm();
            else Close();
        }
    }

    /// <summary>
    /// 掲示板を開く処理
    /// </summary>
    public void Open()
    {
        if (IsOpen) return;
        StartCoroutine(OpenRoutine());
    }

    /// <summary>
    /// 閉じる処理
    /// </summary>
    public void Close()
    {
        if (!IsOpen || isMoving) return;
        StartCoroutine(CloseRoutine());
    }

    /// <summary>
    /// カメラを掲示板の前まで動かしてから、パネルを出す
    /// </summary>
    private IEnumerator OpenRoutine()
    {
        isMoving = true;

        //パネルの表示とPlayerの制御を不可能にする
        SetPlayerActive(false);

        Transform cam = Camera.main.transform;
        savedLocalPos = cam.localPosition;
        savedLocalRot = cam.localRotation;

        if (cameraPoint != null)
        {
            yield return MoveCamera(cam, cameraPoint.position, cameraPoint.rotation);
        }

        RefreshBoard();
        boardPanel.SetActive(true);
        yield return Fade(0f, 1f);

        //カーソルのロックを解除して表示させる
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isMoving = false;
    }

    /// <summary>
    /// パネルを消してから、カメラを元の位置に戻す
    /// </summary>
    private IEnumerator CloseRoutine()
    {
        isMoving = true;
        CloseConfirm();

        //カーソルのロックを解除して表示させる
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yield return Fade(1f, 0f);

        //パネルの非表示とPlayerの制御を可能にする
        boardPanel.SetActive(false);

        // 元の位置は「プレイヤーから見た位置」で覚えているので、今の世界の位置に直して戻す
        Transform cam = Camera.main.transform;
        Transform parent = cam.parent;
        Vector3 backPos = parent != null ? parent.TransformPoint(savedLocalPos) : savedLocalPos;
        Quaternion backRot = parent != null ? parent.rotation * savedLocalRot : savedLocalRot;
        yield return MoveCamera(cam, backPos, backRot);

        cam.localPosition = savedLocalPos;
        cam.localRotation = savedLocalRot;

        SetPlayerActive(true);

        //同じキーの設定画面まで開かないようにするため、閉じたフレームを記録する
        closedFrame = Time.frameCount;

        isMoving = false;
    }

    /// <summary>
    /// カメラを目的の位置と向きまで、なめらかに動かす
    /// </summary>
    private IEnumerator MoveCamera(Transform cam, Vector3 targetPos, Quaternion targetRot)
    {
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, moveTime);
            // 最初と最後がゆっくりになる動き
            float k = Mathf.SmoothStep(0f, 1f, t);
            cam.position = Vector3.Lerp(startPos, targetPos, k);
            cam.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }
        cam.position = targetPos;
        cam.rotation = targetRot;
    }

    /// <summary>
    /// パネルの透明度を変える
    /// </summary>
    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, fadeTime);
            panelGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        panelGroup.alpha = to;
    }

    /// <summary>
    /// 持ち物のお宝をカードにして並べ直す
    /// </summary>
    private void RefreshBoard()
    {
        if (cardParent == null || cardPrefab == null) return;

        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }
        cards.Clear();
        selectedCard = null;

        List<PurchasedItem> items = InventoryManager.Instance != null ? InventoryManager.Instance.items : new List<PurchasedItem>();

        // 持ち物からなくなったお宝は、出品リストからも外す
        listedItems.RemoveAll(item => !items.Contains(item));

        for (int i = 0; i < items.Count; i++)
        {
            BoardCard card = Instantiate(cardPrefab, cardParent);
            card.Setup(items[i], i + 1, GetIcon(items[i].itemName), listedItems.Contains(items[i]), SelectCard, ToggleListed);
            cards.Add(card);
        }

        // すでに出品中のお宝は、うしろに「出品」のはんこ付きで並べる（選べない）
        List<PurchasedItem> onAuction = AuctionManager.Instance != null ? AuctionManager.Instance.onAuction : new List<PurchasedItem>();
        for (int i = 0; i < onAuction.Count; i++)
        {
            BoardCard card = Instantiate(cardPrefab, cardParent);
            card.Setup(onAuction[i], items.Count + i + 1, GetIcon(onAuction[i].itemName), true, SelectCard, ToggleListed);
            card.SetLocked(true);
            cards.Add(card);
        }

        if (emptyText != null) emptyText.SetActive(cards.Count == 0);
        if (detailArea != null) detailArea.SetActive(cards.Count > 0);

        if (cards.Count > 0) SelectCard(cards[0]);
        UpdateHeader();
    }

    /// <summary>
    /// カードを選んで、右の紙に詳しい情報を出す
    /// </summary>
    private void SelectCard(BoardCard card)
    {
        if (selectedCard != null) selectedCard.SetSelected(false);
        selectedCard = card;
        if (selectedCard != null) selectedCard.SetSelected(true);
        UpdateDetail();
    }

    /// <summary>
    /// お宝を出品リストに入れたり外したりする
    /// </summary>
    private void ToggleListed(BoardCard card)
    {
        if (card == null || card.Item == null || card.IsLocked) return;

        bool listed = listedItems.Contains(card.Item);
        if (listed) listedItems.Remove(card.Item);
        else listedItems.Add(card.Item);

        card.SetListed(!listed);
        UpdateHeader();
        if (card == selectedCard) UpdateDetail();
    }

    /// <summary>
    /// 上のバー（所持金と出品リストの数）を書き直す
    /// </summary>
    private void UpdateHeader()
    {
        if (moneyText != null && MonayManager.Instance != null) moneyText.text = $"{MonayManager.Instance.CurrentMonay:N0}$";
        if (listCountText != null) listCountText.text = listedItems.Count.ToString();
    }

    /// <summary>
    /// 右の紙（選んでいるお宝の詳しい情報）を書き直す
    /// </summary>
    private void UpdateDetail()
    {
        if (selectedCard == null || selectedCard.Item == null) return;
        PurchasedItem item = selectedCard.Item;

        if (detailIcon != null)
        {
            Sprite icon = GetIcon(item.itemName);
            detailIcon.sprite = icon;
            detailIcon.enabled = icon != null;
            detailIcon.preserveAspect = true;
        }
        if (detailNameText != null) detailNameText.text = item.itemName.ToString();
        if (detailMemoText != null) detailMemoText.text = BoardCard.MakeMemo(item);
        if (detailPriceText != null) detailPriceText.text = $"{item.paidPrice:N0}$";
        if (detailFeeText != null) detailFeeText.text = $"{Mathf.RoundToInt(item.paidPrice * feeRate):N0}$";
        if (selectedCard.IsLocked)
        {
            if (detailAddButtonText != null) detailAddButtonText.text = "出品中";
            if (detailAddButton != null) detailAddButton.interactable = false;
            return;
        }
        if (detailAddButton != null) detailAddButton.interactable = true;
        if (detailAddButtonText != null) detailAddButtonText.text = listedItems.Contains(item) ? "出品リストから外す" : "出品リストに貼る";
    }

    /// <summary>
    /// 出品申込書を開く
    /// </summary>
    private void OpenConfirm()
    {
        if (confirmPanel == null || isMoving) return;
        confirmPanel.SetActive(true);
        RefreshConfirm();
    }

    /// <summary>
    /// 出品申込書を閉じる
    /// </summary>
    private void CloseConfirm()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    /// <summary>
    /// 申込書の行と合計を書き直す
    /// </summary>
    private void RefreshConfirm()
    {
        if (confirmRowParent != null)
        {
            foreach (Transform child in confirmRowParent)
            {
                Destroy(child.gameObject);
            }
            if (confirmRowPrefab != null)
            {
                foreach (PurchasedItem item in listedItems)
                {
                    ConfirmRow row = Instantiate(confirmRowPrefab, confirmRowParent);
                    row.Setup(item, GetIcon(item.itemName), RemoveFromConfirm);
                }
            }
        }

        int total = 0;
        foreach (PurchasedItem item in listedItems) total += item.paidPrice;
        int fee = GetTotalFee();

        if (confirmTotalText != null) confirmTotalText.text = $"{total:N0}$";
        if (confirmFeeText != null) confirmFeeText.text = $"{fee:N0}$";

        bool hasItems = listedItems.Count > 0;
        bool enoughMoney = MonayManager.Instance == null || MonayManager.Instance.CurrentMonay >= fee;

        if (confirmMessageText != null)
        {
            if (!hasItems) confirmMessageText.text = "出品するお宝がまだ選ばれていません";
            else if (!enoughMoney) confirmMessageText.text = "手数料を払うお金が足りません";
            else confirmMessageText.text = "結果は「次の日の朝」に、この掲示板へ貼り出されます";
        }
        if (submitButton != null) submitButton.interactable = hasItems && enoughMoney;
    }

    /// <summary>
    /// 申込書の「－」ボタンで、お宝を出品リストから外す
    /// </summary>
    private void RemoveFromConfirm(PurchasedItem item)
    {
        listedItems.Remove(item);
        foreach (BoardCard card in cards)
        {
            if (card.Item == item) card.SetListed(false);
        }
        UpdateHeader();
        UpdateDetail();
        RefreshConfirm();
    }

    /// <summary>
    /// 出品リストのお宝の手数料の合計
    /// </summary>
    private int GetTotalFee()
    {
        int fee = 0;
        foreach (PurchasedItem item in listedItems) fee += Mathf.RoundToInt(item.paidPrice * feeRate);
        return fee;
    }

    /// <summary>
    /// 「この内容で貼り出す」：手数料を払い、お宝を持ち物から出品中へ移す
    /// </summary>
    private void Submit()
    {
        if (listedItems.Count == 0) return;

        int fee = GetTotalFee();
        if (MonayManager.Instance != null)
        {
            if (MonayManager.Instance.CurrentMonay < fee) return;
            MonayManager.Instance.SpendMonay(fee);
        }

        if (AuctionManager.Instance != null) AuctionManager.Instance.AddListings(listedItems);
        else Debug.LogWarning("[AuctionBoardUI] AuctionManager がシーンにありません。GameData に付けてください。");

        if (InventoryManager.Instance != null)
        {
            foreach (PurchasedItem item in listedItems) InventoryManager.Instance.items.Remove(item);
        }
        listedItems.Clear();

        PlaySubmitSE();
        CloseConfirm();
        RefreshBoard();
    }

    /// <summary>
    /// 貼り出したときのSEを鳴らす
    /// </summary>
    private void PlaySubmitSE()
    {
        if (submitSE == null) return;
        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
            seSource.playOnAwake = false;
            seSource.spatialBlend = 0f;
        }
        seSource.PlayOneShot(submitSE, submitSEVolume);
    }

    /// <summary>
    /// お宝の種類から絵を探す
    /// </summary>
    private Sprite GetIcon(ItemData.ItemCategory category)
    {
        if (itemIcons == null) return null;
        foreach (ItemIcon pair in itemIcons)
        {
            if (pair != null && pair.category == category) return pair.icon;
        }
        return null;
    }

    /// <summary>
    /// プレイヤーの移動、視点、クリックを止めたり、動かしたりする処理
    /// </summary>
    /// <param name="active"></param>
    private void SetPlayerActive(bool active)
    {
        if (mouseLook == null) mouseLook = FindObjectOfType<MouseLook>();
        if (playerMove == null) playerMove = FindObjectOfType<PlayerMove>();
        if (playerLook == null) playerLook = FindObjectOfType<PlayerLook>();

        if (mouseLook != null) mouseLook.enabled = active;
        if (playerMove != null) playerMove.enabled = active;
        if (playerLook != null) playerLook.enabled = active;
    }

}

