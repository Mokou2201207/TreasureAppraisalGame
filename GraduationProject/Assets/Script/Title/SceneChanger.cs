using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ボタンなどからシーンを変える処理（タイトルの「はじめる」など）
/// </summary>
public class SceneChanger : MonoBehaviour
{
    [Header("移る先のシーン名（Build Settings に入れておく）"), SerializeField]
    private string sceneName = "MainScene";

    /// <summary>
    /// 決めたシーンに移る処理（ボタンの On Click から呼ぶ）
    /// </summary>
    public void ChangeScene()
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// ゲームを終わる処理（「やめる」ボタン用。エディタでは止まらず、ビルドしたゲームで終わる）
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }
}
