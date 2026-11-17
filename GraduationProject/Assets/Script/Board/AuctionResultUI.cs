using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ポストで受け取るオークション結果の手紙の処理（手紙1枚に全部まとめる）
/// </summary>
public class AuctionResultUI : MonoBehaviour
{
    private static AuctionResultUI instance;
    private static float nextFindTime;
    public static AuctionResultUI Instance
    {
        get
        {
            //チェックを外していてまだ動いていなくても使えるように、シーンから探す（探しすぎないように1秒に1回まで）
            if (instance == null && Time.unscaledTime >= nextFindTime)
            {
                nextFindTime = Time.unscaledTime + 1f;
                instance = FindObjectOfType<AuctionResultUI>(true);
            }
            return instance;
        }
        private set { instance = value; }
    }

    [Header("手紙のパネル（開いたら表示）"), SerializeField]
    private GameObject resultPanel;

    [Header("行を並べる場所"), SerializeField]
    private Transform rowParent;

    [Header("行をスクロールさせるScroll View（空ならスクロールしない）"), SerializeField]
    private ScrollRect scrollRect;

    [Header("1行のプレハブ（AuctionResultRowを付けた物）"), SerializeField]
    private AuctionResultRow rowPrefab;

    [Header("お宝の種類ごとの絵"), SerializeField]
    private AuctionBoardUI.ItemIcon[] itemIcons;

    [Header("売上の合計"), SerializeField]
    private Text totalText;

    [Header("もうけ（損）"), SerializeField]
    private Text profitText;

    [Header("受け取るボタン"), SerializeField]
    private Button receiveButton;

    [Header("もうけ・損の文字の色"), SerializeField]
    private Color profitColor = new Color(0.16f, 0.47f, 0.24f);
    [SerializeField]
    private Color lossColor = new Color(0.67f, 0.16f, 0.12f);

    [Header("手紙を開くときの音"), SerializeField]
    private AudioClip openSE;

    [Header("お金を受け取るときの音"), SerializeField]
    private AudioClip receiveSE;

    //手紙を開いているか
    public bool IsOpen => resultPanel != null && resultPanel.activeInHierarchy;

    //受け取る売上の合計
    private int totalPrice;

    /// <summary>
    /// 開始前
    /// </summary>
    private void Awake()
    {
        Instance = this;

        //最初は手紙を隠しておく
        if (resultPanel != null) resultPanel.SetActive(false);

        //受け取るボタンを押したら受け取る処理へ
        if (receiveButton != null) receiveButton.onClick.AddListener(Receive);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 手紙を開いて、結果を並べる処理（ポストから呼ぶ）
    /// </summary>
    public void Open()
    {
        //チェックが外れていたら、ここで動くようにする（Awakeで手紙が隠れて準備される）
        if (!gameObject.activeInHierarchy) ActivateWithParents(transform);

        //手紙の親のチェックが外れていると、手紙を出しても見えないので、親のチェックを入れる
        if (resultPanel != null) ActivateWithParents(resultPanel.transform.parent);

        //受け取る結果がなければ何もしない（手紙がもう見えていても、結果は並べ直す）
        if (AuctionManager.Instance == null || !AuctionManager.Instance.HasResults) return;

        //前に並べた行を消す
        if (rowParent != null)
        {
            foreach (Transform child in rowParent) Destroy(child.gameObject);
        }

        //結果を1行ずつ並べて、売上と買値の合計を数える
        int total = 0;
        int paid = 0;
        foreach (AuctionResult result in AuctionManager.Instance.results)
        {
            if (result == null || result.item == null) continue;

            if (rowPrefab != null && rowParent != null)
            {
                AuctionResultRow row = Instantiate(rowPrefab, rowParent);
                row.Setup(result, GetIcon(result.item.itemName));
            }
            total += result.soldPrice;
            paid += result.item.paidPrice;
        }
        totalPrice = total;

        //売上の合計と、もうけ（損）を出す
        int profit = total - paid;
        if (totalText != null) totalText.text = $"{total:N0}$";
        if (profitText != null)
        {
            profitText.text = profit >= 0 ? $"（もうけ +{profit:N0}$）" : $"（損 {profit:N0}$）";
            profitText.color = profit >= 0 ? profitColor : lossColor;
        }

        //手紙を出して、カーソルを出して、プレイヤーを止める
        resultPanel.SetActive(true);

        //行が多いときも、一番上の行から見えるようにする
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
        SetPlayerActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlaySE(openSE);
    }

    /// <summary>
    /// 渡した物から上の親まで、チェックが外れていたら全部入れる処理
    /// </summary>
    private void ActivateWithParents(Transform t)
    {
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    /// <summary>
    /// 売上を受け取って、手紙を閉じる処理（受け取るボタンから呼ぶ）
    /// </summary>
    public void Receive()
    {
        if (!IsOpen) return;

        //結果を取り出して、売上の合計をお金に足す
        if (AuctionManager.Instance != null) AuctionManager.Instance.TakeResults();
        if (MonayManager.Instance != null) MonayManager.Instance.AddMonay(totalPrice);
        PlaySE(receiveSE);

        //手紙を閉じて、カーソルを隠して、プレイヤーを動かせるように戻す
        resultPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPlayerActive(true);
    }

    /// <summary>
    /// プレイヤーの移動、視点、クリックを止めたり、動かしたりする処理
    /// </summary>
    private void SetPlayerActive(bool isActive)
    {
        //プレイヤーはあとからスポーンするので、そのときに探す
        MouseLook mouseLook = FindObjectOfType<MouseLook>();
        PlayerMove playerMove = FindObjectOfType<PlayerMove>();
        PlayerLook playerLook = FindObjectOfType<PlayerLook>();

        if (mouseLook != null) mouseLook.enabled = isActive;
        if (playerMove != null) playerMove.enabled = isActive;
        if (playerLook != null) playerLook.enabled = isActive;
    }

    /// <summary>
    /// お宝の種類から絵を探す処理
    /// </summary>
    private Sprite GetIcon(ItemData.ItemCategory category)
    {
        if (itemIcons == null) return null;
        foreach (AuctionBoardUI.ItemIcon pair in itemIcons)
        {
            if (pair != null && pair.category == category) return pair.icon;
        }
        return null;
    }

    /// <summary>
    /// SEを鳴らす処理
    /// </summary>
    private void PlaySE(AudioClip clip)
    {
        //音が入っていなければ鳴らさない
        if (clip == null) return;

        //手紙を閉じると手紙の中の音も止まるので、音だけの別のオブジェクトで鳴らす
        GameObject seObject = new GameObject("SE_" + clip.name);
        AudioSource source = seObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.PlayOneShot(clip);
        Destroy(seObject, clip.length + 0.5f);
    }
}
