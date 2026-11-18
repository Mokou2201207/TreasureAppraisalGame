using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 闇市に映り変わるスクリプト
/// </summary>
public class CarToBlackMarket : MonoBehaviour
{
    [Header("ローディングパネルを表示")]
    [SerializeField] private GameObject loadingPanel;

    [Header("画面が消えるアニメーション")]
    [SerializeField]private Animator fadeOutPanel;

    [Header("ローディングアイコン")]
    [SerializeField] private GameObject loadingIcon;

    [Header("ローディングロゴ")]
    [SerializeField] private GameObject loadingRogo;

    [Header("闇市の画面（最初は隠しておく）")]
    [SerializeField] private GameObject blackMarketPanel;

    [Header("闇市のBGM（AudioSource）")]
    [SerializeField] private AudioSource marketBgm;

    [Header("エンジン音（出発と帰り）")]
    [SerializeField] private AudioClip engineSE;

    [Header("暗くなる・明るくなるアニメーションの時間（秒）")]
    [SerializeField] private float fadeTime = 2.25f;

    [Header("ローディングを出している時間（秒）")]
    [SerializeField] private float loadingWaitTime = 2f;

    //車が出発するかどうかのフラグ
    public bool isGoCar = false;

    //闇市の中にいるかどうか
    private bool isInMarket = false;


    private void Start()
    {
        //ローディングパネルを非表示にする
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        if (loadingIcon != null)
        {
            loadingIcon.SetActive(false);
        }
        if (loadingRogo != null)
        {
            loadingRogo.SetActive(false);
        }

        //闇市の画面は最初は隠しておく
        if (blackMarketPanel != null)
        {
            blackMarketPanel.SetActive(false);
        }

        //闇市のBGMは、マップの音を止めても鳴るようにする
        if (marketBgm != null)
        {
            marketBgm.ignoreListenerVolume = true;
            marketBgm.Stop();
        }

    }

    /// <summary>
    /// 車が出発する際の処理
    /// </summary>
    public void GoCar()
    {
        //もう出発している間は何もしない
        if (isGoCar) return;


        //フラグオン
        isGoCar = true;

        //移動中はプレイヤーを動かせないようにする
        SetPlayerActive(false);

        StartCoroutine(GoRoutine());
    }

    /// <summary>
    /// 出発の流れ（エンジン音→だんだん暗く→ローディング→2秒後にだんだん明るく→闇市）
    /// </summary>
    private IEnumerator GoRoutine()
    {
        //エンジン音を鳴らして、だんだん暗くする
        BlackMarketSound.PlayClip(engineSE);
        yield return FadeDark();

        //真っ暗になったらローディングのアイコンとロゴを出して、少し待つ
        SetLoadingIcon(true);
        yield return new WaitForSeconds(loadingWaitTime);
        SetLoadingIcon(false);

        //真っ暗の間に闇市の画面に切り替えて、マップの音を止める
        if (blackMarketPanel != null) blackMarketPanel.SetActive(true);
        if (HudHider.Instance != null) HudHider.Instance.Hide();
        if (DebtTalk.Instance != null) DebtTalk.Instance.Prepare();
        BlackMarketSound.Mute();
        if (marketBgm != null) marketBgm.Play();

        //だんだん明るくして、闇市を見せる
        yield return FadeBright();

        //闇市の中では、カーソルを出す
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isInMarket = true;

        //借金取りとの会話を始める
        if (DebtTalk.Instance != null) DebtTalk.Instance.Begin();
    }

    /// <summary>
    /// 闇市から戻る処理（会話が終わったら DebtTalk から呼ぶ）
    /// </summary>
    public void ReturnFromBlackMarket()
    {
        //闇市にいるときだけ戻れる
        if (!isInMarket) return;
        isInMarket = false;

        StartCoroutine(ReturnRoutine());
    }

    /// <summary>
    /// 帰りの流れ（エンジン音→だんだん暗く→ローディング→2秒後にだんだん明るく→マップ）
    /// </summary>
    private IEnumerator ReturnRoutine()
    {
        //エンジン音を鳴らして、だんだん暗くする
        BlackMarketSound.PlayClip(engineSE);
        yield return FadeDark();

        //真っ暗の間に闇市の画面を閉じて、ローディングのアイコンとロゴを出す
        if (blackMarketPanel != null) blackMarketPanel.SetActive(false);
        if (HudHider.Instance != null) HudHider.Instance.Show();
        if (marketBgm != null) marketBgm.Stop();
        SetLoadingIcon(true);
        yield return new WaitForSeconds(loadingWaitTime);
        SetLoadingIcon(false);

        //マップの音を戻して、だんだん明るくする
        BlackMarketSound.Unmute();
        yield return FadeBright();

        //プレイヤーを動かせるように戻して、カーソルを隠す
        SetPlayerActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //次にまた車に乗れるようにする
        isGoCar = false;
    }

    /// <summary>
    /// ローディングパネルを出して、だんだん暗くなるアニメーションを動かす処理
    /// </summary>
    private IEnumerator FadeDark()
    {
        //パネルを表示してからアニメーションを再生（表示していないとアニメーションが動かない）
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);

            //闇市の画面より手前で暗くなるように、一番前に出す
            loadingPanel.transform.SetAsLastSibling();
        }
        if (fadeOutPanel != null) fadeOutPanel.SetBool("Open", true);

        //暗くなりきるまで待つ
        yield return new WaitForSeconds(fadeTime);
    }

    /// <summary>
    /// だんだん明るくなるアニメーションを動かして、終わったらローディングパネルを隠す処理
    /// </summary>
    private IEnumerator FadeBright()
    {
        if (fadeOutPanel != null) fadeOutPanel.SetBool("Open", false);

        //明るくなりきるまで待つ
        yield return new WaitForSeconds(fadeTime);

        //明るくなったらパネルを隠す（ボタンを押すのを邪魔しないように）
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    /// <summary>
    /// ローディングのアイコンとロゴを出したり隠したりする処理
    /// </summary>
    private void SetLoadingIcon(bool isOn)
    {
        if (loadingIcon != null) loadingIcon.SetActive(isOn);
        if (loadingRogo != null) loadingRogo.SetActive(isOn);
    }

    /// <summary>
    /// プレイヤーの移動、視点、クリックを止めたり、動かしたりする処理
    /// </summary>
    private void SetPlayerActive(bool isActive)
    {
        //プレイヤーはあとからスポーンするので、そのときに探す
        MouseLook mouseLook = FindObjectOfType<MouseLook>();
        PlayerMove playerMove = FindObjectOfType<PlayerMove>();
        PlayerLook playerLook = FindObjectOfType<PlayerLook>();

        if (mouseLook != null) mouseLook.enabled = isActive;
        if (playerMove != null) playerMove.enabled = isActive;
        if (playerLook != null) playerLook.enabled = isActive;
    }
}
