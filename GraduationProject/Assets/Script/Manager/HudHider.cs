using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 会話（演出）の間だけ、いらないUIを隠して、終わったら元に戻す処理
/// </summary>
public class HudHider : MonoBehaviour
{
    private static HudHider instance;
    private static float nextFindTime;
    public static HudHider Instance
    {
        get
        {
            //チェックを外していても使えるように、シーンから探す（探しすぎないように1秒に1回まで）
            if (instance == null && Time.unscaledTime >= nextFindTime)
            {
                nextFindTime = Time.unscaledTime + 1f;
                instance = FindObjectOfType<HudHider>(true);
            }
            return instance;
        }
        private set { instance = value; }
    }

    [Header("会話の間だけ隠すUI（時計・お金・キー表示・照準など）"), SerializeField]
    private GameObject[] hideTargets;

    //このスクリプトが隠した物（戻すときは、これだけを表示に戻す）
    private readonly List<GameObject> hiddenByMe = new List<GameObject>();

    //今UIを隠しているか
    public bool IsHidden { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 決めたUIを隠す処理（表示されていた物だけ隠して、覚えておく）
    /// </summary>
    public void Hide()
    {
        if (IsHidden) return;

        hiddenByMe.Clear();
        foreach (GameObject target in hideTargets)
        {
            //最初から非表示の物は触らない
            if (target == null || !target.activeSelf) continue;

            target.SetActive(false);
            hiddenByMe.Add(target);
        }
        IsHidden = true;
    }

    /// <summary>
    /// 隠したUIを元に戻す処理（このスクリプトが隠した物だけ表示する）
    /// </summary>
    public void Show()
    {
        if (!IsHidden) return;

        foreach (GameObject target in hiddenByMe)
        {
            if (target != null) target.SetActive(true);
        }
        hiddenByMe.Clear();
        IsHidden = false;
    }
}
