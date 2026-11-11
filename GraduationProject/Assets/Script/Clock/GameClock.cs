using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// お店の時間を進めて、閉店時間になったらお客さんを帰す
/// </summary>
public class GameClock : MonoBehaviour
{
    [Header("看板（OpenSignSwitch）をアタッチ"), SerializeField]
    private OpenSignSwitch sign;

    [Header("時刻を表示するText（Canvas）"), SerializeField]
    private Text timeText;

    [Header("開店の時間（時）"), SerializeField]
    private int startHour = 8;

    [Header("閉店の時間（時）"), SerializeField]
    private int endHour = 21;

    [Header("1秒で進む分数"), SerializeField]
    private float minutesPerSecond = 0.87f;

    private float currentMinutes;
    private bool isRunning = false;

    public int Day { get; private set; } = 1;
    public bool IsClosingTime { get; private set; } = false;

    private void Start()
    {
        currentMinutes = startHour * 60f;
        UpdateText();
    }

    private void Update()
    {
        if (!isRunning && !IsClosingTime && sign != null && sign.isOpen)
        {
            isRunning = true;
        }

        if (isRunning)
        {
            UpdateTime();
        }
    }

    private void UpdateTime()
    {
        currentMinutes += minutesPerSecond * Time.deltaTime;

        if (currentMinutes >= endHour * 60f)
        {
            currentMinutes = endHour * 60f;
            isRunning = false;
            IsClosingTime = true;
            SendEveryoneHome();
        }

        UpdateText();
    }

    /// <summary>
    /// 次の日を始める（ベッドで寝たあとに呼ぶ。時間は開店時間に戻して止めておく）
    /// </summary>
    public void StartNewDay()
    {
        Day++;
        currentMinutes = startHour * 60f;
        isRunning = false;
        IsClosingTime = false;
        UpdateText();
    }

    /// <summary>
    /// 列に並んでいるお客さんを全員帰らせる
    /// </summary>
    private void SendEveryoneHome()
    {
        foreach (MoveWaypoint customer in FindObjectsOfType<MoveWaypoint>())
        {
            customer.GoHome();
        }
    }

    private void UpdateText()
    {
        if (timeText == null) return;

        int hour = (int)(currentMinutes / 60f);
        int minute = (int)(currentMinutes % 60f);
        timeText.text = $"{hour:00}:{minute:00}";
    }
}
