using UnityEngine;
using UnityEngine.UI;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    public Light sunLight;
    public Camera mainCamera;

    public enum TimeOfDay { Pagi, Siang, Sore, Malam }
    public enum Weather { Cerah, Hujan }

    public TimeOfDay currentTime = TimeOfDay.Siang;
    public Weather currentWeather = Weather.Cerah;

    public bool autoCycle = true;
    [Tooltip("Durasi Pagi + Siang + Sore (detik), dibagi rata 3 fase")]
    public float dayDuration = 120f;
    [Tooltip("Durasi Malam (detik)")]
    public float nightDuration = 60f;
    [Tooltip("Kecepatan transisi cahaya antar fase (makin besar makin cepat)")]
    public float transitionSpeed = 0.6f;
    float cycleTimer = 0f;

    struct LightPreset
    {
        public Vector3 sunEuler;     // sudut matahari (X = tinggi, Y = arah)
        public float intensity;
        public Color color;          // warna sinar matahari
        public float shadow;         // kepekatan bayangan
        public Color sky;            // warna langit (background kamera)
        public Color ambSky, ambEquator, ambGround; // cahaya pantul dari langit/horizon/tanah
        public Color fog;
        public float fogDensity;
    }

    static readonly LightPreset[] presets =
    {
        // Pagi: matahari rendah dari timur, hangat keemasan, bayangan panjang, sedikit kabut
        new LightPreset {
            sunEuler = new Vector3(30, -70, 0), intensity = 1.15f, color = new Color(1f, 0.82f, 0.62f), shadow = 0.7f,
            sky = new Color(0.72f, 0.78f, 0.86f),
            ambSky = new Color(0.66f, 0.68f, 0.76f), ambEquator = new Color(0.66f, 0.58f, 0.5f), ambGround = new Color(0.3f, 0.27f, 0.22f),
            fog = new Color(0.9f, 0.84f, 0.76f), fogDensity = 0.006f },
        // Siang: matahari tinggi, putih netral, bayangan pendek & tegas, langit biru
        new LightPreset {
            sunEuler = new Vector3(62, -20, 0), intensity = 1.0f, color = new Color(1f, 0.97f, 0.92f), shadow = 0.85f,
            sky = new Color(0.52f, 0.72f, 0.9f),
            ambSky = new Color(0.6f, 0.68f, 0.78f), ambEquator = new Color(0.48f, 0.52f, 0.52f), ambGround = new Color(0.24f, 0.26f, 0.2f),
            fog = new Color(0.7f, 0.8f, 0.9f), fogDensity = 0.004f },
        // Sore: matahari rendah dari barat, oranye kemerahan, bayangan panjang ke arah sebaliknya
        new LightPreset {
            sunEuler = new Vector3(26, 110, 0), intensity = 1.1f, color = new Color(1f, 0.55f, 0.3f), shadow = 0.75f,
            sky = new Color(0.9f, 0.55f, 0.38f),
            ambSky = new Color(0.62f, 0.5f, 0.52f), ambEquator = new Color(0.7f, 0.5f, 0.36f), ambGround = new Color(0.28f, 0.2f, 0.14f),
            fog = new Color(0.9f, 0.62f, 0.44f), fogDensity = 0.005f },
        // Malam: cahaya bulan biru redup, bayangan samar, gelap kebiruan
        new LightPreset {
            sunEuler = new Vector3(40, 160, 0), intensity = 0.32f, color = new Color(0.55f, 0.62f, 0.9f), shadow = 0.45f,
            sky = new Color(0.03f, 0.04f, 0.12f),
            ambSky = new Color(0.17f, 0.2f, 0.36f), ambEquator = new Color(0.11f, 0.13f, 0.22f), ambGround = new Color(0.03f, 0.03f, 0.06f),
            fog = new Color(0.04f, 0.05f, 0.12f), fogDensity = 0.012f },
    };

    // Rain
    ParticleSystem rainParticle;

    // UI
    public Text timeText;
    public Text weatherText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (sunLight == null)
            sunLight = Object.FindObjectOfType<Light>();
        if (mainCamera == null)
            mainCamera = Camera.main;

        RenderSettings.reflectionIntensity = 0.45f; // kurangi kilap pantulan langit di permukaan
        // Ambient 3 warna (langit/horizon/tanah) supaya warna cahaya ikut fase, bukan dari skybox biru
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        CreateRainSystem();
        ApplyLight(presets[(int)currentTime], 1f);
    }

    float PhaseDuration => currentTime == TimeOfDay.Malam ? nightDuration : dayDuration / 3f;

    // Untuk bar hitung mundur: Pagi+Siang+Sore dihitung sebagai satu "siang"
    public bool IsNight => currentTime == TimeOfDay.Malam;
    public float SwitchTotal => IsNight ? nightDuration : dayDuration;
    public float TimeUntilSwitch
    {
        get
        {
            if (IsNight) return Mathf.Max(0f, nightDuration - cycleTimer);
            float phase = dayDuration / 3f;
            int phasesLeft = 2 - (int)currentTime;
            return Mathf.Max(0f, phase - cycleTimer) + phasesLeft * phase;
        }
    }

    void Update()
    {
        if (autoCycle)
        {
            cycleTimer += Time.deltaTime;
            if (cycleTimer >= PhaseDuration)
            {
                cycleTimer = 0f;
                NextTime();
            }
        }

        ApplyLight(presets[(int)currentTime], 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime));
        UpdateUI();
    }

    void NextTime()
    {
        currentTime = (TimeOfDay)(((int)currentTime + 1) % 4);
    }

    public void SetTime(TimeOfDay time)
    {
        currentTime = time;
        cycleTimer = 0f;
    }

    public void ToggleAutoCycle()
    {
        autoCycle = !autoCycle;
        cycleTimer = 0f;
    }

    // t = 1 langsung ke preset, t kecil = bergerak halus menuju preset
    void ApplyLight(LightPreset p, float t)
    {
        if (sunLight == null) return;
        sunLight.transform.rotation = Quaternion.Slerp(sunLight.transform.rotation, Quaternion.Euler(p.sunEuler), t);
        sunLight.intensity = Mathf.Lerp(sunLight.intensity, p.intensity, t);
        sunLight.color = Color.Lerp(sunLight.color, p.color, t);
        sunLight.shadowStrength = Mathf.Lerp(sunLight.shadowStrength, p.shadow, t);
        RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, p.ambSky, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, p.ambEquator, t);
        RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, p.ambGround, t);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, p.fog, t);
        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, p.fogDensity, t);
        if (mainCamera) mainCamera.backgroundColor = Color.Lerp(mainCamera.backgroundColor, p.sky, t);
    }

    public void SetWeather(Weather weather)
    {
        currentWeather = weather;
        if (rainParticle != null)
        {
            if (weather == Weather.Hujan)
                rainParticle.Play();
            else
                rainParticle.Stop();
        }
    }

    public void ToggleWeather()
    {
        SetWeather(currentWeather == Weather.Cerah ? Weather.Hujan : Weather.Cerah);
    }

    void CreateRainSystem()
    {
        GameObject rainObj = new GameObject("Rain");
        rainObj.transform.SetParent(mainCamera != null ? mainCamera.transform : transform);
        rainObj.transform.localPosition = new Vector3(0, 15, 0);

        rainParticle = rainObj.AddComponent<ParticleSystem>();
        rainParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = rainParticle.main;
        main.startLifetime = 1.5f;
        main.startSpeed = 20f;
        main.startSize = 0.05f;
        main.startColor = new Color(0.7f, 0.8f, 0.9f, 0.6f);
        main.maxParticles = 2000;
        main.loop = true;
        main.playOnAwake = false;
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = rainParticle.emission;
        emission.rateOverTime = 500;

        var shape = rainParticle.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30, 1, 30);

        var renderer = rainObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 5f;

        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader != null)
        {
            Material rainMat = new Material(particleShader);
            rainMat.color = new Color(0.7f, 0.8f, 0.9f, 0.4f);
            renderer.material = rainMat;
        }
    }

    void UpdateUI()
    {
        if (timeText != null)
        {
            string[] timeNames = { "Pagi", "Siang", "Sore", "Malam" };
            string icon = currentTime == TimeOfDay.Malam ? "* " : "o ";
            string timerStr = "";
            if (autoCycle)
            {
                float remaining = Mathf.Max(0f, PhaseDuration - cycleTimer);
                int min = Mathf.FloorToInt(remaining / 60f);
                int sec = Mathf.FloorToInt(remaining % 60f);
                timerStr = " (" + min + ":" + sec.ToString("D2") + ")";
            }
            timeText.text = icon + timeNames[(int)currentTime] + timerStr;
        }
        if (weatherText != null)
        {
            weatherText.text = currentWeather == Weather.Cerah ? "Cerah" : "Hujan";
        }
    }
}
