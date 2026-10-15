using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Loding処理
/// </summary>
public class SceneTeleporter : MonoBehaviour
{
    [Header("ローディングに使うパネル"),SerializeField]
    private GameObject loadingPanel;

    [Header("LoadingPanelEventのスクリプト"),SerializeField]
    private LoadingPanelEvent loadingPanelEvent;

    [Header("AudioSouseをアタッチ"),SerializeField]
    private AudioSource audioSource;

    [Header("ローディングの文字Image"),SerializeField]
    private GameObject loadingTextImage;
    [Header("ローディングの車Image"), SerializeField]
    private GameObject loadingCarImage;

    /// <summary>
    /// 開始処理
    /// </summary>
    private void Start()
    {
        //LoadingPanelEventのスクリプトがアタッチされているか確認※ない場合自動でアタッチする処理を入れる
        GameObject obj = GameObject.Find("LoadingPanel");
        if (loadingPanelEvent!=null)
        {
            loadingPanelEvent=obj.GetComponent<LoadingPanelEvent>();
        }
        else
        {
            Debug.LogError("LoadingPanelの名前が違うか、スクリプトがないかです");
        }

        //最初にImageたちを非表示にする
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        if (loadingTextImage != null)
        {
            loadingTextImage.SetActive(false);
        }
        if (loadingCarImage != null)
        {
            loadingCarImage.SetActive(false);
        }

        //audioSourceがアタッチされているか確認※ない場合自動でアタッチする処理を入れる
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
        else
        {
            Debug.LogError("AudioSourceがありません");
        }
    }

    /// <summary>
    /// 外部からLodingを開く処理
    /// </summary>
    public void OpenLodingPanel()
    {
        // ローディングパネルがnullでない場合は処理を行う
        if (loadingPanel == null) return;

        loadingPanel.SetActive(true);

        StartCoroutine(PlayEngineAndWait());
    }

    /// <summary>
    /// ローディングパネルのアニメーションイベントから呼ばれる処理
    /// </summary>
    public void OpenLodingIcon()
    {
        loadingTextImage.SetActive(true);
        loadingCarImage.SetActive(true);
    }

    /// <summary>
    /// エンジン音を鳴らして、鳴り終わったら次の処理をする
    /// </summary>
    private IEnumerator PlayEngineAndWait()
    {
        audioSource.Play();

        // 音が鳴っている間は待つ
        while (audioSource.isPlaying)
        {
            yield return null;
        }

        Debug.Log("エンジン音がなり終わったのでシーンへ移動します。");
        SceneManager.LoadScene("AppraisalRoom");
    }
}
