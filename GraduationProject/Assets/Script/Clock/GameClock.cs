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

    [Header("太陽（Directional Light）"), SerializeField]
    private Light sunLight;

    [Header("夜の空の色"), SerializeField]
    private Color nightTint = new Color(0.15f, 0.17f, 0.25f);

    [Header("夜の空の明るさ"), SerializeField]
    private float nightExposure = 0.3f;

    [Header("夜の太陽の明るさ"), SerializeField]
    private float nightSunIntensity = 0.2f;

    [Header("夕焼けの空と太陽の色"), SerializeField]
    private Color sunsetTint = new Color(1f, 0.6f, 0.45f);

    [Header("夕焼けが始まる時間（時）"), SerializeField]
    private float sunsetStartHour = 16f;

    [Header("夕焼けの色になる時間（時）"), SerializeField]
    private float sunsetPeakHour = 17f;

    [Header("暗くなる時間（時）"), SerializeField]
    private float nightHour = 19f;

    private Material skyMaterial;
    private Color dayTint;
    private float dayExposure;
    private float daySunIntensity;
    private Color dayLightColor;

    public int Day { get; private set; } = 1;
    public bool IsClosingTime { get; private set; } = false;

    private void Start()
    {
        // 時計を開店の時間に合わせる
        currentMinutes = startHour * 60f;
        UpdateText();

        // 入れ忘れていても、シーンの太陽を探す
        if (sunLight == null) sunLight = RenderSettings.sun;

        if (RenderSettings.skybox != null)
        {
            // アセットの空をそのまま書きかえないように、コピーを作って使う
            skyMaterial = new Material(RenderSettings.skybox);

            // 色と明るさを変えられるシェーダーに切りかえる
            Shader sixSided = Shader.Find("Skybox/6 Sided");
            if (sixSided != null) skyMaterial.shader = sixSided;
            RenderSettings.skybox = skyMaterial;

            // 昼の色と明るさを覚えておく（変えられない空なら、空の変化はあきらめる）
            if (skyMaterial.HasProperty("_Tint") && skyMaterial.HasProperty("_Exposure"))
            {
                dayTint = skyMaterial.GetColor("_Tint");
                dayExposure = skyMaterial.GetFloat("_Exposure");
            }
            else
            {
                skyMaterial = null;
            }
        }

        // 昼の太陽の明るさと色を覚えておく
        if (sunLight != null)
        {
            daySunIntensity = sunLight.intensity;
            dayLightColor = sunLight.color;
        }
    }

    private void Update()
    {
        // 看板をOpenにしたら時間が動き出す
        if (!isRunning && !IsClosingTime && sign != null && sign.isOpen)
        {
            isRunning = true;
        }

        if (isRunning)
        {
            UpdateTime();
        }

        // 時間が止まっていても、空は今の時刻の見た目にしておく
        UpdateSky();
    }

    private void UpdateTime()
    {
        // 時間を進める
        currentMinutes += minutesPerSecond * Time.deltaTime;

        // 閉店の時間になったら、時間を止めて閉店の合図を出す
        if (currentMinutes >= endHour * 60f)
        {
            currentMinutes = endHour * 60f;
            isRunning = false;
            IsClosingTime = true;
        }

        UpdateText();
    }

    /// <summary>
    /// 次の日を始める（ベッドで寝たあとに呼ぶ。時間は開店時間に戻して止めておく）
    /// </summary>
    public void StartNewDay()
    {
        // 日にちを1つ進めて、時計を開店の時間に戻す
        Day++;
        currentMinutes = startHour * 60f;

        // 看板をOpenにするまでは時間を止めておく
        isRunning = false;
        IsClosingTime = false;
        UpdateText();
    }

    /// <summary>
    /// 時間に合わせて、空の色・明るさ・太陽の向きを変える
    /// </summary>
    private void UpdateSky()
    {
        float hour = currentMinutes / 60f;

        // 1日の進み具合（太陽の向きに使う）
        float t = Mathf.InverseLerp(startHour, endHour, hour);

        // 夕焼けの濃さと、暗さの進み具合
        float sunset = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(sunsetStartHour, sunsetPeakHour, hour));
        float dark = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(sunsetPeakHour, nightHour, hour));

        if (skyMaterial != null)
        {
            // 昼の色から夕焼けの色へ、そこから夜の色へ変えていく
            Color tint = Color.Lerp(dayTint, sunsetTint, sunset);
            skyMaterial.SetColor("_Tint", Color.Lerp(tint, nightTint, dark));
            skyMaterial.SetFloat("_Exposure", Mathf.Lerp(dayExposure, nightExposure, dark));
        }

        if (sunLight != null)
        {
            // 夜に近づくほど太陽を暗くして、夕方は光の色を夕焼けに寄せる
            sunLight.intensity = Mathf.Lerp(daySunIntensity, nightSunIntensity, dark);
            sunLight.color = Color.Lerp(dayLightColor, sunsetTint, sunset);

            // 朝から夜へ、太陽を少しずつ動かす
            sunLight.transform.rotation = Quaternion.Euler(50f, Mathf.Lerp(-30f, 210f, t), 0f);
        }
    }

    private void UpdateText()
    {
        if (timeText == null) return;

        // 分の合計を「時」と「分」に分けて、00:00の形で出す
        int hour = (int)(currentMinutes / 60f);
        int minute = (int)(currentMinutes % 60f);
        timeText.text = $"{hour:00}:{minute:00}";
    }
}
