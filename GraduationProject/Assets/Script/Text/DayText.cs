using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// どのシーンでも、残り日数の表示を DayManager から読んで更新する
/// </summary>
public class DayText : MonoBehaviour
{
    [SerializeField] private Text text;

    private void Update()
    {
        if (text == null || DayManager.Instance == null) return;
        text.text = $"{DayManager.Instance.dayCount}日";
    }
}
