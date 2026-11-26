using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

/// <summary>
/// クリアシーンのタイムラインに、Cinemachine のカメラショットを組み立てる※AI担当
/// Tools/クリアシーン/カメラを作る で何度でも作り直せる（前に作ったものは消してから作る）
/// </summary>
public static class ClearCameraBuilder
{
    private const string Prefix = "CM_";
    private const string TrackName = "CM Camera Shots";

    [MenuItem("Tools/クリアシーン/カメラを作る")]
    private static void Build()
    {
        PlayableDirector director = Object.FindObjectsOfType<PlayableDirector>(true).FirstOrDefault(d => d.name == "ClearTimeline");
        if (director == null || !(director.playableAsset is TimelineAsset timeline))
        {
            Debug.LogError("[ClearCameraBuilder] ClearTimeline（PlayableDirector）が見つかりません。");
            return;
        }

        RemoveOld(director, timeline);

        Transform root3 = FindTransform("Cusromer3Root");
        Transform oldCam = root3 != null ? root3.Find("Camera") : null;
        Transform shop = FindTransform("Shopkeeper");
        if (root3 == null || oldCam == null || shop == null)
        {
            Debug.LogError("[ClearCameraBuilder] Cusromer3Root / その子の Camera / Shopkeeper のどれかが見つかりません。");
            return;
        }

        //---- 3人目（視点の人物）の動きを、タイムラインの録画から計算する
        AnimationTrack track3 = FindAnimationTrack(director, timeline, "Cusromer3Root");
        Vector3 p0 = root3.position, pTurn = root3.position + root3.forward * 25f;
        Quaternion r0 = root3.rotation;
        if (track3 != null && track3.infiniteClip != null)
        {
            p0 = SampleRoot(track3, root3, 0f, out r0);
            pTurn = SampleRoot(track3, root3, 15.02f, out _);
        }
        Debug.Log($"[ClearCameraBuilder] 3人目 シーン上={root3.position} 0秒={p0} 曲がり角={pTurn}");
        Vector3 walkDir = Flat(r0 * Vector3.forward);
        Vector3 walkRight = Vector3.Cross(Vector3.up, walkDir);

        //---- メインカメラ（Brain）
        Camera oldCamera = oldCam.GetComponent<Camera>();
        GameObject mainObj = new GameObject(Prefix + "MainCamera");
        Undo.RegisterCreatedObjectUndo(mainObj, "カメラを作る");
        mainObj.tag = "MainCamera";
        Camera cam = mainObj.AddComponent<Camera>();
        if (oldCamera != null)
        {
            cam.fieldOfView = oldCamera.fieldOfView;
            cam.nearClipPlane = oldCamera.nearClipPlane;
            cam.farClipPlane = oldCamera.farClipPlane;
            oldCamera.enabled = false;
        }
        AudioListener oldListener = oldCam.GetComponent<AudioListener>();
        if (oldListener != null) oldListener.enabled = false;
        mainObj.AddComponent<AudioListener>();
        CinemachineBrain brain = mainObj.AddComponent<CinemachineBrain>();
        mainObj.transform.SetPositionAndRotation(oldCam.position, oldCam.rotation);

        GameObject shotsRoot = new GameObject(Prefix + "Shots");
        Undo.RegisterCreatedObjectUndo(shotsRoot, "カメラを作る");

        //---- 入口の位置（3人目が曲がる所と、店に入っていく向き）
        Vector3 pTurnIn = pTurn, pQueue = pTurn + Vector3.left * 10f;
        if (track3 != null && track3.infiniteClip != null)
        {
            pQueue = SampleRoot(track3, root3, 20.08f, out _);
        }
        Vector3 inDir = Flat(pQueue - pTurnIn);
        Vector3 shopInside = pTurnIn + inDir * 9f + Vector3.up * 1.2f;

        //---- ショット（場所を切り替えず、重ねてゆっくり移動させる）
        //1. 背後：お客さんの後ろに付いて一緒に進む
        Transform body3 = MakeFollowPivot(root3);
        CinemachineVirtualCamera vFollow = MakeLocalCam("Follow", body3, new Vector3(0.3f, 1.75f, -2.2f), Quaternion.Euler(12f, 0f, 0f));

        //2. 列で待つ：背後からゆっくり肩越しまで寄る
        CinemachineVirtualCamera vQueueClose = MakeLocalCam("QueueClose", body3, new Vector3(0.5f, 1.7f, -1.6f), Quaternion.Euler(10f, 0f, 0f));

        //3. 店主の正面（お辞儀を見せる）
        Vector3 shopFwd = Flat(shop.forward);
        Vector3 shopRight = Vector3.Cross(Vector3.up, shopFwd);
        Vector3 shopPos = shop.position + shopFwd * 2.2f + shopRight * 0.7f + Vector3.up * 1.45f;
        CinemachineVirtualCamera vShop = MakeWorldCam("Shopkeeper", shotsRoot.transform, shopPos, shop.position + Vector3.up * 1.25f);

        //4. お辞儀の後、まっすぐ後ろへ下がって、お店の門の外へ（ずっと店の中を向いたまま）
        CinemachineVirtualCamera vDoorOut = MakeWorldCam("DoorOutside", shotsRoot.transform, pTurnIn - inDir * 3.5f + Vector3.up * 2.0f, shopInside);
        //タイムラインが終わった後も、お店の門のショットのまま止める
        vDoorOut.m_Priority = 20;

        //---- タイムラインに並べる（重なった所でゆっくり移動する）
        CinemachineTrack cmTrack = timeline.CreateTrack<CinemachineTrack>(null, TrackName);
        director.SetGenericBinding(cmTrack, brain);

        List<TimelineClip> clips = new List<TimelineClip>
        {
            AddShot(director, cmTrack, vFollow, "背後", 0.0, 40.0),
            AddShot(director, cmTrack, vQueueClose, "寄る", 21.6, 42.0),
            AddShot(director, cmTrack, vShop, "店主正面", 40.0, 62.0),
            AddShot(director, cmTrack, vDoorOut, "門へゆっくり下がる", 44.3, 67.5),
        };
        for (int i = 1; i < clips.Count; i++)
        {
            double overlap = clips[i - 1].end - clips[i].start;
            if (overlap > 0.0001)
            {
                clips[i - 1].blendOutDuration = overlap;
                clips[i].blendInDuration = overlap;
            }
        }

        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        EditorSceneManager.SaveScene(director.gameObject.scene);
        Debug.Log("[ClearCameraBuilder] カメラのショットを作って保存しました。Playで確認してください。");
    }

    [MenuItem("Tools/クリアシーン/カメラを元に戻す")]
    private static void Revert()
    {
        PlayableDirector director = Object.FindObjectsOfType<PlayableDirector>(true).FirstOrDefault(d => d.name == "ClearTimeline");
        if (director == null || !(director.playableAsset is TimelineAsset timeline)) return;
        RemoveOld(director, timeline);
        Transform root3 = FindTransform("Cusromer3Root");
        Transform oldCam = root3 != null ? root3.Find("Camera") : null;
        if (oldCam != null)
        {
            Camera c = oldCam.GetComponent<Camera>();
            if (c != null) c.enabled = true;
            AudioListener l = oldCam.GetComponent<AudioListener>();
            if (l != null) l.enabled = true;
        }
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        EditorSceneManager.SaveScene(director.gameObject.scene);
        Debug.Log("[ClearCameraBuilder] カメラを作る前の状態に戻して保存しました。");
    }

    [MenuItem("Tools/クリアシーン/エンディングUIを作る")]
    private static void BuildEnding()
    {
        const string logoPath = "Assets/Sprite/Title/Logo_Moneydori_NoCopy.png";

        //ロゴの画像を、UIで使える設定にする
        TextureImporter imp = AssetImporter.GetAtPath(logoPath) as TextureImporter;
        if (imp == null)
        {
            Debug.LogError("[ClearCameraBuilder] ロゴが見つかりません：" + logoPath);
            return;
        }
        if (imp.textureType != TextureImporterType.Sprite || imp.textureCompression != TextureImporterCompression.Uncompressed || imp.mipmapEnabled)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
        Sprite logo = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);

        //前に作ったものは消して作り直す
        GameObject old = GameObject.Find("ClearEndingCanvas");
        if (old != null) Object.DestroyImmediate(old);

        GameObject canvasObj = new GameObject("ClearEndingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObj, "エンディングUIを作る");
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        //タイトルロゴ（画面の少し上の真ん中）
        GameObject logoObj = new GameObject("TitleLogo", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        logoObj.transform.SetParent(canvasObj.transform, false);
        Image logoImage = logoObj.GetComponent<Image>();
        logoImage.sprite = logo;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;
        RectTransform lrt = (RectTransform)logoObj.transform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.62f);
        float w = 1100f;
        float h = logo != null ? w * logo.rect.height / logo.rect.width : 330f;
        lrt.sizeDelta = new Vector2(w, h);
        lrt.anchoredPosition = Vector2.zero;
        CanvasGroup logoGroup = logoObj.GetComponent<CanvasGroup>();
        logoGroup.alpha = 0f;
        logoGroup.blocksRaycasts = false;

        //画面全体を暗くする黒（一番上）
        GameObject fadeObj = new GameObject("ScreenFade", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        fadeObj.transform.SetParent(canvasObj.transform, false);
        Image fadeImage = fadeObj.GetComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;
        RectTransform frt = (RectTransform)fadeObj.transform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        CanvasGroup fadeGroup = fadeObj.GetComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        CanvasGroup thankGroup = MakeThankYou(canvasObj.transform, lrt);
        fadeObj.transform.SetAsLastSibling();

        ClearEndingUI ui = canvasObj.AddComponent<ClearEndingUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("titleLogo").objectReferenceValue = logoGroup;
        so.FindProperty("screenFade").objectReferenceValue = fadeGroup;
        so.FindProperty("thankYou").objectReferenceValue = thankGroup;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(canvasObj.scene);
        EditorSceneManager.SaveScene(canvasObj.scene);
        Debug.Log("[ClearCameraBuilder] エンディングUI（ロゴと画面の暗転）を作って保存しました。");
    }

    [MenuItem("Tools/クリアシーン/Thank youを追加")]
    private static void AddThankYou()
    {
        ClearEndingUI ui = Object.FindObjectsOfType<ClearEndingUI>(true).FirstOrDefault();
        if (ui == null)
        {
            Debug.LogError("[ClearCameraBuilder] ClearEndingCanvas がありません。先に「エンディングUIを作る」を実行してください。");
            return;
        }
        Transform canvas = ui.transform;
        Transform oldThanks = canvas.Find("ThankYou");
        if (oldThanks != null) Object.DestroyImmediate(oldThanks.gameObject);
        RectTransform logoRect = canvas.Find("TitleLogo") as RectTransform;
        CanvasGroup group = MakeThankYou(canvas, logoRect);
        Transform fade = canvas.Find("ScreenFade");
        if (fade != null) fade.SetAsLastSibling();

        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("thankYou").objectReferenceValue = group;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        Debug.Log("[ClearCameraBuilder] Thank you for playing を追加して保存しました。");
    }

    private static CanvasGroup MakeThankYou(Transform canvas, RectTransform logoRect)
    {
        GameObject go = new GameObject("ThankYou", typeof(RectTransform), typeof(CanvasGroup), typeof(Text), typeof(UnityEngine.UI.Outline), typeof(Shadow));
        Undo.RegisterCreatedObjectUndo(go, "Thank youを追加");
        go.transform.SetParent(canvas, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "Thank you for playing!";
        text.fontSize = 64;
        text.fontStyle = FontStyle.BoldAndItalic;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = new Color(0.96f, 0.84f, 0.52f);
        text.raycastTarget = false;
        UnityEngine.UI.Outline outline = go.GetComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.35f, 0.05f, 0.03f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);
        Shadow shadow = go.GetComponents<Shadow>().First(x => x.GetType() == typeof(Shadow));
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(5f, -6f);

        RectTransform rt = (RectTransform)go.transform;
        Vector2 anchor = logoRect != null ? logoRect.anchorMin : new Vector2(0.5f, 0.62f);
        float below = logoRect != null ? logoRect.sizeDelta.y * 0.5f + 60f : 230f;
        Vector2 basePos = logoRect != null ? logoRect.anchoredPosition : Vector2.zero;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(1200f, 100f);
        rt.anchoredPosition = basePos - new Vector2(0f, below);

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        return group;
    }

    [MenuItem("Tools/クリアシーン/3人目の曲がり方を直す")]
    private static void FixCustomer3Turn()
    {
        PlayableDirector director = Object.FindObjectsOfType<PlayableDirector>(true).FirstOrDefault(d => d.name == "ClearTimeline");
        if (director == null || !(director.playableAsset is TimelineAsset timeline))
        {
            Debug.LogError("[ClearCameraBuilder] ClearTimeline が見つかりません。");
            return;
        }
        Transform root3 = FindTransform("Cusromer3Root");
        Transform body3 = root3 != null ? root3.Find("ClearCutamar") : null;
        AnimationTrack rootTrack = FindAnimationTrack(director, timeline, "Cusromer3Root");
        AnimationTrack bodyTrack = FindAnimationTrack(director, timeline, "ClearCutamar");
        if (root3 == null || body3 == null || rootTrack == null || bodyTrack == null)
        {
            Debug.LogError("[ClearCameraBuilder] Cusromer3Root / ClearCutamar かそのトラックが見つかりません。");
            return;
        }

        //1. 親の回転キーを0にする（親は回さず、体のアニメーションで曲がる）
        AnimationClip rootClip = rootTrack.infiniteClip;
        if (rootClip != null)
        {
            Undo.RecordObject(rootClip, "3人目の曲がり方を直す");
            foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(rootClip))
            {
                if (b.path != "" || !b.propertyName.StartsWith("localEulerAngles")) continue;
                AnimationCurve c = AnimationUtility.GetEditorCurve(rootClip, b);
                Keyframe[] keys = c.keys;
                for (int i = 0; i < keys.Length; i++) { keys[i].value = 0f; keys[i].inTangent = 0f; keys[i].outTangent = 0f; }
                c.keys = keys;
                AnimationUtility.SetEditorCurve(rootClip, b, c);
            }
            EditorUtility.SetDirty(rootClip);
        }

        //2. 体のクリップ：ターンは0度から、ターンより後は-90度（左を向いたまま）
        List<TimelineClip> clips = bodyTrack.GetClips().OrderBy(c => c.start).ToList();
        TimelineClip turn = clips.FirstOrDefault(c => c.displayName.Contains("Turn") || (c.animationClip != null && c.animationClip.name.Contains("Turn")));
        if (turn == null)
        {
            Debug.LogError("[ClearCameraBuilder] ターンのクリップが見つかりません。");
            return;
        }
        foreach (TimelineClip c in clips)
        {
            if (!(c.asset is AnimationPlayableAsset a)) continue;
            Undo.RecordObject(a, "3人目の曲がり方を直す");
            if (c == turn) a.rotation = Quaternion.identity;
            else if (c.start > turn.start) a.rotation = Quaternion.Euler(0f, -90f, 0f);
            EditorUtility.SetDirty(a);
        }

        //3. 背後のカメラを、親ではなく体に付ける（ターンに合わせて回り込む）
        foreach (string n in new[] { Prefix + "Follow", Prefix + "QueueClose" })
        {
            Transform cam = root3.Find(n);
            if (cam == null) continue;
            Vector3 lp = cam.localPosition;
            Quaternion lr = cam.localRotation;
            Undo.SetTransformParent(cam, body3, "3人目の曲がり方を直す");
            cam.localPosition = lp;
            cam.localRotation = lr;
        }

        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        EditorSceneManager.SaveScene(director.gameObject.scene);
        Debug.Log("[ClearCameraBuilder] 3人目の曲がり方を直して保存しました。");
    }

    [MenuItem("Tools/クリアシーン/カメラの揺れを止める")]
    private static void StopCameraShake()
    {
        Transform root3 = FindTransform("Cusromer3Root");
        Transform body3 = root3 != null ? root3.Find("ClearCutamar") : null;
        if (root3 == null || body3 == null)
        {
            Debug.LogError("[ClearCameraBuilder] Cusromer3Root / ClearCutamar が見つかりません。");
            return;
        }
        Transform pivot = MakeFollowPivot(root3);
        foreach (string n in new[] { Prefix + "Follow", Prefix + "QueueClose" })
        {
            Transform cam = body3.Find(n) ?? root3.Find(n);
            if (cam == null) continue;
            Vector3 lp = cam.localPosition;
            Quaternion lr = cam.localRotation;
            Undo.SetTransformParent(cam, pivot, "カメラの揺れを止める");
            cam.localPosition = lp;
            cam.localRotation = lr;
        }
        EditorSceneManager.MarkSceneDirty(root3.gameObject.scene);
        EditorSceneManager.SaveScene(root3.gameObject.scene);
        Debug.Log("[ClearCameraBuilder] 背後カメラを揺れない土台（CM_FollowPivot）に付け替えて保存しました。");
    }

    /// <summary>
    /// 体についていく土台を作る（位置は体と同じ、向きはゆっくり追いかける）
    /// </summary>
    private static Transform MakeFollowPivot(Transform root3)
    {
        Transform body3 = root3.Find("ClearCutamar");
        Transform pivot = root3.Find(Prefix + "FollowPivot");
        if (pivot == null)
        {
            GameObject go = new GameObject(Prefix + "FollowPivot");
            Undo.RegisterCreatedObjectUndo(go, "カメラの土台");
            pivot = go.transform;
            pivot.SetParent(root3, false);
        }
        pivot.localPosition = body3 != null ? body3.localPosition : Vector3.zero;
        pivot.localRotation = Quaternion.Euler(0f, body3 != null ? body3.localEulerAngles.y : 0f, 0f);
        SmoothYawFollow f = pivot.GetComponent<SmoothYawFollow>();
        if (f == null) f = pivot.gameObject.AddComponent<SmoothYawFollow>();
        SerializedObject so = new SerializedObject(f);
        so.FindProperty("target").objectReferenceValue = body3;
        so.ApplyModifiedProperties();
        return pivot;
    }

    [MenuItem("Tools/クリアシーン/エピローグを追加")]
    private static void AddEpilogue()
    {
        ClearEndingUI ui = Object.FindObjectsOfType<ClearEndingUI>(true).FirstOrDefault();
        if (ui == null)
        {
            Debug.LogError("[ClearCameraBuilder] ClearEndingCanvas がありません。先に「エンディングUIを作る」を実行してください。");
            return;
        }
        Transform canvas = ui.transform;
        Transform old = canvas.Find("Epilogue");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        //右側の帯（うすい黒。はみ出た文字は隠す）
        GameObject area = new GameObject("Epilogue", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(RectMask2D));
        Undo.RegisterCreatedObjectUndo(area, "エピローグを追加");
        area.transform.SetParent(canvas, false);
        RectTransform art = (RectTransform)area.transform;
        art.anchorMin = new Vector2(1f, 0f);
        art.anchorMax = new Vector2(1f, 1f);
        art.pivot = new Vector2(1f, 0.5f);
        art.sizeDelta = new Vector2(860f, 0f);
        art.anchoredPosition = new Vector2(-40f, 0f);
        Image bg = area.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = false;
        CanvasGroup group = area.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        //流れる文章
        GameObject textObj = new GameObject("EpilogueText", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter), typeof(Shadow));
        textObj.transform.SetParent(area.transform, false);
        RectTransform trt = (RectTransform)textObj.transform;
        trt.anchorMin = new Vector2(0.5f, 0f);
        trt.anchorMax = new Vector2(0.5f, 0f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(820f, 100f);
        trt.anchoredPosition = Vector2.zero;
        Text text = textObj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "借金を返し終えたあの日から、町にはひとつの噂が広まった。\n\n「あの鑑定屋は、お宝の売り上げだけで\n　多額の借金を返しきったらしい」\n\nけれど、店主が稼げたのは\n客の品を安く買いたたいたからではない。\n\n誰も気づかなかった本当の値打ちを見抜き、\n持ち込んだ客には、それに見合った値段をきちんと払う。\nそして、その目利きで闇のオークションを勝ち抜いてきたからだ。\n\n「あの店の鑑定なら信用できる」\n\nそんな声が客から客へと伝わり、\nいまでは毎日のように、たくさんのお宝が持ち込まれる。\n\n今日も店の扉が開く。\n店主はこれからも、ひとつひとつのお宝と向き合い、\nその価値を見極め続ける。";
        text.fontSize = 26;
        text.lineSpacing = 1.4f;
        text.alignment = TextAnchor.UpperCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = new Color(0.97f, 0.93f, 0.84f);
        text.raycastTarget = false;
        ContentSizeFitter fitter = textObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Shadow shadow = textObj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);

        //暗転の黒は一番上のまま
        Transform fade = canvas.Find("ScreenFade");
        if (fade != null) fade.SetAsLastSibling();

        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("epilogueGroup").objectReferenceValue = group;
        so.FindProperty("epilogueText").objectReferenceValue = trt;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        Debug.Log("[ClearCameraBuilder] エピローグを追加して保存しました。");
    }

    //---- 補助 ----

    private static void RemoveOld(PlayableDirector director, TimelineAsset timeline)
    {
        foreach (TrackAsset t in timeline.GetOutputTracks().ToList())
        {
            if (t is CinemachineTrack && t.name == TrackName) timeline.DeleteTrack(t);
        }
        foreach (Transform t in Object.FindObjectsOfType<Transform>(true).ToList())
        {
            if (t != null && t.name.StartsWith(Prefix) && t.gameObject.scene.IsValid()) Object.DestroyImmediate(t.gameObject);
        }
    }

    private static Transform FindTransform(string name)
    {
        return Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == name && t.gameObject.scene.IsValid());
    }

    private static AnimationTrack FindAnimationTrack(PlayableDirector director, TimelineAsset timeline, string objectName)
    {
        foreach (TrackAsset t in timeline.GetOutputTracks())
        {
            if (!(t is AnimationTrack at)) continue;
            Object b = director.GetGenericBinding(t);
            GameObject go = b is Component c ? c.gameObject : b as GameObject;
            if (go != null && go.name == objectName) return at;
        }
        return null;
    }

    private static Vector3 SampleRoot(AnimationTrack track, Transform fallback, float time, out Quaternion rotation)
    {
        AnimationClip clip = track.infiniteClip;
        Vector3 key = new Vector3(Eval(clip, "m_LocalPosition.x", time), Eval(clip, "m_LocalPosition.y", time), Eval(clip, "m_LocalPosition.z", time));
        Vector3 euler = new Vector3(Eval(clip, "localEulerAnglesRaw.x", time), Eval(clip, "localEulerAnglesRaw.y", time), Eval(clip, "localEulerAnglesRaw.z", time));
        Vector3 offPos = track.infiniteClipOffsetPosition;
        Quaternion offRot = track.infiniteClipOffsetRotation;
        if (offPos == Vector3.zero)
        {
            offPos = fallback.position;
            offRot = fallback.rotation;
        }
        rotation = offRot * Quaternion.Euler(euler);
        return offPos + offRot * key;
    }

    private static float Eval(AnimationClip clip, string property, float time)
    {
        foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(clip))
        {
            if (b.path == "" && b.propertyName == property) return AnimationUtility.GetEditorCurve(clip, b).Evaluate(time);
        }
        return 0f;
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude < 0.0001f ? Vector3.forward : v.normalized;
    }

    private static CinemachineVirtualCamera MakeWorldCam(string name, Transform parent, Vector3 pos, Vector3 lookAt)
    {
        GameObject go = new GameObject(Prefix + name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(lookAt - pos, Vector3.up);
        return SetupCam(go);
    }

    private static CinemachineVirtualCamera MakeLocalCam(string name, Transform parent, Vector3 localPos, Quaternion localRot)
    {
        GameObject go = new GameObject(Prefix + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        return SetupCam(go);
    }

    private static CinemachineVirtualCamera SetupCam(GameObject go)
    {
        Undo.RegisterCreatedObjectUndo(go, "カメラを作る");
        CinemachineVirtualCamera v = go.AddComponent<CinemachineVirtualCamera>();
        v.m_Lens.FieldOfView = 55f;
        v.m_Lens.NearClipPlane = 0.1f;
        v.m_Lens.FarClipPlane = 1000f;
        //Body と Aim は付けない（置いた位置と向きのまま映す）
        return v;
    }

    private static TimelineClip AddShot(PlayableDirector director, CinemachineTrack track, CinemachineVirtualCamera vcam, string label, double start, double end)
    {
        TimelineClip clip = track.CreateClip<CinemachineShot>();
        clip.start = start;
        clip.duration = end - start;
        clip.displayName = label;
        CinemachineShot shot = (CinemachineShot)clip.asset;
        shot.DisplayName = label;
        shot.VirtualCamera.exposedName = GUID.Generate().ToString();
        director.SetReferenceValue(shot.VirtualCamera.exposedName, vcam);
        EditorUtility.SetDirty(shot);
        return clip;
    }
}
