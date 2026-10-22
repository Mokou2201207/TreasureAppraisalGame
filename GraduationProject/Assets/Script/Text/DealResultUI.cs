using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 買い取りの結果（成功・失敗）のはんこを出して、上へスライドさせて消す※AI担当
/// </summary>
public class DealResultUI : MonoBehaviour
{
    public static DealResultUI Instance { get; private set; }

    [Header("表示する部品")]
    [Tooltip("はんこと金額をまとめた親（透明度をまとめて変える）")]
    [SerializeField] private CanvasGroup group;
    [Tooltip("はんこの画像")]
    [SerializeField] private Image stampImage;
    [Tooltip("減った金額を出すテキスト")]
    [SerializeField] private Text amountText;

    [Header("はんこの画像")]
    [SerializeField] private Sprite successSprite;
    [SerializeField] private Sprite failureSprite;

    [Header("結果のSE")]
    [SerializeField] private AudioClip successSE;
    [SerializeField] private AudioClip failureSE;
    [SerializeField, Range(0f, 1f)] private float seVolume = 1f;
    [Tooltip("SEを鳴らすAudioSource（空なら自動で用意する）")]
    [SerializeField] private AudioSource seSource;

    [Header("金額の文字の色")]
    [SerializeField] private Color successColor = new Color(0.94f, 0.39f, 0.35f);
    [SerializeField] private Color failureColor = new Color(0.69f, 0.73f, 0.80f);

    [Header("動きの設定（秒）")]
    [SerializeField] private float popDuration = 0.18f;
    [Tooltip("出始めの大きさの倍率。1より小さいとInspectorの大きさが一番大きくなる")]
    [SerializeField] private float popScale = 0.6f;
    [SerializeField] private float holdTime = 1.2f;
    [SerializeField] private float slideDuration = 0.7f;
    [SerializeField] private float slideDistance = 70f;

    private RectTransform groupRect;
    private Vector2 basePosition;
    // Inspectorで決めたはんこの大きさ（この大きさを基準に動かす）
    private Vector3 stampBaseScale = Vector3.one;
    private Coroutine playCoroutine;

    private void Awake()
    {
        Instance = this;

        if (stampImage != null) stampBaseScale = stampImage.transform.localScale;

        // SE用のAudioSourceがなければ用意する（画面のUIなので距離で音量が変わらない2Dにする）
        if (seSource == null) seSource = GetComponent<AudioSource>();
        if (seSource == null) seSource = gameObject.AddComponent<AudioSource>();
        seSource.playOnAwake = false;
        seSource.spatialBlend = 0f;

        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            groupRect = group.GetComponent<RectTransform>();
            basePosition = groupRect.anchoredPosition;
            group.alpha = 0f;
            // 表示中も後ろのボタンなどを押せるようにする
            group.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 結果のはんこを出す
    /// </summary>
    /// <param name="success">買い取りが成立したか</param>
    /// <param name="spent">支払った金額（鑑定料込み）</param>
    public void Play(bool success, int spent)
    {
        if (group == null || stampImage == null) return;

        stampImage.sprite = success ? successSprite : failureSprite;
        // 大きさはInspectorの幅と高さのままにして、絵の縦横比だけ保つ
        stampImage.preserveAspect = true;

        if (amountText != null)
        {
            amountText.text = success ? $"-{spent:N0}$" : $"鑑定料 -{spent:N0}$";
            amountText.color = success ? successColor : failureColor;
        }

        // 結果に合わせたSEを鳴らす
        AudioClip se = success ? successSE : failureSE;
        if (se != null && seSource != null) seSource.PlayOneShot(se, seVolume);

        if (playCoroutine != null) StopCoroutine(playCoroutine);
        playCoroutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        Transform stamp = stampImage.transform;
        groupRect.anchoredPosition = basePosition;

        // ドンッと押す（大きい状態から縮んで止まる）
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / popDuration);
            stamp.localScale = stampBaseScale * Mathf.Lerp(popScale, 1f, EaseOut(p));
            group.alpha = p;
            yield return null;
        }
        stamp.localScale = stampBaseScale;
        group.alpha = 1f;

        // しばらく見せる
        float wait = 0f;
        while (wait < holdTime)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        // 上へスライドしながら消える
        t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / slideDuration);
            float smooth = p * p * (3f - 2f * p);
            groupRect.anchoredPosition = basePosition + Vector2.up * slideDistance * smooth;
            group.alpha = 1f - p;
            yield return null;
        }

        group.alpha = 0f;
        groupRect.anchoredPosition = basePosition;
        playCoroutine = null;
    }

    // 最初は速く、最後はゆっくり止まる動き（行き過ぎない）
    private float EaseOut(float p)
    {
        return 1f - Mathf.Pow(1f - p, 3f);
    }
}
