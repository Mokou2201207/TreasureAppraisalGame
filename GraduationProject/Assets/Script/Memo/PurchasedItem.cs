using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 買い取ったお宝の情報
/// </summary>
[System.Serializable]
public class PurchasedItem
{
    public ItemData.ItemCategory itemName; // どのお宝か
    public float highestPrice;             // 最高金額

    // 本当の状態（ItemData のフラグ）
    public bool isDamaged;
    public bool isCustomerDescription;
    public bool isAppraisalMission1;
    public bool isAppraisalMission2;

    // プレイヤーが鑑定リストに付けたチェック
    public bool checkedDamaged;
    public bool checkedDescriptionWrong;
    public bool checkedMission1;
    public bool checkedMission2;

    public int paidPrice;                  // 払った金額（鑑定料は除く）
}
