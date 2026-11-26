using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// クリアシーンの最後：タイトルロゴをふわっと出して、画面全体を暗くしてタイトルへ戻る※AI担当
/// 時間はシーンが始まってからの秒数（タイムラインと同じ時間）で決める
/// </summary>
public class ClearEndingUI : MonoBehaviour
{
    [Header("表示するもの")]
    [SerializeField] private CanvasGroup titleLogo;
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private CanvasGroup thankYou;

    [Header("エピローグ（右側に下から上へ流れる文章）")]
    [SerializeField] private CanvasGroup epilogueGroup;
    [SerializeField] private RectTransform epilogueText;
    [SerializeField] private float epilogueStart = 2.5f;
    [SerializeField] private float epilogueStop = 40.5f;
    [SerializeField] private float epilogueFadeTime = 1f;
    [Tooltip("流し終わったとき、文章の一番下を帯の下から何割の高さで止めるか")]
    [SerializeField] private float epilogueStopHeight = 0.18f;

    [Header("ロゴを出すタイミング（秒）")]
    [SerializeField] private float logoFadeInStart = 52f;
    [SerializeField] private float logoFadeInTime = 3f;
    [SerializeField] private float logoStartScale = 1.08f;

    [Header("Thank you を出すタイミング（秒）")]
    [SerializeField] private float thankYouFadeInStart = 56f;
    [SerializeField] private float thankYouFadeInTime = 2f;

    [Header("最初の暗転（真っ黒から明るくなるまでの秒数）")]
    [SerializeField] private float openingFadeTime = 2f;

    [Header("画面を暗くするタイミング（秒）")]
    [SerializeField] private float screenFadeStart = 64f;
    [SerializeField] private float screenFadeTime = 3f;
    [SerializeField] private float waitAfterFade = 0.5f;

    [Header("戻るシーン")]
    [SerializeField] private string titleSceneName = "TitleScene";

    private float startTime;
    private bool isLoading;

    private void Awake()
    {
        //別のシーンから移ってきたとき、環境光（影の部分を照らす光）が真っ黒にならないよう、空から計算し直す
        DynamicGI.UpdateEnvironment();
    }

    private void Start()
    {
        DynamicGI.UpdateEnvironment();
        startTime = Time.time;
        if (titleLogo != null) titleLogo.alpha = 0f;
        if (screenFade != null) screenFade.alpha = 1f;
        if (thankYou != null) thankYou.alpha = 0f;
        if (epilogueGroup != null) epilogueGroup.alpha = 0f;
    }

    private void Update()
    {
        float t = Time.time - startTime;

        //ロゴ：だんだん見えて、少し大きい所から元の大きさへ
        if (titleLogo != null)
        {
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - logoFadeInStart) / logoFadeInTime));
            titleLogo.alpha = p;
            titleLogo.transform.localScale = Vector3.one * Mathf.Lerp(logoStartScale, 1f, p);
        }

        //Thank you：ロゴより少し遅れて、だんだん見える
        if (thankYou != null)
        {
            thankYou.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - thankYouFadeInStart) / thankYouFadeInTime));
        }

        UpdateEpilogue(t);

        //画面全体：だんだん暗く（音も一緒に小さく）
        float f = Mathf.Clamp01((t - screenFadeStart) / screenFadeTime);
        float opening = openingFadeTime > 0f ? 1f - Mathf.Clamp01(t / openingFadeTime) : 0f;
        if (screenFade != null) screenFade.alpha = Mathf.Max(f, opening);
        if (f > 0f) AudioListener.volume = 1f - f;

        if (!isLoading && t >= screenFadeStart + screenFadeTime + waitAfterFade)
        {
            isLoading = true;
            ReturnToTitle();
        }
    }

    /// <summary>
    /// エピローグ：下から上へ流して、最後の行が見えるところで止め、お辞儀までに消す
    /// </summary>
    private void UpdateEpilogue(float t)
    {
        if (epilogueGroup == null || epilogueText == null) return;

        float fadeIn = Mathf.Clamp01((t - epilogueStart) / epilogueFadeTime);
        float fadeOut = 1f - Mathf.Clamp01((t - epilogueStop) / epilogueFadeTime);
        epilogueGroup.alpha = Mathf.Min(fadeIn, fadeOut);

        RectTransform area = epilogueText.parent as RectTransform;
        float areaHeight = area != null ? area.rect.height : 1080f;
        float textHeight = epilogueText.rect.height;
        float p = Mathf.Clamp01((t - epilogueStart) / (epilogueStop - epilogueStart));
        //最初は文章の一番上が帯の下の端、最後は文章の一番下が帯の下から少し上
        float startY = 0f;
        float endY = textHeight + areaHeight * epilogueStopHeight;
        Vector2 pos = epilogueText.anchoredPosition;
        pos.y = Mathf.Lerp(startY, endY, p);
        epilogueText.anchoredPosition = pos;
    }

    /// <summary>
    /// タイトルに戻る処理（音を戻し、お金や日にちのマネージャーを消してからシーンを移る）
    /// </summary>
    private void ReturnToTitle()
    {
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        BlackMarketSound.Unmute();

        DestroyRoot(DayManager.Instance);
        DestroyRoot(MonayManager.Instance);
        DestroyRoot(AuctionManager.Instance);
        DestroyRoot(ReputationManager.Instance);
        DestroyRoot(InventoryManager.Instance);

        SceneManager.LoadScene(titleSceneName);
    }

    private void DestroyRoot(Component manager)
    {
        if (manager == null) return;
        Destroy(manager.transform.root.gameObject);
    }
}
