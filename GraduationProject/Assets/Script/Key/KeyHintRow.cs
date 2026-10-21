using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 右下の説明操作の１行※AI担当
/// </summary>
public class KeyHintRow : MonoBehaviour
{
    [SerializeField] private Text labelText;
    [SerializeField] private Image keyImage;

    /// <summary>
    /// 文字とアイコンを書き込む
    /// </summary>
    public void Setup(string label, Sprite icon)
    {
        FindParts();

        if (labelText != null) labelText.text = label;
        if (keyImage != null)
        {
            keyImage.sprite = icon;
            // アイコンを元の大きさにする（キーごとに幅が違うため）
            keyImage.SetNativeSize();
        }
    }

    /// <summary>
    /// Inspectorの欄が空のときに、子から文字とアイコンを探す
    /// </summary>
    private void FindParts()
    {
        if (labelText == null) labelText = GetComponentInChildren<Text>(true);

        if (keyImage == null)
        {
            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                // 自分自身ではなく子のImageを使う
                if (image.gameObject != gameObject)
                {
                    keyImage = image;
                    break;
                }
            }
        }

        if (labelText == null || keyImage == null)
        {
            Debug.LogWarning("[KeyHintRow] 文字かアイコンが見つかりません。プレハブのKeyHintRowの欄を確認してください。", this);
        }
    }
}
