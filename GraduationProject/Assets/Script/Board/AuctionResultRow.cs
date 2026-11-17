using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// オークション結果の手紙に並べる1行分の処理
/// </summary>
public class AuctionResultRow : MonoBehaviour
{
    [Header("お宝の絵"), SerializeField]
    private Image iconImage;

    [Header("お宝の名前"), SerializeField]
    private Text nameText;

    [Header("買値"), SerializeField]
    private Text paidPriceText;

    [Header("鑑定チェックの○×（傷・話・ミッション①・ミッション②の順）"), SerializeField]
    private Image[] checkMarks = new Image[4];

    [Header("落札価格"), SerializeField]
    private Text soldPriceText;

    [Header("スタンプ"), SerializeField]
    private Image stampImage;

    [Header("○と×の絵"), SerializeField]
    private Sprite correctSprite;
    [SerializeField]
    private Sprite wrongSprite;

    [Header("スタンプの絵（大もうけ・もうけ・損）"), SerializeField]
    private Sprite bigProfitStamp;
    [SerializeField]
    private Sprite profitStamp;
    [SerializeField]
    private Sprite lossStamp;

    [Header("大もうけになる、買値に対するもうけの割合"), SerializeField]
    private float bigProfitRate = 0.5f;

    [Header("もうけ・損の文字の色"), SerializeField]
    private Color profitColor = new Color(0.16f, 0.47f, 0.24f);
    [SerializeField]
    private Color lossColor = new Color(0.67f, 0.16f, 0.12f);

    /// <summary>
    /// 1行分の結果をセットする処理
    /// </summary>
    public void Setup(AuctionResult result, Sprite icon)
    {
        PurchasedItem item = result.item;

        //お宝の絵と名前と買値をセット
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
        }
        if (nameText != null) nameText.text = item.itemName.ToString();
        if (paidPriceText != null) paidPriceText.text = $"買値 {item.paidPrice:N0}$";

        //鑑定チェックが合っていたら○、違っていたら×
        bool[] correct =
        {
            item.checkedDamaged == item.isDamaged,
            item.checkedDescriptionWrong == item.isDescriptionWrong,
            item.checkedMission1 == item.isAppraisalMission1,
            item.checkedMission2 == item.isAppraisalMission2,
        };
        for (int i = 0; i < checkMarks.Length && i < correct.Length; i++)
        {
            if (checkMarks[i] == null) continue;
            checkMarks[i].sprite = correct[i] ? correctSprite : wrongSprite;
            checkMarks[i].preserveAspect = true;
        }

        //落札価格は、もうけなら緑、損なら赤
        int diff = result.soldPrice - item.paidPrice;
        if (soldPriceText != null)
        {
            soldPriceText.text = $"{result.soldPrice:N0}$";
            soldPriceText.color = diff >= 0 ? profitColor : lossColor;
        }

        //もうけの大きさでスタンプを変える
        if (stampImage != null)
        {
            Sprite stamp;
            if (diff >= item.paidPrice * bigProfitRate) stamp = bigProfitStamp;
            else if (diff >= 0) stamp = profitStamp;
            else stamp = lossStamp;

            stampImage.sprite = stamp;
            stampImage.enabled = stamp != null;
            stampImage.preserveAspect = true;
        }
    }
}
