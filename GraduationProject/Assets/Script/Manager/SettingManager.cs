using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// ESCキーから押した際の設定画面の処理
/// </summary>
public class SettingManager : MonoBehaviour
{
    [Header("設定画面"), SerializeField]
    private GameObject settingObj;

    [Header("オプション設定"), SerializeField]
    private GameObject optionsSettingObj;

    [Header("設定にあるボタンやタイトル")]
    [Header("設定画面のTitle"),SerializeField]
    private Image settingTitleImage;
    [Header("設定画面のボタン全種類"),SerializeField]
    private Button[] settingButtons;


    //設定画面を開いているかどうか
    private bool openSetting=false;

    //オプション設定が開いているかどうか
    private bool openOptionSetting=false;

    private void Start()
    {
        //非表示
        settingObj.SetActive(false);
        optionsSettingObj.SetActive(false);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 掲示板を閉じるためのEscでは、設定画面を開かない
            if (AuctionBoardUI.Instance != null && AuctionBoardUI.Instance.BlockEscape) return;

            //オプションが開いてなければ開く、開いていれば閉じる。
            if (!openSetting)
            {
                settingObj.SetActive(true);
                openSetting = true;

                //ゲームを止める
                Time.timeScale = 0f;

                //カーソルを表示
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            //画面設定が開いていてオプション設定も開いていたら
            else if (openSetting && openOptionSetting)
            {
                //オプション設定を閉じる関数
                CloseOptionsSetting();

                openSetting = false;

                //ゲームを動かす
                Time.timeScale = 1f;

                //カーソルを非表示
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                settingObj.SetActive(false);
                openSetting = false;

                //ゲームを動かす
                Time.timeScale = 1f;

                //カーソルを非表示
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

        }
    }

    /// <summary>
    /// ゲームに戻るのボタンを押した際
    /// </summary>
    public void StartGameButton()
    {
        settingObj.SetActive(false);
        openSetting = false;

        //ゲームを動かす
        Time.timeScale = 1f;

        //カーソルを非表示
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// ゲームのオプション設定を押した際
    /// </summary>
    public void OptionsSettingButton()
    {
        //ボタンをすべて非表示
        foreach (Button btn in settingButtons)
        {
            if (btn!=null)
            {
                btn.gameObject.SetActive(false);
            }
        }
        //設定のタイトルを非表示
        settingTitleImage.gameObject.SetActive(false);

        //オプション設定を表示
        optionsSettingObj.gameObject.SetActive(true);

        openOptionSetting = true;
    }

    /// <summary>
    /// ゲーム終了ボタンを押した際
    /// </summary>
    public void ExitButton()
    {
        Debug.Log("ゲームを終了します。");
        Application.Quit();
    }

    /// <summary>
    /// オプション設定を閉じる処理
    /// </summary>
    public void CloseOptionsSetting()
    {
        //ボタンをすべて表示
        foreach (Button btn in settingButtons)
        {
            if (btn != null)
            {
                btn.gameObject.SetActive(true);
            }
        }
        //設定のタイトルを表示
        settingTitleImage.gameObject.SetActive(true);

        //オプション設定を非表示
        optionsSettingObj.gameObject.SetActive(false);

        settingObj.SetActive(false);

        openOptionSetting = false;
    }
}
