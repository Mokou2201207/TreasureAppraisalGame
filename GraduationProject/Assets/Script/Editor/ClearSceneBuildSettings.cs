using System.Linq;
using UnityEditor;

/// <summary>
/// ClearScene をビルド設定に入れておく（入っていないとシーンの移動ができないため）※AI担当
/// </summary>
public static class ClearSceneBuildSettings
{
    private const string ClearScenePath = "Assets/Scenes/ClearScene.unity";

    [InitializeOnLoadMethod]
    private static void EnsureClearScene()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        EditorBuildSettingsScene found = scenes.FirstOrDefault(s => s.path == ClearScenePath);
        if (found != null)
        {
            if (found.enabled) return;
            found.enabled = true;
            EditorBuildSettings.scenes = scenes;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ClearScenePath) == null) return;
        EditorBuildSettings.scenes = scenes.Concat(new[] { new EditorBuildSettingsScene(ClearScenePath, true) }).ToArray();
        UnityEngine.Debug.Log("[ClearSceneBuildSettings] ClearScene をビルド設定に追加しました。");
    }
}
