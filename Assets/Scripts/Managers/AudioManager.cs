using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    [SerializeField] private AudioClip musicHome;
    [SerializeField] private AudioClip musicGame;

    [Header("SFX")]
    [SerializeField] private AudioClip click;
    [SerializeField] private AudioClip pickup;
    [SerializeField] private AudioClip drop;
    [SerializeField] private AudioClip merge;
    [SerializeField] private AudioClip win;
    [SerializeField] private AudioClip star;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        sfxSource = gameObject.AddComponent<AudioSource>();

        ApplySettings();
    }

    private void ApplySettings()
    {
        musicSource.volume = PlayerPrefs.GetInt("MusicOn", 1) == 1 ? 0.6f : 0f;
        sfxSource.volume = PlayerPrefs.GetInt("SoundOn", 1) == 1 ? 1f : 0f;
    }

    public void RefreshSettings()
    {
        ApplySettings();
    }

    public void PlayHomeMusic() { PlayMusic(musicHome); }
    public void PlayGameMusic() { PlayMusic(musicGame); }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlayClick() { PlaySFX(click); }
    public void PlayPickup() { PlaySFX(pickup); }
    public void PlayDrop() { PlaySFX(drop); }
    public void PlayMerge() { PlaySFX(merge); }
    public void PlayWin() { PlaySFX(win); }
    public void PlayStar() { PlaySFX(star); }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }
}