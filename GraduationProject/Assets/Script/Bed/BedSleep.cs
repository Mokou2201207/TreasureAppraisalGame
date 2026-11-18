using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ベッドで寝て一日を終える処理（お店を閉めて21時になっていると寝られる）
/// </summary>
public class BedSleep : MonoBehaviour
{
    [Header("時間管理（GameClock）をアタッチ（空ならシーンから探す）"), SerializeField]
    private GameClock clock;

    [Header("看板（OpenSignSwitch）をアタッチ（空ならシーンから探す）"), SerializeField]
    private OpenSignSwitch sign;

    [Header("並んでいるお客さんがいたら寝られないようにするか"), SerializeField]
    private bool needNoCustomers = true;

    [Header("操作できる距離"), SerializeField]
    private float interactDistance = 3f;

    [Header("画面を暗くするImageのAnimator"), SerializeField]
    private Animator fadeAnimator;

    [Header("Animatorのboolの名前（trueで暗くなる、falseで明るくなる）"), SerializeField]
    private string fadeParameter = "isDark";

    [Header("暗くなる・明るくなるアニメーションの長さ"), SerializeField]
    private float fadeTime = 1f;

    [Header("真っ暗になったら出すローディング（空なら出さない）"), SerializeField]
    private GameObject loadingObject;

    [Header("ローディングを出しておく時間"), SerializeField]
    private float sleepTime = 1f;

    [Header("寝られないときの理由を出すText"), SerializeField]
    private Text messageText;

    [Header("スライドさせる物（空ならTextをそのままスライド）"), SerializeField]
    private RectTransform messagePanel;

    [Header("右からスライドしてくる距離"), SerializeField]
    private float slideDistance = 600f;

    [Header("スライドにかかる時間"), SerializeField]
    private float slideTime = 0.3f;

    [Header("理由を出しておく時間"), SerializeField]
    private float messageTime = 2f;

    [Header("寝るときの音"), SerializeField]
    private AudioClip sleepSE;

    [Header("寝られないときの音"), SerializeField]
    private AudioClip cannotSleepSE;

    //今寝ている途中か
    public bool IsSleeping { get; private set; }
    //操作できる距離
    public float InteractDistance => interactDistance;

    private Coroutine messageRoutine;
    //ローディングとして出したり隠したりする物
    private readonly List<GameObject> loadingParts = new List<GameObject>();
    private AudioSource audioSource;
    //文字が出ているときの位置と、隠れているときの位置
    private Vector2 shownPos;
    private Vector2 hiddenPos;

    /// <summary>
    /// 開始
    /// </summary>
    private void Start()
    {
        //入れ忘れていてもシーンから探す
        if (clock == null) clock = FindObjectOfType<GameClock>();
        if (sign == null) sign = FindObjectOfType<OpenSignSwitch>();

        //スライドさせる物がなければTextをそのまま使う
        if (messagePanel == null && messageText != null) messagePanel = messageText.rectTransform;

        //暗くするAnimatorが入っていなければ、isDarkを持っているAnimatorをシーンから探す
        if (fadeAnimator == null) fadeAnimator = FindFadeAnimator();
        if (fadeAnimator == null) Debug.LogWarning("[BedSleep] 画面を暗くするAnimatorが見つかりません。Fade Animator に入れてください。");

        //黒い画像は透明でもクリックを受け止めて、交渉などのボタンが押せなくなるので、クリックを通すようにする
        Graphic fadeGraphic = fadeAnimator != null ? fadeAnimator.GetComponent<Graphic>() : null;
        if (fadeGraphic != null) fadeGraphic.raycastTarget = false;

        //ローディングに出す物を集めて、最初は隠しておく（暗くする画像は隠さない）
        CollectLoadingParts();
        SetLoadingActive(false);

        //置いてある位置を出ているときの位置にして、最初は右の画面外に隠しておく
        if (messagePanel != null)
        {
            shownPos = messagePanel.anchoredPosition;
            hiddenPos = shownPos + new Vector2(slideDistance, 0f);
            messagePanel.anchoredPosition = hiddenPos;
        }
    }

    /// <summary>
    /// ベッドで寝ようとする処理（PlayerLookから呼ぶ）
    /// </summary>
    public void TrySleep()
    {
        //もう寝ている途中なら何もしない
        if (IsSleeping) return;

        //寝られないときは音を鳴らして理由を出して終わり（視点はそのまま動かせる）
        if (!CanSleep(out string reason))
        {
            PlaySE(cannotSleepSE);
            ShowMessage(reason);
            return;
        }

        StartCoroutine(SleepRoutine());
    }

    /// <summary>
    /// 今寝られるかを調べる処理（寝られないときは理由も返す）
    /// </summary>
    public bool CanSleep(out string reason)
    {
        reason = "";

        //21時になっていなければ寝られない
        if (clock == null || !clock.IsClosingTime)
        {
            reason = "21時になるまで寝られない";
            return false;
        }

        //看板がOpenのままなら寝られない
        if (sign != null && sign.isOpen)
        {
            reason = "看板をCLOSEにしてから寝よう";
            return false;
        }

        //並んでいるお客さんがいたら寝られない
        if (needNoCustomers && HasCustomerInLine())
        {
            reason = "まだ並んでいるお客さんがいる";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 列に並んでいるお客さんがいるかを調べる処理
    /// </summary>
    private bool HasCustomerInLine()
    {
        //列の番号が0以上なら並んでいるお客さん（帰っている途中の人は-1）
        foreach (MoveWaypoint customer in FindObjectsOfType<MoveWaypoint>())
        {
            if (customer.queuePositionNumber >= 0) return true;
        }
        return false;
    }

    /// <summary>
    /// 寝て次の日にする流れ（暗くする→日にちを進める→明るくする）
    /// </summary>
    private IEnumerator SleepRoutine()
    {
        IsSleeping = true;

        //次の日になるまで視点を動かせないようにする
        SetPlayerLookActive(false);
        PlaySE(sleepSE);

        //画面を暗くするアニメーションを動かして、真っ黒になりきるまで待つ
        if (fadeAnimator != null) fadeAnimator.SetBool(fadeParameter, true);
        yield return WaitForFade(true);

        //完全に暗くなったらローディングを出す
        SetLoadingActive(true);

        //ローディングの間に、時計を朝の8時に戻して残り日数を減らす（空も朝に戻る）
        if (clock != null) clock.StartNewDay();
        if (DayManager.Instance != null) DayManager.Instance.NextDay();

        //前の日に出品したお宝を売って、結果を作る（ポストで受け取る）
        if (AuctionManager.Instance != null) AuctionManager.Instance.ResolveAuctions();

        //ローディングを少しの間出しておいてから消す
        yield return new WaitForSeconds(sleepTime);
        SetLoadingActive(false);

        //画面を明るくするアニメーションを動かして、明るくなりきるまで待つ
        if (fadeAnimator != null) fadeAnimator.SetBool(fadeParameter, false);
        yield return WaitForFade(false);

        //返済の日（4日目の朝）なら、怖い人が来て返済の流れを出す
        if (DayManager.Instance != null && DayManager.Instance.IsRepaymentDay && RepaymentUI.Instance != null)
        {
            yield return RepaymentUI.Instance.Run();
        }

        //次の日になったので視点を動かせるように戻す
        SetPlayerLookActive(true);
        IsSleeping = false;
    }

    /// <summary>
    /// 黒い画像が暗くなりきる（明るくなりきる）まで待つ処理
    /// </summary>
    private IEnumerator WaitForFade(bool toDark)
    {
        //黒い画像が見つからなければ、決めた時間だけ待つ
        Graphic graphic = fadeAnimator != null ? fadeAnimator.GetComponent<Graphic>() : null;
        if (graphic == null)
        {
            yield return new WaitForSeconds(fadeTime);
            yield break;
        }

        //画像の透明度を見て、なりきるまで待つ（アニメーションが止まったとき用に、長くても決めた時間＋3秒まで）
        float limit = Time.time + fadeTime + 3f;
        while (Time.time < limit)
        {
            float alpha = graphic.color.a;
            if (toDark ? alpha >= 0.99f : alpha <= 0.01f) break;
            yield return null;
        }
    }

    /// <summary>
    /// isDarkを持っているAnimatorをシーンから探す処理
    /// </summary>
    private Animator FindFadeAnimator()
    {
        foreach (Animator anim in FindObjectsOfType<Animator>(true))
        {
            //Animatorの中身が入っていなければ飛ばす
            if (anim.runtimeAnimatorController == null) continue;

            //暗くする用のboolを持っていたら、それを使う
            foreach (AnimatorControllerParameter param in anim.parameters)
            {
                if (param.name == fadeParameter && param.type == AnimatorControllerParameterType.Bool) return anim;
            }
        }
        return null;
    }

    /// <summary>
    /// ローディングに出す物を集める処理（暗くする画像やその親が入っていたら、中身だけを使う）
    /// </summary>
    private void CollectLoadingParts()
    {
        loadingParts.Clear();
        if (loadingObject == null) return;

        //暗くする画像を含んでいなければ、そのまま使う
        if (fadeAnimator == null || !fadeAnimator.transform.IsChildOf(loadingObject.transform))
        {
            loadingParts.Add(loadingObject);
            return;
        }

        //暗くする画像ごと隠すと暗くならないので、中のローディングだけを集める
        CollectParts(loadingObject.transform);
    }

    /// <summary>
    /// 暗くする画像以外の子を集める処理
    /// </summary>
    private void CollectParts(Transform root)
    {
        foreach (Transform child in root)
        {
            //暗くする画像（またはその親）は隠さずに、その中をさらに調べる
            if (fadeAnimator.transform.IsChildOf(child)) CollectParts(child);
            else loadingParts.Add(child.gameObject);
        }
    }

    /// <summary>
    /// ローディングを出したり隠したりする処理
    /// </summary>
    private void SetLoadingActive(bool isActive)
    {
        foreach (GameObject part in loadingParts)
        {
            if (part != null) part.SetActive(isActive);
        }
    }

    /// <summary>
    /// プレイヤーの視点とクリックを動かせるか切りかえる処理
    /// </summary>
    private void SetPlayerLookActive(bool isActive)
    {
        //プレイヤーはあとからスポーンするので、そのときに探す
        MouseLook mouseLook = FindObjectOfType<MouseLook>();
        PlayerLook playerLook = FindObjectOfType<PlayerLook>();

        //視点の移動と、クリックで物を調べる処理を止めたり戻したりする
        if (mouseLook != null) mouseLook.enabled = isActive;
        if (playerLook != null) playerLook.enabled = isActive;
    }

    /// <summary>
    /// 寝られない理由をスライドで出す処理
    /// </summary>
    private void ShowMessage(string message)
    {
        //Textがなければ、Consoleに出しておく
        if (messageText == null || messagePanel == null)
        {
            Debug.Log($"[BedSleep] {message}");
            return;
        }

        //前の文字が出ていたら止めてから出し直す
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    /// <summary>
    /// 右からスライドして入ってきて、時間がたったら右へ戻る処理
    /// </summary>
    private IEnumerator MessageRoutine(string message)
    {
        messageText.text = message;

        //右から入ってくる
        yield return Slide(messagePanel.anchoredPosition, shownPos);

        //少しの間出しておく
        yield return new WaitForSeconds(messageTime);

        //右へ戻っていく
        yield return Slide(messagePanel.anchoredPosition, hiddenPos);
        messageRoutine = null;
    }

    /// <summary>
    /// 文字を今の位置から目的の位置まで動かす処理
    /// </summary>
    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        float timer = 0f;
        while (timer < slideTime)
        {
            timer += Time.deltaTime;

            //最後に少しゆっくりになるように動かす
            float t = Mathf.SmoothStep(0f, 1f, timer / slideTime);
            messagePanel.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        //最後はぴったりの位置にそろえる
        messagePanel.anchoredPosition = to;
    }

    /// <summary>
    /// SEを鳴らす処理
    /// </summary>
    private void PlaySE(AudioClip clip)
    {
        //音が入っていなければ鳴らさない
        if (clip == null) return;

        //AudioSourceがなければ付けて、距離で音量が変わらないようにする
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        audioSource.PlayOneShot(clip);
    }
}
