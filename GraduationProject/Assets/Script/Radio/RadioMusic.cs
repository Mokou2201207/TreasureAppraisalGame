using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ラジオを左クリックするたびにBGMを次の曲へ切り替える処理※AI担当
/// </summary>
public class RadioMusic : MonoBehaviour
{
    [Header("ラジオで流すBGM"), SerializeField]
    private AudioClip[] bgmClips;

    [Header("BGMを鳴らすAudioSource（空なら自動で取得）"), SerializeField]
    private AudioSource audioSource;

    [Header("ラジオを操作できる距離"), SerializeField]
    private float interactDistance = 4.0f;

    [Header("ラジオを見たときに出す文字"), SerializeField]
    private string crosshairMessage = "ラジオの曲を変える(左クリック)";

    [Header("クロスヘア用のText（空なら自動で取得）"), SerializeField]
    private Text crosshairText;

    // 今流している曲の番号
    private int currentIndex = 0;
    // カーソルがラジオに合っているかどうか
    private bool isLookingAtRadio = false;
    private Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (audioSource == null)
        {
            Debug.LogError("[RadioMusic] AudioSourceがありません。ラジオにAudioSourceを付けてください。");
            return;
        }

        if (crosshairText == null)
        {
            GameObject textObj = GameObject.Find("CrosshairText");
            if (textObj != null) crosshairText = textObj.GetComponent<Text>();
        }

        mainCamera = Camera.main;

        if (bgmClips == null || bgmClips.Length == 0)
        {
            Debug.LogWarning("[RadioMusic] BGMが登録されていません。");
            return;
        }

        // 最初の曲はランダムに選ぶ
        currentIndex = Random.Range(0, bgmClips.Length);
        PlayCurrent();
    }

    // Update is called once per frame
    void Update()
    {
        isLookingAtRadio = CheckLookingAtRadio();

        if (isLookingAtRadio && Input.GetMouseButtonDown(0))
        {
            PlayNext();
        }
    }

    private void LateUpdate()
    {
        // PlayerLookが毎フレーム文字を消すので、そのあとに表示する
        if (isLookingAtRadio && crosshairText != null)
        {
            crosshairText.text = crosshairMessage;
        }
    }

    /// <summary>
    /// 画面中央の先にこのラジオがあるか調べる
    /// </summary>
    private bool CheckLookingAtRadio()
    {
        // 鑑定中や交渉中などカーソルを出している間は操作しない
        if (InspectionManager.Instance != null && InspectionManager.Instance.IsInspecting) return false;
        if (Cursor.lockState != CursorLockMode.Locked) return false;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return false;

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            return hit.collider.GetComponentInParent<RadioMusic>() == this;
        }
        return false;
    }

    /// <summary>
    /// 次の曲へ切り替える（最後の曲の次は最初の曲に戻る）
    /// </summary>
    public void PlayNext()
    {
        if (audioSource == null || bgmClips == null || bgmClips.Length == 0) return;

        currentIndex = (currentIndex + 1) % bgmClips.Length;
        PlayCurrent();
    }

    /// <summary>
    /// 今の番号の曲をループで流す
    /// </summary>
    private void PlayCurrent()
    {
        audioSource.clip = bgmClips[currentIndex];
        audioSource.loop = true;
        audioSource.Play();
    }
}
