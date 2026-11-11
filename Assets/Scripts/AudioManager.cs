using UnityEngine;

// Taruh file audio di Assets/Resources/Audio/ dengan nama:
// bgm, sfx_click, sfx_buy, sfx_plant, sfx_harvest  (format .mp3 / .wav / .ogg)
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    AudioSource musicSource;
    AudioSource sfxSource;
    AudioClip click, buy, plant, harvest, water;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        new GameObject("AudioManager").AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        click = Resources.Load<AudioClip>("Audio/sfx_click") ?? Synth("click", new[] { 880f, 1320f }, 0.06f);
        buy = Resources.Load<AudioClip>("Audio/sfx_buy") ?? Synth("buy", new[] { 988f, 1319f }, 0.09f);
        plant = Resources.Load<AudioClip>("Audio/sfx_plant") ?? Synth("plant", new[] { 330f, 262f }, 0.08f);
        harvest = Resources.Load<AudioClip>("Audio/sfx_harvest") ?? Synth("harvest", new[] { 523f, 659f, 784f, 1047f }, 0.08f);
        water = Resources.Load<AudioClip>("Audio/sfx_water") ?? SynthSplash(1.1f);

        AudioClip bgm = Resources.Load<AudioClip>("Audio/bgm");
        if (bgm != null)
        {
            musicSource.clip = bgm;
            musicSource.Play();
        }
    }

    public void PlayClick() => Play(click);
    public void PlayBuy() => Play(buy);
    public void PlayPlant() => Play(plant);
    public void PlayHarvest() => Play(harvest);
    public void PlayWater() => Play(water);

    // Desis air sederhana: noise yang difilter, naik lalu memudar
    static AudioClip SynthSplash(float length)
    {
        const int rate = 22050;
        int n = (int)(rate * length);
        float[] data = new float[n];
        var rng = new System.Random(7);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float env = Mathf.Clamp01(t * 12f) * (1f - t) * (1f - t);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * 0.35f;
            float trickle = 0.6f + 0.4f * Mathf.Sin(i * 0.0021f) * Mathf.Sin(i * 0.0007f);
            data[i] = lp * env * trickle * 0.5f;
        }
        AudioClip clip = AudioClip.Create("synth_water", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void Play(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // Bunyi "blip" sederhana sebagai pengganti kalau file SFX belum ada
    static AudioClip Synth(string name, float[] notes, float noteLen)
    {
        const int rate = 44100;
        int perNote = (int)(rate * noteLen);
        float[] data = new float[perNote * notes.Length];
        for (int n = 0; n < notes.Length; n++)
        {
            for (int i = 0; i < perNote; i++)
            {
                float t = (float)i / rate;
                float env = 1f - (float)i / perNote;
                float wave = Mathf.Sin(2f * Mathf.PI * notes[n] * t) > 0 ? 1f : -1f;
                data[n * perNote + i] = wave * env * env * 0.25f;
            }
        }
        AudioClip clip = AudioClip.Create("synth_" + name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
