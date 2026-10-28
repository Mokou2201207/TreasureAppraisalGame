using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掲示板に貼るお宝のカード1枚分の処理※AI担当
/// </summary>
public class BoardCard : MonoBehaviour
{
    [Header("カードの中身")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text numberText;
    [SerializeField] private Text memoText;
    [SerializeField] private Text priceText;

    [Header("ボタン")]
    [Tooltip("カード全体を押したとき（詳しい情報を見る）")]
    [SerializeField] private Button selectButton;
    [Tooltip("出品リストに入れたり外したりするボタン")]
    [SerializeField] private Button addButton;
    [SerializeField] private Image addButtonImage;
    [SerializeField] private Sprite addSprite;
    [SerializeField] private Sprite listedSprite;

    [Header("出品リストに入っているときに出すはんこ")]
    [SerializeField] private GameObject listedStamp;
    [Header("選んでいるときに出す光るふち")]
    [SerializeField] private GameObject selectFrame;

    // このカードのお宝
    public PurchasedItem Item { get; private set; }

    private Action<BoardCard> onSelect;
    private Action<BoardCard> onToggle;

    /// <summary>
    /// カードに情報を書き込む
    /// </summary>
    public void Setup(PurchasedItem item, int number, Sprite icon, bool listed, Action<BoardCard> selectAction, Action<BoardCard> toggleAction)
    {
        Item = item;
        onSelect = selectAction;
        onToggle = toggleAction;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
        }
        if (nameText != null) nameText.text = item.itemName.ToString();
        if (numberText != null) numberText.text = $"No.{number:000}";
        if (memoText != null) memoText.text = MakeMemo(item);
        if (priceText != null) priceText.text = $"買値 {item.paidPrice:N0}$";

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke(this));
        }
        if (addButton != null)
        {
            addButton.onClick.RemoveAllListeners();
            addButton.onClick.AddListener(() => onToggle?.Invoke(this));
        }

        SetListed(listed);
        SetSelected(false);
    }

    /// <summary>
    /// 出品リストに入っているかどうかで見た目を変える
    /// </summary>
    public void SetListed(bool listed)
    {
        if (listedStamp != null) listedStamp.SetActive(listed);
        if (addButtonImage != null)
        {
            Sprite s = listed ? listedSprite : addSprite;
            if (s != null) addButtonImage.sprite = s;
        }
    }

    /// <summary>
    /// 選んでいるカードだけふちを光らせる
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectFrame != null) selectFrame.SetActive(selected);
    }

    /// <summary>
    /// プレイヤーが付けたチェックから鑑定メモの文を作る
    /// </summary>
    public static string MakeMemo(PurchasedItem item)
    {
        string memo = "";
        if (item.checkedDamaged) memo += "・傷あり\n";
        if (item.checkedDescriptionWrong) memo += "・話がうそ\n";
        if (item.checkedMission1) memo += "・ミッション①\n";
        if (item.checkedMission2) memo += "・ミッション②\n";
        if (memo == "") memo = "・問題なし\n";
        return memo.TrimEnd('\n');
    }
}
