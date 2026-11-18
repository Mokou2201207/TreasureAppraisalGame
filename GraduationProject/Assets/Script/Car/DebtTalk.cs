using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 借金取りの事務所での会話（登場→セリフ→選択肢→返済・借金・ゲームオーバー→自動で戻る）
/// </summary>
public class DebtTalk : MonoBehaviour
{
    private static DebtTalk instance;
    private static float nextFindTime;
    public static DebtTalk Instance
    {
        get
        {
            //チェックを外していても使えるように、シーンから探す（探しすぎないように1秒に1回まで）
            if (instance == null && Time.unscaledTime >= nextFindTime)
            {
                nextFindTime = Time.unscaledTime + 1f;
                instance = FindObjectOfType<DebtTalk>(true);
            }
            return instance;
        }
        private set { instance = value; }
    }

    [Header("借金取りの立ち絵"), SerializeField]
    private Image characterImage;

    [Header("会話の文字枠のまとまり（文字枠・名前欄・セリフ・▼。最初は隠しておく）"), SerializeField]
    private GameObject messageWindow;

    [Header("名前欄（プレートごと）"), SerializeField]
    private GameObject namePlate;

    [Header("名前欄の文字"), SerializeField]
    private Text nameText;

    [Header("セリフの文字"), SerializeField]
    private Text messageText;

    [Header("送りカーソル（▼）"), SerializeField]
    private GameObject nextArrow;

    [Header("選択肢のまとまり（最初は隠しておく）"), SerializeField]
    private GameObject choicePanel;

    [Header("選択肢A（返す・返済する）のボタンと文字"), SerializeField]
    private Button choiceAButton;
    [SerializeField]
    private Text choiceAText;

    [Header("選択肢B（返せない・お金を借りる）のボタンと文字"), SerializeField]
    private Button choiceBButton;
    [SerializeField]
    private Text choiceBText;

    [Header("選択肢C（様子を見に来ただけ）のボタンと文字。返済日は隠れる"), SerializeField]
    private Button choiceCButton;
    [SerializeField]
    private Text choiceCText;

    [Header("ゲームオーバーの画面（最初は隠しておく）"), SerializeField]
    private GameObject gameOverPanel;

    [Header("車（会話が終わったら自動で戻るため。空なら探す）"), SerializeField]
    private CarToBlackMarket car;

    [Header("キャラクターの名前"), SerializeField]
    private string characterName = "影の怪しい男";

    [Header("足音"), SerializeField]
    private AudioClip footstepSE;

    [Header("お金を払う・受け取る音"), SerializeField]
    private AudioClip moneySE;

    [Header("ゲームオーバーの音"), SerializeField]
    private AudioClip gameOverSE;

    [Header("文字を出すときの声"), SerializeField]
    private AudioClip voiceSE;

    [Header("何文字ごとに声を鳴らすか"), SerializeField]
    private int voiceEvery = 2;

    [Header("声の高さのゆらぎ（低い〜高い）"), SerializeField]
    private Vector2 voicePitch = new Vector2(0.9f, 1.05f);

    [Header("立ち絵が見えてくる時間（秒）"), SerializeField]
    private float appearTime = 2.5f;

    [Header("1文字を出す間隔（秒）"), SerializeField]
    private float charInterval = 0.04f;

    [Header("1回ごとに貸すお金（「まだいる」と言うたびに、次の額を貸す）"), SerializeField]
    private int[] borrowAmounts = { 10000, 20000, 30000 };

    [Header("借りたお金の何倍を返すか"), SerializeField]
    private int borrowRate = 2;

    [Header("登場のときの文字（自動で進む）"), SerializeField, TextArea]
    private string[] appearLines = { "ざ……", "ざざ……" };

    [Header("返済日のセリフ（{借金}{所持金}{貸す}{合計}{上乗せ} は数字になる）"), SerializeField, TextArea]
    private string[] repaymentDayLines = { "……返済の期限だ。", "今回は {借金}$。返してもらおうか。", "で、返せるのか？" };

    [Header("返済日でない日のセリフ"), SerializeField, TextArea]
    private string[] normalDayLines = { "……何の用だ。", "返済か？ それとも、金を借りに来たか？" };

    [Header("返済できたとき"), SerializeField, TextArea]
    private string[] repaidLines = { "……確かに {借金}$、受け取った。", "次の期限も守れよ。" };

    [Header("返済日にお金が足りなかったとき（ゲームオーバー）"), SerializeField, TextArea]
    private string[] shortOnRepaymentDayLines = { "……足りないじゃないか。", "約束は約束だ。覚悟はできているな？" };

    [Header("返済日に「返せない」を選んだとき（ゲームオーバー）"), SerializeField, TextArea]
    private string[] refuseLines = { "……そうか。", "なら、払えるものを払ってもらうしかないな。" };

    [Header("返済日でない日に、お金が足りなかったとき"), SerializeField, TextArea]
    private string[] shortOnNormalDayLines = { "金が足りないな。", "期限までに用意して、出直してこい。" };

    [Header("「様子を見に来ただけ」を選んだとき"), SerializeField, TextArea]
    private string[] justLookingLines = { "……冷やかしか。", "用がないなら、さっさと帰れ。" };

    [Header("最初に借りたとき（{貸す}{追加}{合計}{上乗せ} は数字になる）"), SerializeField, TextArea]
    private string[] firstLendLines = { "いいだろう。{貸す}$ 貸してやる。", "その代わり、返済に {追加}$ 追加だ。" };

    [Header("借りたあとの確認（このあと「もう大丈夫」「まだいる」を出す）"), SerializeField, TextArea]
    private string[] askEnoughLines = { "……もう大丈夫か？" };

    [Header("「まだいる」を選んだときの反応（1回目、2回目…の順）"), SerializeField, TextArea]
    private string[] moreReactions = { "……まだいるのか。", "……欲張りだな。これが最後だぞ。" };

    [Header("追加で貸すとき"), SerializeField, TextArea]
    private string[] extraLendLines = { "ほらよ、{貸す}$ だ。", "返済には、さらに {追加}$ 追加だ。" };

    [Header("「もう大丈夫」を選んだとき"), SerializeField, TextArea]
    private string[] enoughLines = { "そうか。", "期限までに、合計 {上乗せ}$ 上乗せで返してもらう。忘れるなよ。" };

    [Header("もう上限まで借りているとき（返済するまで貸さない）"), SerializeField, TextArea]
    private string[] noMoreLendLines = { "……前の分を、まだ返してもらっていない。", "これ以上は貸せないな。返済してから出直してこい。" };

    [Header("上限まで貸したとき"), SerializeField, TextArea]
    private string[] borrowLimitLines = { "これ以上は貸せないな。", "期限までに、合計 {上乗せ}$ 上乗せで返してもらう。忘れるなよ。" };

    //会話の途中か
    private bool isTalking = false;

    //選んだ選択肢（-1:まだ 0:A 1:B）
    private int choice = -1;

    //セリフに出す借金の額
    private int shownDebt = 0;

    //今回貸した額
    private int lastLend = 0;

    //声を鳴らすためのAudioSource（音の高さを変えるので専用にする）
    private AudioSource voiceSource;

    //この行で、声を出す文字を何文字出したか
    private int voicedCount = 0;

    //最後に声を鳴らした時間（声が重ならないようにするため）
    private float lastVoiceTime = -1f;

    /// <summary>
    /// 開始前：画面を隠して、ボタンの処理をつなぐ
    /// </summary>
    private void Awake()
    {
        Instance = this;

        Prepare();
        if (choicePanel != null) choicePanel.SetActive(false);

        //声用のAudioSource（マップの音を止めていても鳴るようにする）
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.ignoreListenerVolume = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (nextArrow != null) nextArrow.SetActive(false);

        if (choiceAButton != null) choiceAButton.onClick.AddListener(() => choice = 0);
        if (choiceBButton != null) choiceBButton.onClick.AddListener(() => choice = 1);
        if (choiceCButton != null) choiceCButton.onClick.AddListener(() => choice = 2);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 会話の前の状態にする処理（文字枠を隠して、立ち絵を透明にする。闇市に着いて真っ暗の間に呼ぶ）
    /// </summary>
    public void Prepare()
    {
        if (messageWindow != null) messageWindow.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        if (nextArrow != null) nextArrow.SetActive(false);
        SetCharacterAlpha(0f);
    }

    /// <summary>
    /// 会話を始める処理（闇市に着いて、画面が明るくなったあとに呼ぶ）
    /// </summary>
    public void Begin()
    {
        if (isTalking) return;
        StartCoroutine(TalkRoutine());
    }

    /// <summary>
    /// 会話の流れ全体
    /// </summary>
    private IEnumerator TalkRoutine()
    {
        isTalking = true;
        bool isRepaymentDay = DayManager.Instance != null && DayManager.Instance.IsRepaymentDay;

        //最初は立ち絵を透明にして、名前欄と選択肢を隠しておく
        SetCharacterAlpha(0f);
        if (namePlate != null) namePlate.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        if (messageText != null) messageText.text = "";

        //会話の文字枠を出す
        if (messageWindow != null) messageWindow.SetActive(true);

        //足音と一緒に、立ち絵をだんだん見せる（「ざ……ざざ……」は自動で進む）
        BlackMarketSound.PlayClip(footstepSE);
        Coroutine appear = StartCoroutine(FadeCharacter(appearTime));
        yield return ShowLinesAuto(appearLines, 0.6f);
        yield return appear;

        //名前を出して、会話を始める
        if (namePlate != null) namePlate.SetActive(true);
        if (nameText != null) nameText.text = characterName;
        shownDebt = MonayManager.Instance != null ? MonayManager.Instance.CurrentTargetMoney : 0;

        if (isRepaymentDay)
        {
            //返済日：返すか、返せないか
            yield return ShowLines(repaymentDayLines);
            yield return Choose("返す", "返せない");

            if (choice == 0 && CanPay())
            {
                Pay();
                yield return ShowLines(repaidLines);
                Finish();
            }
            else if (choice == 0)
            {
                yield return ShowLines(shortOnRepaymentDayLines);
                GameOver();
            }
            else
            {
                yield return ShowLines(refuseLines);
                GameOver();
            }
        }
        else
        {
            //返済日でない日：返済するか、お金を借りるか
            yield return ShowLines(normalDayLines);
            yield return Choose("返済する", "お金を借りる", "様子を見に来ただけ");

            if (choice == 0 && CanPay())
            {
                Pay();
                yield return ShowLines(repaidLines);
            }
            else if (choice == 0)
            {
                yield return ShowLines(shortOnNormalDayLines);
            }
            else if (choice == 1)
            {
                yield return BorrowRoutine();
            }
            else
            {
                //様子を見に来ただけなら、すぐ帰る
                yield return ShowLines(justLookingLines);
            }
            Finish();
        }
    }

    /// <summary>
    /// お金を借りる流れ（貸す→もう大丈夫か？→まだいるなら、上限まで増やして貸す）
    /// </summary>
    private IEnumerator BorrowRoutine()
    {
        //前に借りた分の続きの段階から始める（1万だけ借りて帰ったら、次は2万から）
        int step = MonayManager.Instance != null ? MonayManager.Instance.BorrowStep : 0;

        //もう上限まで借りていたら、返済するまで貸してくれない
        if (step >= borrowAmounts.Length)
        {
            yield return ShowLines(noMoreLendLines);
            yield break;
        }

        bool isFirst = true;
        while (step < borrowAmounts.Length)
        {
            //今の段階の額を貸してもらって、段階を1つ進める
            lastLend = borrowAmounts[step];
            if (MonayManager.Instance != null)
            {
                MonayManager.Instance.Borrow(lastLend, borrowRate);
                MonayManager.Instance.AdvanceBorrowStep();
            }
            step++;
            BlackMarketSound.PlayClip(moneySE);
            yield return ShowLines(isFirst ? firstLendLines : extraLendLines);
            isFirst = false;

            //上限まで貸したら終わり
            if (step >= borrowAmounts.Length)
            {
                yield return ShowLines(borrowLimitLines);
                yield break;
            }

            //もう大丈夫か、まだいるかを聞く
            yield return ShowLines(askEnoughLines);
            yield return Choose("もう大丈夫", "まだいる");
            if (choice == 0)
            {
                yield return ShowLines(enoughLines);
                yield break;
            }

            //まだいるなら、段階に合わせた反応を言う
            if (moreReactions.Length > 0)
            {
                string reaction = moreReactions[Mathf.Clamp(step - 1, 0, moreReactions.Length - 1)];
                yield return ShowLines(new[] { reaction });
            }
        }
    }

    /// <summary>
    /// 返済できるお金があるかを調べる処理
    /// </summary>
    private bool CanPay()
    {
        return MonayManager.Instance != null && MonayManager.Instance.CurrentMonay >= MonayManager.Instance.CurrentTargetMoney;
    }

    /// <summary>
    /// 返済する処理（払った額をセリフに出せるように、先に覚えておく）
    /// </summary>
    private void Pay()
    {
        shownDebt = MonayManager.Instance.CurrentTargetMoney;
        MonayManager.Instance.TryRepay();
        BlackMarketSound.PlayClip(moneySE);
    }

    /// <summary>
    /// 会話を終えて、自動でマップに戻る処理
    /// </summary>
    private void Finish()
    {
        isTalking = false;
        if (car == null) car = FindObjectOfType<CarToBlackMarket>();
        if (car != null) car.ReturnFromBlackMarket();
    }

    /// <summary>
    /// ゲームオーバーの画面を出す処理
    /// </summary>
    private void GameOver()
    {
        //ゲームオーバーの演出があれば、それを使う（残った借金の額を新聞に出す）
        if (GameOverSequence.Instance != null)
        {
            GameOverSequence.Instance.Play(shownDebt);
            return;
        }

        //演出がないときは、今までのゲームオーバーの画面を出す
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        BlackMarketSound.PlayClip(gameOverSE);
    }

    /// <summary>
    /// 選択肢を出して、どれかが押されるまで待つ処理（3つ目は c を渡したときだけ出る）
    /// </summary>
    private IEnumerator Choose(string a, string b, string c = null)
    {
        if (choiceAText != null) choiceAText.text = a;
        if (choiceBText != null) choiceBText.text = b;

        //3つ目は、渡されたときだけ出す
        if (choiceCButton != null) choiceCButton.gameObject.SetActive(c != null);
        if (choiceCText != null && c != null) choiceCText.text = c;
        choice = -1;
        if (choicePanel != null) choicePanel.SetActive(true);

        while (choice < 0) yield return null;

        if (choicePanel != null) choicePanel.SetActive(false);
    }

    /// <summary>
    /// セリフを順番に出す処理（1行ずつクリックで進む）
    /// </summary>
    private IEnumerator ShowLines(string[] lines)
    {
        foreach (string line in lines)
        {
            yield return TypeLine(Fill(line));
            yield return WaitClick();
        }
    }

    /// <summary>
    /// セリフを順番に出す処理（クリックを待たずに、決めた時間で進む）
    /// </summary>
    private IEnumerator ShowLinesAuto(string[] lines, float wait)
    {
        foreach (string line in lines)
        {
            yield return TypeLine(Fill(line), false);
            yield return new WaitForSeconds(wait);
        }
    }

    /// <summary>
    /// 1文字ずつ文字を出す処理（途中でクリックしたら全部出す。withVoice が false なら声を鳴らさない）
    /// </summary>
    private IEnumerator TypeLine(string line, bool withVoice = true)
    {
        if (messageText == null) yield break;

        messageText.text = "";
        voicedCount = 0;

        //前のクリックを拾わないように、1フレーム待つ
        yield return null;

        float timer = 0f;
        int shown = 0;
        while (shown < line.Length)
        {
            if (Input.GetMouseButtonDown(0)) break;

            timer += Time.deltaTime;
            while (timer >= charInterval && shown < line.Length)
            {
                timer -= charInterval;
                shown++;

                //声を出す文字だけを数えて、決めた文字数ごとに声を鳴らす
                if (withVoice) OnCharShown(line[shown - 1]);
            }
            messageText.text = line.Substring(0, shown);
            yield return null;
        }
        messageText.text = line;
    }

    /// <summary>
    /// 1文字出たときの処理（記号や空白は数えず、声を出す文字の1文字目と、決めた文字数ごとに声を鳴らす）
    /// </summary>
    private void OnCharShown(char c)
    {
        if ("…、。！？!?・ 　「」（）$,".IndexOf(c) >= 0) return;

        voicedCount++;
        if ((voicedCount - 1) % Mathf.Max(1, voiceEvery) == 0) PlayVoice();
    }

    /// <summary>
    /// 声を1回鳴らす処理（高さを少しずつ変えて、しゃべっているように聞こえさせる）
    /// </summary>
    private void PlayVoice()
    {
        if (voiceSE == null || voiceSource == null) return;

        //前の声と近すぎるときは鳴らさない（文字が一気に出たときに重ならないように）
        if (Time.time - lastVoiceTime < voiceSE.length * 0.8f) return;
        lastVoiceTime = Time.time;

        voiceSource.pitch = Random.Range(voicePitch.x, voicePitch.y);
        voiceSource.PlayOneShot(voiceSE);
    }

    /// <summary>
    /// ▼を出して、クリックされるまで待つ処理
    /// </summary>
    private IEnumerator WaitClick()
    {
        if (nextArrow != null) nextArrow.SetActive(true);

        //文字を全部出したときのクリックを拾わないように、1フレーム待つ
        yield return null;
        while (!Input.GetMouseButtonDown(0)) yield return null;

        if (nextArrow != null) nextArrow.SetActive(false);
    }

    /// <summary>
    /// セリフの {借金} などを、今の数字に置きかえる処理
    /// </summary>
    private string Fill(string line)
    {
        int money = MonayManager.Instance != null ? MonayManager.Instance.CurrentMonay : 0;
        int debtAdd = MonayManager.Instance != null ? MonayManager.Instance.BorrowedDebt : 0;
        return line
            .Replace("{借金}", shownDebt.ToString("N0"))
            .Replace("{所持金}", money.ToString("N0"))
            .Replace("{貸す}", lastLend.ToString("N0"))
            .Replace("{合計}", (debtAdd / Mathf.Max(1, borrowRate)).ToString("N0"))
            .Replace("{追加}", (lastLend * borrowRate).ToString("N0"))
            .Replace("{上乗せ}", debtAdd.ToString("N0"));
    }

    /// <summary>
    /// 立ち絵をだんだん見せる処理
    /// </summary>
    private IEnumerator FadeCharacter(float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            SetCharacterAlpha(Mathf.Clamp01(t / time));
            yield return null;
        }
        SetCharacterAlpha(1f);
    }

    /// <summary>
    /// 立ち絵の透明度だけを変える処理
    /// </summary>
    private void SetCharacterAlpha(float alpha)
    {
        if (characterImage == null) return;
        Color c = characterImage.color;
        c.a = alpha;
        characterImage.color = c;
    }
}
