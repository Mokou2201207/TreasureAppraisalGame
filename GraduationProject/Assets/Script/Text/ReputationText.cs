using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// お店の評価を「30/100」の形で表示する処理
/// </summary>
public class ReputationText : MonoBehaviour
{
    [Header("評価の数字を出す文字"), SerializeField]
    private Text text;

    /// <summary>
    /// 毎フレーム、ReputationManager の評価を読んで表示する
    /// </summary>
    private void Update()
    {
        if (text == null || ReputationManager.Instance == null) return;
        text.text = $"{ReputationManager.Instance.Reputation}/{ReputationManager.Instance.MaxReputation}";
    }
}
