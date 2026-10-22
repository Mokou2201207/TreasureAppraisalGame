using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// お宝の情報
/// </summary>
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
    [Header("お客さんの話がちがうか"), SerializeField]
    public bool isDescriptionWrong = false;
    [Header("鑑定オリジナルミッション①"), SerializeField]
    public bool isAppraisalMission1 = false;
    [Header("鑑定オリジナルミッション②"), SerializeField]
    public bool isAppraisalMission2 = false;

    [Header("ダメージの見た目")]
    public GameObject damagedVisual;
    [Header("ミッション①の見た目")]
    public GameObject mission1Visual;
    [Header("ミッション②の見た目")]
    public GameObject mission2Visual;

    [Header("ダメージが出る確率")]
    [Range(0f, 1f)] public float damagedChance = 0.4f;
    [Header("ミッション①確率")]
    [Range(0f, 1f)] public float mission1Chance = 0.3f;
    [Header("ミッション②確率")]
    [Range(0f, 1f)] public float mission2Chance = 0.4f;
    [Header("ご情報を出す確率")]
    [Range(0f, 1f)] public float wrongDescriptionChance = 0.5f;

    // 一度ランダムに決めたら決め直さないための目印
    [HideInInspector] public bool isRandomized = false;

    /// <summary>
    /// 開始処理からitemNameによってインスペクターがアタッチされる処理に移行
    /// </summary>
    private void Start()
    {
        OnValidate();
        RandomizeState();
    }

    /// <summary>
    /// お宝のミッションや値段を決める処理
    /// </summary>
    private void OnValidate()
    {
        //同じ宝でセリフが選びなおされないようにされるための
        if (!isRandomized)　ApplyRandomCustomerDescription();
        switch (itemName) 
        {
            case ItemCategory.古い壺:
                itemDescription = "江戸時代後期のものと思われる、素朴な土肌の古い壺。\n表面に幾重ものスレや傷があり、使い込まれた歴史を感じさせる。";
                appraisalMission1 = "雑に修復された「金継ぎ」や接着跡がないか見極める";
                appraisalMission2 = "全体に付着した錆の広がり具合を確認する";
                //最高金額
                highestPrice = 5000f;
                string oldPotPriceText1 = highestPrice.ToString("N0") + "$";
                //傷つきのマイナス値段
                damagedPrice = -1000f;
                string oldPotPriceText2 = highestPrice.ToString("N0") + "$";
                //情報が違う場合のマイナス値段
                customerDescriptionPrice = -500f;
                string oldPotPriceText3 = highestPrice.ToString("N0") + "$";
                //鑑定オリジナルミッションの値段
                isCustomerDescriptionPrice1 = -1500f;
                string oldPotPriceText4 = highestPrice.ToString("N0") + "$";
                //鑑定オリジナルミッションの値段
                isCustomerDescriptionPrice2 = -2000f;
                string oldPotPriceText5 = highestPrice.ToString("N0") + "$";
                break;

            case ItemCategory.ヨーロッパ風の壺:
                itemDescription = "遠い国の古いお城で、花を生けるのに使われていたと言われる壺。\n金色のふちどりと、つるバラの絵付けがうつくしい。";
                appraisalMission1 = "バラが一輪だけ書き足されているか";
                appraisalMission2 = "絵が繋がっていない";
                //最高金額
                highestPrice = 8500f;
                string europePotPriceText1 = highestPrice.ToString("N0") + "$";
                //傷つきのマイナス値段
                damagedPrice = -1000f;
                string europePotPriceText2 = highestPrice.ToString("N0") + "$";
                //情報が違う場合のマイナス値段
                customerDescriptionPrice = -500f;
                string europePotPriceText3 = highestPrice.ToString("N0") + "$";
                //鑑定オリジナルミッションの値段
                isCustomerDescriptionPrice1 = -1000f;
                string europePotPriceText4 = highestPrice.ToString("N0") + "$";
                //鑑定オリジナルミッションの値段
                isCustomerDescriptionPrice2 = -3000f;
                string europePotPriceText5 = highestPrice.ToString("N0") + "$";
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
                    "実家の古い蔵を片付けてたら、床下からゴロゴロ出てきたんだ。間違いなく先祖の代からのものさ",
                    "うちのじいちゃんが昔、骨董市で太鼓判を押されて買ったやつだよ",
                    "ひいおじいちゃんの家の蔵から出てきた、江戸時代の壺らしいです",
                    "つやのない、土そのままの色の壺なんです",
                    "派手な絵とかは何もない、地味な壺ですけど高く売れるかな？？",
                    "ずっと使われてきたから、すり傷がいっぱいあるんです",
                };
                // 古い壺用の不正解セリフ候補
                incorrectTexts = new string[]
                {
                    "さっき裏の畑を掘り返したら、ピカピカの壺が出てきたんだ！すごいお宝だろ？",
                    "みてくれよ！！これけっこう古いだろう？？自分はこれは縄文時代に作られたやつじゃないかな？？",
                    "この宝みてくれよ！古いだろ？？傷がすごすぎるんだ今にも砕けそうな傷じゃない？？",
                    "江戸時代のはじめごろの壺だって、鑑定書にありました",
                    "この壺濡らしたら虹色になるらしいよ",
                    "平安時代のものらしくて、すごく古いんですよ",
                    "土の色の壺で、表面はつるつるにつやが出ているんです",
                    "江戸の後期の土の壺で、スレも多いんですけど、実は去年、職人さんが昔風に作ったものなんです",
                    "素朴で、使い込まれた、江戸時代後期の壺……だと思ってたんですけど、焼き物の教室で作ったやつでした",
                };
                break;

            case ItemCategory.ヨーロッパ風の壺:
                correctTexts = new string[]
                {
                    "おばあちゃんが若いころ、外国のお城の売り立てで買ってきた壺なんです",
                    "昔はお城の広間で、バラを生けていたって聞いてます",
                    "白い地にピンクのバラって、なんだか上品ですよね",
                    "遠い国のお城にあったものだって、箱の紙に書いてありました",
                    "お城の持ち主が手放したときに、日本に渡ってきたらしいです",
                    "口と底が金色で、ちょっとお姫さまっぽい壺なんです",
                    "この壺は、遠い国の古いお城で、花を生けるのに使われていたそうですよ",
                    "この壺ってふちどりは必ず金色で作るらしいですよ、決して銀で作らないらしいですよ、何でかな？？"

                };
                incorrectTexts = new string[]
                {
                    "近所の雑貨屋のセールワゴンで、お皿と一緒に100円で山積みになってたやつだ！",
                    "この壺はねイタリア産らしいよ！みてて惚れ惚れするよね...",
                    "青い線で、竹の絵が描いてある壺なんですよ",
                    "バラが特徴的なんですよね...ちなみにくもると黒に変わるらしいですよ",
                    "お寺のお坊さんが、お米を入れるのに使ってたそうです",
                    "遠い国のお城で、ワインを入れるのに使われていたそうです",
                    "金色のふちどりと、つるのアサガオの絵がきれいでしょう?",
                    "遠い国の古い修道院で、花を生けていた壺だと聞きました",
                    "お城で花を生けていた壺で、つぼみは一つも描いてないんですよ",
                };
                break;
        }

        if (correctTexts == null || incorrectTexts == null) return;

        // isCustomerDescriptionをアタッチ
        if (isDescriptionWrong)
        {
            int index = Random.Range(0, incorrectTexts.Length);
            itemCustomerDescription = incorrectTexts[index];
        }
        else
        {
            int index = Random.Range(0, correctTexts.Length);
            itemCustomerDescription = correctTexts[index];
        }
    }

    /// <summary>
    /// お宝の状態をランダムに決める処理
    /// </summary>
    public void RandomizeState()
    {
        //すでにランダムに決めた場合は、再度決めないようにする
        if (isRandomized) return;


        isDamaged=Random.value < damagedChance;
        isAppraisalMission1 = Random.value < mission1Chance;
        isAppraisalMission2 = Random.value < mission2Chance;
        isDescriptionWrong = Random.value < wrongDescriptionChance;

        isRandomized = true;

        ApplyVisuals();
        ApplyRandomCustomerDescription();
    }

    /// <summary>
    /// フラグに合わせて、傷やカビの見た目を出したり隠したりする
    /// </summary>
    private void ApplyVisuals()
    {
        if (damagedVisual != null) damagedVisual.SetActive(isDamaged);
        if (mission1Visual != null) mission1Visual.SetActive(isAppraisalMission1);
        if (mission2Visual != null) mission2Visual.SetActive(isAppraisalMission2);
    }
}
