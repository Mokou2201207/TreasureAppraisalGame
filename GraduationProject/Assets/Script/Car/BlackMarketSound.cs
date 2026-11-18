using UnityEngine;

/// <summary>
/// 闇市の画面を開いている間、マップの音（BGM・鳥の声など）を止めて、閉じたら戻す処理
/// </summary>
public static class BlackMarketSound
{
    //マップの音の大きさを覚えておく（閉じたときに戻すため）
    private static float savedVolume = 1f;
    private static bool isMuted = false;

    /// <summary>
    /// 闇市に入るとき：マップの音を全部止める
    /// </summary>
    public static void Mute()
    {
        if (isMuted) return;

        //全体の音量を0にして、マップの音を止める（ignoreListenerVolumeにチェックした音だけ残る）
        savedVolume = AudioListener.volume;
        AudioListener.volume = 0f;
        isMuted = true;
    }

    /// <summary>
    /// 闇市から出るとき：マップの音を元に戻す
    /// </summary>
    public static void Unmute()
    {
        if (!isMuted) return;

        AudioListener.volume = savedVolume;
        isMuted = false;
    }

    /// <summary>
    /// 闇市の中で効果音を鳴らす処理（マップの音が止まっていても鳴るように別のオブジェクトで鳴らす）
    /// </summary>
    public static void PlayClip(AudioClip clip)
    {
        //音が入っていなければ鳴らさない
        if (clip == null) return;

        GameObject seObject = new GameObject("SE_" + clip.name);
        AudioSource source = seObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.ignoreListenerVolume = true;
        source.PlayOneShot(clip);
        Object.Destroy(seObject, clip.length + 0.5f);
    }
}
