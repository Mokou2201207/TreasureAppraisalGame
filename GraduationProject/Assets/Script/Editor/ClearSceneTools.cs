using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// クリアシーンを作るためのエディタ用の道具※AI担当
/// </summary>
public static class ClearSceneTools
{
    private const string ShopkeeperPrefabPath = "Assets/Assets/Human/Player/Prefabs/male02_1.prefab";
    private const string ShopkeeperModelPath = "Assets/Assets/Human/Player/Models/male02_1.fbx";

    //MainScene でプレイヤーがお店に立つ位置（PlayerPoint）
    private static readonly Vector3 CounterPosition = new Vector3(-3.48f, -18.01f, -11.96f);

    /// <summary>
    /// 店主（male02_1）を、カウンターの位置に、お客さんの方を向けて置く
    /// </summary>
    [MenuItem("Tools/クリアシーン/店主を置く")]
    private static void PlaceShopkeeper()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopkeeperPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[ClearSceneTools] 店主のプレハブが見つかりません：" + ShopkeeperPrefabPath);
            return;
        }

        //すでに置いてあれば、それを使う
        GameObject shopkeeper = GameObject.Find("Shopkeeper");
        if (shopkeeper == null)
        {
            shopkeeper = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            shopkeeper.name = "Shopkeeper";
            Undo.RegisterCreatedObjectUndo(shopkeeper, "店主を置く");
        }

        //カウンターの位置で、床の高さに合わせる（お客さんは -Z の方から来るので、そちらを向く）
        Vector3 pos = CounterPosition;
        if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f)) pos.y = hit.point.y;
        Undo.RecordObject(shopkeeper.transform, "店主の位置");
        shopkeeper.transform.position = pos;
        shopkeeper.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        //おじぎ（人型のアニメーション）を使えるように、Animator と人型のアバターを付ける
        Animator animator = shopkeeper.GetComponentInChildren<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(shopkeeper);
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ShopkeeperModelPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null) Debug.LogWarning("[ClearSceneTools] male02_1 のアバターが見つかりません。モデルの Rig がヒューマノイドか確認してください。");
        Undo.RecordObject(animator, "店主のアバター");
        animator.avatar = avatar;
        animator.applyRootMotion = false;

        //選んで、シーンの画面で見えるようにする
        Selection.activeGameObject = shopkeeper;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        EditorSceneManager.MarkSceneDirty(shopkeeper.scene);
        Debug.Log("[ClearSceneTools] 店主を置きました。位置と向きはシーンの画面で調整してください。");
    }
}
