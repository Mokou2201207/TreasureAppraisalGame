using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// どのシーンでも、残り日数の表示を DayManager から読んで更新する（返済日は赤く「返済日」と出す）
/// </summary>
public class DayText : MonoBehaviour
{
    [SerializeField] private Text text;

    [Header("返済日の文字の色"), SerializeField]
    private Color repaymentColor = new Color(0.85f, 0.12f, 0.1f);

    //ふだんの文字の色（最初の色を覚えておく）
    private Color normalColor;

    private void Start()
    {
        if (text != null) normalColor = text.color;
    }

    private void Update()
    {
        if (text == null || DayManager.Instance == null) return;

        //返済日は赤い文字で「返済日」と出す
        if (DayManager.Instance.IsRepaymentDay)
        {
            text.text = "返済日";
            text.color = repaymentColor;
        }
        else
        {
            text.text = $"{DayManager.Instance.dayCount}日";
            text.color = normalColor;
        }
    }
}
