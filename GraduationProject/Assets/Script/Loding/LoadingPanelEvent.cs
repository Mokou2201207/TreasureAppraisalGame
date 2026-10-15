using UnityEngine;

/// <summary>
/// ローディングパネルのアニメーションイベントを SceneTeleporter に伝える処理
/// </summary>
public class LoadingPanelEvent : MonoBehaviour
{
    [Header("呼び出し先のSceneTeleporter"), SerializeField]
    private SceneTeleporter sceneTeleporter;

    /// <summary>
    /// アニメーションイベントから呼ばれる処理
    /// </summary>
    public void OnPanelOpened()
    {
        if (sceneTeleporter != null)
        {
            sceneTeleporter.OpenLodingIcon();
        }
    }
}