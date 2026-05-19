using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class WorldAreaMood2D : MonoBehaviour
{
    [Header("Area")]
    [SerializeField] private string areaId = "french-card-town";
    [SerializeField] private Vector2 triggerSize = new Vector2(13f, 11f);
    [SerializeField] private Vector2 triggerOffset = Vector2.zero;
    [SerializeField] private float transitionSpeed = 2.75f;

    [Header("Palette")]
    [SerializeField] private Color outsideCameraColor = new Color32(128, 176, 197, 255);
    [SerializeField] private Color areaCameraColor = new Color32(196, 178, 145, 255);

    [Header("Procedural Music")]
    [SerializeField] private bool playProceduralMusic = true;
    [SerializeField] private float outsideToneHz = 220f;
    [SerializeField] private float areaToneHz = 330f;
    [SerializeField] private float musicVolume = 0.035f;

    private const int SampleRate = 44100;
    private const float ClipLengthSeconds = 2f;

    private PlayerMovement2D cachedPlayer;
    private Camera cachedCamera;
    private AudioSource outsideSource;
    private AudioSource areaSource;
    private AudioClip outsideClip;
    private AudioClip areaClip;
    private float blend;
    private bool isPlayerInside;

    public string AreaId => areaId;
    public bool IsPlayerInside => isPlayerInside;

    private void Awake()
    {
        EnsureTriggerCollider(true);
    }

    private void OnEnable()
    {
        EnsureTriggerCollider(true);
    }

    private void Reset()
    {
        EnsureTriggerCollider(true);
    }

    private void OnValidate()
    {
        EnsureTriggerCollider(false);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        PlayerMovement2D player = ResolvePlayer();
        isPlayerInside = player != null && IsPlayerInArea(player);
        float targetBlend = isPlayerInside ? 1f : 0f;
        blend = Mathf.MoveTowards(blend, targetBlend, transitionSpeed * Time.deltaTime);

        ApplyCameraPalette();
        ApplyMusic();
    }

    private void OnDisable()
    {
        SetSourceVolume(outsideSource, 0f);
        SetSourceVolume(areaSource, 0f);
    }

    private BoxCollider2D EnsureTriggerCollider(bool addMissingCollider)
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider == null && addMissingCollider)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        if (boxCollider == null)
        {
            return null;
        }

        boxCollider.isTrigger = true;
        boxCollider.size = triggerSize;
        boxCollider.offset = triggerOffset;
        return boxCollider;
    }

    private PlayerMovement2D ResolvePlayer()
    {
        if (cachedPlayer == null || !cachedPlayer.isActiveAndEnabled)
        {
            cachedPlayer = FindAnyObjectByType<PlayerMovement2D>(FindObjectsInactive.Exclude);
        }

        return cachedPlayer;
    }

    private bool IsPlayerInArea(PlayerMovement2D player)
    {
        Vector2 center = transform.TransformPoint(triggerOffset);
        Vector2 size = Vector2.Scale(triggerSize, Abs(transform.lossyScale));
        Bounds areaBounds = new Bounds(center, new Vector3(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y), 10f));
        Collider2D playerCollider = player.GetComponent<Collider2D>();

        if (playerCollider != null)
        {
            return areaBounds.Intersects(playerCollider.bounds);
        }

        return areaBounds.Contains(player.transform.position);
    }

    private void ApplyCameraPalette()
    {
        Camera camera = ResolveCamera();

        if (camera == null)
        {
            return;
        }

        float easedBlend = Mathf.SmoothStep(0f, 1f, blend);
        camera.backgroundColor = Color.Lerp(outsideCameraColor, areaCameraColor, easedBlend);
    }

    private void ApplyMusic()
    {
        if (!playProceduralMusic)
        {
            SetSourceVolume(outsideSource, 0f);
            SetSourceVolume(areaSource, 0f);
            return;
        }

        EnsureMusicSources();

        if (outsideSource == null || areaSource == null)
        {
            return;
        }

        outsideSource.volume = Mathf.Clamp01(1f - blend) * musicVolume;
        areaSource.volume = Mathf.Clamp01(blend) * musicVolume;
    }

    private Camera ResolveCamera()
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera == null)
        {
            cachedCamera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
        }

        return cachedCamera;
    }

    private void EnsureMusicSources()
    {
        if (outsideClip == null)
        {
            outsideClip = CreateToneClip($"{areaId}-outside", outsideToneHz);
        }

        if (areaClip == null)
        {
            areaClip = CreateToneClip($"{areaId}-inside", areaToneHz);
        }

        AudioSource[] sources = GetComponents<AudioSource>();

        if (outsideSource == null)
        {
            outsideSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        }

        if (areaSource == null)
        {
            areaSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        }

        ConfigureSource(outsideSource, outsideClip, Mathf.Clamp01(1f - blend) * musicVolume);
        ConfigureSource(areaSource, areaClip, Mathf.Clamp01(blend) * musicVolume);
    }

    private static void ConfigureSource(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null)
        {
            return;
        }

        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    private static AudioClip CreateToneClip(string clipName, float frequency)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * ClipLengthSeconds);
        float[] data = new float[sampleCount];
        float safeFrequency = Mathf.Max(20f, frequency);

        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / SampleRate;
            float root = Mathf.Sin(2f * Mathf.PI * safeFrequency * time);
            float fifth = Mathf.Sin(2f * Mathf.PI * safeFrequency * 1.5f * time);
            data[i] = (root * 0.55f + fifth * 0.25f) * 0.18f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.hideFlags = HideFlags.HideAndDontSave;
        clip.SetData(data, 0);
        return clip;
    }

    private static void SetSourceVolume(AudioSource source, float volume)
    {
        if (source != null)
        {
            source.volume = volume;
        }
    }

    private static Vector2 Abs(Vector3 value)
    {
        return new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
    }
}
