using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ゲームオーバーの演出（急に暗転→ドン→数日後――→新聞→GAME OVER）
/// </summary>
public class GameOverSequence : MonoBehaviour
{
    private static GameOverSequence instance;
    private static float nextFindTime;
    public static GameOverSequence Instance
    {
        get
        {
            //チェックを外していても使えるように、シーンから探す（探しすぎないように1秒に1回まで）
            if (instance == null && Time.unscaledTime >= nextFindTime)
            {
                nextFindTime = Time.unscaledTime + 1f;
                instance = FindObjectOfType<GameOverSequence>(true);
            }
            return instance;
        }
        private set { instance = value; }
    }

    [Header("ゲームオーバーの画面全体（黒い背景。最初は隠しておく）"), SerializeField]
    private GameObject gameOverRoot;

    [Header("「数日後――」の文字"), SerializeField]
    private Text daysLaterText;

    [Header("新聞（新聞の絵と、その上の文字をまとめた物）"), SerializeField]
    private RectTransform newspaper;

    [Header("新聞の名前・見出し・小見出しの文字"), SerializeField]
    private Text mastheadText;
    [SerializeField]
    private Text headlineText;
    [SerializeField]
    private Text subheadText;

    [Header("「GAME OVER」のロゴ（ハンコ）"), SerializeField]
    private RectTransform gameOverLogo;

    [Header("新聞に出す文字（{借金} は残った借金の額になる）"), SerializeField]
    private string mastheadName = "日刊 古物新報";
    [SerializeField]
    private string headline = "古物店店主、行方知れず";
    [SerializeField]
    private string subhead = "― 多額の借金 {借金}$ を残したまま ―";
    [SerializeField]
    private string daysLater = "数日後――";

    [Header("「クリックでタイトルへ」などの案内（なくてもよい。最初は隠す）"), SerializeField]
    private GameObject clickHint;

    [Header("クリックしたら戻るタイトルのシーン名"), SerializeField]
    private string titleSceneName = "TitleScene";

    [Header("暗転したときの重い音"), SerializeField]
    private AudioClip impactSE;

    [Header("新聞が出るときの紙の音"), SerializeField]
    private AudioClip paperSE;

    [Header("ハンコを押す音"), SerializeField]
    private AudioClip stampSE;

    [Header("暗転してから「数日後――」までの時間（秒）"), SerializeField]
    private float blackHoldTime = 1.5f;

    [Header("「数日後――」を出しておく時間（秒）"), SerializeField]
    private float daysLaterTime = 2f;

    [Header("新聞が出てくる時間（秒）"), SerializeField]
    private float newspaperFadeTime = 1.2f;

    [Header("新聞が出てから「GAME OVER」までの時間（秒）"), SerializeField]
    private float gameOverDelay = 2f;

    //演出の途中か
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// 開始前：画面を隠しておく
    /// </summary>
    private void Awake()
    {
        Instance = this;

        //演出を始めるときに表示されて動き出した場合は、隠さない
        if (!IsPlaying && gameOverRoot != null) gameOverRoot.SetActive(false);
        if (clickHint != null) clickHint.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// ゲームオーバーの演出を始める処理（残った借金の額を渡す）
    /// </summary>
    public void Play(int debt)
    {
        if (IsPlaying) return;
        IsPlaying = true;

        //このスクリプトが非表示の物に付いていると演出を始められないので、先に親まで表示にする
        if (gameOverRoot != null) gameOverRoot.SetActive(true);
        Transform t = transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }

        StartCoroutine(PlayRoutine(debt));
    }

    /// <summary>
    /// 演出の流れ
    /// </summary>
    private IEnumerator PlayRoutine(int debt)
    {
        //一瞬で真っ暗にする（一番手前に出す）
        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(true);
            gameOverRoot.transform.SetAsLastSibling();
        }
        SetGroupAlpha(daysLaterText, 0f);
        SetGroupAlpha(newspaper, 0f);
        SetGroupAlpha(gameOverLogo, 0f);

        //ほかの音を全部止めて、重い音を1回だけ鳴らす
        AudioListener.pause = true;
        PlayClip(impactSE);
        yield return new WaitForSeconds(blackHoldTime);

        //「数日後――」を出して、消す
        if (daysLaterText != null) daysLaterText.text = daysLater;
        yield return Fade(daysLaterText, 0f, 1f, 0.6f);
        yield return new WaitForSeconds(daysLaterTime);
        yield return Fade(daysLaterText, 1f, 0f, 0.6f);

        //新聞の文字を入れて、紙の音と一緒に、少し大きい所からふわっと出す
        if (mastheadText != null) mastheadText.text = mastheadName;
        if (headlineText != null) headlineText.text = headline;
        if (subheadText != null) subheadText.text = subhead.Replace("{借金}", debt.ToString("N0"));
        PlayClip(paperSE);
        yield return NewspaperAppear();

        //少し読ませてから「GAME OVER」のハンコを押す
        yield return new WaitForSeconds(gameOverDelay);
        yield return StampIn();

        //カーソルを出して、案内を出す
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (clickHint != null) clickHint.SetActive(true);

        //ハンコのあと少し待ってから、画面がクリックされるまで待つ（押した勢いで進まないように）
        yield return new WaitForSeconds(0.5f);
        while (!Input.GetMouseButtonDown(0)) yield return null;

        ReturnToTitle();
    }

    /// <summary>
    /// タイトルに戻る処理（止めた音を戻し、お金や日にちのマネージャーを消してからシーンを移る）
    /// </summary>
    private void ReturnToTitle()
    {
        //止めていた音と、マップの音量を元に戻す
        AudioListener.pause = false;
        BlackMarketSound.Unmute();

        //シーンをまたいで残るマネージャーを消す（次に遊ぶときは、MainSceneで新しく作られる）
        DestroyRoot(DayManager.Instance);
        DestroyRoot(MonayManager.Instance);
        DestroyRoot(AuctionManager.Instance);

        SceneManager.LoadScene(titleSceneName);
    }

    /// <summary>
    /// マネージャーが付いている物を、一番上の親ごと消す処理
    /// </summary>
    private void DestroyRoot(Component manager)
    {
        if (manager == null) return;
        Destroy(manager.transform.root.gameObject);
    }

    /// <summary>
    /// 新聞を、少し大きい所から元の大きさへ縮めながら、だんだん見せる処理
    /// </summary>
    private IEnumerator NewspaperAppear()
    {
        if (newspaper == null) yield break;

        Vector3 baseScale = newspaper.localScale;
        float t = 0f;
        while (t < newspaperFadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / newspaperFadeTime);
            float ease = 1f - (1f - k) * (1f - k);
            newspaper.localScale = baseScale * Mathf.Lerp(1.08f, 1f, ease);
            SetGroupAlpha(newspaper, k);
            yield return null;
        }
        newspaper.localScale = baseScale;
        SetGroupAlpha(newspaper, 1f);
    }

    /// <summary>
    /// ハンコを押す動き（大きい所から一瞬で縮んで、押した瞬間に音を鳴らす）
    /// </summary>
    private IEnumerator StampIn()
    {
        if (gameOverLogo == null) yield break;

        Vector3 baseScale = gameOverLogo.localScale;
        float time = 0.18f;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            gameOverLogo.localScale = baseScale * Mathf.Lerp(1.6f, 1f, k * k);
            SetGroupAlpha(gameOverLogo, Mathf.Clamp01(k * 2f));
            yield return null;
        }
        gameOverLogo.localScale = baseScale;
        SetGroupAlpha(gameOverLogo, 1f);
        PlayClip(stampSE);
    }

    /// <summary>
    /// 決めた時間で、だんだん見せる（消す）処理
    /// </summary>
    private IEnumerator Fade(Component target, float from, float to, float time)
    {
        if (target == null) yield break;

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            SetGroupAlpha(target, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        SetGroupAlpha(target, to);
    }

    /// <summary>
    /// 物の透明度を変える処理（CanvasGroupがなければ付ける。子の文字や絵もまとめて変わる）
    /// </summary>
    private void SetGroupAlpha(Component target, float alpha)
    {
        if (target == null) return;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
        group.alpha = alpha;
    }

    /// <summary>
    /// 音を鳴らす処理（ほかの音を止めていても鳴るようにする）
    /// </summary>
    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;

        GameObject seObject = new GameObject("SE_" + clip.name);
        AudioSource source = seObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.ignoreListenerVolume = true;
        source.PlayOneShot(clip);
        Destroy(seObject, clip.length + 0.5f);
    }
}
