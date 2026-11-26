using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 店主のお辞儀（Playerbow）を設定するエディタ用の道具※AI担当
/// </summary>
public static class ClearSceneAnimatorTools
{
    private const string ControllerPath = "Assets/Animation/Shopkeeper/ShopkeeperController.controller";
    private const string BowClipPath = "Assets/Animation/Player/Playerbow.anim";
    private const string BowTrigger = "Bow";

    [MenuItem("Tools/クリアシーン/店主のお辞儀を設定")]
    private static void SetupBow()
    {
        GameObject shopkeeper = GameObject.Find("Shopkeeper");
        if (shopkeeper == null)
        {
            Debug.LogError("[ClearSceneAnimatorTools] Shopkeeper がシーンにいません。先に「店主を置く」を実行してください。");
            return;
        }

        AnimationClip bowClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(BowClipPath);
        if (bowClip == null)
        {
            Debug.LogError("[ClearSceneAnimatorTools] お辞儀のクリップが見つかりません：" + BowClipPath);
            return;
        }

        //保存先のフォルダがなければ作る
        if (!AssetDatabase.IsValidFolder("Assets/Animation/Shopkeeper"))
            AssetDatabase.CreateFolder("Assets/Animation", "Shopkeeper");

        //Animator Controller を作る（すでにあれば作り直す）
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter(BowTrigger, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idle = sm.AddState("Idle");
        AnimatorState bow = sm.AddState("Bow");
        bow.motion = bowClip;
        sm.defaultState = idle;

        //Idle → Bow（トリガーで） → Idle（お辞儀が終わったら戻る）
        AnimatorStateTransition toBow = idle.AddTransition(bow);
        toBow.AddCondition(AnimatorConditionMode.If, 0, BowTrigger);
        toBow.hasExitTime = false;
        toBow.duration = 0.1f;

        AnimatorStateTransition backToIdle = bow.AddTransition(idle);
        backToIdle.hasExitTime = true;
        backToIdle.exitTime = 1f;
        backToIdle.hasExitTime = true;
        backToIdle.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        //店主に付ける
        Animator animator = shopkeeper.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("[ClearSceneAnimatorTools] 店主に Animator がありません。「店主を置く」をもう一度実行してください。");
            return;
        }
        Undo.RecordObject(animator, "店主のアニメーター");
        animator.runtimeAnimatorController = controller;
        EditorSceneManager.MarkSceneDirty(shopkeeper.scene);
        Debug.Log("[ClearSceneAnimatorTools] 店主のお辞儀を設定しました。再生するときは Animator の \"" + BowTrigger + "\" トリガーを発火させます。");
    }
}
