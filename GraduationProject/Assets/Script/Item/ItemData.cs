using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemData : MonoBehaviour
{
    [Header("アイテムタイプ"), SerializeField]
    public string itemTypeName = "TreasureType";

    /// <summary>
    /// アイテムの名前をここで追加
    /// </summary>
    public enum ItemCategory
    {
        古い壺,
        ヨーロッパ風の壺,
    }

    [Header("アイテムの名称")]
    public ItemCategory itemName;

    [Header("アイテムの説明")]
    public string itemDescription;
    [Header("お客さんからのアイテムの説明")]
    public string itemCustomerDescription;

    [Header("鑑定オリジナルミッション①")]
    public string appraisalMission1;
    [Header("鑑定オリジナルミッション②")]
    public string appraisalMission2;

    [Header("基本最高金額の値段")]
    public float highestPrice;
    [Header("傷がついていた場合のマイナス値段")]
    public float damagedPrice;
    [Header("情報が違う場合のマイナス値段")]
    public float customerDescriptionPrice;
    [Header("鑑定オリジナルミッションの値段")]
    public float isCustomerDescriptionPrice1;
    [Header("鑑定オリジナルミッションの値段")]
    public float isCustomerDescriptionPrice2;

    [Header("傷がついているか"), SerializeField]
    public bool isDamaged = false;
    [Header("お客さんの情報とあっているか"), SerializeField]
    public bool isCustomerDescription = false;
    [Header("鑑定オリジナルミッション①"), SerializeField]
    public bool isAppraisalMission1 = false;
    [Header("鑑定オリジナルミッション②"), SerializeField]
    public bool isAppraisalMission2 = false;

    /// <summary>
    /// 開始処理からitemNameによってインスペクターがアタッチされる処理に移行
    /// </summary>
    private void Start()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        switch (itemName) 
        {
            case ItemCategory.古い壺:
                itemDescription = "江戸時代後期のものと思われる、素朴な土肌の古い壺。\n表面に幾重ものスレや傷があり、使い込まれた歴史を感じさせる。";
                appraisalMission1 = "雑に修復された「金継ぎ」や接着跡がないか見極める";
                appraisalMission2 = "全体に付着したカビ・錆の広がり具合を確認する";
                //最高金額
                highestPrice = 10000f;
                string priceText1 = highestPrice.ToString("N0") + "$";
                //傷つきのマイナス値段
                damagedPrice = -2000f;
                string priceText2 = highestPrice.ToString("N0") + "$";
                //情報が違う場合のマイナス値段
                customerDescriptionPrice = -500f;
                string priceText3 = highestPrice.ToString("N0") + "$";
                //鑑定オリジナルミッションの値段
                isCustomerDescriptionPrice1 = -1500f;
                string priceText4 = highestPrice.ToString("N0") + "$";
                //鑑定オリジナルミッションの値段
                isCustomerDescriptionPrice2 = -2000f;
                string priceText5 = highestPrice.ToString("N0") + "$";
                break;

            case ItemCategory.ヨーロッパ風の壺:
                break;
        }
            ApplyRandomCustomerDescription();

    }

    /// <summary>
    /// 正解・不正解のセリフ配列からランダムに1つ選んでセットする処理
    /// </summary>
    public void ApplyRandomCustomerDescription()
    {
        string[] correctTexts = null;
        string[] incorrectTexts = null;

        switch (itemName)
        {
            case ItemCategory.古い壺:
                // 古い壺用の正解セリフ候補
                correctTexts = new string[]
                {
                    "「実家の古い蔵を片付けてたら、床下からゴロゴロ出てきたんだ。間違いなく先祖の代からのものさ」",
                    "「うちのじいちゃんが昔、骨董市で太鼓判を押されて買ったやつだよ」"
                };
                // 古い壺用の不正解セリフ候補
                incorrectTexts = new string[]
                {
                    "「さっき裏の畑を掘り返したら、ピカピカの壺が出てきたんだ！すごいお宝だろ？」",
                    "「みてくれよ！！これけっこう古いだろう？？自分はこれは縄文時代に作られたやつじゃないかな？？」"
                };
                break;

            case ItemCategory.ヨーロッパ風の壺:
                correctTexts = new string[]
                {
                    "「旅の商人からアンティーク品として正式に譲り受けたインポート品の壺さ」",
                    "「祖母が外国の貿易商から形見として受け継いだものだと聞いてい」"
                };
                incorrectTexts = new string[]
                {
                    "「近所の雑貨屋のセールワゴンで、お皿と一緒に100円で山積みになってたやつだ！」",
                    "「外国の王様が使っていたお下がりだって、道行く外国人が言ってたぞ！」"
                };
                break;
        }

        if (correctTexts == null || incorrectTexts == null) return;

        // isCustomerDescriptionをアタッチ
        if (isCustomerDescription)
        {
            int index = Random.Range(0, correctTexts.Length);
            itemCustomerDescription = correctTexts[index];
        }
        else
        {
            int index = Random.Range(0, incorrectTexts.Length);
            itemCustomerDescription = incorrectTexts[index];
        }
    }
}
