using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// クリアシーンの流れを管理する（お客さんの入店、レジの前の列、店主のいらっしゃいませ、クリア表示、タイトルへ戻る）※AI担当
/// </summary>
public class ClearSceneDirector : MonoBehaviour
{
    [System.Serializable]
    public class Visitor
    {
        [Tooltip("お客さんの本体（ClearCutamar など）")]
        public Transform body;

        [Tooltip("お客さんのAnimator（isMoving というbool が必要）")]
        public Animator animator;

        [Tooltip("レジ前の列の場所（このお客さんが止まる場所）")]
        public Transform queuePoint;

        [Tooltip("入口から列までの途中の道（任意。空なら今いる場所から直接向かう）")]
        public Transform[] path;

        public float speed = 1.6f;
    }

    [Header("お客さん（入店する順番に並べる）")]
    [SerializeField] private Visitor[] visitors;
    [SerializeField] private float visitorInterval = 1.2f;
    [SerializeField] private float turnSpeed = 540f;

    [Header("店主（レジ）")]
    [SerializeField] private Transform shopkeeper;
    [SerializeField] private Animator shopkeeperAnimator;
    [SerializeField] private string bowTrigger = "Bow";

    [Header("セリフ")]
    [SerializeField] private Text messageText;
    [SerializeField] private string greeting = "いらっしゃいませ";
    [SerializeField] private float greetingTime = 2f;

    [Header("画面の暗転（フェード）")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeTime = 1.5f;

    [Header("クリア表示")]
    [SerializeField] private GameObject clearRoot;
    [SerializeField] private CanvasGroup clearGroup;
    [SerializeField] private GameObject clickHint;
    [SerializeField] private float clearFadeTime = 0.6f;

    [Header("タイミング")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private string titleSceneName = "TitleScene";

    private int arrivedCount;
    private bool canReturnToTitle;

    private void Awake()
    {
        //お客さんに既存のお客さん用スクリプトが付いていたら止める（この演出だけで動かすため）
        if (visitors == null) return;
        foreach (Visitor v in visitors)
        {
            if (v == null || v.body == null) continue;
            foreach (var nav in v.body.GetComponentsInChildren<NavMeshAgent>(true)) nav.enabled = false;
            foreach (var mw in v.body.GetComponentsInChildren<MoveWaypoint>(true)) mw.enabled = false;
            foreach (var ca in v.body.GetComponentsInChildren<CustomerAnim>(true)) ca.enabled = false;
        }
    }

    private void Start()
    {
        if (clearRoot != null) clearRoot.SetActive(false);
        if (clickHint != null) clickHint.SetActive(false);
        if (messageText != null) messageText.gameObject.SetActive(false);
        if (fadeGroup != null) fadeGroup.alpha = 1f;

        StartCoroutine(Sequence());
    }

    private void Update()
    {
        //クリア後のクリックでタイトルへ
        if (canReturnToTitle && Input.GetMouseButtonDown(0))
        {
            canReturnToTitle = false;
            SceneManager.LoadScene(titleSceneName);
        }
    }

    private IEnumerator Sequence()
    {
        yield return new WaitForSeconds(startDelay);

        //暗転を明ける
        yield return Fade(fadeGroup, 0f, fadeTime);

        //お客さんを順番に入店させ、レジ前の列まで歩かせる
        int count = 0;
        if (visitors != null)
        {
            foreach (Visitor v in visitors)
            {
                if (v == null) continue;
                count++;
                StartCoroutine(Walk(v));
                yield return new WaitForSeconds(visitorInterval);
            }
        }

        //全員が列に着くまで待つ
        while (arrivedCount < count) yield return null;

        //店主がお辞儀して、いらっしゃいませ
        if (shopkeeperAnimator != null && !string.IsNullOrEmpty(bowTrigger))
            shopkeeperAnimator.SetTrigger(bowTrigger);

        if (messageText != null)
        {
            messageText.text = greeting;
            messageText.gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(greetingTime);
        if (messageText != null) messageText.gameObject.SetActive(false);

        //クリア表示
        if (clearRoot != null) clearRoot.SetActive(true);
        if (clearGroup != null)
        {
            clearGroup.alpha = 0f;
            yield return Fade(clearGroup, 1f, clearFadeTime);
        }
        if (clickHint != null) clickHint.SetActive(true);
        canReturnToTitle = true;
    }

    private IEnumerator Walk(Visitor v)
    {
        if (v.body == null || v.queuePoint == null)
        {
            arrivedCount++;
            yield break;
        }

        if (v.animator != null) v.animator.SetBool("isMoving", true);

        //途中の道（あれば）→ 列の場所 の順で歩く
        List<Transform> points = new List<Transform>();
        if (v.path != null) points.AddRange(v.path);
        points.Add(v.queuePoint);

        foreach (Transform point in points)
        {
            if (point == null) continue;
            yield return MoveTo(v, point.position);
        }

        if (v.animator != null) v.animator.SetBool("isMoving", false);

        //レジの方（店主）を向いて止まる
        if (shopkeeper != null)
        {
            Vector3 toShop = shopkeeper.position - v.body.position;
            toShop.y = 0f;
            if (toShop.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(toShop.normalized, Vector3.up);
                while (Quaternion.Angle(v.body.rotation, look) > 1f)
                {
                    v.body.rotation = Quaternion.RotateTowards(v.body.rotation, look, turnSpeed * Time.deltaTime);
                    yield return null;
                }
            }
        }

        arrivedCount++;
    }

    private IEnumerator MoveTo(Visitor v, Vector3 target)
    {
        while (Vector3.Distance(v.body.position, target) > 0.05f)
        {
            Vector3 flat = target - v.body.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(flat.normalized, Vector3.up);
                v.body.rotation = Quaternion.RotateTowards(v.body.rotation, look, turnSpeed * Time.deltaTime);
            }
            v.body.position = Vector3.MoveTowards(v.body.position, target, v.speed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator Fade(CanvasGroup group, float target, float time)
    {
        if (group == null) yield break;
        float start = group.alpha;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        group.alpha = target;
    }
}
