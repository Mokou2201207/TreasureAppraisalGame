using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 人工知能を活用したお客さんとの交渉管理スクリプト※AI担当
/// </summary>
public class NegotiationManager : MonoBehaviour
{
    public static NegotiationManager Instance { get; private set; }

    [Header("交渉パラメータ・UI設定")]
    [Tooltip("交渉パネル")]
    [SerializeField] private GameObject appraisalAcceptancePanel;
    [Tooltip("交渉失敗確率を表示するテキスト")]
    [SerializeField] private Text failureRateText;
    [Tooltip("現在の交渉提示額を表示するテキスト")]
    [SerializeField] private Text negotiationPriceText;
    [Tooltip("交渉値段を1000$上げるボタン")]
    [SerializeField] private Button increasePriceButton;
    [Tooltip("交渉値段を1000$下げるボタン")]
    [SerializeField] private Button decreasePriceButton;
    [Tooltip("提示額で取引してお客さんを帰すボタン")]
    [SerializeField] private Button sendButton;
    [Tooltip("交渉結果を表示するテキスト")]
    [SerializeField] private Text resultText;
    [Tooltip("押すと鑑定に戻るボタン（なくても右クリックで戻れる）")]
    [SerializeField] private Button backToInspectionButton;

    [Header("失敗確率計算設定")]
    [Tooltip("基本失敗確率（パーセント）")]
    [SerializeField] private float baseFailureRate = 30f;
    [Tooltip("基準額から1000$安くするごとの失敗確率上昇（パーセント）")]
    [SerializeField] private float priceFailureRatePer1000 = 2f;

    [Header("鑑定料設定")]
    [Tooltip("1回の鑑定でかかる鑑定料（交渉が決裂しても支払う）")]
    [SerializeField] private int appraisalFee = 2000;
    [Tooltip("鑑定料を表示するテキスト")]
    [SerializeField] private Text appraisalFeeText;

    // 今の鑑定料
    public int AppraisalFee => appraisalFee;

    // 交渉パネルを開いているかどうか
    public bool IsNegotiating => appraisalAcceptancePanel != null && appraisalAcceptancePanel.activeSelf;

    // 現在の交渉提示金額
    private float currentNegotiationPrice = 0f;
    // 基準査定額
    private float baseAppraisalPrice = 0f;
    // 交渉結果の判定中かどうか
    private bool isResolving = false;
    // 交渉パネルを開いたフレーム（鑑定を終えた右クリックで、すぐ戻ってしまわないようにする）
    private int openedFrame = -1;

    // 交渉結果が出たときに成否と提示額を通知する
    public event Action<bool, float> OnNegotiationResolved;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        FindAppraisalAcceptancePanel();
        if (appraisalAcceptancePanel != null)
        {
            appraisalAcceptancePanel.SetActive(false);
        }

        SetupButtonListeners();
    }

    /// <summary>
    /// UIボタンのイベント登録
    /// </summary>
    private void SetupButtonListeners()
    {
        if (increasePriceButton != null)
        {
            increasePriceButton.onClick.RemoveListener(IncreaseNegotiationPrice);
            increasePriceButton.onClick.AddListener(IncreaseNegotiationPrice);
        }

        if (decreasePriceButton != null)
        {
            decreasePriceButton.onClick.RemoveListener(DecreaseNegotiationPrice);
            decreasePriceButton.onClick.AddListener(DecreaseNegotiationPrice);
        }

        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        if (backToInspectionButton != null)
        {
            backToInspectionButton.onClick.RemoveListener(BackToInspection);
            backToInspectionButton.onClick.AddListener(BackToInspection);
        }
    }

    /// <summary>
    /// パネルオブジェクトの自動検出
    /// </summary>
    private void FindAppraisalAcceptancePanel()
    {
        if (appraisalAcceptancePanel == null)
        {
            GameObject panelObj = GameObject.Find("AppraisalAcceptancePanel");
            if (panelObj != null)
            {
                appraisalAcceptancePanel = panelObj;
            }
        }

        if (appraisalAcceptancePanel != null)
        {
            // 各種UIの自動検索
            if (failureRateText == null) failureRateText = FindComponentInChildren<Text>(appraisalAcceptancePanel, "FailureRateText", "FailRateText", "FailureRate");
            if (negotiationPriceText == null) negotiationPriceText = FindComponentInChildren<Text>(appraisalAcceptancePanel, "NegotiationPriceText", "PriceText", "NegotiationPrice");
            if (increasePriceButton == null) increasePriceButton = FindComponentInChildren<Button>(appraisalAcceptancePanel, "IncreasePriceButton", "PriceUpButton", "UpButton");
            if (decreasePriceButton == null) decreasePriceButton = FindComponentInChildren<Button>(appraisalAcceptancePanel, "DecreasePriceButton", "PriceDownButton", "DownButton");
            if (sendButton == null) sendButton = FindComponentInChildren<Button>(appraisalAcceptancePanel, "SendButton", "DecideButton", "OkButton");
            if (appraisalFeeText == null) appraisalFeeText = FindComponentInChildren<Text>(appraisalAcceptancePanel, "AppraisalFeeText", "FeeText");
            if (resultText == null) resultText = FindComponentInChildren<Text>(appraisalAcceptancePanel, "ResultText", "NegotiationResultText");
            if (backToInspectionButton == null) backToInspectionButton = FindComponentInChildren<Button>(appraisalAcceptancePanel, "BackToInspectionButton", "BackButton");
        }
    }

    /// <summary>
    /// 子オブジェクトから名前候補でコンポーネントを検索
    /// </summary>
    private T FindComponentInChildren<T>(GameObject parent, params string[] candidateNames) where T : Component
    {
        T[] components = parent.GetComponentsInChildren<T>(true);
        foreach (var comp in components)
        {
            foreach (var name in candidateNames)
            {
                if (comp.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return comp;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 鑑定終了時に交渉パネルを表示する処理
    /// </summary>
    public void ShowAppraisalAcceptancePanel()
    {
        if (appraisalAcceptancePanel == null)
        {
            FindAppraisalAcceptancePanel();
        }

        if (appraisalAcceptancePanel != null)
        {
            appraisalAcceptancePanel.SetActive(true);
        }

        // 初期価格と基準額をInspectionManagerから反映
        if (InspectionManager.Instance != null)
        {
            currentNegotiationPrice = InspectionManager.Instance.CurrentCalculatedPrice;
            baseAppraisalPrice = currentNegotiationPrice;
        }

        isResolving = false;
        openedFrame = Time.frameCount;
        if (resultText != null) resultText.text = string.Empty;

        // UI表示を更新
        UpdateNegotiationUI();
        SetupButtonListeners();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!IsNegotiating) return;

        // ESCの設定画面を閉じるとカーソルが固定されるので、交渉中は出し直す
        if (Time.timeScale > 0f && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(1) && Time.frameCount != openedFrame)
        {
            BackToInspection();
        }
    }

    /// <summary>
    /// 交渉をやめて鑑定に戻る処理
    /// </summary>
    public void BackToInspection()
    {
        if (isResolving || Time.timeScale == 0f) return;
        if (InspectionManager.Instance == null) return;

        HideAppraisalAcceptancePanel();
        InspectionManager.Instance.ReturnToInspection();
    }

    /// <summary>
    /// 現在の失敗確率を計算する処理
    /// </summary>
    public float CalculateFailureRate()
    {
        float rate = baseFailureRate;

        // 値段による変動（基準額より安く買い叩くほど失敗確率が上昇）
        float priceDifference = baseAppraisalPrice - currentNegotiationPrice;
        rate += (priceDifference / 1000f) * priceFailureRatePer1000;

        return Mathf.Clamp(rate, 0f, 100f);
    }

    /// <summary>
    /// 交渉UI（値段、失敗確率、残り回数）の表示更新
    /// </summary>
    private void UpdateNegotiationUI()
    {
        if (negotiationPriceText != null)
        {
            negotiationPriceText.text = $"{currentNegotiationPrice:N0}$";
        }

        if (appraisalFeeText != null)
        {
            appraisalFeeText.text = $"鑑定料：{appraisalFee:N0}$";
        }

        float failureRate = CalculateFailureRate();
        if (failureRateText != null)
        {
            failureRateText.text = $"{Mathf.RoundToInt(failureRate)}%";
        }
    }

    /// <summary>
    /// 鑑定料を変える処理（お客さんのレベルに合わせて外から設定する）
    /// </summary>
    public void SetAppraisalFee(int fee)
    {
        appraisalFee = Mathf.Max(0, fee);
        UpdateNegotiationUI();
    }

    /// <summary>
    /// 交渉値段を1000$上げる処理
    /// </summary>
    public void IncreaseNegotiationPrice()
    {
        if (isResolving) return;
        currentNegotiationPrice += 1000f;
        UpdateNegotiationUI();
    }

    /// <summary>
    /// 交渉値段を1000$下げる処理
    /// </summary>
    public void DecreaseNegotiationPrice()
    {
        if (isResolving) return;
        currentNegotiationPrice = Mathf.Max(0f, currentNegotiationPrice - 1000f);
        UpdateNegotiationUI();
    }

    /// <summary>
    /// 交渉パネルを非表示にする処理
    /// </summary>
    public void HideAppraisalAcceptancePanel()
    {
        if (appraisalAcceptancePanel != null)
        {
            appraisalAcceptancePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 送信ボタン押下時の処理
    /// </summary>
    public void OnSendButtonClicked()
    {
        if (isResolving) return;
        isResolving = true;

        // 提示額で取引を確定し、お客さんを帰す
        ResolveNegotiation();
    }

    /// <summary>
    /// 失敗確率で交渉の成否を判定し、すぐにお客さんを帰す処理
    /// </summary>
    private void ResolveNegotiation()
    {
        int price = Mathf.RoundToInt(currentNegotiationPrice);
        int total = appraisalFee + price;

        // 鑑定料と提示額を合わせた金額が所持金に足りないときは交渉せず、値段を下げてもらう
        if (MonayManager.Instance != null && MonayManager.Instance.CurrentMonay < total)
        {
            if (resultText != null) resultText.text = "お金が足りません（鑑定料込み）";
            isResolving = false;
            return;
        }

        float failureRate = CalculateFailureRate();
        bool isSuccess = UnityEngine.Random.Range(0f, 100f) >= failureRate;

        // 鑑定料は結果にかかわらず支払い、成立したときだけ提示額も支払う
        if (MonayManager.Instance != null)
        {
            MonayManager.Instance.SpendMonay(isSuccess ? total : appraisalFee);
        }

        // 買取成功・失敗のはんこを出す
        if (DealResultUI.Instance != null)
        {
            DealResultUI.Instance.Play(isSuccess, isSuccess ? total : appraisalFee);
        }

        // 鑑定結果を受け取り、成立したときだけ買取額を付けて持ち物に入れる
        PurchasedItem record = InspectionManager.Instance != null ? InspectionManager.Instance.TakeAppraisalRecord() : null;
        if (isSuccess && record != null && InventoryManager.Instance != null)
        {
            record.paidPrice = price;
            InventoryManager.Instance.AddItem(record);
        }

        if (resultText != null)
        {
            resultText.text = isSuccess ? "交渉成立！" : "交渉決裂…";
        }

        OnNegotiationResolved?.Invoke(isSuccess, currentNegotiationPrice);

        if (InspectionManager.Instance != null)
        {
            if (isSuccess)
            {
                InspectionManager.Instance.BuyItemAndFinishCustomer();
            }
            else
            {
                InspectionManager.Instance.FinishCustomerWithoutBuying();
            }
        }

        isResolving = false;
    }
}
