using UnityEngine;

/// <summary>
/// 青いポストの処理（結果が届いていたら手紙を出して、クリックで結果を見る）
/// </summary>
public class MailboxPost : MonoBehaviour
{
    [Header("結果が届いているときに出す手紙（空なら出さない）"), SerializeField]
    private GameObject letterObject;

    [Header("操作できる距離"), SerializeField]
    private float interactDistance = 3f;

    //結果が届いているか
    public bool HasResults => AuctionManager.Instance != null && AuctionManager.Instance.HasResults;
    //操作できる距離
    public float InteractDistance => interactDistance;

    /// <summary>
    /// 更新処理
    /// </summary>
    private void Update()
    {
        //結果が届いているときだけ手紙を出す
        if (letterObject != null && letterObject.activeSelf != HasResults)
        {
            letterObject.SetActive(HasResults);
        }
    }

    /// <summary>
    /// 結果の手紙を開く処理（PlayerLookから呼ぶ）
    /// </summary>
    public void OpenResult()
    {
        //結果が届いていなければ何もしない
        if (!HasResults || AuctionResultUI.Instance == null) return;

        AuctionResultUI.Instance.Open();
    }
}
