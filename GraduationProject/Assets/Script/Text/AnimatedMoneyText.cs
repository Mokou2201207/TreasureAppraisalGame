using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// お金の表示を、増減するときに数字が動いて色が変わるようにする※AI担当
/// </summary>
public class AnimatedMoneyText : MonoBehaviour
{
    [SerializeField] private Text text;

    [Header("数字が変わりきるまでの秒数")]
    [SerializeField] private float countDuration = 0.6f;

    [Header("減るとき・増えるときの文字の色")]
    [SerializeField] private Color decreaseColor = new Color(0.94f, 0.35f, 0.31f);
    [SerializeField] private Color increaseColor = new Color(1f, 0.85f, 0.35f);

    // ふだんの文字の色（最初にTextに付いている色）
    private Color normalColor;
    // 今画面に出している金額
    private float shownMoney;
    // 数字が動き始めたときの金額
    private float startMoney;
    private int lastTarget;
    private float elapsed;
    private bool isCounting = false;
    private bool hasValue = false;

    private void Awake()
    {
        if (text == null) text = GetComponent<Text>();
        if (text != null) normalColor = text.color;
    }

    private void Update()
    {
        if (text == null || MonayManager.Instance == null) return;

        int target = MonayManager.Instance.CurrentMonay;

        // 最初は動かさずにそのまま出す
        if (!hasValue)
        {
            shownMoney = target;
            lastTarget = target;
            hasValue = true;
        }

        // お金が変わったら、今の表示から数字を動かし始める
        if (target != lastTarget)
        {
            startMoney = shownMoney;
            lastTarget = target;
            elapsed = 0f;
            isCounting = true;
            text.color = target < startMoney ? decreaseColor : increaseColor;
        }

        if (isCounting)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = countDuration > 0f ? Mathf.Clamp01(elapsed / countDuration) : 1f;
            shownMoney = Mathf.Lerp(startMoney, target, 1f - (1f - p) * (1f - p));

            if (p >= 1f)
            {
                shownMoney = target;
                isCounting = false;
                text.color = normalColor;
            }
        }

        // MonayManagerが作った文字の、先頭の金額だけを動いている数字に置きかえる
        string fullText = MonayManager.Instance.GetMoneyText();
        string actualHead = $"{target}$";
        int shown = Mathf.RoundToInt(shownMoney);
        text.text = fullText.StartsWith(actualHead)
            ? $"{shown}$" + fullText.Substring(actualHead.Length)
            : fullText;
    }
}
