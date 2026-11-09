using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 選んだ天井の板の大きさを測って、ぴったりの屋根を作るツール※AI担当
/// </summary>
public class RoofBuilder : EditorWindow
{
    private enum RoofStyle { 切妻, 寄棟 }
    private enum RidgeAxis { 長い方向, X方向, Z方向 }

    private const string SaveFolder = "Assets/Model/Roof";
    private const string AlbedoPath = SaveFolder + "/Roof_Shingle_Albedo.png";
    private const string NormalPath = SaveFolder + "/Roof_Shingle_Normal.png";
    private const string MaterialPath = SaveFolder + "/Roof_Shingle.mat";

    private RoofStyle style = RoofStyle.切妻;
    private RidgeAxis ridgeAxis = RidgeAxis.長い方向;
    private float pitch = 35f;
    private float overhang = 0.4f;
    private float thickness = 0.12f;
    private float heightOffset = 0f;
    private float tileSize = 2f;
    private bool addCollider = false;
    private Material roofMaterial;
    private Material gableMaterial;

    [MenuItem("Tools/屋根を作る")]
    private static void Open()
    {
        GetWindow<RoofBuilder>("屋根を作る");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("ヒエラルキーで天井の板（floor.〇〇）をまとめて選んでから「屋根を作る」を押してください。\nL字の家は、本体と出っぱりを別々に選んで2回作ります。", MessageType.Info);

        style = (RoofStyle)EditorGUILayout.EnumPopup("屋根の形", style);
        ridgeAxis = (RidgeAxis)EditorGUILayout.EnumPopup("てっぺんの線の向き", ridgeAxis);
        pitch = EditorGUILayout.Slider("傾き（度）", pitch, 10f, 60f);
        overhang = EditorGUILayout.Slider("壁からはみ出す長さ", overhang, 0f, 1.5f);
        thickness = EditorGUILayout.Slider("屋根の厚み", thickness, 0.02f, 0.5f);
        heightOffset = EditorGUILayout.FloatField("上下の位置調整", heightOffset);
        tileSize = EditorGUILayout.FloatField("テクスチャー1枚の大きさ", tileSize);
        addCollider = EditorGUILayout.Toggle("Mesh Collider を付ける", addCollider);

        EditorGUILayout.Space();
        roofMaterial = (Material)EditorGUILayout.ObjectField("屋根のマテリアル", roofMaterial, typeof(Material), false);
        gableMaterial = (Material)EditorGUILayout.ObjectField("三角の壁のマテリアル", gableMaterial, typeof(Material), false);
        EditorGUILayout.LabelField("空なら、屋根は板ぶきのマテリアルを自動で作り、三角の壁も屋根と同じにします", EditorStyles.miniLabel);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("屋根を作る", GUILayout.Height(32))) Build();
        }
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void Build()
    {
        GameObject[] selected = Selection.gameObjects;
        Transform parent = selected[0].transform.parent;

        // 家が回っていても合うように、親の向き（横回転だけ）を基準にして測る
        Quaternion frame = Quaternion.Euler(0f, parent != null ? parent.eulerAngles.y : 0f, 0f);
        Quaternion inv = Quaternion.Inverse(frame);

        bool found = false;
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (GameObject go in selected)
        {
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                Bounds b = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z);
                    Vector3 p = inv * mf.transform.TransformPoint(c);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                    found = true;
                }
            }
        }
        if (!found)
        {
            EditorUtility.DisplayDialog("屋根を作る", "選んだ物に形（Mesh Filter）が見つかりませんでした。", "OK");
            return;
        }

        float sizeX = max.x - min.x;
        float sizeZ = max.z - min.z;
        bool ridgeAlongX = ridgeAxis == RidgeAxis.X方向 || (ridgeAxis == RidgeAxis.長い方向 && sizeX >= sizeZ);
        float length = ridgeAlongX ? sizeX : sizeZ;
        float width = ridgeAlongX ? sizeZ : sizeX;

        Mesh mesh = CreateRoofMesh(length, width, ridgeAlongX);

        if (!AssetDatabase.IsValidFolder(SaveFolder)) AssetDatabase.CreateFolder("Assets/Model", "Roof");
        string meshPath = AssetDatabase.GenerateUniqueAssetPath(SaveFolder + "/Roof_" + (parent != null ? parent.name : "House") + ".asset");
        AssetDatabase.CreateAsset(mesh, meshPath);

        Material roofMat = roofMaterial != null ? roofMaterial : GetDefaultMaterial();
        Material gableMat = gableMaterial != null ? gableMaterial : roofMat;

        GameObject roof = new GameObject("Roof");
        Undo.RegisterCreatedObjectUndo(roof, "屋根を作る");
        Vector3 center = new Vector3((min.x + max.x) * 0.5f, max.y + heightOffset, (min.z + max.z) * 0.5f);
        roof.transform.SetPositionAndRotation(frame * center, frame);
        if (parent != null) roof.transform.SetParent(parent, true);

        roof.AddComponent<MeshFilter>().sharedMesh = mesh;
        roof.AddComponent<MeshRenderer>().sharedMaterials = new[] { roofMat, gableMat };
        if (addCollider) roof.AddComponent<MeshCollider>().sharedMesh = mesh;

        Selection.activeGameObject = roof;
        EditorGUIUtility.PingObject(roof);
    }

    /// <summary>
    /// 屋根の形を作る（0番が屋根、1番が三角の壁）
    /// </summary>
    private Mesh CreateRoofMesh(float length, float width, bool ridgeAlongX)
    {
        float half = width * 0.5f;
        float height = half * Mathf.Tan(pitch * Mathf.Deg2Rad);
        float slope = height / half;
        float eaveY = -overhang * slope;
        float endX = length * 0.5f + overhang;
        float endZ = half + overhang;

        // 屋根の上の面を、てっぺんの線をX方向として作る
        List<Vector3[]> faces = new List<Vector3[]>();
        if (style == RoofStyle.切妻)
        {
            faces.Add(new[] { V(-endX, height, 0f), V(endX, height, 0f), V(endX, eaveY, endZ), V(-endX, eaveY, endZ) });
            faces.Add(new[] { V(-endX, height, 0f), V(endX, height, 0f), V(endX, eaveY, -endZ), V(-endX, eaveY, -endZ) });
        }
        else
        {
            float r = Mathf.Max(0f, length * 0.5f - half);
            faces.Add(new[] { V(-r, height, 0f), V(r, height, 0f), V(endX, eaveY, endZ), V(-endX, eaveY, endZ) });
            faces.Add(new[] { V(-r, height, 0f), V(r, height, 0f), V(endX, eaveY, -endZ), V(-endX, eaveY, -endZ) });
            faces.Add(new[] { V(r, height, 0f), V(endX, eaveY, endZ), V(endX, eaveY, -endZ) });
            faces.Add(new[] { V(-r, height, 0f), V(-endX, eaveY, endZ), V(-endX, eaveY, -endZ) });
        }

        if (!ridgeAlongX)
        {
            foreach (Vector3[] f in faces)
            {
                for (int i = 0; i < f.Length; i++) f[i] = Swap(f[i]);
            }
        }

        MeshData roofData = new MeshData(tileSize);
        MeshData gableData = new MeshData(tileSize);
        Vector3 down = Vector3.down * thickness;

        foreach (Vector3[] f in faces)
        {
            Vector3 n = Newell(f);
            if (n.y < 0f) n = -n;
            roofData.AddPolygon(f, n);

            Vector3[] under = new Vector3[f.Length];
            for (int i = 0; i < f.Length; i++) under[i] = f[i] + down;
            roofData.AddPolygon(under, -n);

            // ほかの面とつながっていない辺は、厚みの側面でふさぐ
            Vector3 centroid = Vector3.zero;
            foreach (Vector3 p in f) centroid += p;
            centroid /= f.Length;
            for (int i = 0; i < f.Length; i++)
            {
                Vector3 a = f[i];
                Vector3 b = f[(i + 1) % f.Length];
                if (IsSharedEdge(faces, f, a, b)) continue;
                Vector3 edge = (b - a).normalized;
                Vector3 outward = (a + b) * 0.5f - centroid;
                outward -= edge * Vector3.Dot(outward, edge);
                roofData.AddPolygon(new[] { a, b, b + down, a + down }, outward.normalized);
            }
        }

        // 切妻の両はしの三角の壁（裏からも見えるように両面）
        if (style == RoofStyle.切妻)
        {
            float wallX = length * 0.5f;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3[] tri = { V(s * wallX, 0f, -half), V(s * wallX, 0f, half), V(s * wallX, height, 0f) };
                if (!ridgeAlongX)
                {
                    for (int i = 0; i < tri.Length; i++) tri[i] = Swap(tri[i]);
                }
                Vector3 n = Newell(tri);
                gableData.AddPolygon(tri, n);
                gableData.AddPolygon(tri, -n);
            }
        }

        Mesh mesh = new Mesh { name = "Roof" };
        List<Vector3> verts = new List<Vector3>(roofData.verts);
        verts.AddRange(gableData.verts);
        List<Vector3> normals = new List<Vector3>(roofData.normals);
        normals.AddRange(gableData.normals);
        List<Vector2> uvs = new List<Vector2>(roofData.uvs);
        uvs.AddRange(gableData.uvs);
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(roofData.tris, 0);
        List<int> gableTris = new List<int>();
        foreach (int t in gableData.tris) gableTris.Add(t + roofData.verts.Count);
        mesh.SetTriangles(gableTris, 1);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    private static Vector3 V(float x, float y, float z)
    {
        return new Vector3(x, y, z);
    }

    private static Vector3 Swap(Vector3 p)
    {
        return new Vector3(p.z, p.y, p.x);
    }

    private static Vector3 Newell(Vector3[] poly)
    {
        Vector3 n = Vector3.zero;
        for (int i = 0; i < poly.Length; i++)
        {
            Vector3 a = poly[i];
            Vector3 b = poly[(i + 1) % poly.Length];
            n.x += (a.y - b.y) * (a.z + b.z);
            n.y += (a.z - b.z) * (a.x + b.x);
            n.z += (a.x - b.x) * (a.y + b.y);
        }
        return n.normalized;
    }

    private static bool IsSharedEdge(List<Vector3[]> faces, Vector3[] self, Vector3 a, Vector3 b)
    {
        foreach (Vector3[] f in faces)
        {
            if (f == self) continue;
            for (int i = 0; i < f.Length; i++)
            {
                Vector3 c = f[i];
                Vector3 d = f[(i + 1) % f.Length];
                if ((Near(a, c) && Near(b, d)) || (Near(a, d) && Near(b, c))) return true;
            }
        }
        return false;
    }

    private static bool Near(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude < 0.0001f;
    }

    /// <summary>
    /// 板ぶきのマテリアルを探す（なければ作る）
    /// </summary>
    private static Material GetDefaultMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;

        TextureImporter normalImporter = AssetImporter.GetAtPath(NormalPath) as TextureImporter;
        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader);

        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        if (albedo != null)
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTexture("_MainTex", albedo);
        }
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.SetFloat("_Smoothness", 0.15f);
        mat.SetFloat("_Glossiness", 0.15f);

        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }

    /// <summary>
    /// 頂点と三角形をためておく入れ物
    /// </summary>
    private class MeshData
    {
        public readonly List<Vector3> verts = new List<Vector3>();
        public readonly List<Vector3> normals = new List<Vector3>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public readonly List<int> tris = new List<int>();
        private readonly float tile;

        public MeshData(float tileSize)
        {
            tile = Mathf.Max(0.01f, tileSize);
        }

        /// <summary>
        /// 平らな多角形を足す（表が normal の向きになるようにそろえる）
        /// </summary>
        public void AddPolygon(Vector3[] poly, Vector3 normal)
        {
            // テクスチャーの横は水平、縦は斜面を登る向きにする
            Vector3 u = Vector3.Cross(Vector3.up, normal);
            if (u.sqrMagnitude < 0.0001f) u = Vector3.right;
            u.Normalize();
            Vector3 v = Vector3.Cross(normal, u).normalized;

            int start = verts.Count;
            foreach (Vector3 p in poly)
            {
                verts.Add(p);
                normals.Add(normal);
                uvs.Add(new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, v)) / tile);
            }

            for (int i = 1; i < poly.Length - 1; i++)
            {
                Vector3 a = poly[0];
                Vector3 b = poly[i];
                Vector3 c = poly[i + 1];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f)
                {
                    tris.Add(start); tris.Add(start + i); tris.Add(start + i + 1);
                }
                else
                {
                    tris.Add(start); tris.Add(start + i + 1); tris.Add(start + i);
                }
            }
        }
    }
}
