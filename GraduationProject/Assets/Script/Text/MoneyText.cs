using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// どのシーンでも、お金の表示を MonayManager から読んで更新する
/// </summary>
public class MoneyText : MonoBehaviour
{
    [SerializeField] private Text text;

    private void Update()
    {
        if (text == null || MonayManager.Instance == null) return;
        text.text = MonayManager.Instance.GetMoneyText();
    }
}
