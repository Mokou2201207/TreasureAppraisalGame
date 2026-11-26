using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// クリアシーンを一括で組み立てる（カメラ、レジ前の列、お客さん、UI、演出スクリプト）※AI担当
/// 何回実行しても、同じ名前のものは作り直さず使い回す
/// </summary>
public static class ClearSceneBuilder
{
    private const string VisitorFolder = "Assets/Prefab/ClearCharacter";
    private static readonly string[] VisitorNames = { "ClearCutamar", "ClearCutamar2", "ClearCutamar3" };

    [MenuItem("Tools/クリアシーン/全部作る")]
    private static void BuildAll()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "ClearScene")
        {
            Debug.LogError("[ClearSceneBuilder] ClearSceneを開いてから実行してください。");
            return;
        }

        GameObject shopObj = GameObject.Find("Shopkeeper");
        if (shopObj == null)
        {
            Debug.LogError("[ClearSceneBuilder] Shopkeeper がシーンにありません。");
            return;
        }

        Transform shop = shopObj.transform;

        //MainScene のお客さんの通り道と同じ座標を使う
        //  入口の曲がり角 = Routes/CustomerBuyRoutes/Point(1)、レジ前 = Point (2)
        Vector3 doorCorner = new Vector3(3.26f, -18.62f, -13.87f);
        Vector3 counterFront = new Vector3(-8.43f, -18.62f, -14.11f);
        Vector3 lineDir = (doorCorner - counterFront);
        lineDir.y = 0f;
        lineDir.Normalize();

        //入口の曲がり角と、レジ前の列（先頭がレジに一番近い）
        Transform door = FindOrCreatePoint("ClearDoorPoint", doorCorner);
        Transform[] queue = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            queue[i] = FindOrCreatePoint("ClearQueue_" + (i + 1), counterFront + lineDir * (1.0f * i));
        }
        GameObject oldEntrance = GameObject.Find("ClearEntrance");
        if (oldEntrance != null) Object.DestroyImmediate(oldEntrance);

        //お客さん3人（店の外の通り道に、少しずつ間をあけて置く）
        GameObject[] visitors = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            Vector3 start = new Vector3(doorCorner.x, doorCorner.y, doorCorner.z - 4f - 1.5f * i);
            visitors[i] = FindOrCreateVisitor(VisitorNames[i], start);
            visitors[i].transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        //店主はレジ前（列の先頭）の方を向く
        Vector3 toCustomer = counterFront - shop.position;
        toCustomer.y = 0f;
        if (toCustomer.sqrMagnitude > 0.001f)
        {
            Undo.RecordObject(shop, "店主の向き");
            shop.rotation = Quaternion.LookRotation(toCustomer.normalized, Vector3.up);
        }

        //カメラ（入口側の少し高い所から、レジと列を見る）
        Camera cam = FindOrCreateCamera();
        cam.transform.position = counterFront + lineDir * 5.5f + Vector3.up * 2.4f + Vector3.forward * -3.0f;
        cam.transform.LookAt((counterFront + shop.position) * 0.5f + Vector3.up * 1.0f);
        cam.fieldOfView = 55f;

        //UI
        GameObject canvasObj = FindOrCreateRoot("ClearCanvas");
        Canvas canvas = Root<Canvas>(canvasObj);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Component scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
        ((CanvasScaler)scaler).uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)scaler).referenceResolution = new Vector2(1920, 1080);
        if (canvasObj.GetComponent<GraphicRaycaster>() == null) canvasObj.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        //いらっしゃいませのセリフ（下の方）
        Text message = MakeText(canvasObj.transform, "MessageText", "いらっしゃいませ", 56, Color.white,
            new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(1200, 120), font);
        message.gameObject.SetActive(false);

        //クリア表示（黒い幕＋体験版クリア＋Thank you）
        GameObject clearRoot = Child(canvasObj.transform, "ClearRoot");
        CanvasGroup clearGroup = clearRoot.GetComponent<CanvasGroup>();
        if (clearGroup == null) clearGroup = clearRoot.AddComponent<CanvasGroup>();
        Image clearBg = MakeImage(clearRoot.transform, "ClearBG", new Color(0, 0, 0, 0.6f));
        Stretch(clearBg);
        MakeText(clearRoot.transform, "ClearTitle", "体験版クリア", 140, new Color(0.85f, 0.15f, 0.15f),
            new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1600, 220), font);
        MakeText(clearRoot.transform, "ThankYou", "Thank you for playing", 48, Color.white,
            new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(1400, 80), font);
        clearRoot.SetActive(false);

        //クリック後のヒント
        Text clickHint = MakeText(canvasObj.transform, "ClickHint", "クリックでタイトルへ", 40, Color.white,
            new Vector2(0.5f, 0.04f), Vector2.zero, new Vector2(1000, 80), font);
        clickHint.gameObject.SetActive(false);

        //暗転（一番上に置く）
        Image fadeImage = MakeImage(canvasObj.transform, "FadeImage", Color.black);
        Stretch(fadeImage);
        fadeImage.raycastTarget = false;
        CanvasGroup fadeGroup = fadeImage.GetComponent<CanvasGroup>();
        if (fadeGroup == null) fadeGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();

        //演出スクリプト（全部の参照を入れる）
        GameObject directorObj = FindOrCreateRoot("ClearDirector");
        ClearSceneDirector director = directorObj.GetComponent<ClearSceneDirector>();
        if (director == null) director = directorObj.AddComponent<ClearSceneDirector>();

        Animator shopAnim = shopObj.GetComponentInChildren<Animator>(true);
        SerializedObject so = new SerializedObject(director);
        SerializedProperty list = so.FindProperty("visitors");
        list.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            SerializedProperty el = list.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("body").objectReferenceValue = visitors[i].transform;
            el.FindPropertyRelative("animator").objectReferenceValue = visitors[i].GetComponentInChildren<Animator>(true);
            el.FindPropertyRelative("queuePoint").objectReferenceValue = queue[i];
            SerializedProperty pathProp = el.FindPropertyRelative("path");
            pathProp.arraySize = 1;
            pathProp.GetArrayElementAtIndex(0).objectReferenceValue = door;
            el.FindPropertyRelative("speed").floatValue = 2.0f;
        }
        so.FindProperty("shopkeeper").objectReferenceValue = shop;
        so.FindProperty("shopkeeperAnimator").objectReferenceValue = shopAnim;
        so.FindProperty("bowTrigger").stringValue = "Bow";
        so.FindProperty("messageText").objectReferenceValue = message;
        so.FindProperty("greeting").stringValue = "いらっしゃいませ";
        so.FindProperty("fadeGroup").objectReferenceValue = fadeGroup;
        so.FindProperty("fadeTime").floatValue = 1.5f;
        so.FindProperty("clearRoot").objectReferenceValue = clearRoot;
        so.FindProperty("clearGroup").objectReferenceValue = clearGroup;
        so.FindProperty("clickHint").objectReferenceValue = clickHint.gameObject;
        so.FindProperty("titleSceneName").stringValue = "TitleScene";
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ClearSceneBuilder] クリアシーンを組み立てて保存しました。Playで確認してください。");
    }

    [MenuItem("Tools/クリアシーン/元に戻す")]
    private static void Revert()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "ClearScene")
        {
            Debug.LogError("[ClearSceneBuilder] ClearSceneを開いてから実行してください。");
            return;
        }

        string[] added = { "Main Camera", "ClearEntrance", "ClearDoorPoint", "ClearQueue_1", "ClearQueue_2", "ClearQueue_3",
            "ClearCutamar", "ClearCutamar2", "ClearCutamar3", "ClearCanvas", "ClearDirector" };
        foreach (string n in added)
        {
            GameObject go = GameObject.Find(n);
            while (go != null)
            {
                Object.DestroyImmediate(go);
                go = GameObject.Find(n);
            }
        }

        //店主の向きを元に戻す
        GameObject shopObj = GameObject.Find("Shopkeeper");
        if (shopObj != null) shopObj.transform.rotation = Quaternion.identity;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ClearSceneBuilder] クリアシーンを組み立てる前の状態に戻して保存しました。");
    }

    //---- 補助 ----

    private static Camera FindOrCreateCamera()
    {
        Camera cam = Object.FindObjectOfType<Camera>();
        if (cam != null) return cam;
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        return go.AddComponent<Camera>();
    }

    private static Transform FindOrCreatePoint(string name, Vector3 pos)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, pos.z);
        return go.transform;
    }

    private static GameObject FindOrCreateVisitor(string name, Vector3 pos)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisitorFolder + "/" + name + ".prefab");
            if (prefab == null)
            {
                Debug.LogError("[ClearSceneBuilder] お客さんのプレハブが見つかりません：" + name);
                return new GameObject(name + "_missing");
            }
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
        }
        go.transform.position = pos;
        return go;
    }

    private static GameObject FindOrCreateRoot(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name, typeof(RectTransform));
        return go;
    }

    private static T Root<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    private static GameObject Child(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t.gameObject;
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static T Child<T>(Transform parent, string name) where T : Component
    {
        GameObject go = Child(parent, name);
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    private static void Place(Component c, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        RectTransform rt = (RectTransform)c.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    private static void Stretch(Component c)
    {
        RectTransform rt = (RectTransform)c.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Text MakeText(Transform parent, string name, string value, int size, Color color,
        Vector2 anchor, Vector2 pos, Vector2 box, Font font)
    {
        Text t = Child<Text>(parent, name);
        t.font = font;
        t.text = value;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Place(t, anchor, box, pos);
        return t;
    }

    private static Image MakeImage(Transform parent, string name, Color color)
    {
        Image img = Child<Image>(parent, name);
        img.color = color;
        return img;
    }
}
