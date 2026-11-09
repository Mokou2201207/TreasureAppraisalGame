using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Model に入れたモデルから、マテリアル付きのプレハブを自動で作る※AI担当
/// </summary>
public class ModelPrefabMaker : AssetPostprocessor
{
    private const string ModelFolder = "Assets/Model/";
    private const string PrefabFolder = "Assets/Prefab/Model";

    private static readonly List<string> waiting = new List<string>();

    /// <summary>
    /// アウトラインで光らせられるように、Read/Write を有効にして読み込む
    /// </summary>
    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ModelFolder)) return;
        ModelImporter importer = (ModelImporter)assetImporter;
        importer.isReadable = true;
    }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (string path in imported)
        {
            if (!IsModel(path)) continue;
            if (File.Exists(GetPrefabPath(path))) continue;
            if (!waiting.Contains(path)) waiting.Add(path);
        }

        // 読み込みの途中ではプレハブを保存できないので、終わってから作る
        if (waiting.Count > 0)
        {
            EditorApplication.delayCall -= CreateWaiting;
            EditorApplication.delayCall += CreateWaiting;
        }
    }

    private static void CreateWaiting()
    {
        string[] paths = waiting.ToArray();
        waiting.Clear();
        foreach (string path in paths) CreatePrefab(path);
    }

    [MenuItem("Assets/モデルからプレハブを作る")]
    [MenuItem("Tools/選んだモデルからプレハブを作る")]
    private static void CreateFromSelection()
    {
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".obj") || path.EndsWith(".fbx") || path.EndsWith(".OBJ") || path.EndsWith(".FBX")) CreatePrefab(path);
        }
    }

    private static bool IsModel(string path)
    {
        if (!path.StartsWith(ModelFolder)) return false;
        string ext = Path.GetExtension(path).ToLower();
        return ext == ".obj" || ext == ".fbx";
    }

    private static string GetPrefabPath(string modelPath)
    {
        return PrefabFolder + "/" + Path.GetFileNameWithoutExtension(modelPath) + ".prefab";
    }

    /// <summary>
    /// モデル1つからプレハブを作る（同じ名前のプレハブがあれば上書きしない）
    /// </summary>
    private static void CreatePrefab(string modelPath)
    {
        string prefabPath = GetPrefabPath(modelPath);
        if (File.Exists(prefabPath))
        {
            Debug.Log($"[ModelPrefabMaker] {prefabPath} はもうあるので作りませんでした。");
            return;
        }

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null) return;

        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Prefab", "Model");

        GameObject instance = Object.Instantiate(model);
        instance.name = model.name;

        Material mat = GetOrCreateMaterial(modelPath);
        if (mat != null)
        {
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }

        // クリックで調べられるように、全体を包む当たり判定を付ける
        if (instance.GetComponentInChildren<Collider>() == null) AddBoxCollider(instance);

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        Debug.Log($"[ModelPrefabMaker] プレハブを作りました：{prefabPath}");
    }

    private static void AddBoxCollider(GameObject root)
    {
        bool found = false;
        Bounds bounds = new Bounds();
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            Bounds b = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                Vector3 p = root.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
                if (!found)
                {
                    bounds = new Bounds(p, Vector3.zero);
                    found = true;
                }
                else bounds.Encapsulate(p);
            }
        }
        if (!found) return;

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
    }

    /// <summary>
    /// モデルと同じフォルダの「名前_Albedo」「名前_Normal」の絵からマテリアルを作る
    /// </summary>
    private static Material GetOrCreateMaterial(string modelPath)
    {
        string folder = Path.GetDirectoryName(modelPath).Replace("\\", "/");
        string name = Path.GetFileNameWithoutExtension(modelPath);
        string matPath = folder + "/" + name + ".mat";

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat != null) return mat;

        string albedoPath = FindTexture(folder, name, "_Albedo");
        if (albedoPath == null) return null;
        string normalPath = FindTexture(folder, name, "_Normal");

        if (normalPath != null)
        {
            TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader);

        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
        mat.SetTexture("_BaseMap", albedo);
        mat.SetTexture("_MainTex", albedo);
        if (normalPath != null)
        {
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.SetFloat("_Smoothness", 0.2f);
        mat.SetFloat("_Glossiness", 0.2f);

        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }

    private static string FindTexture(string folder, string name, string suffix)
    {
        foreach (string ext in new[] { ".png", ".jpg", ".tga" })
        {
            string path = folder + "/" + name + suffix + ext;
            if (File.Exists(path)) return path;
        }
        return null;
    }
}
