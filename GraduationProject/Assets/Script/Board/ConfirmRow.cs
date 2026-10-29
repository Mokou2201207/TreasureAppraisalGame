using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 出品申込書の1行分の処理※AI担当
/// </summary>
public class ConfirmRow : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text memoText;
    [SerializeField] private Text priceText;
    [Tooltip("この行のお宝を申込書から外すボタン")]
    [SerializeField] private Button removeButton;

    /// <summary>
    /// 1行分の情報を書き込む
    /// </summary>
    public void Setup(PurchasedItem item, Sprite icon, Action<PurchasedItem> onRemove)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
        }
        if (nameText != null) nameText.text = item.itemName.ToString();
        // 1行に収まるように、鑑定メモを横に並べる
        if (memoText != null) memoText.text = BoardCard.MakeMemo(item).Replace("\n", "　");
        if (priceText != null) priceText.text = $"{item.paidPrice:N0}$";

        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() => onRemove?.Invoke(item));
        }
    }
}
