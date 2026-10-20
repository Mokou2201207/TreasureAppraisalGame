using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 持ち物のカード1枚に情報を書き込む
/// </summary>
public class ItemCard : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text priceText;
    [SerializeField] private Text memoText;
    [SerializeField] private Text numberText;

    /// <summary>
    /// カードに情報を書き込む
    /// </summary>
    public void Setup(PurchasedItem item, int number)
    {
        nameText.text = item.itemName.ToString();
        priceText.text = $"買取額：{item.paidPrice:N0}$";
        numberText.text = $"No.{number:000}";

        // プレイヤーが付けたチェックだけを鑑定メモにする
        string memo = "鑑定メモ：\n";
        if (item.checkedDamaged) memo += "・傷あり\n";
        if (item.checkedDescriptionWrong) memo += "・情報ちがい\n";
        if (item.checkedMission1) memo += "オリジナルミッション①に該当\n";
        if (item.checkedMission2) memo += "オリジナルミッション②に該当\n";
        if (!item.checkedDamaged && !item.checkedDescriptionWrong && !item.checkedMission1 && !item.checkedMission2)
        {
            memo += "・問題なし\n";
        }
        memoText.text = memo;
    }
}
