using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// 人工知能を活用したお客さんとの交渉管理スクリプト
/// </summary>
public class NegotiationManager : MonoBehaviour
{
    public static NegotiationManager Instance { get; private set; }

    [Header("人工知能接続設定")]
    [Tooltip("サービス利用用のAPIキー")]
    [SerializeField] private string apiKey = "";

    [Tooltip("使用するモデル名")]
    [SerializeField] private string modelName = "gemini-flash-lite-latest";

    [Header("交渉用UI")]
    [SerializeField] private GameObject appraisalAcceptancePanel;
    [SerializeField] private InputField playerInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Text customerDialogueText;
    [SerializeField] private Text statusText;

    [Header("交渉パラメータ・UI設定")]
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
    // 会話による失敗確率の累積変動値
    private float conversationFailureRateModifier = 0f;
    // 自動退場用コルーチン
    private Coroutine autoFinishCoroutine;

    [Header("お客さんの基本性格・設定")]
    [TextArea(3, 8)]
    [SerializeField] private string systemPrompt = "あなたはお宝の持ち主（お客さん）です。質屋の店主（プレイヤー）と交渉しています。\nゲームのキャラクターとして自然な日本語のセリフのみを2〜3文で返答してください。解説や前置きは含めないでください。";

    // 通信中かどうかの判定
    private bool isSending = false;

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

        UpdateStatusText(string.Empty);
    }

    /// <summary>
    /// UIボタンのイベント登録
    /// </summary>
    private void SetupButtonListeners()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

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

        // 会話による確率変動をリセット
        conversationFailureRateModifier = 0f;

        // UI表示を更新
        UpdateNegotiationUI();
        SetupButtonListeners();

        if (playerInputField != null) playerInputField.interactable = true;
        if (sendButton != null) sendButton.interactable = true;

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

        // 会話による累積変動を加算
        rate += conversationFailureRateModifier;

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
        if (isSending || remainingNegotiationCount <= 0) return;

        if (playerInputField == null || string.IsNullOrWhiteSpace(playerInputField.text))
        {
            UpdateStatusText("メッセージを入力してください。");
            return;
        }

        string playerMessage = playerInputField.text.Trim();
        playerInputField.text = string.Empty;

        StartCoroutine(SendNegotiationRequest(playerMessage));
    }

    /// <summary>
    /// 人工知能サービスへ交渉メッセージを送信する処理
    /// </summary>
    private IEnumerator SendNegotiationRequest(string playerMessage)
    {
        isSending = true;
        if (sendButton != null) sendButton.interactable = false;
        UpdateStatusText("返答を待っています...");

        // 現在のお宝情報と査定額を取得
        ItemData itemData = null;
        float currentPrice = 0f;

        if (InspectionManager.Instance != null)
        {
            itemData = InspectionManager.Instance.CurrentItemData;
            currentPrice = InspectionManager.Instance.CurrentCalculatedPrice;
        }

        // プロンプト文章の構築
        StringBuilder promptBuilder = new StringBuilder();
        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine();

        if (itemData != null)
        {
            promptBuilder.AppendLine($"【持ち込んだお宝】：{itemData.itemName}（{itemData.itemTypeName}）");
            promptBuilder.AppendLine($"【お宝の本来の価値・基準額】：{itemData.highestPrice:N0}$");
            promptBuilder.AppendLine($"【お宝の説明】：{itemData.itemDescription}");
            if (!string.IsNullOrEmpty(itemData.itemCustomerDescription))
            {
                promptBuilder.AppendLine($"【お客さんの言い分・思い入れ】：{itemData.itemCustomerDescription}");
            }
        }

        promptBuilder.AppendLine($"【店主が現在提示している交渉金額】：{currentNegotiationPrice:N0}$");
        promptBuilder.AppendLine($"【店主（プレイヤー）の発言】：『{playerMessage}』");
        promptBuilder.AppendLine("上記を踏まえて、この提示や発言に対するお客さんのセリフを返してください。");

        if (remainingNegotiationCount <= 1)
        {
            promptBuilder.AppendLine("【重要：最後の交渉・退場セリフ】");
            promptBuilder.AppendLine("これが最後の会話です。この返答をもってお客さんはお店を退場・帰宅します。");
            promptBuilder.AppendLine("これまでのやり取りや今回の提示金額を踏まえ、満足して売って帰る、または不満や諦めで持ち帰るなど、帰宅するセリフにしてください。");
        }

        promptBuilder.AppendLine("【返答形式の厳守】");
        promptBuilder.AppendLine("解説や前置きは含めず、必ず以下の形式で返してください。");
        promptBuilder.AppendLine("1行目：お客さんのセリフ（喜怒哀楽の感情を込めた2〜3文）");
        promptBuilder.AppendLine("2行目：[確率変動: -40〜+40の整数]");
        promptBuilder.AppendLine("【確率変動の評価基準（店主の発言内容とお宝情報から精密に評価）】");
        promptBuilder.AppendLine("・すごく嬉しい情報・深い理解・感動（お宝の隠れた歴史や価値を教えてくれた、大切な思い出を尊重し絶賛してくれたなど）: -25〜-40（失敗確率を大幅に下げる）");
        promptBuilder.AppendLine("・嬉しい・納得・好印象（礼儀正しい態度、適正価格の提示、筋の通った褒め言葉など）: -10〜-24（失敗確率を下げる）");
        promptBuilder.AppendLine("・普通・淡々としたやり取り（形式的な挨拶、可もなく不可もない事務的会話など）: 0（または-5〜+5）");
        promptBuilder.AppendLine("・悲しい・がっかり・不満（思い出を軽く扱われた、理由の分からない安値、素っ気ない態度など）: +10〜+24（失敗確率を上げる）");
        promptBuilder.AppendLine("・腹立つ・激怒・侮辱（お宝をガラクタ扱いされた、失礼・横暴な物言い、一方的な安値の押し付けなど）: +25〜+40（失敗確率を大幅に上げる）");

        // リクエスト用JSONデータの作成
        GeminiRequest requestData = new GeminiRequest
        {
            contents = new GeminiContent[]
            {
                new GeminiContent
                {
                    parts = new GeminiPart[]
                    {
                        new GeminiPart { text = promptBuilder.ToString() }
                    }
                }
            }
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        string primaryModel = (string.IsNullOrEmpty(modelName) || modelName == "gemini-1.5-flash" || modelName == "gemini-flash-latest") ? "gemini-flash-lite-latest" : modelName;
        string fallbackModel = "gemini-3.5-flash-lite";

        string reply = null;
        int probabilityChange = 0;
        string[] targetModels = new string[] { primaryModel, fallbackModel };

        foreach (string currentModel in targetModels)
        {
            string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{currentModel}:generateContent?key={apiKey.Trim()}";

            using (UnityWebRequest request = new UnityWebRequest(requestUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    reply = ParseGeminiResponse(responseText, out probabilityChange);
                    break;
                }
                else
                {
                    Debug.LogWarning($"[NegotiationManager] {currentModel} 通信失敗 ({request.error})。代替モデルを試行します。");
                }
            }
        }

        if (!string.IsNullOrEmpty(reply))
        {
            // 会話による確率変動を加算
            conversationFailureRateModifier += probabilityChange;

            // 残り交渉回数を減らす
            remainingNegotiationCount = Mathf.Max(0, remainingNegotiationCount - 1);

            // UI表示を更新
            UpdateNegotiationUI();

            if (customerDialogueText != null)
            {
                customerDialogueText.text = reply;
            }

            if (remainingNegotiationCount <= 0)
            {
                UpdateStatusText("交渉が終了しました。お客さんが帰宅します...");
                if (playerInputField != null) playerInputField.interactable = false;
                autoFinishCoroutine = StartCoroutine(AutoFinishCustomerAfterDelay());
            }
            else
            {
                UpdateStatusText(string.Empty);
            }
        }
        else
        {
            UpdateStatusText("通信に失敗しました。キーや接続を確認してください。");
        }

        isSending = false;
        if (sendButton != null && remainingNegotiationCount > 0) sendButton.interactable = true;
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

    /// <summary>
    /// 返答データからセリフ文字列と確率変動値を取り出す処理
    /// </summary>
    private string ParseGeminiResponse(string json, out int probabilityChange)
    {
        probabilityChange = 0;
        try
        {
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(json);
            if (response != null && response.candidates != null && response.candidates.Length > 0)
            {
                var candidate = response.candidates[0];
                if (candidate.content != null && candidate.content.parts != null && candidate.content.parts.Length > 0)
                {
                    string rawText = candidate.content.parts[0].text.Trim();

                    // 正規表現で [確率変動: ±数値] を抽出
                    Match match = Regex.Match(rawText, @"[\[【]?\s*確率変動\s*[:：]\s*([+-]?\d+)\s*%?[\]】]?");
                    if (match.Success)
                    {
                        if (int.TryParse(match.Groups[1].Value, out int parsedVal))
                        {
                            probabilityChange = parsedVal;
                        }
                        rawText = Regex.Replace(rawText, @"[\[【]?\s*確率変動\s*[:：]\s*[+-]?\d+\s*%?[\]】]?", "").Trim();
                    }

                    Debug.Log($"[NegotiationManager] 会話による感情判定確率変動: {probabilityChange}% / セリフ: {rawText}");
                    return rawText;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NegotiationManager] 返答の解析に失敗しました: {ex.Message}");
        }

        return "（お客さんは言葉に詰まっているようだ…）";
    }

    /// <summary>
    /// 状態表示用テキストの更新
    /// </summary>
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    #region 通信データ送受信用クラス定義

    [Serializable]
    private class GeminiRequest
    {
        public GeminiContent[] contents;
    }

    [Serializable]
    private class GeminiContent
    {
        public GeminiPart[] parts;
    }

    [Serializable]
    private class GeminiPart
    {
        public string text;
    }

    [Serializable]
    private class GeminiResponse
    {
        public GeminiCandidate[] candidates;
    }

    [Serializable]
    private class GeminiCandidate
    {
        public GeminiContent content;
    }

    #endregion
}
