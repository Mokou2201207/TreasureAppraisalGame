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
    [Tooltip("残りの交渉可能回数を表示するテキスト")]
    [SerializeField] private Text remainingNegotiationCountText;
    [Tooltip("交渉値段を1000$上げるボタン")]
    [SerializeField] private Button increasePriceButton;
    [Tooltip("交渉値段を1000$下げるボタン")]
    [SerializeField] private Button decreasePriceButton;

    [Header("交渉回数設定")]
    [Tooltip("交渉回数の下限")]
    [SerializeField] private int minNegotiationCount = 3;
    [Tooltip("交渉回数の上限")]
    [SerializeField] private int maxNegotiationCount = 5;

    [Header("失敗確率計算設定")]
    [Tooltip("基本失敗確率（パーセント）")]
    [SerializeField] private float baseFailureRate = 30f;
    [Tooltip("基準額から1000$安くするごとの失敗確率上昇（パーセント）")]
    [SerializeField] private float priceFailureRatePer1000 = 2f;

    [Header("自動帰宅設定")]
    [Tooltip("最後の会話終了後、お客さんが自動で帰るまでの待機秒数")]
    [SerializeField] private float autoFinishDelay = 3.5f;

    // 現在の交渉提示金額
    private float currentNegotiationPrice = 0f;
    // 基準査定額
    private float baseAppraisalPrice = 0f;
    // 残りの交渉回数
    private int remainingNegotiationCount = 0;
    // 自動退場用コルーチン
    private Coroutine autoFinishCoroutine;

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
    }

    private void Update()
    {
        // 交渉パネル表示中にEnterキーで送信
        bool isPanelActive = appraisalAcceptancePanel != null && appraisalAcceptancePanel.activeSelf;
        if (isPanelActive && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            OnSendButtonClicked();
        }

        // 鑑定中に右クリックされたら交渉パネルを表示
        if (InspectionManager.Instance != null && InspectionManager.Instance.IsInspecting && Input.GetMouseButtonDown(1))
        {
            ShowAppraisalAcceptancePanel();
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
            if (remainingNegotiationCountText == null) remainingNegotiationCountText = FindComponentInChildren<Text>(appraisalAcceptancePanel, "RemainingNegotiationCountText", "RemainingCountText", "NegotiationCountText");
            if (increasePriceButton == null) increasePriceButton = FindComponentInChildren<Button>(appraisalAcceptancePanel, "IncreasePriceButton", "PriceUpButton", "UpButton");
            if (decreasePriceButton == null) decreasePriceButton = FindComponentInChildren<Button>(appraisalAcceptancePanel, "DecreasePriceButton", "PriceDownButton", "DownButton");
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

        if (autoFinishCoroutine != null)
        {
            StopCoroutine(autoFinishCoroutine);
            autoFinishCoroutine = null;
        }

        // 初期価格と基準額をInspectionManagerから反映
        if (InspectionManager.Instance != null)
        {
            currentNegotiationPrice = InspectionManager.Instance.CurrentCalculatedPrice;
            baseAppraisalPrice = currentNegotiationPrice;
        }

        // 交渉回数を乱数で初期化
        remainingNegotiationCount = UnityEngine.Random.Range(minNegotiationCount, maxNegotiationCount + 1);

        // UI表示を更新
        UpdateNegotiationUI();
        SetupButtonListeners();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

        float failureRate = CalculateFailureRate();
        if (failureRateText != null)
        {
            failureRateText.text = $"{Mathf.RoundToInt(failureRate)}%";
        }

        if (remainingNegotiationCountText != null)
        {
            remainingNegotiationCountText.text = $"{remainingNegotiationCount}回";
        }
    }

    /// <summary>
    /// 交渉値段を1000$上げる処理
    /// </summary>
    public void IncreaseNegotiationPrice()
    {
        currentNegotiationPrice += 1000f;
        UpdateNegotiationUI();
    }

    /// <summary>
    /// 交渉値段を1000$下げる処理
    /// </summary>
    public void DecreaseNegotiationPrice()
    {
        currentNegotiationPrice = Mathf.Max(0f, currentNegotiationPrice - 1000f);
        UpdateNegotiationUI();
    }

    /// <summary>
    /// 交渉パネルを非表示にする処理
    /// </summary>
    public void HideAppraisalAcceptancePanel()
    {
        if (autoFinishCoroutine != null)
        {
            StopCoroutine(autoFinishCoroutine);
            autoFinishCoroutine = null;
        }

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
        if (autoFinishCoroutine != null || remainingNegotiationCount <= 0) return;

        float failureRate = CalculateFailureRate();
        float roll = UnityEngine.Random.Range(0f, 100f);

        if (roll >= failureRate)
        {
            autoFinishCoroutine = StartCoroutine(AutoFinishCustomerAfterDelay());
        }
        else
        {
            remainingNegotiationCount = Mathf.Max(0, remainingNegotiationCount - 1);
            UpdateNegotiationUI();

            if (remainingNegotiationCount <= 0)
            {
                autoFinishCoroutine = StartCoroutine(AutoFinishCustomerAfterDelay());
            }
        }
    }

    /// <summary>
    /// 最後の会話後に自動的にお客さんが帰宅する処理
    /// </summary>
    private IEnumerator AutoFinishCustomerAfterDelay()
    {
        yield return new WaitForSeconds(autoFinishDelay);

        if (InspectionManager.Instance != null)
        {
            InspectionManager.Instance.BuyItemAndFinishCustomer();
        }
        autoFinishCoroutine = null;
    }
}
