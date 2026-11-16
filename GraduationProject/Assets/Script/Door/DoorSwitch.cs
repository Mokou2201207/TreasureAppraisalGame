using UnityEngine;

/// <summary>
/// ドアを開けたり閉めたりする処理（Animatorのboolで動かすので、どのドアにも使える）
/// </summary>
public class DoorSwitch : MonoBehaviour
{
    [Header("ドアのAnimator（空なら自分か子から探す）"), SerializeField]
    private Animator animator;

    [Header("Animatorのboolの名前"), SerializeField]
    private string openParameter = "isOpen";

    [Header("最初から開いているか"), SerializeField]
    private bool startOpen = false;

    [Header("ドアが動き終わるまでの時間"), SerializeField]
    private float moveTime = 1f;

    [Header("操作できる距離"), SerializeField]
    private float interactDistance = 3f;

    [Header("鍵がかかっているか"), SerializeField]
    private bool isLocked = false;

    [Header("開ける音"), SerializeField]
    private AudioClip openSE;

    [Header("閉める音"), SerializeField]
    private AudioClip closeSE;

    [Header("音の大きさ"), SerializeField, Range(0f, 1f)]
    private float seVolume = 1f;

    private AudioSource audioSource;
    //次に押せるようになる時間
    private float nextToggleTime = 0f;

    //今ドアが開いているか
    public bool IsOpen { get; private set; }
    //今鍵がかかっているか
    public bool IsLocked => isLocked;
    //操作できる距離
    public float InteractDistance => interactDistance;

    /// <summary>
    /// 開始
    /// </summary>
    private void Start()
    {
        //Animatorが入っていなければ自分か子から探す
        if (animator == null) animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        //最初の開け閉めの状態をAnimatorにもセット
        IsOpen = startOpen;
        if (animator != null) animator.SetBool(openParameter, IsOpen);
    }

    /// <summary>
    /// ドアの開け閉めを切りかえる処理（PlayerLookから呼ぶ）
    /// </summary>
    public void ToggleDoor()
    {
        if (IsOpen) CloseDoor();
        else OpenDoor();
    }

    /// <summary>
    /// ドアを開ける処理
    /// </summary>
    public void OpenDoor()
    {
        //もう開いている、または動いている途中なら何もしない
        if (IsOpen || Time.time < nextToggleTime) return;

        SetDoorState(true);
    }

    /// <summary>
    /// ドアを閉める処理
    /// </summary>
    public void CloseDoor()
    {
        //もう閉まっている、または動いている途中なら何もしない
        if (!IsOpen || Time.time < nextToggleTime) return;

        SetDoorState(false);
    }

    /// <summary>
    /// 鍵をかけたり外したりする処理
    /// </summary>
    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    /// <summary>
    /// ドアの状態を変えて、アニメーションと音を出す処理
    /// </summary>
    private void SetDoorState(bool open)
    {
        //動き終わるまで次の操作を受け付けないようにする
        IsOpen = open;
        nextToggleTime = Time.time + moveTime;

        //Animatorのboolを切りかえてアニメーションを動かす
        if (animator != null) animator.SetBool(openParameter, open);

        PlaySE(open ? openSE : closeSE);
    }

    /// <summary>
    /// SEを鳴らす処理
    /// </summary>
    private void PlaySE(AudioClip clip)
    {
        //音が入っていなければ鳴らさない
        if (clip == null) return;

        //AudioSourceがなければ付けて、ドアの場所から聞こえるようにする
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
        audioSource.PlayOneShot(clip, seVolume);
    }
}
