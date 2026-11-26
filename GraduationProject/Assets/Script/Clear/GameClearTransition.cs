using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 借金を全部返し終えたときの演出：画面を暗くして「一年後……」を出し、クリアシーンへ移る※AI担当
/// 画面（黒い幕と文字）はこのスクリプトが自分で作るので、シーンに置く準備はいらない
/// </summary>
public class GameClearTransition : MonoBehaviour
{
    private const string ClearSceneName = "ClearScene";

    private float darkTime = 2f;
    private float textFadeTime = 1.2f;
    private float textHoldTime = 2.2f;

    private CanvasGroup blackGroup;
    private CanvasGroup textGroup;

    /// <summary>
    /// 演出を始める（どこからでも呼べる）
    /// </summary>
    public static void Begin()
    {
        if (FindObjectOfType<GameClearTransition>() != null) return;
        GameObject go = new GameObject("GameClearTransition");
        go.AddComponent<GameClearTransition>();
    }

    private void Awake()
    {
        BuildScreen();
        StartCoroutine(Routine());
    }

    private void BuildScreen()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        //画面全体の黒（クリックも止める）
        GameObject black = new GameObject("Black", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        black.transform.SetParent(transform, false);
        RectTransform brt = (RectTransform)black.transform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        black.GetComponent<Image>().color = Color.black;
        blackGroup = black.GetComponent<CanvasGroup>();
        blackGroup.alpha = 0f;

        //「一年後……」
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasGroup), typeof(Text));
        textObj.transform.SetParent(transform, false);
        RectTransform trt = (RectTransform)textObj.transform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(1200f, 200f);
        Text text = textObj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "一年後……";
        text.fontSize = 72;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = new Color(0.95f, 0.92f, 0.85f);
        text.raycastTarget = false;
        textGroup = textObj.GetComponent<CanvasGroup>();
        textGroup.alpha = 0f;
    }

    private IEnumerator Routine()
    {
        float startVolume = AudioListener.volume;

        //画面と音をだんだん暗く・小さく
        for (float t = 0f; t < darkTime; t += Time.unscaledDeltaTime)
        {
            float p = t / darkTime;
            blackGroup.alpha = p;
            AudioListener.volume = Mathf.Lerp(startVolume, 0f, p);
            yield return null;
        }
        blackGroup.alpha = 1f;
        AudioListener.volume = 0f;

        //「一年後……」を出して、少し待って、消す
        yield return FadeText(0f, 1f);
        yield return new WaitForSecondsRealtime(textHoldTime);
        yield return FadeText(1f, 0f);
        yield return new WaitForSecondsRealtime(0.4f);

        //お店のマネージャー（お客さんの出現・時計など）はクリアシーンでは使わないので片付ける
        //（残しておくと、クリアシーンでもお客さんを出そうとしてエラーになる）
        DestroyRoot(DayManager.Instance);
        DestroyRoot(MonayManager.Instance);
        DestroyRoot(AuctionManager.Instance);
        DestroyRoot(ReputationManager.Instance);
        DestroyRoot(InventoryManager.Instance);

        //音を元に戻してから、クリアシーンへ（タイムラインはクリアシーンで自動で始まる）
        AudioListener.pause = false;
        BlackMarketSound.Unmute();
        AudioListener.volume = 1f;
        Time.timeScale = 1f;
        SceneManager.LoadScene(ClearSceneName);
    }

    private void DestroyRoot(Component manager)
    {
        if (manager == null) return;
        GameObject root = manager.transform.root.gameObject;
        if (root == gameObject) return;
        Destroy(root);
    }

    private IEnumerator FadeText(float from, float to)
    {
        for (float t = 0f; t < textFadeTime; t += Time.unscaledDeltaTime)
        {
            textGroup.alpha = Mathf.Lerp(from, to, t / textFadeTime);
            yield return null;
        }
        textGroup.alpha = to;
    }
}
