using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
    [SerializeField] private InputField playerInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Text customerDialogueText;
    [SerializeField] private Text statusText;

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
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        UpdateStatusText(string.Empty);
    }

    private void Update()
    {
        if (playerInputField != null && playerInputField.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            OnSendButtonClicked();
        }
    }

    /// <summary>
    /// 送信ボタン押下時の処理
    /// </summary>
    public void OnSendButtonClicked()
    {
        if (isSending) return;

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

        promptBuilder.AppendLine($"【店主が現在提示している査定金額】：{currentPrice:N0}$");
        promptBuilder.AppendLine($"【店主（プレイヤー）の発言】：『{playerMessage}』");
        promptBuilder.AppendLine("上記を踏まえて、この提示や発言に対するお客さんのセリフを返してください。");

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
                    reply = ParseGeminiResponse(responseText);
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
            if (customerDialogueText != null)
            {
                customerDialogueText.text = reply;
            }
            UpdateStatusText(string.Empty);
        }
        else
        {
            UpdateStatusText("通信に失敗しました。キーや接続を確認してください。");
        }

        isSending = false;
        if (sendButton != null) sendButton.interactable = true;
    }

    /// <summary>
    /// 返答データからセリフ文字列を取り出す処理
    /// </summary>
    private string ParseGeminiResponse(string json)
    {
        try
        {
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(json);
            if (response != null && response.candidates != null && response.candidates.Length > 0)
            {
                var candidate = response.candidates[0];
                if (candidate.content != null && candidate.content.parts != null && candidate.content.parts.Length > 0)
                {
                    return candidate.content.parts[0].text.Trim();
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
