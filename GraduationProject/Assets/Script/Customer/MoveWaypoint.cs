using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPCの移動プログラム
/// </summary>
public class MoveWaypoint : MonoBehaviour
{
    [Header("WaypointManagerのscriptをアタッチ"), SerializeField]
    private WaypointManager manager;

    [Header("NavMeshAgentをアタッチ"), SerializeField]
    private NavMeshAgent agent;

    [Header("どのWaypointに向かっているか"), SerializeField]
    private int currentIndex = 0;

    [Header("何番目のポイントについたら停止するか"), SerializeField]
    private int stopIndex = -1;

    [Header("前の客との車間距離"), SerializeField]
    private float stopDistanceToFrontCustomer = 1.8f;

    [Header("自分が何番目のお客さんか"), SerializeField]
    public int queuePositionNumber = 0;

    /// 経路が確定して移動中かどうか
    public bool isMoving = false;
    // 停止中かどうか（最終目的地に到着して完了した状態）
    public bool isStopped = false;

    // 列で前のお客さんの後ろに並んで待機中かどうか（ONの間はアニメーションがIdle固定）
    public bool isWaitingInQueue = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            // 横に回避して二列になるのを防ぐため、回避挙動を無効化（一列に直進させる）
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        if (manager == null)
        {
            manager = FindClosestManager();
        }
    }

    /// <summary>
    /// 開始
    /// </summary>
    [System.Obsolete]
    private void Start()
    {
        // マネージャー設定なければ最も近いものを探す
        if (manager == null)
        {
            manager = FindClosestManager();
            if (manager == null)
            {
                Debug.LogError("WaypointManagerが見つかりません。");
            }
        }

        // もし停止するポイントが設定されていなければ最後のポイントに設定
        if (stopIndex < 0 && manager != null && manager.m_Waypoints != null && manager.m_Waypoints.Length > 0)
        {
            stopIndex = manager.m_Waypoints.Length - 1;
        }

        // 最初のPointへ
        MoveToNextPoint();
    }

    /// <summary>
    /// 更新
    /// </summary>
    private void Update()
    {
        // 最終目的地で停止中なら何もしない
        if (isStopped) return;

        // 列で待機中なら何もしない
        if (isWaitingInQueue) return;

        // 前方に他の客がいる場合は一時停止し、いなくなったら再開する（退場中のお客さんは除く）
        if (queuePositionNumber >= 0 && CheckCustomerAhead())
        {
            if (agent != null && agent.enabled && !agent.isStopped)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            isMoving = false;
            isWaitingInQueue = true;
            return;
        }
        else
        {
            if (agent != null && agent.enabled && agent.isStopped)
            {
                agent.isStopped = false;
            }
        }

        // NavMeshが有効かどうか
        if (agent != null && agent.enabled)
        {
            // まだ経路計算中なら何もしない
            if (agent.pathPending)
                return;

            // 経路が確定したらフラグをON
            if (!isMoving && agent.hasPath)
            {
                isMoving = true;
            }

            // 移動中かつ目的地に到着したら次のPointへ
            float arrivalDistance = Mathf.Max(agent.stoppingDistance, 0.25f);
            if (isMoving && agent.remainingDistance <= arrivalDistance)
            {
                // 会計が終わり、最後のポイント（出口）に到着した場合は削除する
                if (queuePositionNumber < 0 && (currentIndex - 1) >= stopIndex)
                {
                    isMoving = false;
                    Debug.Log("【退場完了】お客さんが最後まで到着したため削除します。");
                    Destroy(gameObject);
                    return;
                }

                if ((currentIndex - 1) == stopIndex)
                {
                    // 指定ポイントに到着したので停止する
                    StopAtTarget();
                }
                else
                {
                    // まだ目的地があるので次のポイントへ
                    MoveToNextPoint();
                }
            }
        }
    }

    /// <summary>
    /// 前方に他の客が居るかチェック（前方範囲の判定）
    /// </summary>
    private bool CheckCustomerAhead()
    {
        // 前方へ少し進んだ位置を中心に円で検知して確実に前のお客さんを捕らえる
        Vector3 checkCenter = transform.position + Vector3.up * 0.5f + transform.forward * (stopDistanceToFrontCustomer * 0.5f);
        Collider[] hitColliders = Physics.OverlapSphere(checkCenter, stopDistanceToFrontCustomer * 0.6f);

        foreach (var hit in hitColliders)
        {
            MoveWaypoint otherCustomer = hit.GetComponentInParent<MoveWaypoint>();
            // 自分以外の「列に並んでいる最中の客」が前にいる場合のみ停止対象にする
            if (hit.gameObject != gameObject && otherCustomer != null && otherCustomer.gameObject != gameObject && otherCustomer.queuePositionNumber >= 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 会計完了処理（先頭の客が対応終了した時）
    /// </summary>
    public void FinishCheckout()
    {
        // 既に列を離脱している場合は処理しない
        if (queuePositionNumber < 0) return;

        Debug.Log($"【会計完了】客 (元Queue: {queuePositionNumber}) の会計が完了しました。退場します。");

        // 列から離脱
        queuePositionNumber = -1;
        isStopped = false;
        isMoving = false;
        isWaitingInQueue = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
        }

        // 後ろに並んでいる客を全員1つ前へ詰める
        AdvanceQueue();

        // 最後のウェイポイント（出口）まで進むように停止インデックスを最後のポイントに更新
        if (manager != null && manager.m_Waypoints != null && manager.m_Waypoints.Length > 0)
        {
            stopIndex = manager.m_Waypoints.Length - 1;

            // もし既に最後のウェイポイントにいる場合はその場で削除
            if ((currentIndex - 1) >= stopIndex)
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 退場（最後のポイント）に向けて移動再開
        MoveToNextPoint();
    }

    /// <summary>
    /// 列に並んでいる全員の順番を1つずつ繰り上げて前に詰める
    /// </summary>
    public static void AdvanceQueue()
    {
        MoveWaypoint[] customers = FindObjectsOfType<MoveWaypoint>();
        foreach (var customer in customers)
        {
            // 列の中に並んでいる客（queuePositionNumber >= 1）
            if (customer.queuePositionNumber > 0)
            {
                customer.SetQueuePosition(customer.queuePositionNumber - 1);
                customer.ResumeMove();
            }
        }
    }

    /// <summary>
    /// 一時停止を解除して前進を再開する
    /// </summary>
    public void ResumeMove()
    {
        isStopped = false;
        isMoving = false;
        isWaitingInQueue = false;
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// 現在のPointに着いたら次のPointへ
    /// </summary>
    void MoveToNextPoint()
    {
        if (manager == null || manager.m_Waypoints == null || manager.m_Waypoints.Length == 0)
        {
            Debug.Log("WaypointManagerのscriptが設定されていないか、Waypointsが空です。");
            return;
        }

        // 最後の waypoint に到達チェック
        if (currentIndex >= manager.m_Waypoints.Length)
        {
            // 削除
            Destroy(gameObject);
            return;
        }

        // 次の移動ポイントセット
        agent.destination = manager.m_Waypoints[currentIndex].position;

        // フラグリセット（経路計算待ち）
        isMoving = false;

        // 次の index へ
        currentIndex++;
    }

    /// <summary>
    /// シーン内のWaypointManagerの中で最も近いものを探す
    /// </summary>
    /// <returns>最も近いWaypointManager</returns>
    [System.Obsolete]
    WaypointManager FindClosestManager()
    {
        // シーンにある全てのWaypointManagerを取得
        WaypointManager[] managers = FindObjectsOfType<WaypointManager>();

        // 最も近いものを保存する変数
        WaypointManager closest = null;

        // 最小距離の初期値
        float minDist = Mathf.Infinity;

        // 各WaypointManagerを調べる
        foreach (var m in managers)
        {
            // 距離計算
            float dist = Vector3.Distance(transform.position, m.transform.position);

            // 今までで最も近いものなら更新
            if (dist < minDist)
            {
                minDist = dist;
                closest = m;
            }
        }

        // 最終的に最も近いものを返す
        return closest;
    }

    /// <summary>
    /// ターゲット地点（カウンター前など）に到着して停止するときの処理
    /// </summary>
    void StopAtTarget()
    {
        isStopped = true;
        isMoving = false;

        // エージェントの移動を止める
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        Debug.Log("お客さんがカウンター（指定ポイント）に到着して停止しました。");
    }

    /// <summary>
    /// 外部から並び順を設定するためのメソッド
    /// </summary>
    /// <param name="queueNumber"></param>
    [System.Obsolete]
    public void SetQueuePosition(int queueNumber)
    {
        queuePositionNumber = queueNumber;
    }

    // デバッグ表示：前方の検知範囲をシーンビューに視覚表示
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 checkCenter = transform.position + Vector3.up * 0.5f + transform.forward * (stopDistanceToFrontCustomer * 0.5f);
        Gizmos.DrawWireSphere(checkCenter, stopDistanceToFrontCustomer * 0.6f);
    }
}
