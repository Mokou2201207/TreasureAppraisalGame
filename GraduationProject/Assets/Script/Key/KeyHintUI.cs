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

    [Header("カーソルの先を調べる距離（PlayerLookが見つからないときに使う）")]
    [SerializeField] private float lookDistance = 2f;

    // 今表示している状態
    private string currentState = "";
    private InventoryUI inventoryUI;
    private PlayerLook playerLook;

    // PlayerLookのRayの長さ（rayDistance）を読み取るための情報
    private static readonly FieldInfo rayDistanceField =
        typeof(PlayerLook).GetField("rayDistance", BindingFlags.NonPublic | BindingFlags.Instance);

    private void Update()
    {
        string state = GetState();

        // 状態が変わったときだけ、表示を作り直す
        if (state == currentState) return;
        currentState = state;
        ShowHints(state);
    }

    /// <summary>
    /// 今どの状態かを調べる（上にあるものほど優先）
    /// </summary>
    private string GetState()
    {
        if (inventoryUI == null) inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null && inventoryUI.IsOpen) return "Inventory";

        if (InspectionManager.Instance != null && InspectionManager.Instance.IsInspecting) return "Inspecting";
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
            return (customer.queuePositionNumber == 0 && !customer.isMoving) ? "Customer" : "None";
        }

        if (hit.collider.GetComponentInParent<ItemData>() != null) return "Item";

        // ラジオは操作できる距離の中にあるときだけ出す
        RadioMusic radio = hit.collider.GetComponentInParent<RadioMusic>();
        if (radio != null)
        {
            if (hit.distance > radio.InteractDistance) return "None";
            return radio.IsPowerOn ? "RadioOn" : "RadioOff";
        }

        if (hit.collider.GetComponentInParent<SceneTeleporter>() != null) return "Car";

        return "None";
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
                AddRow("鑑定を終える", rightClickIcon);
                break;
            case "Negotiating":
                AddRow("ボタンで値段を決める", leftClickIcon);
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
