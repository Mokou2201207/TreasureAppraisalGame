using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 画面右下に、今できる操作とキーを表示する※AI担当
/// </summary>
public class KeyHintUI : MonoBehaviour
{
    [Header("行を並べる場所")]
    [SerializeField] private Transform rowParent;
    [Header("行のプレハブ")]
    [SerializeField] private KeyHintRow rowPrefab;

    [Header("キーのアイコン")]
    [SerializeField] private Sprite leftClickIcon;
    [SerializeField] private Sprite rightClickIcon;
    [SerializeField] private Sprite wheelIcon;
    [SerializeField] private Sprite eKeyIcon;
    [SerializeField] private Sprite rKeyIcon;
    [SerializeField] private Sprite tabKeyIcon;
    [SerializeField] private Sprite qKeyIcon;
    [SerializeField] private Sprite escKeyIcon;

    [Header("カーソルの先を調べる距離（PlayerLookが見つからないときに使う）")]
    [SerializeField] private float lookDistance = 2f;

    [Header("キーの表示が出ている物を光らせる（Quick Outline）")]
    [SerializeField] private bool useOutline = true;
    [SerializeField] private Color outlineColor = new Color(1f, 0.85f, 0.35f);
    [SerializeField, Range(0f, 10f)] private float outlineWidth = 5f;
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineVisible;

    // 今表示している状態
    private string currentState = "";
    private InventoryUI inventoryUI;
    private PlayerLook playerLook;

    // 今光らせている物
    private GameObject outlineTarget;
    private Outline currentOutline;
    // Read/Writeが無効で光らせられないと、もう知らせた物
    private readonly HashSet<GameObject> warnedTargets = new HashSet<GameObject>();

    // PlayerLookのRayの長さ（rayDistance）を読み取るための情報
    private static readonly FieldInfo rayDistanceField =
        typeof(PlayerLook).GetField("rayDistance", BindingFlags.NonPublic | BindingFlags.Instance);

    // Outlineの見え方をすぐ反映するための処理（Quick Outline の中にある）
    private static readonly MethodInfo updateOutlineMethod =
        typeof(Outline).GetMethod("UpdateMaterialProperties", BindingFlags.NonPublic | BindingFlags.Instance);

    private void Update()
    {
        string state = GetState(out GameObject target);
        SetOutlineTarget(useOutline ? target : null);

        // 状態が変わったときだけ、表示を作り直す
        if (state == currentState) return;
        currentState = state;
        ShowHints(state);
    }

    /// <summary>
    /// 今どの状態かを調べる（上にあるものほど優先）
    /// </summary>
    private string GetState(out GameObject target)
    {
        target = null;

        if (inventoryUI == null) inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null && inventoryUI.IsOpen) return "Inventory";
        if (AuctionBoardUI.Instance != null && AuctionBoardUI.Instance.IsOpen) return "BoardOpen";

        if (InspectionManager.Instance != null && InspectionManager.Instance.IsInspecting)
        {
            return InspectionManager.Instance.IsSpeechBubbleOpen ? "InspectingTalk" : "Inspecting";
        }
        if (NegotiationManager.Instance != null && NegotiationManager.Instance.IsNegotiating) return "Negotiating";

        // 画面中央の先に何があるか調べる
        Camera cam = Camera.main;
        if (cam == null) return "None";
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, GetLookDistance())) return "None";

        // PlayerLookと同じく、列の先頭で止まっているお客さんだけを対象にする
        MoveWaypoint customer = hit.collider.GetComponentInParent<MoveWaypoint>();
        if (customer != null)
        {
            // 歩いている途中で一瞬立ち止まっただけのときは出さない（カウンターに着いたときだけ）
            if (customer.queuePositionNumber == 0 && !customer.isMoving && customer.isStopped)
            {
                target = customer.gameObject;
                return "Customer";
            }
            return "None";
        }

        ItemData item = hit.collider.GetComponentInParent<ItemData>();
        if (item != null)
        {
            target = item.gameObject;
            return "Item";
        }

        // ラジオは操作できる距離の中にあるときだけ出す
        RadioMusic radio = hit.collider.GetComponentInParent<RadioMusic>();
        if (radio != null)
        {
            if (hit.distance > radio.InteractDistance) return "None";
            target = radio.gameObject;
            return radio.IsPowerOn ? "RadioOn" : "RadioOff";
        }

        SceneTeleporter car = hit.collider.GetComponentInParent<SceneTeleporter>();
        if (car != null)
        {
            target = car.gameObject;
            return "Car";
        }

        // PlayerLookと同じく、レイヤーで掲示板かどうかを見分ける
        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("AuctionBoard"))
        {
            target = GetLayerRoot(hit.collider.transform);
            return "Board";
        }

        return "None";
    }

    /// <summary>
    /// 同じレイヤーが続くところまで親をたどって、物全体を返す（掲示板の親など）
    /// </summary>
    private GameObject GetLayerRoot(Transform t)
    {
        int layer = t.gameObject.layer;
        while (t.parent != null && t.parent.gameObject.layer == layer)
        {
            t = t.parent;
        }
        return t.gameObject;
    }

    /// <summary>
    /// 光らせる物を切りかえる（前の物は消して、新しい物に付ける）
    /// </summary>
    private void SetOutlineTarget(GameObject target)
    {
        if (target == outlineTarget) return;

        if (currentOutline != null) currentOutline.enabled = false;
        outlineTarget = target;
        currentOutline = null;

        if (target == null) return;

        // 付いていなければ、そのときに付ける
        currentOutline = target.GetComponent<Outline>();
        if (currentOutline == null)
        {
            // 形のデータが読めないモデルに付けるとエラーで止まるので、付けずに知らせる
            if (!CanOutline(target)) return;
            currentOutline = target.AddComponent<Outline>();
        }

        currentOutline.OutlineMode = outlineMode;
        currentOutline.OutlineColor = outlineColor;
        currentOutline.OutlineWidth = outlineWidth;
        currentOutline.enabled = true;

        // 付けた直後の1フレームだけ、カウンターを貫通して光るのを防ぐ
        if (updateOutlineMethod != null) updateOutlineMethod.Invoke(currentOutline, null);
    }

    private void OnDisable()
    {
        SetOutlineTarget(null);
    }

    /// <summary>
    /// 光らせる物のモデルが、全部 Read/Write 有効になっているか調べる
    /// </summary>
    private bool CanOutline(GameObject target)
    {
        foreach (MeshFilter mf in target.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh != null && !mf.sharedMesh.isReadable) return WarnNotReadable(target, mf.sharedMesh);
        }
        foreach (SkinnedMeshRenderer sr in target.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (sr.sharedMesh != null && !sr.sharedMesh.isReadable) return WarnNotReadable(target, sr.sharedMesh);
        }
        return true;
    }

    /// <summary>
    /// Read/Writeが無効なモデルを、1つにつき1回だけ知らせる
    /// </summary>
    private bool WarnNotReadable(GameObject target, Mesh mesh)
    {
        if (warnedTargets.Add(target))
        {
            Debug.LogWarning($"[KeyHintUI] {target.name} のモデル「{mesh.name}」は Read/Write が無効なので光らせません。モデルの Import Settings で Read/Write Enabled にチェックを入れてください。");
        }
        return false;
    }

    /// <summary>
    /// PlayerLookと同じRayの長さを返す（クリックできる距離と表示をそろえるため）
    /// </summary>
    private float GetLookDistance()
    {
        // プレイヤーはあとからスポーンするので、見つかるまで探す
        if (playerLook == null) playerLook = FindObjectOfType<PlayerLook>();

        if (playerLook != null && rayDistanceField != null)
        {
            return (float)rayDistanceField.GetValue(playerLook);
        }
        return lookDistance;
    }

    /// <summary>
    /// 状態に合わせて行を並べ直す
    /// </summary>
    private void ShowHints(string state)
    {
        if (rowParent == null || rowPrefab == null) return;

        // 前の行を消す
        foreach (Transform child in rowParent)
        {
            Destroy(child.gameObject);
        }

        switch (state)
        {
            case "Inventory":
                AddRow("閉じる", tabKeyIcon);
                break;
            case "Inspecting":
                AddRow("回す（ドラッグ）", leftClickIcon);
                AddRow("拡大・縮小", wheelIcon);
                AddRow("向きを戻す", rKeyIcon);
                AddRow("お客さんの話を聞く", qKeyIcon);
                AddRow("鑑定を終える", rightClickIcon);
                break;
            case "InspectingTalk":
                AddRow("回す（ドラッグ）", leftClickIcon);
                AddRow("拡大・縮小", wheelIcon);
                AddRow("向きを戻す", rKeyIcon);
                AddRow("話を閉じる", qKeyIcon);
                AddRow("鑑定を終える", rightClickIcon);
                break;
            case "Negotiating":
                AddRow("ボタンで値段を決める", leftClickIcon);
                AddRow("鑑定に戻る", rightClickIcon);
                break;
            case "Customer":
                AddRow("お宝を見せてもらう", leftClickIcon);
                break;
            case "Item":
                AddRow("鑑定する", leftClickIcon);
                break;
            case "RadioOn":
                AddRow("曲を変える", leftClickIcon);
                AddRow("ラジオを切る", eKeyIcon);
                break;
            case "RadioOff":
                AddRow("ラジオをつける", eKeyIcon);
                break;
            case "Car":
                AddRow("オークションへ行く", leftClickIcon);
                break;
            case "Board":
                AddRow("掲示板を見る", leftClickIcon);
                break;
            case "BoardOpen":
                AddRow("閉じる", escKeyIcon);
                break;
        }
    }

    /// <summary>
    /// 1行追加する
    /// </summary>
    private void AddRow(string label, Sprite icon)
    {
        KeyHintRow row = Instantiate(rowPrefab, rowParent);
        row.Setup(label, icon);
    }
}
