using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 返済日（4日目の朝）に怖い人が来て、借金を返せたら続行、返せなければゲームオーバーにする処理
/// </summary>
public class RepaymentUI : MonoBehaviour
{
    private static RepaymentUI instance;
    private static float nextFindTime;
    public static RepaymentUI Instance
    {
        get
        {
            //チェックを外していても使えるように、シーンから探す（探しすぎないように1秒に1回まで）
            if (instance == null && Time.unscaledTime >= nextFindTime)
            {
                nextFindTime = Time.unscaledTime + 1f;
                instance = FindObjectOfType<RepaymentUI>(true);
            }
            return instance;
        }
        private set { instance = value; }
    }

    [Header("返済の画面（最初は隠しておく）"), SerializeField]
    private GameObject repaymentPanel;

    [Header("セリフの文字"), SerializeField]
    private Text messageText;

    [Header("次へ進むボタン"), SerializeField]
    private Button nextButton;

    [Header("ゲームオーバーの画面（最初は隠しておく）"), SerializeField]
    private GameObject gameOverPanel;

    [Header("もう一度ボタン"), SerializeField]
    private Button retryButton;

    [Header("怖い人が来たときの音"), SerializeField]
    private AudioClip visitSE;

    [Header("返済できたときの音"), SerializeField]
    private AudioClip paidSE;

    [Header("ゲームオーバーの音"), SerializeField]
    private AudioClip gameOverSE;

    //次へ進むボタンを押したか
    private bool clicked;

    /// <summary>
    /// 開始前：画面を隠して、ボタンの処理をつなぐ
    /// </summary>
    private void Awake()
    {
        Instance = this;

        if (repaymentPanel != null) repaymentPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 返済の日の流れを順番に出す（寝て起きた後に呼ぶ）
    /// </summary>
    public IEnumerator Run()
    {
        if (MonayManager.Instance == null) yield break;

        int target = MonayManager.Instance.CurrentTargetMoney;
        int have = MonayManager.Instance.CurrentMonay;

        //返済の間はプレイヤーを止めて、カーソルを出す
        SetPlayerActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //怖い人が来る
        if (repaymentPanel != null) repaymentPanel.SetActive(true);
        PlaySE(visitSE);
        yield return ShowLine($"怖い人が来た…\n返済の期限だ。{target:N0}$ 返してもらおうか。");

        //お金が足りなければ、ゲームオーバー
        if (have < target)
        {
            yield return ShowLine($"…持っていないようだな。（今は {have:N0}$）");
            ShowGameOver();

            //やり直すボタンを押すまで止めておく
            while (true) yield return null;
        }

        //お金を払って、次の返済の目標へ進める
        MonayManager.Instance.TryRepay();
        PlaySE(paidSE);
        yield return ShowLine($"確かに受け取った。{target:N0}$ 返済完了だ。");

        //返済の画面を閉じて、プレイヤーを動かせるように戻す
        if (repaymentPanel != null) repaymentPanel.SetActive(false);
        SetPlayerActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// セリフを出して、次へ進むボタンが押されるまで待つ処理
    /// </summary>
    private IEnumerator ShowLine(string line)
    {
        if (messageText != null) messageText.text = line;
        clicked = false;
        if (nextButton != null) nextButton.gameObject.SetActive(true);

        while (!clicked) yield return null;

        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 次へ進むボタンを押したときの処理
    /// </summary>
    private void OnNext()
    {
        clicked = true;
    }

    /// <summary>
    /// ゲームオーバーの画面を出す処理
    /// </summary>
    private void ShowGameOver()
    {
        //返済の画面（セリフ）は残したまま、その上にゲームオーバーを出す
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        PlaySE(gameOverSE);
    }

    /// <summary>
    /// もう一度ボタンを押したときの処理（お金・日にち・ミッションを最初に戻してシーンを読み直す）
    /// </summary>
    private void Retry()
    {
        if (DayManager.Instance != null) DayManager.Instance.ResetAll();
        if (MonayManager.Instance != null) MonayManager.Instance.ResetMonay();
        if (AuctionManager.Instance != null)
        {
            AuctionManager.Instance.onAuction.Clear();
            AuctionManager.Instance.TakeResults();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
    /// 音だけを鳴らす処理（画面を閉じても音が止まらないように、別のオブジェクトで鳴らす）
    /// </summary>
    private void PlaySE(AudioClip clip)
    {
        //音が入っていなければ鳴らさない
        if (clip == null) return;

        GameObject seObject = new GameObject("SE_" + clip.name);
        AudioSource source = seObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.PlayOneShot(clip);
        Destroy(seObject, clip.length + 0.5f);
    }
}
