using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LuaBootstrapper : MonoBehaviour
{

    private int trust = 100;
    private int money = 400;
    private bool secretViolated = false;
    private int completedOrdersCount = 0;
    private const int TOTAL_ORDERS_TO_WIN = 4;

    private bool upgPrecisionBits = false;
    private bool upgStabilizer = false;
    private int qualificationRank = 1;

    private string viewState = "ROOM";
    private string orderStage = "NONE";

    private Vector2 playerPos = new Vector2(0f, -55f);
    private Vector2 targetPos = new Vector2(0f, -55f);
    private float moveSpeed = 310f;
    private string pendingStation = null;
    private float walkAnimTimer = 0f;
    private float stepSoundTimer = 0f;

    private string selectedMechTool = null;
    private string selectedSolvent = null;
    private string selectedStyle = null;
    private string activeBenchAction = "CLEAN";

    private int zonesDiscovered = 0;
    private int cleanProgress = 0;
    private int grindProgress = 0;
    private int clickFixProgress = 0;
    private float finishProgress = 0f;

    private bool chemicalStain = false;
    private bool wrongToolScratches = false;
    private float overScrubDamage = 0f;
    private float normMouseSpeed = 0f;
    private Vector2 lastLocalMousePos;
    private Vector2 currentLightPos = new Vector2(-60f, 80f);
    private bool hasLastMouse = false;
    private bool twistChecked = false;
    private float barsikBuffTimer = 0f;
    private GameObject barsikBuffRoot;
    private Text barsikBuffText;

    private List<string> historyLog = new List<string>();
    private List<string> completedOrderTypes = new List<string>();
    private List<Texture2D> createdTextures = new List<Texture2D>();
    private List<AudioClip> createdClips = new List<AudioClip>();

    private AudioSource sfxSource;
    private AudioSource toolLoopSource;
    private AudioSource ambientSource;
    private AudioSource musicSource;
    private AudioClip clipBell, clipStep, clipClickPin, clipSpringReset, clipGrindLoop, clipCleanLoop, clipCoin, clipFail, clipClockTick, clipCrystalChime, clipToolSwitch, clipPurr, clipHearthFire;
    private AudioClip clipClassicalTheme;
    private bool isMuted = false;
    private Text muteBtnText;
    private Image muteBtnImg;

    private class FireplaceSpark
    {
        public RectTransform rt;
        public Image img;
        public Vector2 pos, vel;
        public float life, maxLife, wobblePhase;
    }
    private List<FireplaceSpark> fireplaceSparks = new List<FireplaceSpark>();
    private Transform fireplaceSparkContainer;
    private Image fireplaceGlowImg, ambientTintImg;
    private float fireplaceStokeTimer = 0f;
    private RectTransform[] flameTongueRTs = new RectTransform[5];
    private Vector2[] flameBaseSizes = new Vector2[5];
    private float[] flameFrequencies = new float[5];
    private float[] flamePhases = new float[5];
    private readonly Vector2 posFireplaceInteract = new Vector2(-270f, 84f);

    private RectTransform clockWidgetRT, hourHandRT, minuteHandRT, clockPhaseFillRT;
    private Text clockPhaseText, clockDigitalText;
    private Image[] clockPhaseGemImgs = new Image[4];

    private Vector3 catClickScale = Vector3.one;
    private RectTransform catTailRT, catLeftEarRT, catRightEarRT;
    private RectTransform candleFlameBenchRT;

    private class DiagZone
    {
        public float x, y, r;
        public bool found;
        public string text;
        public DiagZone(float x, float y, float r, string text)
        {
            this.x = x; this.y = y; this.r = r; this.text = text; this.found = false;
        }
    }

    private class ClickNode
    {
        public Vector2 pos;
        public int clicksLeft;
        public int maxClicks;
        public float resetTimer;
        public RectTransform uiRT;
        public RectTransform ringRT;
        public Text uiTxt;
        public Image uiImg;
        public Image ringImg;
    }

    private const float CLICK_RESET_TIME = 0.65f;
    private List<ClickNode> activeClickNodes = new List<ClickNode>();

    private class DustMote
    {
        public RectTransform rt;
        public Image img;
        public Vector2 basePos;
        public float speedX;
        public float speedY;
        public float phase;
        public float radius;
    }
    private List<DustMote> dustMotes = new List<DustMote>();

    private class OrderData
    {
        public string id, shapeType, title, clientName, clientType, shortDesc, briefingText;
        public int baseReward;
        public int requiredRank;
        public List<DiagZone> diagnostics = new List<DiagZone>();
        public string correctMechTool, correctSolvent, preferredStyle;
        public string twistType, twistText;
        public string histPatina, histNew, histSpecial;
    }

    private List<OrderData> currentWindowChoices = new List<OrderData>();
    private OrderData activeOrder = null;

    private readonly Vector2 posWindowInteract   = new Vector2(-355f, -15f);
    private readonly Vector2 posBenchInteract    = new Vector2(0f, 84f);
    private readonly Vector2 posCabinetInteract  = new Vector2(365f, 84f);
    private readonly Vector2 posUpgradesInteract = new Vector2(365f, -135f);

    private RectTransform roomAreaRT, playerRT, clickMarkerRT;
    private Text playerBubbleText, topTitleText, topStatsText, bottomHintText;
    private Image windowBadgeImg, benchBadgeImg, cabinetBadgeImg, benchItemPreviewImg;
    private Image winGlowImg, benchGlowImg, cabGlowImg;
    private float markerAlpha = 0f;

    private GameObject windowOverlay, workbenchOverlay, cabinetOverlay, upgradesOverlay, modalOverlay;
    private Text[] cardTitles = new Text[3];
    private Text[] cardClients = new Text[3];
    private Text[] cardDescs = new Text[3];
    private Text[] cardBtnLabels = new Text[3];
    private Image[] cardBtnImgs = new Image[3];
    private Image[] cardBadgeImgs = new Image[3];
    private Text[] cardBadgeTexts = new Text[3];

    private List<Image> toolBtnImgs = new List<Image>();
    private List<Image> solventBtnImgs = new List<Image>();
    private List<Image> styleBtnImgs = new List<Image>();
    private Text cabinetDiagReminderText;

    private Text upgStatusText;
    private Text[] upgBtnLabels = new Text[4];
    private Image[] upgBtnImgs = new Image[4];
    private Image[] upgBadgeImgs = new Image[4];

    private const int TEX_SIZE = 320;
    private Texture2D itemTex;
    private Color32[] pixelBuffer = new Color32[TEX_SIZE * TEX_SIZE];
    private Color[,] albedoMap = new Color[TEX_SIZE, TEX_SIZE];
    private Vector3[,] normalMap = new Vector3[TEX_SIZE, TEX_SIZE];
    private float[,] metallicMap = new float[TEX_SIZE, TEX_SIZE];
    private byte[,] isItemMask = new byte[TEX_SIZE, TEX_SIZE];
    private byte[,] isGemMask = new byte[TEX_SIZE, TEX_SIZE];
    private byte[,] dirtMap = new byte[TEX_SIZE, TEX_SIZE];
    private float[,] burrMap = new float[TEX_SIZE, TEX_SIZE];

    private RectTransform itemSurfaceRT, loupeRT, speedFillRT, toolCursorRT;
    private Image itemSurfaceImg, speedFillImg, toolCursorImg;
    private GameObject loupeGO, toolCursorGO, benchToolsPanel, clickNodesContainer;
    private Text benchLogText, benchProgressText, benchModeNoticeText, speedBarLabelText;
    private Button btnGoToCabinetFromBench;
    private Image[] benchToolBtnImgs = new Image[4];
    private Text[] benchToolBtnTexts = new Text[4];
    private Image btnDoneImg;
    private Text btnDoneText;
    private float benchZoom = 1.0f;
    private float targetBenchZoom = 1.0f;
    private Text benchZoomBtnText;

    private Sprite spGalleryRing, spGalleryDiamond, spGalleryLocket, spGalleryCross;
    private GameObject upgEquipContainer, upgGalleryContainer;
    private Image upgTabBtnEquipImg, upgTabBtnGalleryImg;
    private Text galleryStatusText;
    private Image[] galleryCardIcons = new Image[4];
    private Text[] galleryCardTitles = new Text[4];
    private Text[] galleryCardBadges = new Text[4];
    private Text[] galleryCardLores = new Text[4];

    private class SparkParticle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 pos, vel;
        public float life, maxLife;
        public Color baseCol;
    }
    private List<SparkParticle> sparks = new List<SparkParticle>();
    private float sparkSpawnTimer = 0f;
    private float gemGleamTimer = 1.5f;

    private class FloatingTextItem
    {
        public RectTransform rt;
        public Text txt;
        public Vector2 pos;
        public float timer, maxTime;
        public Color baseCol;
    }
    private List<FloatingTextItem> floatingTexts = new List<FloatingTextItem>();

    private Text modalTitle, modalBody, modalBtn1Text, modalBtn2Text, modalSealText;
    private Button modalBtn1, modalBtn2;
    private GameObject modalSealGO;

    private Sprite spBeveledPanel, spBeveledButton, spWaxSealBadge, spSoftLightGlow, spLoupeLens, spTargetClamp, spTimerRing;
    private Sprite spStationWindow, spStationWorkbench, spStationCabinet, spStationMasterStand, spArtisanChar, spWorkshopCat;
    private Sprite spClockDial, spHourHand, spMinuteHand, spFireplace, spFlameTongue, spCandleStick, spCatTail, spCatEarLeft, spCatEarRight;
    private Sprite spVictorianWall, spGodRays, spRoomVignette, spWorkbenchMat;
    private Image sunlightGodRaysImg, roomVignetteImg;
    private RectTransform catRT;
    public static LuaBootstrapper Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoEnsureWorkshopBootstrap()
    {
        if (Application.isPlaying && FindAnyObjectByType<LuaBootstrapper>() == null)
        {
            if (Camera.main == null && FindAnyObjectByType<Camera>() == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                Camera cam = camGO.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.06f, 0.05f, 1f);
                camGO.AddComponent<AudioListener>();
            }
            GameObject go = new GameObject("GameManager");
            go.AddComponent<LuaBootstrapper>();
        }
    }

    void Start()
    {
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.08f, 0.06f, 0.05f, 1f);
        }
        if (TimeController.Instance == null)
        {
            gameObject.AddComponent<TimeController>();
        }
        QualitySettings.antiAliasing = 4;
        Application.targetFrameRate = 120;

        InitAudioSystem();
        GeneratePBRSprites();
        BuildAllUI();
        GenerateWindowOrderChoices();
        UpdateRoomIndicators();
        UpdateTopBar();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (itemTex != null) Destroy(itemTex);
        foreach (var t in createdTextures)
        {
            if (t != null) Destroy(t);
        }
        createdTextures.Clear();
        foreach (var c in createdClips)
        {
            if (c != null) Destroy(c);
        }
        createdClips.Clear();
    }

    private Texture2D CreateTrackedTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        createdTextures.Add(tex);
        return tex;
    }

    private Font GetSafeFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    private void InitAudioSystem()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        toolLoopSource = gameObject.AddComponent<AudioSource>();
        toolLoopSource.playOnAwake = false;
        toolLoopSource.loop = true;
        toolLoopSource.volume = 0.32f;

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.volume = 0.20f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = 0.24f;

        clipBell        = LoadOrSynthesize("Audio/bell",   SynthesizeBellClip);
        clipStep        = LoadOrSynthesize("Audio/step",   SynthesizeStepClip);
        clipClickPin    = LoadOrSynthesize("Audio/click",  SynthesizeClickClip);
        clipSpringReset = LoadOrSynthesize("Audio/reset",  SynthesizeSpringResetClip);
        clipGrindLoop   = LoadOrSynthesize("Audio/grind",  SynthesizeGrindLoopClip);
        clipCleanLoop   = LoadOrSynthesize("Audio/clean",  SynthesizeCleanLoopClip);
        clipCoin        = LoadOrSynthesize("Audio/coin",   SynthesizeCoinClip);
        clipFail        = LoadOrSynthesize("Audio/fail",   SynthesizeFailClip);
        clipClockTick   = LoadOrSynthesize("Audio/clock",  SynthesizeClockTickClip);
        clipCrystalChime = LoadOrSynthesize("Audio/chime", SynthesizeCrystalChimeClip);
        clipToolSwitch  = LoadOrSynthesize("Audio/switch", SynthesizeToolSwitchClip);
        clipPurr        = LoadOrSynthesize("Audio/purr",   SynthesizePurrClip);
        clipHearthFire  = LoadOrSynthesize("Audio/hearth", SynthesizeHearthFireClip);

        clipClassicalTheme = LoadOrSynthesize("Audio/classical_theme", SynthesizeClassicalTheme);
        if (clipClassicalTheme != null)
        {
            musicSource.clip = clipClassicalTheme;
            musicSource.Play();
        }

        if (clipClockTick != null)
        {
            ambientSource.clip = clipClockTick;
            ambientSource.Play();
        }
    }

    private AudioClip LoadWavFromFile(string filePath, string clipName)
    {
        if (!System.IO.File.Exists(filePath)) return null;
        try
        {
            byte[] bytes = System.IO.File.ReadAllBytes(filePath);
            if (bytes.Length < 44) return null;
            if (bytes[0] != 'R' || bytes[1] != 'I' || bytes[2] != 'F' || bytes[3] != 'F') return null;
            if (bytes[8] != 'W' || bytes[9] != 'A' || bytes[10] != 'V' || bytes[11] != 'E') return null;

            int channels = 1;
            int sampleRate = 44100;
            int bitsPerSample = 16;
            int pos = 12;

            while (pos + 8 <= bytes.Length)
            {
                string chunkId = System.Text.Encoding.ASCII.GetString(bytes, pos, 4);
                int chunkSize = System.BitConverter.ToInt32(bytes, pos + 4);
                pos += 8;

                if (chunkId == "fmt ")
                {
                    channels = System.BitConverter.ToInt16(bytes, pos + 2);
                    sampleRate = System.BitConverter.ToInt32(bytes, pos + 4);
                    bitsPerSample = System.BitConverter.ToInt16(bytes, pos + 14);
                    pos += chunkSize;
                }
                else if (chunkId == "data")
                {
                    int bytesPerSample = bitsPerSample / 8;
                    if (bytesPerSample <= 0 || channels <= 0) return null;
                    int sampleCount = chunkSize / bytesPerSample;
                    int totalFrames = sampleCount / channels;
                    float[] samples = new float[sampleCount];

                    if (bitsPerSample == 16)
                    {
                        for (int i = 0; i < sampleCount && pos + i * 2 + 1 < bytes.Length; i++)
                        {
                            short s = System.BitConverter.ToInt16(bytes, pos + i * 2);
                            samples[i] = s / 32768f;
                        }
                    }
                    else if (bitsPerSample == 8)
                    {
                        for (int i = 0; i < sampleCount && pos + i < bytes.Length; i++)
                        {
                            samples[i] = (bytes[pos + i] - 128) / 128f;
                        }
                    }
                    else
                    {
                        return null;
                    }

                    AudioClip clip = AudioClip.Create(clipName, totalFrames, channels, sampleRate, false);
                    clip.SetData(samples, 0);
                    return clip;
                }
                else
                {
                    pos += chunkSize;
                }
            }
        }
        catch
        {
            return null;
        }
        return null;
    }

    private AudioClip LoadOrSynthesize(string path, System.Func<AudioClip> synthFunc)
    {
        AudioClip loaded = Resources.Load<AudioClip>(path);
        if (loaded != null) return loaded;

        string fileName = System.IO.Path.GetFileName(path);
        string[] searchPaths = new string[] {
            System.IO.Path.Combine(Application.dataPath, "Resources", "Audio", fileName + ".wav"),
            System.IO.Path.Combine(Application.dataPath, "..", "Audio", fileName + ".wav"),
            System.IO.Path.Combine(Application.dataPath, "Audio", fileName + ".wav"),
            System.IO.Path.Combine(Application.streamingAssetsPath, "Audio", fileName + ".wav")
        };
        for (int i = 0; i < searchPaths.Length; i++)
        {
            if (System.IO.File.Exists(searchPaths[i]))
            {
                AudioClip custom = LoadWavFromFile(searchPaths[i], fileName);
                if (custom != null)
                {
                    createdClips.Add(custom);
                    return custom;
                }
            }
        }

        AudioClip synthesized = synthFunc();
        if (synthesized != null) createdClips.Add(synthesized);
        return synthesized;
    }

    private void PlaySFX(AudioClip clip, float volume = 0.7f, float pitch = 1.0f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    private void ToggleMute()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;
        if (muteBtnText != null) muteBtnText.text = isMuted ? "🔇 ВЫКЛ" : "🔊 ЗВУК";
        if (muteBtnImg != null) muteBtnImg.color = isMuted ? new Color(0.38f, 0.16f, 0.16f, 1f) : new Color(0.24f, 0.18f, 0.14f, 1f);
        if (!isMuted) PlaySFX(clipBell, 0.5f, 1.15f);
    }

    private AudioClip SynthesizeBellClip()
    {
        int sr = 44100;
        int len = Mathf.FloorToInt(sr * 0.75f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float env = Mathf.Exp(-t * 5.5f);
            float w = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.6f
                    + Mathf.Sin(2f * Mathf.PI * 2093.0f * t) * 0.3f
                    + Mathf.Sin(2f * Mathf.PI * 3139.5f * t) * 0.15f;
            data[i] = w * env * 0.5f;
        }
        AudioClip c = AudioClip.Create("ProcBell", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeStepClip()
    {
        int sr = 44100;
        int len = Mathf.FloorToInt(sr * 0.09f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float env = Mathf.Exp(-t * 45f);
            float thud = Mathf.Sin(2f * Mathf.PI * 130f * t) + (Random.value * 2f - 1f) * 0.3f;
            data[i] = thud * env * 0.35f;
        }
        AudioClip c = AudioClip.Create("ProcStep", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeClickClip()
    {
        int sr = 44100;
        int len = Mathf.FloorToInt(sr * 0.11f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float env = Mathf.Exp(-t * 38f);
            float snap = Mathf.Sin(2f * Mathf.PI * 1650f * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * 2850f * t) * 0.3f;
            data[i] = snap * env * 0.5f;
        }
        AudioClip c = AudioClip.Create("ProcClick", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeSpringResetClip()
    {
        int sr = 44100;
        int len = Mathf.FloorToInt(sr * 0.38f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float env = Mathf.Exp(-t * 8f);
            float freq = Mathf.Lerp(520f, 180f, t / 0.38f) + Mathf.Sin(t * 95f) * 35f;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
        }
        AudioClip c = AudioClip.Create("ProcSpringReset", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeGrindLoopClip()
    {
        int sr = 44100;
        int len = sr / 2;
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float buzz = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.4f
                       + Mathf.Sin(2f * Mathf.PI * 440f * t + Mathf.Sin(t * 60f) * 3f) * 0.3f
                       + (Random.value * 2f - 1f) * 0.22f;
            data[i] = buzz * 0.4f;
        }
        AudioClip c = AudioClip.Create("ProcGrind", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeCleanLoopClip()
    {
        int sr = 44100;
        int len = sr / 2;
        float[] data = new float[len];
        float last = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float n = (Random.value * 2f - 1f);
            last = Mathf.Lerp(last, n, 0.12f);
            data[i] = last * (0.6f + 0.4f * Mathf.Sin(t * Mathf.PI * 4f)) * 0.35f;
        }
        AudioClip c = AudioClip.Create("ProcClean", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeCoinClip()
    {
        int sr = 44100;
        int len = Mathf.FloorToInt(sr * 0.55f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float freq = (t < 0.14f) ? 1318.5f : 1760.0f;
            float env = Mathf.Exp(-(t % 0.14f) * 9f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.45f;
        }
        AudioClip c = AudioClip.Create("ProcCoin", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeFailClip()
    {
        int sr = 44100;
        int len = Mathf.FloorToInt(sr * 0.65f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float freq = (t < 0.3f) ? 196f : 146.8f;
            float env = Mathf.Exp(-t * 3.5f);
            data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t)) * env * 0.4f;
        }
        AudioClip c = AudioClip.Create("ProcFail", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeClockTickClip()
    {
        int sr = 44100;
        int len = sr * 4;
        float[] data = new float[len];
        System.Random rnd = new System.Random(42);
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float s = 0f;
            float secFrac = t - Mathf.Floor(t);
            int secIdx = Mathf.FloorToInt(t);
            if (secFrac < 0.12f)
            {
                float freq = (secIdx % 2 == 0) ? 1160f : 940f;
                float bodyFreq = (secIdx % 2 == 0) ? 280f : 240f;
                float env = Mathf.Exp(-secFrac * 58f);
                float strike = Mathf.Sin(2f * Mathf.PI * freq * secFrac) * 0.55f + Mathf.Sin(2f * Mathf.PI * bodyFreq * secFrac) * 0.45f;
                s += strike * env * 0.28f;
            }
            if (rnd.NextDouble() < 0.00075)
            {
                float pop = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.14f;
                s += pop;
            }
            s += Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.012f;
            data[i] = Mathf.Clamp(s, -0.95f, 0.95f);
        }
        AudioClip c = AudioClip.Create("ProcAmbience", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeCrystalChimeClip()
    {
        int sr = 44100;
        int len = Mathf.FloorToInt(sr * 0.55f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float env = Mathf.Exp(-t * 9f);
            float tone = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.65f + Mathf.Sin(2f * Mathf.PI * 3520f * t) * 0.25f + Mathf.Sin(2f * Mathf.PI * 5280f * t) * 0.1f;
            data[i] = tone * env * 0.42f;
        }
        AudioClip c = AudioClip.Create("ProcCrystalChime", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizePurrClip()
    {
        int sr = 44100;
        int len = Mathf.FloorToInt(sr * 1.35f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float purrRhythm = Mathf.Sin(2f * Mathf.PI * 23f * t);
            float purrMod = Mathf.Max(0f, purrRhythm);
            float tone = Mathf.Sin(2f * Mathf.PI * 72f * t) * 0.65f + Mathf.Sin(2f * Mathf.PI * 144f * t) * 0.35f;
            float breath = Mathf.Sin(2f * Mathf.PI * 1.5f * t) * 0.5f + 0.5f;
            float env = Mathf.Sin(Mathf.PI * (t / 1.35f));
            data[i] = tone * purrMod * breath * env * 0.55f;
        }
        AudioClip c = AudioClip.Create("ProcPurr", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeToolSwitchClip()
    {
        int sr = 44100;
        int len = Mathf.FloorToInt(sr * 0.16f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float env = Mathf.Exp(-t * 26f);
            float s = Mathf.Sin(2f * Mathf.PI * (920f + t * 450f) * t) * 0.4f + (Random.value * 2f - 1f) * 0.15f * env;
            data[i] = s * env * 0.45f;
        }
        AudioClip c = AudioClip.Create("ProcToolSwitch", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    public AudioClip SynthesizeClassicalTheme()
    {
        int sr = 44100;
        int len = sr * 12;
        float[] data = new float[len];

        float[] bassFreqs = { 110.0f, 87.31f, 73.42f, 82.41f };
        float[] melodyFreqs = { 659.25f, 523.25f, 493.88f, 523.25f, 440.0f, 415.30f, 440.0f, 493.88f, 523.25f, 659.25f, 587.33f, 493.88f };
        float noteDur = len / (float)(melodyFreqs.Length * sr);

        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            int barIdx = Mathf.Clamp(Mathf.FloorToInt(t / 3.0f), 0, 3);
            float bassF = bassFreqs[barIdx];
            float vibrato = Mathf.Sin(2f * Mathf.PI * 4.5f * t) * 1.8f;
            float cello = Mathf.Sin(2f * Mathf.PI * (bassF + vibrato) * t) * 0.45f
                        + Mathf.Sin(2f * Mathf.PI * (bassF * 2f) * t) * 0.22f
                        + Mathf.Sin(2f * Mathf.PI * (bassF * 3f) * t) * 0.10f;

            int melIdx = Mathf.Clamp(Mathf.FloorToInt(t / noteDur), 0, melodyFreqs.Length - 1);
            float noteT = t - (melIdx * noteDur);
            float melF = melodyFreqs[melIdx];
            float melEnv = Mathf.Exp(-noteT * 3.2f);
            float musicBox = (Mathf.Sin(2f * Mathf.PI * melF * t) * 0.50f
                            + Mathf.Sin(2f * Mathf.PI * melF * 2f * t) * 0.25f
                            + Mathf.Sin(2f * Mathf.PI * melF * 3f * t) * 0.12f) * melEnv;

            float pianoT = t - Mathf.Floor(t);
            float pianoEnv = Mathf.Exp(-pianoT * 4.5f);
            float arpegF = bassF * 2f * (1f + (Mathf.Floor(pianoT * 4f) * 0.25f));
            float piano = Mathf.Sin(2f * Mathf.PI * arpegF * t) * pianoEnv * 0.20f;

            float mix = (cello * 0.32f) + (musicBox * 0.38f) + piano;
            data[i] = Mathf.Clamp(mix, -0.92f, 0.92f);
        }

        AudioClip c = AudioClip.Create("ProcClassicalTheme", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private AudioClip SynthesizeHearthFireClip()
    {
        int sr = 44100;
        int len = Mathf.FloorToInt(sr * 1.5f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float rumble = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.2f + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.1f;
            float crackle = (Random.value > 0.992f) ? (Random.value * 2f - 1f) * 0.65f : 0f;
            float noise = (Random.value * 2f - 1f) * 0.08f;
            data[i] = Mathf.Clamp(rumble + crackle + noise, -1f, 1f) * 0.5f;
        }
        AudioClip c = AudioClip.Create("ProcHearthFire", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private Color EvaluatePBRPixel(Color albedo, Vector3 normal, float metallic, float smoothness, Vector3 lightDir)
    {
        normal.Normalize();
        lightDir.Normalize();
        Vector3 viewDir = new Vector3(0f, 0f, 1f);
        Vector3 halfVec = (lightDir + viewDir).normalized;

        float ndotl = Mathf.Max(0f, Vector3.Dot(normal, lightDir));
        float ndoth = Mathf.Max(0f, Vector3.Dot(normal, halfVec));
        float rim = 1f - Mathf.Max(0f, Vector3.Dot(normal, viewDir));

        float shininess = Mathf.Lerp(8f, 96f, smoothness);
        float specPower = Mathf.Pow(ndoth, shininess) * smoothness * (0.4f + 0.8f * metallic);

        Color ambient = albedo * 0.36f;
        Color diffuse = albedo * (ndotl * (1f - metallic * 0.35f) * 0.82f);
        Color specCol = Color.Lerp(new Color(1f, 0.96f, 0.86f, 1f), albedo + new Color(0.25f, 0.22f, 0.15f, 0f), metallic);
        Color specular = specCol * specPower;
        Color rimGlow = new Color(0.95f, 0.82f, 0.55f, 1f) * (Mathf.Pow(rim, 3f) * 0.18f * metallic);

        Color final = ambient + diffuse + specular + rimGlow;
        final.r = Mathf.Clamp01(final.r);
        final.g = Mathf.Clamp01(final.g);
        final.b = Mathf.Clamp01(final.b);
        final.a = albedo.a;
        return final;
    }

    private Color32 EvaluatePBRPixelFast(Color albedo, Vector3 normal, float metallic, float smoothness, Vector3 lightDir)
    {
        float lLen = Mathf.Sqrt(lightDir.x * lightDir.x + lightDir.y * lightDir.y + lightDir.z * lightDir.z);
        if (lLen > 0.0001f) { lightDir.x /= lLen; lightDir.y /= lLen; lightDir.z /= lLen; }

        float hx = lightDir.x;
        float hy = lightDir.y;
        float hz = lightDir.z + 1f;
        float hLen = Mathf.Sqrt(hx * hx + hy * hy + hz * hz);
        if (hLen > 0.0001f) { hx /= hLen; hy /= hLen; hz /= hLen; }

        float ndotl = normal.x * lightDir.x + normal.y * lightDir.y + normal.z * lightDir.z;
        if (ndotl < 0f) ndotl = 0f;

        float ndoth = normal.x * hx + normal.y * hy + normal.z * hz;
        if (ndoth < 0f) ndoth = 0f;

        float rim = 1f - (normal.z > 0f ? normal.z : 0f);
        if (rim < 0f) rim = 0f;

        float shininess = 8f + smoothness * 88f;
        float specPower = Mathf.Pow(ndoth, shininess) * smoothness * (0.4f + 0.8f * metallic);

        float diffMult = ndotl * (1f - metallic * 0.35f) * 0.82f;
        float specR = (1f - metallic) * 1f + metallic * (albedo.r + 0.25f);
        float specG = (1f - metallic) * 0.96f + metallic * (albedo.g + 0.22f);
        float specB = (1f - metallic) * 0.86f + metallic * (albedo.b + 0.15f);

        float rimFactor = rim * rim * rim * 0.18f * metallic;

        float rf = albedo.r * 0.36f + albedo.r * diffMult + specR * specPower + 0.95f * rimFactor;
        float gf = albedo.g * 0.36f + albedo.g * diffMult + specG * specPower + 0.82f * rimFactor;
        float bf = albedo.b * 0.36f + albedo.b * diffMult + specB * specPower + 0.55f * rimFactor;

        byte r = (byte)(rf >= 1f ? 255 : (rf <= 0f ? 0 : (int)(rf * 255f)));
        byte g = (byte)(gf >= 1f ? 255 : (gf <= 0f ? 0 : (int)(gf * 255f)));
        byte b = (byte)(bf >= 1f ? 255 : (bf <= 0f ? 0 : (int)(bf * 255f)));
        byte a = (byte)(albedo.a * 255f);

        return new Color32(r, g, b, a);
    }

    private void GeneratePBRSprites()
    {
        spBeveledPanel       = CreatePBRFrameSprite(128, 128, 18, new Color(0.20f, 0.15f, 0.11f, 0.98f), new Color(0.88f, 0.68f, 0.28f, 1f));
        spBeveledButton      = CreatePBRFrameSprite(128, 64, 12, Color.white, new Color(0.95f, 0.78f, 0.38f, 0.85f));
        spWaxSealBadge       = CreateWaxSealSprite(64);
        spTargetClamp        = CreateJewelerClampSprite(72);
        spTimerRing          = CreateTimerRingSprite(96);
        spSoftLightGlow      = CreateRadialGlowSprite(128);
        spLoupeLens          = CreateLoupeSprite(168);

        spStationWindow      = CreateWindowStationSprite(240, 290);
        spStationWorkbench   = CreateWorkbenchStationSprite(380, 175);
        spStationCabinet     = CreateCabinetStationSprite(320, 175);
        spStationMasterStand = CreateMasterStandSprite(260, 190);
        spArtisanChar        = CreateDetailedArtisanSprite(116, 146);
        spGalleryRing        = CreateRelicIconSprite("RING", 128);
        spGalleryDiamond     = CreateRelicIconSprite("DIAMOND", 128);
        spGalleryLocket      = CreateRelicIconSprite("LOCKET", 128);
        spGalleryCross       = CreateRelicIconSprite("CROSS", 128);
        spWorkshopCat        = CreateWorkshopCatSprite(110, 80);
        spClockDial          = CreateVictorianClockDialSprite(64);
        spHourHand           = CreateVictorianClockHandSprite(8, 20, true);
        spMinuteHand         = CreateVictorianClockHandSprite(6, 26, false);
        spFireplace          = CreateFireplaceStationSprite(220, 180);
        spFlameTongue        = CreateFlameTongueSprite(64, 128);
        spCandleStick        = CreateCandleStickSprite(24, 48);
        spCatTail            = CreateCatTailSprite(36, 48);
        spCatEarLeft         = CreateCatEarSprite(18, 22, true);
        spCatEarRight        = CreateCatEarSprite(18, 22, false);
        spVictorianWall      = CreateVictorianWallSprite(128, 128);
        spGodRays            = CreateGodRaySprite(256, 160);
        spRoomVignette       = CreateRoomVignetteSprite(128, 128);
        spWorkbenchMat       = CreateWorkbenchMatSprite(128, 128);
    }

    private Sprite CreateVictorianWallSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        Vector3 lightDir = new Vector3(-0.35f, 0.65f, 0.75f).normalized;
        Color wallBase = new Color(0.18f, 0.08f, 0.10f, 1f);
        Color damaskGold = new Color(0.38f, 0.22f, 0.14f, 1f);
        Color woodBase = new Color(0.24f, 0.13f, 0.08f, 1f);
        Color brassRail = new Color(0.85f, 0.70f, 0.32f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color albedo;
                Vector3 norm = new Vector3(0f, 0f, 1f);
                float met = 0.05f;
                float smooth = 0.30f;

                if (y > 54)
                {
                    float px = Mathf.Abs(((x % 32) - 16f) / 16f);
                    float py = Mathf.Abs(((y % 32) - 16f) / 16f);
                    float damask = Mathf.Cos(px * Mathf.PI) * Mathf.Cos(py * Mathf.PI);
                    float damaskPattern = Mathf.Clamp01(damask * damask);
                    albedo = Color.Lerp(wallBase, damaskGold, damaskPattern * 0.65f);
                    norm = new Vector3(Mathf.Sin(x * 0.2f) * damaskPattern * 0.2f, Mathf.Cos(y * 0.2f) * damaskPattern * 0.2f, 0.95f).normalized;
                    met = damaskPattern * 0.35f;
                    smooth = 0.40f;
                }
                else if (y >= 48)
                {
                    float rPos = (y - 48f) / 6f;
                    albedo = (rPos > 0.4f && rPos < 0.8f) ? brassRail : woodBase * 0.8f;
                    norm = new Vector3(0f, (rPos - 0.5f) * 1.5f, 0.7f).normalized;
                    met = (rPos > 0.4f && rPos < 0.8f) ? 0.85f : 0.25f;
                    smooth = 0.80f;
                }
                else
                {
                    float pX = x % 32f;
                    float beveled = Mathf.Min(pX, 32f - pX);
                    float woodGrain = Mathf.Sin(x * 0.3f + Mathf.Cos(y * 0.15f) * 2f) * 0.025f;
                    albedo = woodBase + new Color(woodGrain, woodGrain * 0.7f, woodGrain * 0.4f, 0f);
                    if (beveled < 3f)
                    {
                        albedo *= 0.65f;
                        norm = new Vector3((pX < 16f ? -0.6f : 0.6f), 0f, 0.8f).normalized;
                    }
                    else if (y < 8)
                    {
                        albedo *= 0.75f;
                        norm = new Vector3(0f, -0.5f, 0.85f).normalized;
                    }
                    else
                    {
                        norm = new Vector3(0f, 0f, 1f);
                    }
                    met = 0.15f;
                    smooth = 0.55f;
                }

                cols[y * w + x] = EvaluatePBRPixel(albedo, norm, met, smooth, lightDir);
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateGodRaySprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - 12f;
                float expectedY = (h - 16f) - (dx / (w - 24f)) * (h - 32f);
                float dist = Mathf.Abs(y - expectedY);
                float beam = Mathf.Exp(-dist * dist / (38f * 38f));
                float streaks = 0.82f + Mathf.Sin(x * 0.09f + y * 0.05f) * 0.18f;
                float distFade = Mathf.Clamp01(1.1f - (x / (float)w) * 0.75f);
                float alpha = Mathf.Clamp01(beam * streaks * distFade);

                Color rayCol = new Color(1f, 0.96f, 0.82f, alpha);
                cols[y * w + x] = rayCol;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateRoomVignetteSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        float cx = w * 0.5f;
        float cy = h * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - cx) / cx;
                float ny = (y - cy) / cy;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float vig = Mathf.Clamp01((d - 0.55f) / 0.65f);
                vig = vig * vig;
                cols[y * w + x] = new Color(0.04f, 0.02f, 0.01f, vig * 0.72f);
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateWorkbenchMatSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        Vector3 lightDir = new Vector3(-0.35f, 0.65f, 0.75f).normalized;
        Color velvetCol = new Color(0.08f, 0.22f, 0.14f, 1f);
        Color goldTrim = new Color(0.92f, 0.75f, 0.32f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int bDist = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                float plush = Mathf.Sin(x * 1.5f) * Mathf.Cos(y * 1.5f) * 0.03f;
                Color albedo = velvetCol + new Color(plush, plush * 1.2f, plush * 0.8f, 0f);
                Vector3 norm = new Vector3(plush * 1.5f, plush * 1.5f, 1f).normalized;
                float met = 0.05f;
                float smooth = 0.35f;

                if (bDist >= 6 && bDist <= 10)
                {
                    float leafPattern = Mathf.Sin((x + y) * 0.45f);
                    if (leafPattern > 0.1f)
                    {
                        albedo = goldTrim;
                        met = 0.90f;
                        smooth = 0.85f;
                        norm = new Vector3(leafPattern * 0.3f, leafPattern * 0.3f, 0.9f).normalized;
                    }
                }
                else if (bDist < 5)
                {
                    albedo = new Color(0.12f, 0.08f, 0.05f, 1f);
                    norm = (bDist < 2) ? new Vector3((x < w / 2 ? -0.7f : 0.7f), (y < h / 2 ? -0.7f : 0.7f), 0.5f).normalized : new Vector3(0f, 0f, 1f);
                    met = 0.2f;
                    smooth = 0.6f;
                }

                cols[y * w + x] = EvaluatePBRPixel(albedo, norm, met, smooth, lightDir);
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateVictorianClockDialSprite(int size)
    {
        Texture2D tex = CreateTrackedTexture(size, size);
        Color32[] cols = new Color32[size * size];
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.47f;
        Vector3 lightDir = new Vector3(-0.45f, 0.65f, 0.75f).normalized;
        Color brassCol = new Color(0.92f, 0.75f, 0.30f, 1f);
        Color ivoryFace = new Color(0.96f, 0.93f, 0.85f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c.x;
                float dy = y - c.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                Color outCol = Color.clear;

                if (dist <= maxR)
                {
                    float bezelThick = size * 0.12f;
                    if (dist > maxR - bezelThick)
                    {
                        float t = (dist - (maxR - bezelThick)) / bezelThick;
                        Vector3 norm = new Vector3(dx / maxR, dy / maxR, Mathf.Sin(t * Mathf.PI));
                        float notch = Mathf.Sin(ang * 24f) * 0.08f;
                        Color bCol = brassCol * (1f + notch);
                        outCol = EvaluatePBRPixel(bCol, norm, 0.90f, 0.85f, lightDir);
                        outCol.a = Mathf.Clamp01(maxR - dist + 0.8f);
                    }
                    else
                    {
                        float vig = Mathf.Clamp01(1.05f - (dist / maxR) * 0.28f);
                        Color dial = ivoryFace * vig;

                        float innerRingDist = Mathf.Abs(dist - maxR * 0.58f);
                        if (innerRingDist < 1.2f)
                        {
                            dial = new Color(0.82f, 0.65f, 0.24f, 1f);
                        }

                        float angDeg = (ang * Mathf.Rad2Deg + 360f + 90f) % 360f;
                        float tickMod = Mathf.Abs((angDeg % 30f) - 15f);
                        if (dist > maxR * 0.68f && dist < maxR * 0.82f && (tickMod > 13.8f || tickMod < 1.2f))
                        {
                            dial = new Color(0.20f, 0.16f, 0.12f, 1f);
                        }

                        Vector3 faceNorm = new Vector3(0f, 0f, 1f);
                        outCol = EvaluatePBRPixel(dial, faceNorm, 0.10f, 0.40f, lightDir);
                    }
                }
                cols[y * size + x] = outCol;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateVictorianClockHandSprite(int w, int h, bool isHourHand)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        float cx = w * 0.5f;
        Color handCol = isHourHand ? new Color(0.18f, 0.20f, 0.28f, 1f) : new Color(0.22f, 0.24f, 0.35f, 1f);
        Color brassTrim = new Color(0.90f, 0.74f, 0.28f, 1f);

        for (int y = 0; y < h; y++)
        {
            float taper = 1f - (y / (float)h) * 0.65f;
            float maxW = (w * 0.38f) * taper;
            if (isHourHand && y > h * 0.45f && y < h * 0.75f)
            {
                float spadeY = (y - h * 0.60f) / (h * 0.15f);
                maxW = Mathf.Max(maxW, (w * 0.48f) * Mathf.Sqrt(Mathf.Max(0.01f, 1f - spadeY * spadeY)));
            }

            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx);
                if (dx <= maxW && y >= 2)
                {
                    Color c = (dx > maxW - 1.2f) ? brassTrim : handCol;
                    cols[y * w + x] = c;
                }
                else
                {
                    cols[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.12f));
    }

    private Sprite CreateFireplaceStationSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        Vector3 lightDir = new Vector3(-0.35f, 0.65f, 0.75f).normalized;
        Color brickBase = new Color(0.48f, 0.22f, 0.16f, 1f);
        Color mortarCol = new Color(0.65f, 0.60f, 0.54f, 1f);
        Color mantelWood = new Color(0.26f, 0.14f, 0.08f, 1f);
        Color brassTrim = new Color(0.85f, 0.68f, 0.25f, 1f);
        Color ironCol = new Color(0.18f, 0.17f, 0.16f, 1f);
        Color fireboxInner = new Color(0.08f, 0.07f, 0.06f, 1f);

        float cx = w * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                Color albedo = brickBase;
                Vector3 norm = new Vector3(0f, 0f, 1f);
                float met = 0.05f;
                float smooth = 0.25f;

                if (y >= h - 22)
                {
                    albedo = (y >= h - 4 || y == h - 22) ? brassTrim : mantelWood;
                    norm = new Vector3(0f, (y - (h - 11)) / 11f, 0.8f);
                    met = (y >= h - 4 || y == h - 22) ? 0.85f : 0.2f;
                    smooth = 0.7f;
                }
                else if (y < 12)
                {
                    albedo = ironCol;
                    norm = new Vector3(0f, -0.4f, 0.9f);
                    met = 0.7f;
                    smooth = 0.5f;
                }
                else
                {
                    float archW = w * 0.38f;
                    float archH = h * 0.65f;
                    float boxY = y - 12f;
                    bool insideHearth = (Mathf.Abs(dx) < archW && boxY < archH);
                    if (insideHearth)
                    {
                        if (boxY > archH - 16f || Mathf.Abs(dx) > archW - 14f)
                        {
                            albedo = ironCol;
                            norm = new Vector3(dx / archW, (boxY - archH) / 16f, 0.7f);
                            met = 0.75f;
                            smooth = 0.55f;
                        }
                        else
                        {
                            albedo = fireboxInner;
                            if (boxY < 24f)
                            {
                                float logGlow = Mathf.Clamp01(1f - Mathf.Abs(dx) / (archW * 0.7f));
                                albedo = Color.Lerp(fireboxInner, new Color(0.95f, 0.35f, 0.05f, 1f), logGlow * 0.65f);
                            }
                            norm = new Vector3(0f, 0f, 1f);
                            met = 0.1f;
                            smooth = 0.1f;
                        }
                    }
                    else
                    {
                        int brickRow = Mathf.FloorToInt(y / 14f);
                        float xOffset = (brickRow % 2 == 0) ? 0f : 14f;
                        int brickCol = Mathf.FloorToInt((x + xOffset) / 28f);
                        bool isMortar = (y % 14 == 0 || (x + xOffset) % 28 == 0);
                        if (isMortar)
                        {
                            albedo = mortarCol;
                            norm = new Vector3(0f, 0f, 0.7f);
                        }
                        else
                        {
                            float brickNoise = Mathf.Sin(brickCol * 3.7f + brickRow * 2.3f) * 0.08f;
                            albedo = brickBase * (1f + brickNoise);
                            norm = new Vector3(0f, 0f, 1f);
                        }
                    }
                }

                cols[y * w + x] = EvaluatePBRPixel(albedo, norm, met, smooth, lightDir);
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateFlameTongueSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        float cx = w * 0.5f;

        for (int y = 0; y < h; y++)
        {
            float ny = y / (float)h;
            float halfW = (w * 0.45f) * Mathf.Sin(ny * Mathf.PI) * (1f - ny * 0.4f);
            if (halfW < 0.5f) halfW = 0.5f;

            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx);
                if (dx <= halfW)
                {
                    float t = dx / halfW;
                    Color core = Color.Lerp(Color.white, new Color(1f, 0.88f, 0.35f, 1f), ny * 0.7f);
                    Color rim = new Color(0.96f, 0.38f, 0.06f, 1f);
                    Color c = Color.Lerp(core, rim, t);
                    c.a = Mathf.Clamp01((1f - t) * 1.5f) * Mathf.Clamp01((1f - ny) * 2.5f);
                    cols[y * w + x] = c;
                }
                else
                {
                    cols[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.0f));
    }

    private Sprite CreateCandleStickSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        float cx = w * 0.5f;
        Color brassCol = new Color(0.88f, 0.72f, 0.28f, 1f);
        Color waxCol = new Color(0.94f, 0.91f, 0.82f, 1f);
        Color wickCol = new Color(0.2f, 0.18f, 0.16f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx);
                Color c = Color.clear;
                if (y < h * 0.28f)
                {
                    float baseW = (w * 0.45f) * (1f - (y / (h * 0.28f)) * 0.4f);
                    if (dx <= baseW) c = brassCol;
                }
                else if (y < h * 0.88f)
                {
                    float shaftW = w * 0.22f;
                    if (dx <= shaftW) c = waxCol;
                }
                else if (y < h * 0.98f)
                {
                    if (dx <= 1.2f) c = wickCol;
                }
                cols[y * w + x] = c;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.0f));
    }

    private Sprite CreateCatTailSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        Color ginger = new Color(0.88f, 0.48f, 0.18f, 1f);
        Color stripe = new Color(0.65f, 0.32f, 0.10f, 1f);
        Color tip = new Color(0.96f, 0.90f, 0.78f, 1f);

        for (int y = 0; y < h; y++)
        {
            float ny = y / (float)h;
            float curveX = (w * 0.5f) + Mathf.Sin(ny * Mathf.PI * 0.85f) * (w * 0.32f);
            float r = (w * 0.22f) * (1f - ny * 0.25f);

            for (int x = 0; x < w; x++)
            {
                float d = Mathf.Abs(x - curveX);
                if (d <= r)
                {
                    Color c = ginger;
                    if (ny > 0.85f) c = tip;
                    else if (Mathf.Sin(ny * 24f) > 0.35f) c = stripe;
                    c.a = Mathf.Clamp01(r - d + 0.8f);
                    cols[y * w + x] = c;
                }
                else
                {
                    cols[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.85f, 0.20f));
    }

    private Sprite CreateCatEarSprite(int w, int h, bool isLeft)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        Color ginger = new Color(0.88f, 0.48f, 0.18f, 1f);
        Color inner = new Color(0.95f, 0.72f, 0.68f, 1f);

        for (int y = 0; y < h; y++)
        {
            float ny = y / (float)h;
            float maxW = (w * 0.5f) * (1f - ny);
            float cx = w * 0.5f;

            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx);
                if (dx <= maxW)
                {
                    Color c = (dx < maxW * 0.55f && ny < 0.75f) ? inner : ginger;
                    c.a = Mathf.Clamp01(maxW - dx + 0.8f);
                    cols[y * w + x] = c;
                }
                else
                {
                    cols[y * w + x] = Color.clear;
                }
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f));
    }

    private Sprite CreateWorkshopCatSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        float cx = w * 0.5f;
        float cy = h * 0.44f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / (w * 0.5f);
                float dy = (y - cy) / (h * 0.5f);
                float distPillow = (dx * dx) / 0.85f + (dy * dy) / 0.55f;
                Color col = Color.clear;

                if (distPillow < 0.95f)
                {
                    col = new Color(0.22f, 0.12f, 0.14f, 0.85f);
                }

                float catDx = dx + 0.04f;
                float catDy = dy + 0.08f;
                float bodyDist = (catDx * catDx) / 0.48f + (catDy * catDy) / 0.28f;
                if (bodyDist < 0.92f)
                {
                    float shade = Mathf.Clamp01(1f - bodyDist);
                    col = Color.Lerp(new Color(0.78f, 0.45f, 0.18f, 1f), new Color(0.96f, 0.68f, 0.32f, 1f), shade + dy * 0.25f);
                    if (Mathf.Sin(x * 0.35f) > 0.65f)
                    {
                        col *= 0.82f;
                    }
                }

                float headDx = (dx - 0.28f) / 0.24f;
                float headDy = (dy - 0.18f) / 0.24f;
                float headDist = headDx * headDx + headDy * headDy;
                if (headDist < 0.90f)
                {
                    col = new Color(0.92f, 0.62f, 0.28f, 1f);
                    if (Mathf.Abs(headDy + 0.08f) < 0.18f && Mathf.Abs(headDx) > 0.15f && Mathf.Abs(headDx) < 0.55f)
                    {
                        col = new Color(0.20f, 0.12f, 0.08f, 1f);
                    }
                }

                bool leftEar = (dx > 0.14f && dx < 0.26f && dy > 0.34f && dy < 0.52f);
                bool rightEar = (dx > 0.36f && dx < 0.48f && dy > 0.34f && dy < 0.52f);
                if (leftEar || rightEar)
                {
                    col = new Color(0.85f, 0.52f, 0.24f, 1f);
                }

                cols[y * w + x] = col;
            }
        }

        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRelicIconSprite(string shapeType, int size)
    {
        Texture2D tex = CreateTrackedTexture(size, size);
        Color[] cols = new Color[size * size];
        float cx = size * 0.5f;
        float cy = size * 0.5f;
        Vector3 lightDir = new Vector3(0.577f, 0.577f, 0.577f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / (size * 0.5f);
                float dy = (y - cy) / (size * 0.5f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                int idx = y * size + x;

                if (dist > 0.98f)
                {
                    cols[idx] = Color.clear;
                    continue;
                }

                Color albedo = Color.clear;
                Vector3 norm = Vector3.forward;
                float metal = 0.85f;
                float smooth = 0.75f;

                if (shapeType == "RING")
                {
                    float rDist = Mathf.Abs(dist - 0.55f);
                    if (rDist < 0.22f)
                    {
                        albedo = new Color(0.96f, 0.78f, 0.28f, 1f);
                        float nd = (dist - 0.55f) / 0.22f;
                        norm = new Vector3(dx * 0.6f + nd * 0.4f, dy * 0.6f + nd * 0.4f, Mathf.Sqrt(Mathf.Max(0.01f, 1f - nd * nd))).normalized;
                        metal = 0.92f;
                        smooth = 0.88f;
                    }
                    float gDist = Mathf.Sqrt(dx * dx + (dy - 0.55f) * (dy - 0.55f));
                    if (gDist < 0.24f)
                    {
                        albedo = new Color(0.88f, 0.12f, 0.22f, 1f);
                        norm = new Vector3(dx * 2f, (dy - 0.55f) * 2f, 1f).normalized;
                        metal = 0.20f;
                        smooth = 0.95f;
                    }
                }
                else if (shapeType == "DIAMOND")
                {
                    float manhattan = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (dist < 0.72f && manhattan < 0.95f)
                    {
                        albedo = new Color(0.92f, 0.96f, 1.0f, 1f);
                        float ang = Mathf.Atan2(dy, dx);
                        float facet = Mathf.Cos(ang * 8f);
                        norm = new Vector3(dx + facet * 0.2f, dy + facet * 0.2f, 0.8f).normalized;
                        metal = 0.15f;
                        smooth = 0.98f;
                    }
                }
                else if (shapeType == "LOCKET")
                {
                    float starR = 0.62f + Mathf.Cos(Mathf.Atan2(dy, dx) * 8f) * 0.12f;
                    if (dist < starR)
                    {
                        albedo = new Color(0.98f, 0.82f, 0.32f, 1f);
                        norm = new Vector3(dx * 0.8f, dy * 0.8f, 0.9f).normalized;
                        metal = 0.90f;
                        smooth = 0.85f;
                        if (dist < 0.36f)
                        {
                            albedo = new Color(0.12f, 0.32f, 0.70f, 1f);
                            metal = 0.30f;
                            smooth = 0.92f;
                        }
                    }
                }
                else if (shapeType == "CROSS")
                {
                    bool inV = Mathf.Abs(dx) < 0.22f && Mathf.Abs(dy) < 0.78f;
                    bool inH = Mathf.Abs(dy - 0.10f) < 0.22f && Mathf.Abs(dx) < 0.65f;
                    if (inV || inH)
                    {
                        albedo = new Color(0.94f, 0.76f, 0.26f, 1f);
                        norm = new Vector3(dx * 0.7f, dy * 0.7f, 0.9f).normalized;
                        metal = 0.88f;
                        smooth = 0.82f;
                        if (dist < 0.20f)
                        {
                            albedo = new Color(0.18f, 0.28f, 0.85f, 1f);
                            metal = 0.25f;
                            smooth = 0.96f;
                        }
                    }
                }

                if (albedo.a > 0.05f)
                {
                    cols[idx] = EvaluatePBRPixel(albedo, norm, metal, smooth, lightDir);
                }
                else
                {
                    cols[idx] = Color.clear;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreatePBRFrameSprite(int w, int h, float radius, Color innerCol, Color brassCol)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        Vector3 lightDir = new Vector3(-0.45f, 0.65f, 0.75f).normalized;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - w * 0.5f) - (w * 0.5f - radius - 1f), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - h * 0.5f) - (h * 0.5f - radius - 1f), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);

                float edgeDist = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                bool isBorder = (dist > radius - 4.2f) || (edgeDist < 4.2f);

                if (isBorder)
                {
                    Vector3 norm = new Vector3((x - w * 0.5f) / (w * 0.5f), (y - h * 0.5f) / (h * 0.5f), 0.7f);
                    Color pbr = EvaluatePBRPixel(brassCol, norm, 0.85f, 0.78f, lightDir);
                    pbr.a = alpha;
                    cols[y * w + x] = pbr;
                }
                else
                {
                    float woodNoise = (Mathf.PerlinNoise(x * 0.08f, y * 0.25f) - 0.5f) * 0.04f;
                    float vig = Mathf.Lerp(0.86f, 1.06f, y / (float)h);
                    Color c = new Color(
                        Mathf.Clamp01((innerCol.r + woodNoise) * vig),
                        Mathf.Clamp01((innerCol.g + woodNoise) * vig),
                        Mathf.Clamp01((innerCol.b + woodNoise) * vig),
                        alpha * innerCol.a
                    );
                    cols[y * w + x] = c;
                }
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private Sprite CreateWoodFloorPBRSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];

        Vector2[] lampPositions = {
            new Vector2(w * 0.14f, h * 0.48f),
            new Vector2(w * 0.50f, h * 0.78f),
            new Vector2(w * 0.80f, h * 0.78f),
            new Vector2(w * 0.84f, h * 0.28f)
        };

        for (int y = 0; y < h; y++)
        {
            int plankRow = y / 26;
            float rowOffset = (plankRow % 2) * 68f;
            float seamY = Mathf.Min(y % 26, 26 - (y % 26));

            for (int x = 0; x < w; x++)
            {
                float seamX = Mathf.Min((x + rowOffset) % 136f, 136f - ((x + rowOffset) % 136f));
                float grain = Mathf.Sin(x * 0.08f + Mathf.PerlinNoise(x * 0.05f, y * 0.18f) * 5f) * 0.032f;
                float plankTone = ((plankRow + Mathf.FloorToInt((x + rowOffset) / 136f)) % 3) * 0.02f;

                Color albedo = new Color(0.29f + grain + plankTone, 0.19f + grain * 0.8f + plankTone, 0.12f + grain * 0.5f, 1f);

                Vector3 normal = new Vector3(grain * 1.5f, 0f, 1f);
                if (seamY < 2.0f) { albedo *= 0.52f; normal.y = (y % 26 < 13) ? -0.6f : 0.6f; }
                if (seamX < 2.0f) { albedo *= 0.52f; normal.x = ((x + rowOffset) % 136f < 68f) ? -0.6f : 0.6f; }
                normal.Normalize();

                Color totalLight = albedo * 0.35f;
                for (int i = 0; i < lampPositions.Length; i++)
                {
                    Vector2 toLamp = lampPositions[i] - new Vector2(x, y);
                    float dist = toLamp.magnitude;
                    float atten = Mathf.Clamp01(1f - dist / (w * 0.45f));
                    atten = atten * atten;

                    Vector3 lDir = new Vector3(toLamp.x / (dist + 1f), toLamp.y / (dist + 1f), 0.65f).normalized;
                    float diff = Mathf.Max(0f, Vector3.Dot(normal, lDir));
                    Vector3 halfV = (lDir + new Vector3(0, 0, 1)).normalized;
                    float spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(normal, halfV)), 28f) * 0.28f;

                    Color lampCol = new Color(1f, 0.88f, 0.66f, 1f);
                    totalLight += (albedo * diff * 0.85f + lampCol * spec) * atten;
                }

                float nx = (x / (float)w) - 0.5f;
                float ny = (y / (float)h) - 0.5f;
                float ao = Mathf.Clamp01(1.22f - Mathf.Sqrt(nx * nx + ny * ny) * 0.92f);
                totalLight *= ao;
                totalLight.a = 1f;

                cols[y * w + x] = totalLight;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateOrnateRugPBRSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        Vector3 lightDir = new Vector3(-0.3f, 0.5f, 0.8f).normalized;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int borderDist = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                float fabricWeave = Mathf.Sin(x * 1.2f) * Mathf.Cos(y * 1.2f) * 0.08f;
                Vector3 norm = new Vector3(fabricWeave, fabricWeave, 1f).normalized;

                Color c = new Color(0.45f, 0.14f, 0.16f, 1f);
                float metallic = 0.05f;

                if (borderDist < 5) { c = new Color(0.86f, 0.70f, 0.34f, 1f); metallic = 0.6f; }
                else if (borderDist > 12 && borderDist < 22)
                {
                    float pat = Mathf.Sin(x * 0.35f) * Mathf.Cos(y * 0.35f);
                    c = (pat > 0f) ? new Color(0.82f, 0.64f, 0.30f, 1f) : new Color(0.24f, 0.11f, 0.13f, 1f);
                    metallic = (pat > 0f) ? 0.5f : 0.05f;
                }
                else
                {
                    float dx = (x - w * 0.5f) / (w * 0.35f);
                    float dy = (y - h * 0.5f) / (h * 0.35f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ornament = Mathf.Sin(x * 0.18f) * Mathf.Sin(y * 0.18f);
                    if (Mathf.Abs(d - 0.65f) < 0.055f || (d < 0.35f && ornament > 0.2f))
                    {
                        c = new Color(0.76f, 0.56f, 0.26f, 1f);
                        metallic = 0.45f;
                    }
                }

                cols[y * w + x] = EvaluatePBRPixel(c, norm, metallic, 0.35f, lightDir);
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateWindowStationSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        float cx = w * 0.5f, cy = h * 0.48f;
        Vector3 lightDir = new Vector3(0.45f, 0.60f, 0.70f).normalized;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                Color outCol = Color.clear;

                float archWidth = 92f + Mathf.Sin(y * 0.08f) * 3f;
                if (y < 42 || y > h - 22) archWidth = 104f;
                float archDist = (y > cy + 20f)
                    ? Mathf.Sqrt(dx * dx + (y - (cy + 20f)) * (y - (cy + 20f)) * 1.25f)
                    : Mathf.Abs(dx);

                if (archDist <= archWidth && y >= 14 && y <= h - 10)
                {
                    float grain = Mathf.Sin(y * 0.25f + Mathf.Sin(x * 0.12f) * 3f) * 0.04f;
                    Vector3 norm = new Vector3(-dx / archWidth * 0.55f, (y - cy) / h * 0.35f, 0.85f);
                    Color albedo = new Color(0.36f + grain, 0.20f + grain * 0.8f, 0.12f, 1f);
                    float met = 0.15f, smooth = 0.55f;

                    if (Mathf.Abs(archDist - (archWidth - 11f)) < 2.6f)
                    {
                        albedo = new Color(0.90f, 0.72f, 0.28f, 1f);
                        met = 0.88f; smooth = 0.85f;
                    }

                    else if (archDist < archWidth - 22f && y > 36 && y < h - 34)
                    {
                        float glassGlow = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(dx, dy), new Vector2(0, 20f)) / 95f);
                        albedo = new Color(0.24f + glassGlow * 0.45f, 0.18f + glassGlow * 0.32f, 0.16f + glassGlow * 0.15f, 1f);
                        met = 0.3f; smooth = 0.9f;

                        if (Mathf.Abs((x + y) % 28 - 14) < 1.4f || Mathf.Abs((x - y + 500) % 28 - 14) < 1.4f)
                        {
                            albedo = new Color(0.18f, 0.16f, 0.15f, 1f);
                            met = 0.7f;
                        }

                        float curtainCurve = 38f + Mathf.Sin(y * 0.03f) * 18f;
                        if (Mathf.Abs(dx) > curtainCurve)
                        {
                            float foldWave = Mathf.Sin(dx * 0.45f + y * 0.05f);
                            norm = new Vector3(Mathf.Cos(dx * 0.45f) * 0.65f, 0.1f, 0.75f);
                            albedo = new Color(0.56f + foldWave * 0.08f, 0.11f, 0.16f, 1f);
                            met = 0.05f; smooth = 0.35f;
                        }
                    }

                    float pillowDist = (dx * dx) / (46f * 46f) + ((y - 48f) * (y - 48f)) / (18f * 18f);
                    if (pillowDist < 1f)
                    {
                        norm = new Vector3(dx / 46f, (y - 48f) / 18f, Mathf.Sqrt(1f - pillowDist));
                        albedo = new Color(0.68f, 0.14f, 0.22f, 1f);
                        met = 0.1f; smooth = 0.45f;
                    }

                    float bellDome = (dx + 48f) * (dx + 48f) / (13f * 13f + Mathf.Max(0, 75f - y) * 3f) + ((y - 68f) * (y - 68f)) / (14f * 14f);
                    if (bellDome < 1f && y >= 56 && y <= 82)
                    {
                        norm = new Vector3((dx + 48f) / 15f, (y - 68f) / 14f, 0.75f);
                        albedo = new Color(0.96f, 0.80f, 0.28f, 1f);
                        met = 0.92f; smooth = 0.92f;
                    }

                    outCol = EvaluatePBRPixel(albedo, norm, met, smooth, lightDir);
                }
                cols[y * w + x] = outCol;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateWorkbenchStationSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        float cx = w * 0.5f, cy = h * 0.5f;
        Vector3 lightDir = new Vector3(-0.35f, 0.60f, 0.72f).normalized;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                Color outCol = Color.clear;

                float bottomCutout = (Mathf.Abs(dx) < 56f) ? Mathf.Cos((dx / 56f) * Mathf.PI * 0.5f) * 22f : 0f;
                float sideCurve = Mathf.Sin((y / (float)h) * Mathf.PI) * 10f;

                if (Mathf.Abs(dx) <= 172f + sideCurve && y >= 14f + bottomCutout && y <= h - 12f)
                {
                    float grain = Mathf.Sin(x * 0.06f + Mathf.Cos(y * 0.28f) * 2.5f) * 0.04f;
                    float edgeDist = Mathf.Min(172f + sideCurve - Mathf.Abs(dx), y - (14f + bottomCutout));
                    Vector3 norm = new Vector3(grain * 2f, (y - cy) / h * 0.25f, 0.9f);
                    Color albedo = new Color(0.42f + grain, 0.25f + grain * 0.8f, 0.14f, 1f);
                    float met = 0.18f, smooth = 0.62f;

                    if (edgeDist < 6.5f)
                    {
                        albedo = new Color(0.85f, 0.66f, 0.26f, 1f);
                        norm = new Vector3(dx / 172f, -0.5f, 0.6f);
                        met = 0.85f; smooth = 0.85f;
                    }
                    else if (Mathf.Abs(dx) < 126f && y > 38f + bottomCutout * 0.6f && y < h - 26f + Mathf.Sin(dx * 0.08f) * 3f)
                    {
                        float suede = Mathf.Sin(x * 0.5f) * Mathf.Cos(y * 0.5f) * 0.025f;
                        albedo = new Color(0.11f + suede, 0.36f + suede, 0.23f + suede, 1f);
                        norm = new Vector3(suede * 3f, suede * 3f, 1f);
                        met = 0.05f; smooth = 0.28f;
                    }

                    for (int t = 0; t < 3; t++)
                    {
                        float tx = dx - (92f + t * 18f) + (y - cy) * 0.25f;
                        if (Mathf.Abs(tx) < 2.8f && y > 42 && y < 124)
                        {
                            norm = new Vector3(tx / 2.8f, 0f, 0.8f);
                            if (y < 76) { albedo = new Color(0.58f, 0.22f, 0.14f, 1f); met = 0.2f; smooth = 0.6f; }
                            else { albedo = new Color(0.86f, 0.90f, 0.95f, 1f); met = 0.92f; smooth = 0.92f; }
                        }
                    }

                    float shadeDist = Vector2.Distance(new Vector2(dx, dy), new Vector2(-118f, 38f));
                    if (shadeDist < 26f && dy > 26f - Mathf.Abs(dx + 118f) * 0.4f)
                    {
                        norm = new Vector3((dx + 118f) / 26f, (dy - 38f) / 26f, 0.75f);
                        if (shadeDist > 22f) { albedo = new Color(0.94f, 0.78f, 0.30f, 1f); met = 0.9f; smooth = 0.9f; }
                        else { albedo = new Color(0.16f, 0.62f, 0.34f, 1f); met = 0.4f; smooth = 0.88f; }
                    }

                    outCol = EvaluatePBRPixel(albedo, norm, met, smooth, lightDir);
                }
                cols[y * w + x] = outCol;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateCabinetStationSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        float cx = w * 0.5f;
        Vector3 lightDir = new Vector3(-0.5f, 0.55f, 0.75f).normalized;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                Color outCol = Color.clear;

                float crown = (y > h - 28) ? Mathf.Sin(((y - (h - 28)) / 28f) * Mathf.PI) * 14f : 0f;
                if (Mathf.Abs(dx) <= 142f + crown && y >= 12 && y <= h - 10)
                {
                    float grain = Mathf.Sin(y * 0.22f + Mathf.Sin(x * 0.15f) * 2f) * 0.035f;
                    Vector3 norm = new Vector3(dx / 150f * 0.3f, 0.1f, 0.9f);
                    Color albedo = new Color(0.34f + grain, 0.21f + grain * 0.8f, 0.13f, 1f);
                    float met = 0.18f, smooth = 0.60f;

                    float paneX = Mathf.Abs(dx) - 66f;
                    if (Mathf.Abs(paneX) < 54f && y > 28 && y < h - 30)
                    {
                        float glare = Mathf.Max(0f, Mathf.Sin((x + y * 1.4f) * 0.07f)) * 0.14f;
                        albedo = new Color(0.14f + glare, 0.22f + glare, 0.28f + glare, 1f);
                        met = 0.35f; smooth = 0.90f;

                        if (Mathf.Abs(y - 84) < 4)
                        {
                            albedo = new Color(0.42f, 0.26f, 0.15f, 1f);
                            met = 0.15f; smooth = 0.5f;
                        }
                        else if (dx < 0 && y > 32 && y < 78)
                        {
                            float fx = dx + 66f;
                            for (int b = -1; b <= 1; b++)
                            {
                                float bx = fx - b * 32f;
                                float flaskR = (y < 54) ? 11.5f - Mathf.Abs(y - 42) * 0.35f : 3.8f;
                                if (Mathf.Abs(bx) < flaskR)
                                {
                                    norm = new Vector3(bx / flaskR, (y - 44f) / 20f, 0.75f);
                                    albedo = (b == -1) ? new Color(0.25f, 0.82f, 0.96f, 1f) :
                                             (b == 0 ? new Color(0.96f, 0.64f, 0.18f, 1f) : new Color(0.78f, 0.30f, 0.90f, 1f));
                                    met = 0.45f; smooth = 0.95f;
                                }
                            }
                        }
                        else if (dx > 0 && y > 92 && y < 134)
                        {
                            float tx = dx - 66f;
                            for (int t = -1; t <= 1; t++)
                            {
                                float px = tx - t * 30f;
                                float pincer = Mathf.Abs(Mathf.Abs(px) - Mathf.Sin((y - 92f) * 0.12f) * 5f);
                                if (pincer < 2.4f)
                                {
                                    norm = new Vector3(px / 10f, 0f, 0.8f);
                                    albedo = new Color(0.86f, 0.89f, 0.94f, 1f);
                                    met = 0.92f; smooth = 0.90f;
                                }
                            }
                        }
                    }

                    outCol = EvaluatePBRPixel(albedo, norm, met, smooth, lightDir);
                }
                cols[y * w + x] = outCol;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateMasterStandSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        float cx = w * 0.5f, cy = h * 0.5f;
        Vector3 lightDir = new Vector3(-0.4f, 0.6f, 0.7f).normalized;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                Color outCol = Color.clear;

                float waveW = 112f + Mathf.Sin(y * 0.09f) * 6f;
                float waveH = 74f + Mathf.Cos(x * 0.08f) * 5f;

                if (Mathf.Abs(dx) <= waveW && Mathf.Abs(dy) <= waveH)
                {
                    float grain = Mathf.Sin((x + y * 0.5f) * 0.12f) * 0.04f;
                    Vector3 norm = new Vector3(dx / waveW * 0.35f, dy / waveH * 0.35f, 0.88f);
                    Color albedo = new Color(0.36f + grain, 0.22f + grain, 0.14f, 1f);
                    float met = 0.2f, smooth = 0.6f;

                    if (Mathf.Abs(Mathf.Abs(dx) - (waveW - 8f)) < 2.8f || Mathf.Abs(Mathf.Abs(dy) - (waveH - 8f)) < 2.8f)
                    {
                        albedo = new Color(0.90f, 0.72f, 0.28f, 1f);
                        met = 0.88f; smooth = 0.88f;
                    }

                    for (int s = -1; s <= 1; s += 2)
                    {
                        float sx = dx - s * 46f;
                        float scrollW = 30f + Mathf.Sin(y * 0.15f) * 2.5f;
                        if (Mathf.Abs(sx) < scrollW && Mathf.Abs(dy) < 46f)
                        {
                            norm = new Vector3(sx / scrollW * 0.4f, 0f, 0.9f);
                            albedo = new Color(0.94f, 0.87f, 0.70f, 1f);
                            met = 0.05f; smooth = 0.35f;

                            if (y % 9 == 0 && Mathf.Abs(sx) < 20f && dy > -20f)
                                albedo = new Color(0.32f, 0.22f, 0.15f, 1f);

                            float sealD = Vector2.Distance(new Vector2(sx, dy), new Vector2(8f, -28f));
                            if (sealD < 9.5f)
                            {
                                norm = new Vector3((sx - 8f) / 9.5f, (dy + 28f) / 9.5f, 0.7f);
                                albedo = new Color(0.82f, 0.14f, 0.16f, 1f);
                                met = 0.3f; smooth = 0.78f;
                            }
                        }
                    }

                    outCol = EvaluatePBRPixel(albedo, norm, met, smooth, lightDir);
                }
                cols[y * w + x] = outCol;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateDetailedArtisanSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];
        float cx = w * 0.5f;
        Vector3 lightDir = new Vector3(-0.4f, 0.55f, 0.75f).normalized;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                Color outCol = Color.clear;

                float sh = (dx * dx) / (36f * 36f) + ((y - 14f) * (y - 14f)) / (11f * 11f);
                if (sh <= 1f) outCol = new Color(0f, 0f, 0f, (1f - sh) * 0.55f);

                for (int leg = -1; leg <= 1; leg += 2)
                {
                    float lx = dx - leg * 11f;
                    if (y >= 12 && y <= 30 && Mathf.Abs(lx) < 7.8f - (y - 12) * 0.1f)
                    {
                        Vector3 bNorm = new Vector3(lx / 8f, (y - 20f) / 10f, 0.8f);
                        outCol = EvaluatePBRPixel(new Color(0.24f, 0.15f, 0.10f, 1f), bNorm, 0.25f, 0.65f, lightDir);
                    }
                }

                float torsoCurve = 23f + Mathf.Sin(((y - 28f) / 58f) * Mathf.PI) * 10.5f;
                if (y >= 26 && y <= 86 && Mathf.Abs(dx) <= torsoCurve)
                {
                    Vector3 norm = new Vector3(dx / torsoCurve * 0.7f, (y - 56f) / 40f * 0.3f, 0.75f);
                    float folds = Mathf.Sin(dx * 0.45f + y * 0.2f) * 0.04f;
                    Color albedo = new Color(0.20f + folds, 0.36f + folds, 0.52f + folds, 1f);
                    float met = 0.08f, smooth = 0.35f;

                    if (Mathf.Abs(dx) > torsoCurve - 7.5f && y > 36 && y < 52)
                    {
                        albedo = new Color(0.95f, 0.79f, 0.65f, 1f);
                        smooth = 0.45f;
                    }

                    float apronW = (y < 66) ? 18.5f : 11.5f + (86f - y) * 0.35f;
                    if (Mathf.Abs(dx) <= apronW && y >= 28 && y <= 82)
                    {
                        float leather = Mathf.Sin(x * 0.35f) * Mathf.Cos(y * 0.35f) * 0.025f;
                        albedo = new Color(0.54f + leather, 0.31f + leather, 0.15f, 1f);
                        met = 0.22f; smooth = 0.62f;

                        if (y >= 49 && y <= 54)
                        {
                            if (Mathf.Abs(dx) < 5.5f) { albedo = new Color(0.94f, 0.76f, 0.28f, 1f); met = 0.9f; smooth = 0.9f; }
                            else { albedo = new Color(0.24f, 0.14f, 0.08f, 1f); met = 0.3f; smooth = 0.7f; }
                        }
                        else if (dx > 3f && dx < 13f && y > 34 && y < 46)
                        {
                            albedo = new Color(0.44f, 0.24f, 0.11f, 1f);
                            if (Mathf.Abs(dx - 8f) < 1.6f && y > 41) { albedo = new Color(0.88f, 0.92f, 0.96f, 1f); met = 0.92f; smooth = 0.92f; }
                        }
                    }

                    if (y > 78 && Mathf.Abs(dx) < 11f)
                        albedo = new Color(0.94f, 0.90f, 0.82f, 1f);

                    outCol = EvaluatePBRPixel(albedo, norm, met, smooth, lightDir);
                }

                float headY = y - 105f;
                float hairWave = Mathf.Sin(Mathf.Atan2(headY, dx) * 9f) * 1.8f;
                float headDist = Mathf.Sqrt(dx * dx * 1.1f + headY * headY);

                if (headDist <= 20f + (headY > 2f ? hairWave : 0f))
                {
                    Vector3 hNorm = new Vector3(dx / 20f, headY / 20f, 0.8f);
                    Color albedo = new Color(0.96f, 0.80f, 0.66f, 1f);
                    float met = 0.05f, smooth = 0.45f;

                    if (headY > 5f + Mathf.Cos(dx * 0.25f) * 3f || Mathf.Abs(dx) > 14.8f)
                    {
                        float strand = Mathf.Sin(dx * 0.8f + y * 0.3f) * 0.05f;
                        albedo = new Color(0.28f + strand, 0.18f + strand, 0.12f, 1f);
                        smooth = 0.55f;
                    }
                    else if (Mathf.Abs(headY + 3f) < 1.6f && Mathf.Abs(Mathf.Abs(dx) - 6f) < 2.2f)
                    {
                        albedo = new Color(0.20f, 0.15f, 0.12f, 1f);
                    }

                    if (headY >= 1f && headY <= 8.5f && Mathf.Abs(dx) < 15.5f)
                    {
                        albedo = new Color(0.86f, 0.68f, 0.24f, 1f);
                        met = 0.88f; smooth = 0.88f;
                        float lensD = Mathf.Min(Vector2.Distance(new Vector2(dx, headY), new Vector2(-6.5f, 4.5f)),
                                                Vector2.Distance(new Vector2(dx, headY), new Vector2(6.5f, 4.5f)));
                        if (lensD < 4.6f)
                        {
                            if (lensD < 3.3f) { albedo = new Color(0.32f, 0.90f, 0.98f, 1f); met = 0.5f; smooth = 0.96f; }
                            else { albedo = new Color(0.96f, 0.80f, 0.32f, 1f); met = 0.92f; }
                        }
                    }

                    outCol = EvaluatePBRPixel(albedo, hNorm, met, smooth, lightDir);
                }

                cols[y * w + x] = outCol;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.10f));
    }

    private Sprite CreateWaxSealSprite(int size)
    {
        Texture2D tex = CreateTrackedTexture(size, size);
        Color32[] cols = new Color32[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        Vector3 lightDir = new Vector3(-0.4f, 0.6f, 0.7f).normalized;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float ang = Mathf.Atan2(dy, dx);
                float waveR = size * 0.44f + Mathf.Sin(ang * 10f) * 2.2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(waveR - d + 0.8f);

                Vector3 norm = new Vector3(dx / waveR, dy / waveR, 0.75f);
                if (Mathf.Abs(d - size * 0.30f) < 2.5f) norm.z = 0.35f;
                Color pbr = EvaluatePBRPixel(Color.white, norm, 0.35f, 0.75f, lightDir);
                pbr.a = a;
                cols[y * size + x] = pbr;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateJewelerClampSprite(int size)
    {
        Texture2D tex = CreateTrackedTexture(size, size);
        Color32[] cols = new Color32[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        Vector3 lightDir = new Vector3(-0.4f, 0.6f, 0.7f).normalized;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float ang = Mathf.Atan2(dy, dx);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float gearR = size * 0.42f + Mathf.Cos(ang * 8f) * 3.5f;

                Color c = Color.clear;
                if (d <= gearR)
                {
                    Vector3 norm = new Vector3(dx / gearR, dy / gearR, 0.7f);
                    Color baseCol = (d > size * 0.28f) ? new Color(0.95f, 0.78f, 0.28f, 1f) : new Color(0.86f, 0.30f, 0.14f, 1f);
                    c = EvaluatePBRPixel(baseCol, norm, 0.85f, 0.85f, lightDir);
                    c.a = Mathf.Clamp01(gearR - d + 0.8f);
                }
                cols[y * size + x] = c;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateTimerRingSprite(int size)
    {
        Texture2D tex = CreateTrackedTexture(size, size);
        Color32[] cols = new Color32[size * size];
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(2.2f - Mathf.Abs(d - size * 0.43f));
                cols[y * size + x] = new Color(1f, 0.88f, 0.35f, a);
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateRadialGlowSprite(int size)
    {
        Texture2D tex = CreateTrackedTexture(size, size);
        Color32[] cols = new Color32[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxR;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                cols[y * size + x] = new Color(1f, 0.92f, 0.72f, a);
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateLoupeSprite(int size)
    {
        Texture2D tex = CreateTrackedTexture(size, size);
        Color32[] cols = new Color32[size * size];
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float outerR = size * 0.45f;
        float innerR = size * 0.36f;
        Vector3 lightDir = new Vector3(-0.45f, 0.6f, 0.7f).normalized;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                Color col = Color.clear;

                if (d < innerR)
                {
                    float glare = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), c + new Vector2(-innerR * 0.35f, innerR * 0.35f)) / (innerR * 0.45f));
                    col = new Color(0.75f + glare * 0.25f, 0.94f + glare * 0.06f, 1f, 0.20f + glare * 0.40f);
                }
                else if (d <= outerR)
                {
                    float t = (d - innerR) / (outerR - innerR);
                    Vector3 norm = new Vector3((x - c.x) / outerR, (y - c.y) / outerR, Mathf.Sin(t * Mathf.PI));
                    col = EvaluatePBRPixel(new Color(0.92f, 0.74f, 0.26f, 1f), norm, 0.92f, 0.90f, lightDir);
                    col.a = Mathf.Clamp01(outerR - d + 1f);
                }
                cols[y * size + x] = col;
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void GenerateWindowOrderChoices()
    {
        currentWindowChoices.Clear();

        if (secretViolated && completedOrdersCount >= 1)
        {
            currentWindowChoices.Add(CreateCheapOrder(1));
            currentWindowChoices.Add(CreateCheapOrder(2));
            currentWindowChoices.Add(CreateCheapOrder(3));
            return;
        }

        OrderData ringOrder = new OrderData
        {
            id = "ring_" + completedOrdersCount,
            shapeType = "RING",
            title = "Золотой перстень 1812 года",
            clientName = "Граф В. Шереметев — Коллекционер",
            clientType = "COLLECTOR",
            baseReward = 1400,
            requiredRank = 1,
            shortDesc = "Требуемый разряд: I\nЭтапы: Снятие окислов, быстрая шлифовка зазубрин шинки, закрепка каста.\nПожелание: Сохранить следы возраста.",
            briefingText = "«Фамильный перстень прадеда. Обод в глубоких зазубринах, а под сердоликом что-то тихонько звенит. Стачите зазубрины быстрой шлифовкой, но сохраните благородную патину!»",
            correctMechTool = "RING_GRINDER",
            correctSolvent = "SPIRIT",
            preferredStyle = "PATINA",
            twistType = "SECRET_BOX",
            twistText = "ТАЙНИК В ОПРАВЕ: Во время шлифовки отщелкнулась крышечка под сердоликом! Внутри спрятана миниатюрная шифровка декабристов. Заказчик не знает о тайнике.",
            histPatina = "Перстень сохранил вековую патину. Выправленный каст и гладкая шинка вернули реликвии музейное величие.",
            histNew = "Золото отполировано до зеркального блеска. Коллекционер огорчен: перстень выглядит как современная копия.",
            histSpecial = "Сорванная пломба тайника выдала ваше вмешательство. Слухи о любопытстве мастера отпугнули элитных клиентов."
        };
        ringOrder.diagnostics.Add(new DiagZone(-52, 10, 46, "Металл: Золото 56-й пробы с патиной. Нужен мягкий Спиртовой эфир."));
        ringOrder.diagnostics.Add(new DiagZone(52, -15, 46, "Обод: Зазубрины по кругу. Нужна Бормашина для шлифовки колец."));
        ringOrder.diagnostics.Add(new DiagZone(0, 54, 46, "Каст камня: Ослабли 4 лапки-крапана, внутри нащупывается потайная полость."));

        OrderData diamondOrder = new OrderData
        {
            id = "diamond_" + completedOrdersCount,
            shapeType = "DIAMOND",
            title = "Платиновый солитер с бриллиантом",
            clientName = "Анна Морозова — Невеста",
            clientType = "ORDINARY",
            baseReward = 1750,
            requiredRank = 1,
            shortDesc = "Требуемый разряд: I\nЭтапы: Промывка граней, шлифовка оправы, быстрая выправка крапанов.\nПожелание: Восстановить «Как новое».",
            briefingText = "«Помолвочное украшение бабушки с голубым бриллиантом! Камень потускнел, а лапки-крапаны разболтались. Сделайте его ослепительным — как новое!»",
            correctMechTool = "DIAMOND_FILE",
            correctSolvent = "ACID_GEL",
            preferredStyle = "NEW",
            twistType = "OUT_OF_MATERIAL",
            twistText = "НЕХВАТКА ГЕЛЯ: На середине работы закончился ультра-гель для бриллиантов! Выберите, как завершить промывку нижних граней камня.",
            histPatina = "Темный налет платины сохранен по музейному канону, но невеста расстроена тусклым видом украшения.",
            histNew = "Грани бриллианта вспыхнули чистым голубым огнем, а надежно зажатые крапаны вернули кольцу первозданный вид!",
            histSpecial = "Едкий растворитель и неверный инструмент оставили матовые сколы на гранях бриллианта."
        };
        diamondOrder.diagnostics.Add(new DiagZone(0, 0, 46, "Грани камня: Минеральный налет. Очищается Ювелирным ультра-гелем."));
        diamondOrder.diagnostics.Add(new DiagZone(-42, -35, 46, "Оправа: Заусенцы на платине. Нужен Алмазный пинцет и надфиль."));
        diamondOrder.diagnostics.Add(new DiagZone(42, 35, 46, "Закрепка: Требуется серия очень быстрых кликов для поджима 4 угловых крапанов."));

        OrderData royalOrder = new OrderData
        {
            id = "royal_" + completedOrdersCount,
            shapeType = "LOCKET",
            title = "Императорский орден Фаберже",
            clientName = "Директор Государственного Эрмитажа",
            clientType = "COLLECTOR",
            baseReward = 3800,
            requiredRank = 2,
            shortDesc = "Требуемый разряд: II\nЭтапы: Деликатная чистка эмали, шлифовка канта, склейка микро-сколов.\nВысокий гонорар: 3800 руб.",
            briefingText = "«Коронационный медальон с гильошированной эмалью и сапфиром. Доверяем его только мастеру не ниже II разряда. Изначально готовим к музейной экспозиции.»",
            correctMechTool = "MICRO_SPATULA",
            correctSolvent = "SPIRIT",
            preferredStyle = "PATINA",
            twistType = "CLIENT_CHANGES_MIND",
            twistText = "СРОЧНАЯ ТЕЛЕГРАММА: «Мастер, орден отправляется на дипломатический прием! Посол просит придать медальону парадный сияющий вид 'КАК НОВОЕ' вместо музейной патины!»",
            histPatina = "Вы настояли на музейной патине. Хранители фонда довольны, но организаторы приема урезали премию.",
            histNew = "Гильошированная эмаль засияла глубоким императорским пурпуром и лазурью, став главным событием выставки!",
            histSpecial = "Ошибки при обработке повредили редчайшую эмаль XIX века."
        };
        royalOrder.diagnostics.Add(new DiagZone(-38, 30, 46, "Эмаль: Горячая гильошированная эмаль. Переносит только Спиртовой эфир."));
        royalOrder.diagnostics.Add(new DiagZone(38, -25, 46, "Оправа: Микро-трещины по канту. Нужен Ювелирный микро-шпатель."));
        royalOrder.diagnostics.Add(new DiagZone(0, -48, 46, "Крепление сапфира: Требует быстрой фиксации кликами без пауз."));

        OrderData byzantineOrder = new OrderData
        {
            id = "byzantine_" + completedOrdersCount,
            shapeType = "CROSS",
            title = "Византийский энколпион XII века",
            clientName = "Верховный Патриарший Синод",
            clientType = "COLLECTOR",
            baseReward = 6200,
            requiredRank = 3,
            shortDesc = "Требуемый разряд: III\nРеликвия величайшей ценности. Золотая скань и центральный сапфир.\nРекордный гонорар: 6200 руб.",
            briefingText = "«Древнейший крест-энколпион времен Царьграда. Вековой налет скрывает византийскую скань, а заклепки каста ослабли. Работа только для признанного Гранд-мастера III разряда!»",
            correctMechTool = "MICRO_SPATULA",
            correctSolvent = "SPIRIT",
            preferredStyle = "PATINA",
            twistType = "SECRET_BOX",
            twistText = "ДРЕВНИЙ КОВЧЕГ: Внутренняя полость энколпиона приоткрылась! Внутри сокрыта священная реликвия на пожелтевшем пергаменте.",
            histPatina = "Древнее золото сохранило сакральную патину тысячи лет, став жемчужиной сокровищницы.",
            histNew = "Древний крест отполирован до современного блеска, утратив ощущение дыхания византийской эпохи.",
            histSpecial = "Повреждение святыни вызвало скандал в высших церковных кругах."
        };
        byzantineOrder.diagnostics.Add(new DiagZone(-30, 10, 46, "Золотая скань: Тончайшая филигрань. Очищается только мягким Спиртовым эфиром."));
        byzantineOrder.diagnostics.Add(new DiagZone(30, 10, 46, "Лучи энколпиона: Зазубрины на торцах. Требуется Ювелирный микро-шпатель."));
        byzantineOrder.diagnostics.Add(new DiagZone(0, -35, 46, "Крепление сапфира: 4 штифта требуют серии предельно быстрых кликов без задержек."));

        List<OrderData> availableMasterpieces = new List<OrderData>();
        if (!completedOrderTypes.Contains("RING")) availableMasterpieces.Add(ringOrder);
        if (!completedOrderTypes.Contains("DIAMOND")) availableMasterpieces.Add(diamondOrder);
        if (!completedOrderTypes.Contains("LOCKET")) availableMasterpieces.Add(royalOrder);
        if (!completedOrderTypes.Contains("CROSS")) availableMasterpieces.Add(byzantineOrder);

        for (int i = 0; i < availableMasterpieces.Count && currentWindowChoices.Count < 3; i++)
            currentWindowChoices.Add(availableMasterpieces[i]);

        int cheapIdx = 1;
        while (currentWindowChoices.Count < 3)
        {
            currentWindowChoices.Add(CreateCheapOrder(cheapIdx));
            cheapIdx++;
        }
    }

    private OrderData CreateCheapOrder(int idx)
    {
        OrderData cheap = new OrderData
        {
            id = "cheap_" + idx,
            shapeType = "RING",
            title = $"Латунный перстень №{idx}",
            clientName = "Случайный прохожий",
            clientType = "ORDINARY",
            baseReward = 350,
            requiredRank = 1,
            shortDesc = "Требуемый разряд: I\nШтампованная бижутерия. Из-за вскрытого тайника крупные коллекционеры обходят мастерскую стороной.",
            briefingText = "«После той истории со вскрытым тайником антиквары к вам не ходят. Вот простое латунное колечко, снимите заусенцы и отполируйте как новое.»",
            correctMechTool = "RING_GRINDER",
            correctSolvent = "ACETONE",
            preferredStyle = "NEW",
            twistType = "NONE",
            twistText = "",
            histPatina = "Тусклая латунь не обрадовала владельца.",
            histNew = "Колечко блестит, но в нем нет ни тайны, ни большой истории.",
            histSpecial = "Даже простая латунная вещь оказалась безнадежно испорчена."
        };
        cheap.diagnostics.Add(new DiagZone(-45, 0, 46, "Сплав: Обычная латунь. Очищается Техническим ацетоном."));
        cheap.diagnostics.Add(new DiagZone(45, 0, 46, "Обод: Заусенцы литья. Нужна Бормашина для шлифовки колец."));
        cheap.diagnostics.Add(new DiagZone(0, 45, 46, "Крепление: Требует быстрой точечной подгонки кликами."));
        return cheap;
    }

    private GameObject CreatePanel(Transform parent, string name, Color col, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Sprite customSprite = null, bool sliced = false, bool addShadow = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.color = col;
        if (customSprite != null)
        {
            img.sprite = customSprite;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        }
        if (addShadow)
        {
            Shadow sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.62f);
            sh.effectDistance = new Vector2(4f, -5f);
        }
        return go;
    }

    private Text CreateText(Transform parent, string name, string content, int size, Color col, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 delta, TextAnchor align, bool bold = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = delta;
        Text t = go.AddComponent<Text>();
        t.font = GetSafeFont();
        t.text = content;
        t.fontSize = size;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.color = col;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        Shadow sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sh.effectDistance = new Vector2(1.4f, -1.4f);
        return t;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgCol, UnityEngine.Events.UnityAction onClick, out Text labelTxt, out Image bgImg)
    {
        GameObject go = CreatePanel(parent, name, bgCol, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, spBeveledButton, true, true);
        bgImg = go.GetComponent<Image>();
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bgImg;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.20f, 1.20f, 1.20f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.06f;
        btn.colors = cb;

        labelTxt = CreateText(go.transform, "Label", label, 14, new Color(0.98f, 0.95f, 0.88f, 1f), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-14, -6), TextAnchor.MiddleCenter, true);
        btn.onClick.AddListener(() => {
            onClick();
        });

        EventTrigger et = go.AddComponent<EventTrigger>();

        EventTrigger.Entry pEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pEnter.callback.AddListener((d) => {
            go.transform.localScale = new Vector3(1.035f, 1.035f, 1f);
            PlaySFX(clipStep, 0.08f, 2.4f);
        });
        et.triggers.Add(pEnter);

        EventTrigger.Entry pExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pExit.callback.AddListener((d) => {
            go.transform.localScale = Vector3.one;
        });
        et.triggers.Add(pExit);

        EventTrigger.Entry pDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pDown.callback.AddListener((d) => {
            go.transform.localScale = new Vector3(0.94f, 0.94f, 1f);
            PlaySFX(clipClickPin, 0.45f, 1.0f);
        });
        et.triggers.Add(pDown);

        EventTrigger.Entry pUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pUp.callback.AddListener((d) => {
            go.transform.localScale = new Vector3(1.035f, 1.035f, 1f);
            PlaySFX(clipClickPin, 0.22f, 1.35f);
        });
        et.triggers.Add(pUp);

        return btn;
    }

    private Button AddCornerCloseButton(Transform parentOverlay, UnityEngine.Events.UnityAction onClose)
    {
        Button btn = CreateButton(parentOverlay, "CloseCornerBtn", "✕", Vector2.zero, new Vector2(38, 38), new Color(0.52f, 0.20f, 0.18f, 0.95f), () => {
            PlaySFX(clipClickPin, 0.45f, 0.9f);
            onClose();
        }, out Text labelTxt, out _);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-16f, -16f);
        labelTxt.fontSize = 20;
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.color = new Color(1f, 0.94f, 0.82f, 1f);
        TooltipTrigger.Attach(btn.gameObject, "OVERLAY_CLOSE_CORNER");
        return btn;
    }

    private void AddStationClickTrigger(GameObject stationGO, Vector2 interactPos, string stationId)
    {
        Button b = stationGO.AddComponent<Button>();
        b.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.12f, 1.05f, 1f);
        cb.pressedColor = new Color(0.92f, 0.90f, 0.86f, 1f);
        b.colors = cb;
        b.onClick.AddListener(() => {
            if (viewState == "ROOM")
            {
                PlaySFX(clipClickPin, 0.35f, 1.15f);
                MovePlayerToStation(interactPos, stationId);
            }
        });

        string tip = (stationId == "WINDOW") ? "ОКНО ЗАКАЗОВ (ЛКМ): Выбрать старинное украшение или сдать готовую работу заказчику." :
                     ((stationId == "WORKBENCH") ? "РАБОЧИЙ СТОЛ (ЛКМ): Диагностика дефектов под лупой и 4 этапа ручной реставрации." :
                     ((stationId == "CABINET") ? "ШКАФ ИНСТРУМЕНТОВ (ЛКМ): Подбор ювелирных насадок, химии и финишного стиля." :
                     "СТЕНД МАСТЕРА И МУЗЕЙ (ЛКМ): Инструменты, сертификаты разрядов и Галерея отреставрированных шедевров."));

        EventTrigger trigger = stationGO.AddComponent<EventTrigger>();
        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((data) => {
            if (viewState == "ROOM") bottomHintText.text = tip;
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((data) => {
            if (viewState == "ROOM") UpdateRoomIndicators();
        });
        trigger.triggers.Add(exit);
    }

    private void BuildAllUI()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasGO = new GameObject("RestorerWorkshopCanvas");
        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720);
        cs.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject tooltipGO = new GameObject("TooltipRoot");
        tooltipGO.transform.SetParent(canvasGO.transform, false);
        tooltipGO.AddComponent<TooltipManager>();

        GameObject rootBG = CreatePanel(canvasGO.transform, "RootBG", new Color(0.08f, 0.06f, 0.05f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Sprite woodSprite = CreateWoodFloorPBRSprite(512, 256);
        GameObject roomGO = CreatePanel(rootBG.transform, "RoomArea", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(1240, 580), woodSprite, false, true);
        roomAreaRT = roomGO.GetComponent<RectTransform>();

        GameObject tintGO = CreatePanel(roomAreaRT, "AmbientTintOverlay", new Color(0.72f, 0.84f, 1f, 0.05f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        tintGO.GetComponent<Image>().raycastTarget = false;
        ambientTintImg = tintGO.GetComponent<Image>();

        Sprite rugSprite = CreateOrnateRugPBRSprite(320, 160);
        GameObject rug = CreatePanel(roomAreaRT, "WorkshopRug", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10, -45), new Vector2(540, 260), rugSprite, false, true);
        rug.GetComponent<Image>().raycastTarget = false;

        GameObject topWall = CreatePanel(roomAreaRT, "TopWall", Color.white, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), new Vector2(0, 128), spVictorianWall, true, true);
        topWall.GetComponent<Image>().raycastTarget = false;
        GameObject trimTop = CreatePanel(topWall.transform, "WoodTrim", new Color(0.52f, 0.34f, 0.18f, 1f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 8));
        trimTop.GetComponent<Image>().raycastTarget = false;

        GameObject leftWall = CreatePanel(roomAreaRT, "LeftWall", Color.white, new Vector2(0, 0), new Vector2(0, 1), new Vector2(72, 0), new Vector2(144, 0), spVictorianWall, true, true);
        leftWall.GetComponent<Image>().raycastTarget = false;

        GameObject godRaysGO = CreatePanel(roomAreaRT, "SunlightGodRays", new Color(1f, 0.95f, 0.75f, 0.20f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-210f, -40f), new Vector2(580f, 320f), spGodRays);
        godRaysGO.GetComponent<Image>().raycastTarget = false;
        sunlightGodRaysImg = godRaysGO.GetComponent<Image>();

        GameObject vigGO = CreatePanel(roomAreaRT, "RoomVignetteOverlay", new Color(0.04f, 0.02f, 0.01f, 0.55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, spRoomVignette);
        vigGO.GetComponent<Image>().raycastTarget = false;
        roomVignetteImg = vigGO.GetComponent<Image>();

        GameObject winGlow = CreatePanel(roomAreaRT, "WinLampGlow", new Color(1f, 0.86f, 0.55f, 0.30f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-440, -15), new Vector2(380, 380), spSoftLightGlow);
        winGlow.GetComponent<Image>().raycastTarget = false;
        winGlowImg = winGlow.GetComponent<Image>();

        GameObject winStation = CreatePanel(roomAreaRT, "Station_Window", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-485, -15), new Vector2(220, 265), spStationWindow, false, true);
        AddStationClickTrigger(winStation, posWindowInteract, "WINDOW");
        TooltipTrigger.Attach(winStation, "STATION_WINDOW");
        CreateText(winStation.transform, "WinTitle", "ОКНО ЗАКАЗОВ", 17, new Color(1f, 0.95f, 0.78f, 1f), Vector2.zero, Vector2.one, new Vector2(0, -82), new Vector2(-12, -12), TextAnchor.MiddleCenter, true);
        GameObject winBadge = CreatePanel(winStation.transform, "Badge", new Color(0.25f, 0.92f, 0.38f, 1f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -22), new Vector2(32, 32), spWaxSealBadge, false, true);
        windowBadgeImg = winBadge.GetComponent<Image>();
        windowBadgeImg.raycastTarget = false;
        CreateText(winBadge.transform, "Excl", "!", 19, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

        GameObject fireGlow = CreatePanel(roomAreaRT, "HearthGlow", new Color(1f, 0.65f, 0.28f, 0.32f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-270, 185), new Vector2(420, 390), spSoftLightGlow);
        fireGlow.GetComponent<Image>().raycastTarget = false;
        fireplaceGlowImg = fireGlow.GetComponent<Image>();

        GameObject fireStation = CreatePanel(roomAreaRT, "Station_Fireplace", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-270, 195), new Vector2(220, 175), spFireplace, false, true);
        AddStationClickTrigger(fireStation, posFireplaceInteract, "FIREPLACE");
        TooltipTrigger.Attach(fireStation, "STATION_FIREPLACE");
        CreateText(fireStation.transform, "FireplaceTitle", "ЧУГУННЫЙ КАМИН", 16, new Color(1f, 0.90f, 0.72f, 1f), Vector2.zero, Vector2.one, new Vector2(0, -60), new Vector2(-14, -14), TextAnchor.MiddleCenter, true);

        Vector2[] flamePos = { new Vector2(-10f, -22f), new Vector2(-30f, -26f), new Vector2(12f, -26f), new Vector2(-20f, -28f), new Vector2(2f, -28f) };
        Vector2[] flameSizes = { new Vector2(32f, 54f), new Vector2(26f, 46f), new Vector2(26f, 46f), new Vector2(20f, 38f), new Vector2(20f, 38f) };
        float[] flameFreqs = { 7.5f, 11.2f, 13.8f, 16.4f, 9.8f };
        float[] flamePhs = { 0f, 1.4f, 2.8f, 4.2f, 0.7f };
        for (int i = 0; i < 5; i++)
        {
            GameObject flm = CreatePanel(fireStation.transform, "FlameTongue_" + i, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), flamePos[i], flameSizes[i], spFlameTongue);
            flm.GetComponent<Image>().raycastTarget = false;
            flameTongueRTs[i] = flm.GetComponent<RectTransform>();
            flameBaseSizes[i] = flameSizes[i];
            flameFrequencies[i] = flameFreqs[i];
            flamePhases[i] = flamePhs[i];
        }

        GameObject candleMantelL = CreatePanel(fireStation.transform, "CandleMantelL", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-65f, 75f), new Vector2(14f, 28f), spCandleStick);
        candleMantelL.GetComponent<Image>().raycastTarget = false;
        GameObject cFlameML = CreatePanel(candleMantelL.transform, "Flame", new Color(1f, 0.92f, 0.45f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 5f), new Vector2(7f, 12f), spFlameTongue);
        cFlameML.GetComponent<Image>().raycastTarget = false;

        GameObject candleMantelR = CreatePanel(fireStation.transform, "CandleMantelR", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(65f, 75f), new Vector2(14f, 28f), spCandleStick);
        candleMantelR.GetComponent<Image>().raycastTarget = false;
        GameObject cFlameMR = CreatePanel(candleMantelR.transform, "Flame", new Color(1f, 0.92f, 0.45f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 5f), new Vector2(7f, 12f), spFlameTongue);
        cFlameMR.GetComponent<Image>().raycastTarget = false;

        GameObject sparksPoolRoot = CreatePanel(roomAreaRT, "FireplaceSparksRoot", new Color(1, 1, 1, 0), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-270f, 195f), new Vector2(200f, 200f));
        sparksPoolRoot.GetComponent<Image>().raycastTarget = false;
        fireplaceSparkContainer = sparksPoolRoot.transform;
        fireplaceSparks.Clear();
        for (int i = 0; i < 20; i++)
        {
            GameObject spkGO = CreatePanel(fireplaceSparkContainer, "Spark_" + i, new Color(1f, 0.85f, 0.35f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f), spSoftLightGlow);
            spkGO.GetComponent<Image>().raycastTarget = false;
            fireplaceSparks.Add(new FireplaceSpark
            {
                rt = spkGO.GetComponent<RectTransform>(),
                img = spkGO.GetComponent<Image>(),
                pos = Vector2.zero,
                vel = Vector2.zero,
                life = 0f,
                maxLife = 1f,
                wobblePhase = Random.Range(0f, Mathf.PI * 2f)
            });
        }

        GameObject benchGlow = CreatePanel(roomAreaRT, "BenchLampGlow", new Color(1f, 0.90f, 0.60f, 0.36f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 175), new Vector2(490, 360), spSoftLightGlow);
        benchGlow.GetComponent<Image>().raycastTarget = false;
        benchGlowImg = benchGlow.GetComponent<Image>();

        GameObject benchStation = CreatePanel(roomAreaRT, "Station_Workbench", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 195), new Vector2(350, 158), spStationWorkbench, false, true);
        AddStationClickTrigger(benchStation, posBenchInteract, "WORKBENCH");
        TooltipTrigger.Attach(benchStation, "STATION_WORKBENCH");
        GameObject itemOnBench = CreatePanel(benchStation.transform, "ItemOnBench", new Color(1f, 1f, 1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-50, 4), new Vector2(54, 54));
        benchItemPreviewImg = itemOnBench.GetComponent<Image>();
        benchItemPreviewImg.raycastTarget = false;

        GameObject candleBench = CreatePanel(benchStation.transform, "BenchCandle", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(140f, 45f), new Vector2(16f, 32f), spCandleStick);
        candleBench.GetComponent<Image>().raycastTarget = false;
        GameObject candleBenchFlame = CreatePanel(candleBench.transform, "Flame", new Color(1f, 0.92f, 0.45f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(8f, 14f), spFlameTongue);
        candleBenchFlame.GetComponent<Image>().raycastTarget = false;
        candleFlameBenchRT = candleBenchFlame.GetComponent<RectTransform>();
        GameObject candleBenchGlow = CreatePanel(candleBench.transform, "Glow", new Color(1f, 0.85f, 0.4f, 0.25f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(48f, 48f), spSoftLightGlow);
        candleBenchGlow.GetComponent<Image>().raycastTarget = false;

        CreateText(benchStation.transform, "BenchTitle", "РАБОЧИЙ СТОЛ", 18, new Color(1f, 0.96f, 0.82f, 1f), Vector2.zero, Vector2.one, new Vector2(0, -45), new Vector2(-16, -16), TextAnchor.MiddleCenter, true);
        GameObject benchBadge = CreatePanel(benchStation.transform, "Badge", new Color(0.25f, 0.92f, 0.38f, 1f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-22, -20), new Vector2(32, 32), spWaxSealBadge, false, true);
        benchBadgeImg = benchBadge.GetComponent<Image>();
        benchBadgeImg.raycastTarget = false;
        CreateText(benchBadge.transform, "Excl", "!", 19, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

        GameObject cabGlow = CreatePanel(roomAreaRT, "CabLampGlow", new Color(0.75f, 0.92f, 1f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(375, 175), new Vector2(370, 310), spSoftLightGlow);
        cabGlow.GetComponent<Image>().raycastTarget = false;
        cabGlowImg = cabGlow.GetComponent<Image>();

        GameObject cabStation = CreatePanel(roomAreaRT, "Station_Cabinet", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(375, 195), new Vector2(300, 158), spStationCabinet, false, true);
        AddStationClickTrigger(cabStation, posCabinetInteract, "CABINET");
        TooltipTrigger.Attach(cabStation, "STATION_CABINET");
        CreateText(cabStation.transform, "CabTitle", "ШКАФ ИНСТРУМЕНТОВ", 17, new Color(0.98f, 0.92f, 0.78f, 1f), Vector2.zero, Vector2.one, new Vector2(0, -45), new Vector2(-16, -16), TextAnchor.MiddleCenter, true);
        GameObject cabBadge = CreatePanel(cabStation.transform, "Badge", new Color(0.25f, 0.92f, 0.38f, 1f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-22, -20), new Vector2(32, 32), spWaxSealBadge, false, true);
        cabinetBadgeImg = cabBadge.GetComponent<Image>();
        cabinetBadgeImg.raycastTarget = false;
        CreateText(cabBadge.transform, "Excl", "!", 19, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

        GameObject upgGlow = CreatePanel(roomAreaRT, "UpgGlow", new Color(1f, 0.82f, 0.40f, 0.24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(465, -135), new Vector2(320, 290), spSoftLightGlow);
        upgGlow.GetComponent<Image>().raycastTarget = false;

        GameObject upgStation = CreatePanel(roomAreaRT, "Station_Upgrades", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(475, -135), new Vector2(240, 170), spStationMasterStand, false, true);
        AddStationClickTrigger(upgStation, posUpgradesInteract, "UPGRADES");
        TooltipTrigger.Attach(upgStation, "STATION_UPGRADES");
        CreateText(upgStation.transform, "UpgTitle", "СТЕНД МАСТЕРА / МУЗЕЙ", 16, new Color(1f, 0.92f, 0.62f, 1f), Vector2.zero, Vector2.one, new Vector2(0, -52), new Vector2(-14, -14), TextAnchor.MiddleCenter, true);

        GameObject catStation = CreatePanel(roomAreaRT, "Station_Cat", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-235, -180), new Vector2(100, 72), spWorkshopCat, false, true);
        TooltipTrigger.Attach(catStation, "STATION_CAT");
        catRT = catStation.GetComponent<RectTransform>();

        GameObject catTailGO = CreatePanel(catStation.transform, "CatTail", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-34f, -14f), new Vector2(32f, 44f), spCatTail);
        catTailGO.GetComponent<Image>().raycastTarget = false;
        catTailRT = catTailGO.GetComponent<RectTransform>();
        catTailRT.pivot = new Vector2(0.85f, 0.20f);

        GameObject earL = CreatePanel(catStation.transform, "CatLeftEar", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16f, 24f), new Vector2(18f, 22f), spCatEarLeft);
        earL.GetComponent<Image>().raycastTarget = false;
        catLeftEarRT = earL.GetComponent<RectTransform>();
        catLeftEarRT.pivot = new Vector2(0.5f, 0.1f);

        GameObject earR = CreatePanel(catStation.transform, "CatRightEar", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 24f), new Vector2(18f, 22f), spCatEarRight);
        earR.GetComponent<Image>().raycastTarget = false;
        catRightEarRT = earR.GetComponent<RectTransform>();
        catRightEarRT.pivot = new Vector2(0.5f, 0.1f);

        Button catBtn = catStation.AddComponent<Button>();
        catBtn.onClick.AddListener(() => {
            if (viewState == "ROOM")
            {
                PlaySFX(clipPurr, 0.85f, 1.0f);
                catClickScale = new Vector3(1.18f, 0.88f, 1f);
                catRT.localScale = new Vector3(1.18f, 0.88f, 1f);
                EmitSparks(new Vector2(-235, -180), 8, new Color(1f, 0.88f, 0.60f, 1f));
                overScrubDamage = Mathf.Max(0f, overScrubDamage - 3f);
                barsikBuffTimer = 60f;
                if (barsikBuffRoot != null) barsikBuffRoot.SetActive(true);
                if (barsikBuffText != null) barsikBuffText.text = "🐾 Барсик: 01:00";
                SpawnFloatingText(new Vector2(-235, -145), "Мурр... Спокойствие рук (60 сек)", new Color(1f, 0.90f, 0.65f, 1f));
                bottomHintText.text = "Кот Барсик уютно замурчал. Рука мастера спокойна: риск перетирания снижен на 50% (таймер 60 сек)!";
                UpdateTopBar();
            }
        });
        CreateText(catStation.transform, "CatName", "Кот Барсик", 12, new Color(1f, 0.92f, 0.72f, 0.9f), new Vector2(0, 0), new Vector2(1, 0.35f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

        GameObject markerGO = CreatePanel(roomAreaRT, "ClickMarker", new Color(1f, 0.88f, 0.35f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36), spWaxSealBadge);
        markerGO.GetComponent<Image>().raycastTarget = false;
        clickMarkerRT = markerGO.GetComponent<RectTransform>();

        dustMotes.Clear();
        for (int i = 0; i < 16; i++)
        {
            GameObject dGO = CreatePanel(roomAreaRT, "Dust_" + i, new Color(1f, 0.92f, 0.70f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f), spSoftLightGlow);
            dGO.GetComponent<Image>().raycastTarget = false;
            dustMotes.Add(new DustMote
            {
                rt = dGO.GetComponent<RectTransform>(),
                img = dGO.GetComponent<Image>(),
                basePos = new Vector2(Random.Range(-550f, 550f), Random.Range(-220f, 180f)),
                speedX = Random.Range(10f, 25f),
                speedY = Random.Range(6f, 16f),
                phase = Random.Range(0f, Mathf.PI * 2f),
                radius = Random.Range(18f, 42f)
            });
        }

        GameObject playerGO = CreatePanel(roomAreaRT, "PlayerCharacter", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), playerPos, new Vector2(98, 126), spArtisanChar);
        playerGO.GetComponent<Image>().raycastTarget = false;
        playerRT = playerGO.GetComponent<RectTransform>();

        GameObject bubbleGO = CreatePanel(playerRT, "StatusBubble", new Color(0.14f, 0.11f, 0.09f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 78), new Vector2(256, 34), spBeveledButton, true, true);
        bubbleGO.GetComponent<Image>().raycastTarget = false;
        playerBubbleText = CreateText(bubbleGO.transform, "BubbleTxt", "Иду к окну заказов", 12, new Color(0.98f, 0.92f, 0.76f, 1f), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-10, -4), TextAnchor.MiddleCenter, true);

        GameObject topBar = CreatePanel(rootBG.transform, "TopBar", new Color(0.16f, 0.12f, 0.09f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -28), new Vector2(0, 56), null, false, true);
        topTitleText = CreateText(topBar.transform, "Title", "Мастерская «Реставратор»", 16, new Color(0.98f, 0.88f, 0.64f, 1f), new Vector2(0, 0), new Vector2(0.36f, 1), new Vector2(22, 0), new Vector2(-20, 0), TextAnchor.MiddleLeft, true);
        TooltipTrigger.Attach(topTitleText.gameObject, "TOP_TITLE");

        GameObject clockRoot = CreatePanel(topBar.transform, "ClockWidgetRoot", new Color(0.12f, 0.09f, 0.07f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40f, 0f), new Vector2(210f, 48f), spBeveledPanel, true, true);
        clockWidgetRT = clockRoot.GetComponent<RectTransform>();
        TooltipTrigger.Attach(clockRoot, "CLOCK_WIDGET");

        Button clockBtn = clockRoot.AddComponent<Button>();
        clockBtn.transition = Selectable.Transition.ColorTint;
        clockBtn.onClick.AddListener(() => {
            PlaySFX(clipCrystalChime, 0.65f, 1.4f);
            clockWidgetRT.localScale = new Vector3(1.06f, 1.06f, 1f);
            float shiftSec = TimeController.Instance != null ? TimeController.Instance.ShiftTimeSeconds : completedOrdersCount * 180f;
            int m = Mathf.FloorToInt(shiftSec / 60f);
            int s = Mathf.FloorToInt(shiftSec % 60f);
            string phase = TimeController.Instance != null ? TimeController.Instance.CurrentPhase : "Утро";
            int pct = Mathf.RoundToInt(TimeController.Instance != null ? TimeController.Instance.PhaseProgress * 100f : 0f);
            bottomHintText.text = $"Хронометр Буре: Фаза «{phase}» ({pct}%) • Смена: {m:D2}:{s:D2} / 12:00";
        });

        GameObject dialGO = CreatePanel(clockRoot.transform, "ClockDial", Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(40f, 40f), spClockDial);
        dialGO.GetComponent<Image>().raycastTarget = false;

        GameObject hourGO = CreatePanel(dialGO.transform, "HourHand", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 20f), spHourHand);
        hourGO.GetComponent<Image>().raycastTarget = false;
        hourHandRT = hourGO.GetComponent<RectTransform>();
        hourHandRT.pivot = new Vector2(0.5f, 0.12f);

        GameObject minGO = CreatePanel(dialGO.transform, "MinuteHand", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 26f), spMinuteHand);
        minGO.GetComponent<Image>().raycastTarget = false;
        minuteHandRT = minGO.GetComponent<RectTransform>();
        minuteHandRT.pivot = new Vector2(0.5f, 0.12f);

        GameObject pivotCap = CreatePanel(dialGO.transform, "PivotCap", new Color(0.96f, 0.82f, 0.32f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f), spSoftLightGlow);
        pivotCap.GetComponent<Image>().raycastTarget = false;

        barsikBuffRoot = CreatePanel(topBar.transform, "BarsikBuffRoot", new Color(0.20f, 0.15f, 0.11f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(145f, 0f), new Vector2(150f, 38f), spBeveledButton, true, true);
        barsikBuffText = CreateText(barsikBuffRoot.transform, "BuffTxt", "🐾 Барсик: 00:00", 12, new Color(1f, 0.92f, 0.65f, 1f), Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f), TextAnchor.MiddleCenter, true);
        Button buffBtn = barsikBuffRoot.AddComponent<Button>();
        buffBtn.onClick.AddListener(() => {
            PlaySFX(clipPurr, 0.8f, 1.1f);
            if (bottomHintText != null)
                bottomHintText.text = $"Эффект Барсика: Спокойствие мастера активно еще {Mathf.CeilToInt(barsikBuffTimer)} сек (-50% риск перетирания).";
        });
        TooltipTrigger.Attach(barsikBuffRoot, "BARSIK_BUFF_TOOLTIP");
        barsikBuffRoot.SetActive(false);

        clockPhaseText = CreateText(clockRoot.transform, "PhaseTitle", "УТРО", 13, new Color(1f, 0.90f, 0.62f, 1f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(42f, 9f), new Vector2(-50f, 18f), TextAnchor.MiddleLeft, true);
        clockDigitalText = CreateText(clockRoot.transform, "DigitalClock", "00:00 / 12:00", 10, new Color(0.85f, 0.82f, 0.75f, 0.85f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(42f, -8f), new Vector2(-50f, 16f), TextAnchor.MiddleLeft);

        for (int i = 0; i < 4; i++)
        {
            float gx = 138f + i * 14f;
            GameObject gemGO = CreatePanel(clockRoot.transform, "PhaseGem_" + i, Color.gray, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(gx, 7f), new Vector2(10f, 10f), spWaxSealBadge);
            gemGO.GetComponent<Image>().raycastTarget = false;
            clockPhaseGemImgs[i] = gemGO.GetComponent<Image>();
        }

        GameObject barBG = CreatePanel(clockRoot.transform, "PhaseBarBG", new Color(0.08f, 0.06f, 0.05f, 0.9f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(160f, -8f), new Vector2(56f, 6f), spBeveledButton);
        barBG.GetComponent<Image>().raycastTarget = false;
        GameObject barFill = CreatePanel(barBG.transform, "Fill", new Color(0.88f, 0.72f, 0.28f, 1f), new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0f));
        barFill.GetComponent<Image>().raycastTarget = false;
        clockPhaseFillRT = barFill.GetComponent<RectTransform>();
        clockPhaseFillRT.pivot = new Vector2(0f, 0.5f);

        topStatsText = CreateText(topBar.transform, "Stats", "", 14, new Color(0.86f, 0.95f, 0.82f, 1f), new Vector2(0.58f, 0), new Vector2(1, 1), new Vector2(-104, 0), new Vector2(-110, 0), TextAnchor.MiddleRight, true);
        TooltipTrigger.Attach(topStatsText.gameObject, "TOP_STATS_ORDERS");

        Button muteBtn = CreateButton(topBar.transform, "MuteBtn", "🔊 ЗВУК", Vector2.zero, new Vector2(88, 34), new Color(0.24f, 0.18f, 0.14f, 1f), () => {
            ToggleMute();
        }, out muteBtnText, out muteBtnImg);
        RectTransform mbrt = muteBtn.GetComponent<RectTransform>();
        mbrt.anchorMin = new Vector2(1, 0.5f);
        mbrt.anchorMax = new Vector2(1, 0.5f);
        mbrt.anchoredPosition = new Vector2(-54, 0);
        muteBtnText.fontSize = 13;
        TooltipTrigger.Attach(muteBtn.gameObject, "TOP_MUTE_BTN");

        GameObject bottomBar = CreatePanel(rootBG.transform, "BottomBar", new Color(0.16f, 0.12f, 0.09f, 1f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 24), new Vector2(0, 48));
        bottomHintText = CreateText(bottomBar.transform, "BottomHint", "", 15, new Color(0.98f, 0.92f, 0.74f, 1f), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30, 0), TextAnchor.MiddleCenter, true);

        BuildWindowSelectionOverlay(rootBG.transform);
        BuildCabinetOverlay(rootBG.transform);
        BuildWorkbenchOverlay(rootBG.transform);
        BuildUpgradesOverlay(rootBG.transform);
        BuildModalOverlay(rootBG.transform);
    }

    private void BuildWindowSelectionOverlay(Transform parent)
    {
        windowOverlay = CreatePanel(parent, "WindowOverlay", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(1150, 546), spBeveledPanel, true, true);
        CreateText(windowOverlay.transform, "Header", "ОКНО ЗАКАЗОВ — ВЫБЕРИТЕ УКРАШЕНИЕ ДЛЯ РЕСТАВРАЦИИ", 20, new Color(0.98f, 0.88f, 0.60f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -34), new Vector2(-40, 40), TextAnchor.MiddleCenter, true);

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            float xPos = (i - 1) * 358f;
            GameObject card = CreatePanel(windowOverlay.transform, "OrderCard_" + i, new Color(0.24f, 0.18f, 0.14f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(xPos, 10), new Vector2(336, 368), spBeveledPanel, true, true);
            cardTitles[i] = CreateText(card.transform, "Title", "", 16, new Color(1f, 0.90f, 0.62f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -32), new Vector2(-24, 48), TextAnchor.MiddleCenter, true);
            cardClients[i] = CreateText(card.transform, "Client", "", 13, new Color(0.76f, 0.92f, 0.80f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), new Vector2(-24, 32), TextAnchor.MiddleCenter, true);
            cardDescs[i] = CreateText(card.transform, "Desc", "", 14, new Color(0.94f, 0.91f, 0.86f, 1f), new Vector2(0, 0.22f), new Vector2(1, 0.76f), Vector2.zero, new Vector2(-28, -10), TextAnchor.UpperLeft);

            GameObject badgeGO = CreatePanel(card.transform, "CardBadge", new Color(0.85f, 0.68f, 0.26f, 1f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -28), new Vector2(40, 40), spWaxSealBadge, false, true);
            badgeGO.GetComponent<Image>().raycastTarget = false;
            cardBadgeImgs[i] = badgeGO.GetComponent<Image>();
            cardBadgeTexts[i] = CreateText(badgeGO.transform, "BadgeRank", "I", 14, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

            Button cardClick = card.AddComponent<Button>();
            cardClick.transition = Selectable.Transition.ColorTint;
            ColorBlock ccb = cardClick.colors;
            ccb.normalColor = Color.white;
            ccb.highlightedColor = new Color(1.12f, 1.10f, 1.05f, 1f);
            ccb.pressedColor = new Color(0.90f, 0.88f, 0.85f, 1f);
            cardClick.colors = ccb;
            cardClick.onClick.AddListener(() => SelectOrderFromWindow(idx));

            CreateButton(card.transform, "TakeBtn", "Принять заказ", new Vector2(0, -140), new Vector2(280, 44), new Color(0.26f, 0.52f, 0.32f, 1f), () => {
                SelectOrderFromWindow(idx);
            }, out cardBtnLabels[i], out cardBtnImgs[i]);
            TooltipTrigger.Attach(card, "WINDOW_ORDER_" + i);
        }

        Button closeWinBtn = CreateButton(windowOverlay.transform, "CloseWinBtn", "← Отойти от окна", new Vector2(0, -228), new Vector2(340, 42), new Color(0.42f, 0.26f, 0.20f, 1f), () => {
            SetViewState("ROOM");
        }, out _, out _);
        TooltipTrigger.Attach(closeWinBtn.gameObject, "WINDOW_CLOSE_BTN");

        AddCornerCloseButton(windowOverlay.transform, () => SetViewState("ROOM"));

        windowOverlay.SetActive(false);
    }

    private void BuildCabinetOverlay(Transform parent)
    {
        cabinetOverlay = CreatePanel(parent, "CabinetOverlay", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(1120, 546), spBeveledPanel, true, true);
        CreateText(cabinetOverlay.transform, "Header", "ШКАФ ИНСТРУМЕНТОВ И РЕАКТИВОВ", 20, new Color(0.98f, 0.88f, 0.60f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -32), new Vector2(-40, 36), TextAnchor.MiddleCenter, true);

        GameObject memoBox = CreatePanel(cabinetOverlay.transform, "DiagMemo", new Color(0.18f, 0.14f, 0.11f, 1f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, 14), new Vector2(366, 376), spBeveledPanel, true, true);
        TooltipTrigger.Attach(memoBox, "CAB_DIAG_MEMO");
        cabinetDiagReminderText = CreateText(memoBox.transform, "MemoTxt", "", 14, new Color(0.94f, 0.90f, 0.82f, 1f), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-28, -28), TextAnchor.UpperLeft);

        string[] tIds = { "RING_GRINDER", "DIAMOND_FILE", "MICRO_SPATULA" };
        string[] tNames = {
            "1. Бормашина для шлифовки колец",
            "2. Алмазный пинцет и надфиль для бриллиантов",
            "3. Ювелирный микро-шпатель для эмали и оправы"
        };
        CreateText(cabinetOverlay.transform, "Lbl1", "Выберите спец-инструмент:", 15, new Color(0.98f, 0.85f, 0.58f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(185, 185), new Vector2(620, 24), TextAnchor.MiddleLeft, true);
        for (int i = 0; i < 3; i++)
        {
            string id = tIds[i];
            int idx = i;
            Button tBtn = CreateButton(cabinetOverlay.transform, "Tool_" + id, tNames[i], new Vector2(185, 148 - i * 40), new Vector2(620, 36), new Color(0.28f, 0.23f, 0.19f, 1f), () => {
                selectedMechTool = id;
                HighlightList(toolBtnImgs, idx, new Color(0.25f, 0.52f, 0.34f, 1f));
            }, out _, out Image bImg);
            toolBtnImgs.Add(bImg);
            TooltipTrigger.Attach(tBtn.gameObject, "CAB_TOOL_" + (id == "RING_GRINDER" ? "GRINDER" : (id == "DIAMOND_FILE" ? "DIAMOND" : "SPATULA")));
        }

        string[] sIds = { "SPIRIT", "ACID_GEL", "ACETONE" };
        string[] sNames = {
            "1. Мягкий спиртовой эфир — для золота и эмали",
            "2. Ювелирный ультра-гель — для граней бриллиантов",
            "3. Технический ацетон — для простых сплавов"
        };
        CreateText(cabinetOverlay.transform, "Lbl2", "Выберите очищающий растворитель:", 15, new Color(0.98f, 0.85f, 0.58f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(185, 18), new Vector2(620, 24), TextAnchor.MiddleLeft, true);
        for (int i = 0; i < 3; i++)
        {
            string id = sIds[i];
            int idx = i;
            Button sBtn = CreateButton(cabinetOverlay.transform, "Sol_" + id, sNames[i], new Vector2(185, -18 - i * 40), new Vector2(620, 36), new Color(0.28f, 0.23f, 0.19f, 1f), () => {
                selectedSolvent = id;
                HighlightList(solventBtnImgs, idx, new Color(0.25f, 0.52f, 0.34f, 1f));
            }, out _, out Image bImg);
            solventBtnImgs.Add(bImg);
            TooltipTrigger.Attach(sBtn.gameObject, "CAB_SOL_" + (id == "SPIRIT" ? "SPIRIT" : (id == "ACID_GEL" ? "ACID" : "ACETONE")));
        }

        string[] stIds = { "PATINA", "NEW" };
        string[] stNames = { "Сохранить следы возраста", "Восстановить «Как новое»" };
        CreateText(cabinetOverlay.transform, "Lbl3", "Выберите финишный подход:", 15, new Color(0.98f, 0.85f, 0.58f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(185, -142), new Vector2(620, 24), TextAnchor.MiddleLeft, true);
        for (int i = 0; i < 2; i++)
        {
            string id = stIds[i];
            int idx = i;
            Button stBtn = CreateButton(cabinetOverlay.transform, "Style_" + id, stNames[i], new Vector2(32 + i * 315, -178), new Vector2(302, 36), new Color(0.28f, 0.23f, 0.19f, 1f), () => {
                selectedStyle = id;
                HighlightList(styleBtnImgs, idx, new Color(0.52f, 0.40f, 0.18f, 1f));
            }, out _, out Image bImg);
            styleBtnImgs.Add(bImg);
            TooltipTrigger.Attach(stBtn.gameObject, "CAB_STYLE_" + id);
        }

        Button confirmCabBtn = CreateButton(cabinetOverlay.transform, "ConfirmCabBtn", "Взять набор и перейти к Рабочему столу ->", new Vector2(210, -232), new Vector2(440, 44), new Color(0.58f, 0.38f, 0.16f, 1f), () => {
            ConfirmCabinetSelection();
        }, out _, out _);
        TooltipTrigger.Attach(confirmCabBtn.gameObject, "CAB_CONFIRM_BTN");

        Button closeCabBtn = CreateButton(cabinetOverlay.transform, "CloseCabBtn", "← Отойти в комнату", new Vector2(-280, -232), new Vector2(260, 44), new Color(0.40f, 0.25f, 0.20f, 1f), () => {
            SetViewState("ROOM");
        }, out _, out _);
        TooltipTrigger.Attach(closeCabBtn.gameObject, "CAB_CLOSE_BTN");

        AddCornerCloseButton(cabinetOverlay.transform, () => SetViewState("ROOM"));

        cabinetOverlay.SetActive(false);
    }

    private void BuildUpgradesOverlay(Transform parent)
    {
        upgradesOverlay = CreatePanel(parent, "UpgradesOverlay", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(1060, 520), spBeveledPanel, true, true);
        CreateText(upgradesOverlay.transform, "Header", "СТЕНД МАСТЕРА — ОБОРУДОВАНИЕ И МУЗЕЙНАЯ ВИТРИНА", 20, new Color(0.98f, 0.88f, 0.60f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -26), new Vector2(-40, 36), TextAnchor.MiddleCenter, true);

        Button tabEquipBtn = CreateButton(upgradesOverlay.transform, "TabEquipBtn", "✦ ОБОРУДОВАНИЕ И СЕРТИФИКАТЫ", new Vector2(-220, 202), new Vector2(340, 34), new Color(0.38f, 0.52f, 0.36f, 1f), () => {
            SelectMasterStandTab(0);
        }, out _, out upgTabBtnEquipImg);
        TooltipTrigger.Attach(tabEquipBtn.gameObject, "UPG_TAB_EQUIP");

        Button tabGalleryBtn = CreateButton(upgradesOverlay.transform, "TabGalleryBtn", "🏛 МУЗЕЙНАЯ ГАЛЕРЕЯ ШЕДЕВРОВ", new Vector2(220, 202), new Vector2(340, 34), new Color(0.24f, 0.20f, 0.18f, 1f), () => {
            SelectMasterStandTab(1);
        }, out _, out upgTabBtnGalleryImg);
        TooltipTrigger.Attach(tabGalleryBtn.gameObject, "UPG_TAB_GALLERY");

        upgEquipContainer = CreatePanel(upgradesOverlay.transform, "EquipTabContainer", Color.clear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1000, 400));
        upgStatusText = CreateText(upgEquipContainer.transform, "StatusTxt", "", 15, new Color(0.84f, 0.96f, 0.84f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -18), new Vector2(-60, 30), TextAnchor.MiddleCenter, true);

        string[] titles = {
            "1. Алмазные микро-насадки — 800 руб.\nУвеличивают радиус очистки и скорость стачивания зазубрин на +40%.",
            "2. Оптический стабилизатор нажима — 1200 руб.\nСнижает риск повредить базовый слой при срыве руки на 60%.",
            "3. Сертификат Ювелира II разряда — 1600 руб.\nОткрывает доступ к сложным музейным заказам с гонораром 3800 руб.",
            "4. Патент Гранд-мастера III разряда — 3000 руб.\nПовышает выплаты за все заказы на +35% и укрепляет доверие клиентов."
        };

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            float yPos = 135f - i * 72f;
            GameObject row = CreatePanel(upgEquipContainer.transform, "UpgRow_" + i, new Color(0.24f, 0.18f, 0.14f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, yPos), new Vector2(960, 62), spBeveledButton, true, true);
            GameObject badgeGO = CreatePanel(row.transform, "Badge_" + i, Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(46f, 46f), spWaxSealBadge, false, true);
            badgeGO.GetComponent<Image>().raycastTarget = false;
            upgBadgeImgs[i] = badgeGO.GetComponent<Image>();
            string[] badgeLabels = { "✦", "⚙", "II", "III" };
            CreateText(badgeGO.transform, "BadgeTxt", badgeLabels[i], 16, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

            CreateText(row.transform, "Desc", titles[i], 14, new Color(0.95f, 0.92f, 0.86f, 1f), new Vector2(0, 0), new Vector2(0.70f, 1), new Vector2(76, 0), new Vector2(-76, -8), TextAnchor.MiddleLeft, true);

            Button buyBtn = CreateButton(row.transform, "BuyBtn_" + i, "Купить", new Vector2(330, 0), new Vector2(240, 44), new Color(0.28f, 0.52f, 0.32f, 1f), () => {
                BuyUpgrade(idx);
            }, out upgBtnLabels[i], out upgBtnImgs[i]);
            TooltipTrigger.Attach(buyBtn.gameObject, "UPG_BUY_" + i);
        }

        upgGalleryContainer = CreatePanel(upgradesOverlay.transform, "GalleryTabContainer", Color.clear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1000, 400));
        galleryStatusText = CreateText(upgGalleryContainer.transform, "GalStatus", "", 15, new Color(0.98f, 0.90f, 0.65f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -18), new Vector2(-60, 30), TextAnchor.MiddleCenter, true);

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            float xPos = (i - 1.5f) * 238f;
            GameObject card = CreatePanel(upgGalleryContainer.transform, "RelicCard_" + i, new Color(0.20f, 0.16f, 0.13f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(xPos, -20), new Vector2(228, 296), spBeveledPanel, true, true);
            GameObject iconGO = CreatePanel(card.transform, "Icon", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 56), new Vector2(108, 108));
            iconGO.GetComponent<Image>().raycastTarget = false;
            galleryCardIcons[i] = iconGO.GetComponent<Image>();

            galleryCardTitles[i] = CreateText(card.transform, "Title", "", 14, new Color(1f, 0.92f, 0.68f, 1f), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -18), new Vector2(-16, 28), TextAnchor.MiddleCenter, true);
            galleryCardBadges[i] = CreateText(card.transform, "Badge", "", 12, new Color(0.78f, 0.92f, 0.80f, 1f), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -48), new Vector2(-16, 24), TextAnchor.MiddleCenter, true);
            galleryCardLores[i] = CreateText(card.transform, "Lore", "", 11, new Color(0.90f, 0.88f, 0.84f, 1f), new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 14), new Vector2(-20, -56), TextAnchor.UpperCenter);

            Button inspBtn = CreateButton(card.transform, "InspectBtn", "Осмотреть", new Vector2(0, -114), new Vector2(188, 30), new Color(0.32f, 0.44f, 0.36f, 1f), () => {
                InspectGalleryRelic(idx);
            }, out _, out _);
            TooltipTrigger.Attach(inspBtn.gameObject, "UPG_INSPECT_RELIC");
        }

        upgGalleryContainer.SetActive(false);

        Button closeUpgBtn = CreateButton(upgradesOverlay.transform, "CloseUpgBtn", "← Вернуться в мастерскую", new Vector2(0, -222), new Vector2(340, 38), new Color(0.42f, 0.26f, 0.20f, 1f), () => {
            SetViewState("ROOM");
        }, out _, out _);
        TooltipTrigger.Attach(closeUpgBtn.gameObject, "UPG_CLOSE_BTN");

        AddCornerCloseButton(upgradesOverlay.transform, () => SetViewState("ROOM"));

        upgradesOverlay.SetActive(false);
    }

    private void SelectMasterStandTab(int tab)
    {
        bool isEquip = (tab == 0);
        upgEquipContainer.SetActive(isEquip);
        upgGalleryContainer.SetActive(!isEquip);
        upgTabBtnEquipImg.color = isEquip ? new Color(0.38f, 0.52f, 0.36f, 1f) : new Color(0.24f, 0.20f, 0.18f, 1f);
        upgTabBtnGalleryImg.color = !isEquip ? new Color(0.38f, 0.52f, 0.36f, 1f) : new Color(0.24f, 0.20f, 0.18f, 1f);
        PlaySFX(clipToolSwitch, 0.4f, isEquip ? 1.0f : 1.25f);
        if (!isEquip) RefreshGalleryView();
        else RefreshUpgradesUI();
    }

    private void RefreshGalleryView()
    {
        string[] shapes = { "RING", "DIAMOND", "LOCKET", "CROSS" };
        Sprite[] iconSprites = { spGalleryRing, spGalleryDiamond, spGalleryLocket, spGalleryCross };
        string[] titles = {
            "Золотой перстень 1812 г.",
            "Платиновый солитер",
            "Орден Фаберже",
            "Византийский энколпион"
        };
        string[] lores = {
            "Тайник декабристов с шифром в безеле сохранен.",
            "Королевская огранка очищена без микротрещин.",
            "Горячая гильошированная эмаль восстановлена.",
            "Древнейшая золотая скань и сапфир укреплены."
        };

        int completed = 0;
        for (int i = 0; i < 4; i++)
        {
            bool isDone = completedOrderTypes.Contains(shapes[i]);
            if (isDone) completed++;

            galleryCardTitles[i].text = titles[i];
            if (isDone)
            {
                galleryCardIcons[i].sprite = iconSprites[i];
                galleryCardIcons[i].color = Color.white;
                galleryCardBadges[i].text = "★ ОТРЕСТАВРИРОВАНО";
                galleryCardBadges[i].color = new Color(0.98f, 0.88f, 0.40f, 1f);
                galleryCardLores[i].text = lores[i];
            }
            else
            {
                galleryCardIcons[i].sprite = spWaxSealBadge;
                galleryCardIcons[i].color = new Color(0.40f, 0.40f, 0.40f, 0.6f);
                galleryCardBadges[i].text = "🔒 В ПОИСКЕ";
                galleryCardBadges[i].color = new Color(0.70f, 0.70f, 0.70f, 0.7f);
                galleryCardLores[i].text = "Ожидает спасения через Окно заказов.";
            }
        }

        galleryStatusText.text = $"ШЕДЕВРОВ В КОЛЛЕКЦИИ: {completed} ИЗ 4 • ПРЕСТИЖ ГИЛЬДИИ: {completed * 25}%";
    }

    private void InspectGalleryRelic(int idx)
    {
        string[] shapes = { "RING", "DIAMOND", "LOCKET", "CROSS" };
        string[] titles = { "Золотой перстень 1812 года", "Платиновый солитер с бриллиантом", "Императорский орден Фаберже", "Византийский энколпион XII века" };
        bool isDone = completedOrderTypes.Contains(shapes[idx]);
        if (isDone)
        {
            PlaySFX(clipCrystalChime, 0.75f, 1.2f + idx * 0.15f);
            ShowRelicCertificate(idx);
        }
        else
        {
            PlaySFX(clipFail, 0.5f, 1.0f);
            bottomHintText.text = $"Шедевр «{titles[idx]}» еще не отреставрирован. Примите заказ в Окне заказов!";
        }
    }

    private void ShowRelicCertificate(int idx)
    {
        SetViewState("CERTIFICATE");
        modalSealGO.SetActive(true);
        modalSealGO.GetComponent<Image>().color = new Color(0.95f, 0.78f, 0.25f, 1f);
        modalSealText.text = "ЭТАЛОН";

        string[] titles = {
            "Золотой перстень 1812 года",
            "Платиновый солитер с бриллиантом",
            "Императорский орден Фаберже",
            "Византийский энколпион XII века"
        };
        string[] epochs = {
            "Эпоха Отечественной войны 1812 года (Российская империя)",
            "Конец XIX века, придворная мастерская Санкт-Петербурга",
            "Начало XX века, дом Карла Фаберже, императорская серия",
            "XII век, Царьград (Константинополь), Патриаршие мастерские"
        };
        string[] materials = {
            "Червонное золото 958°, уральский рубин огранки «кушон», тайная гравировка вензеля.",
            "Платина 950°, природный якутский алмаз 2.4 карата королевской бриллиантовой огранки.",
            "Золоченое серебро 84 золотника, горячая гильошированная эмаль, золотой гербовый орел.",
            "Древнее византийское золото высокой пробы, филигранная скань, кабошон звездчатого сапфира."
        };
        string[] expertEvaluations = {
            "Экспертиза подтвердила сохранность тайного шифра декабристов в полости безеля. Патина стабилизирована.",
            "Огранка очищена от вековых минеральных налетов. Идеальная дисперсия граней подтверждена оптическим тестом.",
            "Стеклянная эмаль восстановлена без сколов и перегрева. Клеймо мастера Фаберже сохранено без потерь.",
            "Древнейшая золотая скань и реликварийная полость укреплены. Реликвия внесена в Патриарший реестр."
        };

        modalTitle.text = "ПАСПОРТ ПОДЛИННОСТИ: " + titles[idx];
        modalBody.text = $"ГИЛЬДЕЙСКИЙ СЕРТИФИКАТ КАЧЕСТВА №0{idx + 1}-1926\n\n" +
                         $"• Происхождение: {epochs[idx]}\n" +
                         $"• Материалы и сплав: {materials[idx]}\n" +
                         $"• Заключение экспертизы: {expertEvaluations[idx]}\n\n" +
                         $"Статус: Признан музейным эталоном высшей художественной ценности.";
        modalBtn1.gameObject.SetActive(true);
        modalBtn1Text.text = "← Вернуться в музейную галерею";
        modalBtn2.gameObject.SetActive(false);
    }

    private void BuildWorkbenchOverlay(Transform parent)
    {
        workbenchOverlay = CreatePanel(parent, "WorkbenchOverlay", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(1170, 554), spBeveledPanel, true, true);

        GameObject leftLog = CreatePanel(workbenchOverlay.transform, "BenchLog", new Color(0.18f, 0.14f, 0.11f, 1f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(198, 12), new Vector2(344, 448), spBeveledPanel, true, true);
        benchLogText = CreateText(leftLog.transform, "LogTxt", "", 14, new Color(0.94f, 0.90f, 0.82f, 1f), new Vector2(0, 0.30f), new Vector2(1, 1), new Vector2(0, -12), new Vector2(-26, -20), TextAnchor.UpperLeft);
        benchProgressText = CreateText(leftLog.transform, "ProgTxt", "", 14, new Color(0.76f, 0.96f, 0.80f, 1f), new Vector2(0, 0), new Vector2(1, 0.30f), new Vector2(0, 14), new Vector2(-26, -12), TextAnchor.LowerLeft, true);

        GameObject centerMat = CreatePanel(workbenchOverlay.transform, "CenterMat", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(394, 394), spWorkbenchMat, true, true);
        GameObject deskLampGlow = CreatePanel(centerMat.transform, "DeskLampGlow", new Color(1f, 0.94f, 0.72f, 0.24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(380, 380), spSoftLightGlow);
        deskLampGlow.GetComponent<Image>().raycastTarget = false;
        Button zoomOutBtn = CreateButton(centerMat.transform, "ZoomOutBtn", "−", new Vector2(72, 172), new Vector2(28, 28), new Color(0.24f, 0.36f, 0.30f, 1f), () => {
            StepBenchZoom(-0.25f);
        }, out Text zoTxt, out _);
        zoTxt.fontSize = 16;
        TooltipTrigger.Attach(zoomOutBtn.gameObject, "BENCH_ZOOM_OUT");

        Button benchZoomBtn = CreateButton(centerMat.transform, "BenchZoomBtn", "🔍 100%", new Vector2(128, 172), new Vector2(76, 28), new Color(0.24f, 0.38f, 0.32f, 1f), () => {
            CycleBenchZoom();
        }, out benchZoomBtnText, out _);
        benchZoomBtnText.fontSize = 13;
        TooltipTrigger.Attach(benchZoomBtn.gameObject, "BENCH_ZOOM_BTN");

        Button zoomInBtn = CreateButton(centerMat.transform, "ZoomInBtn", "+", new Vector2(178, 172), new Vector2(28, 28), new Color(0.24f, 0.36f, 0.30f, 1f), () => {
            StepBenchZoom(0.25f);
        }, out Text ziTxt, out _);
        ziTxt.fontSize = 16;
        TooltipTrigger.Attach(zoomInBtn.gameObject, "BENCH_ZOOM_IN");

        GameObject surfGO = CreatePanel(centerMat.transform, "ItemSurface", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 360));
        itemSurfaceRT = surfGO.GetComponent<RectTransform>();
        itemSurfaceImg = surfGO.GetComponent<Image>();

        clickNodesContainer = CreatePanel(itemSurfaceRT, "ClickNodesLayer", Color.clear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 360));
        clickNodesContainer.GetComponent<Image>().raycastTarget = false;

        for (int i = 0; i < 28; i++)
        {
            GameObject spGO = CreatePanel(itemSurfaceRT, "Spark_" + i, Color.clear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10), spSoftLightGlow);
            spGO.GetComponent<Image>().raycastTarget = false;
            sparks.Add(new SparkParticle { rt = spGO.GetComponent<RectTransform>(), img = spGO.GetComponent<Image>(), life = 0f });
        }

        loupeGO = CreatePanel(itemSurfaceRT, "LoupeRing", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(114, 114), spLoupeLens);
        loupeGO.GetComponent<Image>().raycastTarget = false;
        loupeRT = loupeGO.GetComponent<RectTransform>();

        toolCursorGO = CreatePanel(itemSurfaceRT, "ToolCursorRing", Color.clear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60), spTimerRing);
        toolCursorGO.GetComponent<Image>().raycastTarget = false;
        toolCursorRT = toolCursorGO.GetComponent<RectTransform>();
        toolCursorImg = toolCursorGO.GetComponent<Image>();
        toolCursorGO.SetActive(false);

        GameObject speedBar = CreatePanel(centerMat.transform, "SpeedBar", new Color(0.10f, 0.10f, 0.10f, 0.95f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -26), new Vector2(360, 24), spBeveledButton, true);
        GameObject speedFill = CreatePanel(speedBar.transform, "Fill", new Color(0.28f, 0.82f, 0.42f, 1f), new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, Vector2.zero);
        speedFillRT = speedFill.GetComponent<RectTransform>();
        speedFillRT.pivot = new Vector2(0, 0.5f);
        speedFillImg = speedFill.GetComponent<Image>();
        speedBarLabelText = CreateText(speedBar.transform, "SpdTxt", "Темп движения мыши", 12, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

        GameObject rightCol = CreatePanel(workbenchOverlay.transform, "BenchRight", new Color(0.18f, 0.14f, 0.11f, 1f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-198, 12), new Vector2(344, 448), spBeveledPanel, true, true);
        benchModeNoticeText = CreateText(rightCol.transform, "ModeNotice", "", 14, new Color(0.98f, 0.88f, 0.65f, 1f), new Vector2(0, 0.68f), new Vector2(1, 1), new Vector2(0, -10), new Vector2(-24, -12), TextAnchor.UpperCenter, true);

        btnGoToCabinetFromBench = CreateButton(rightCol.transform, "BtnGoCab", "Пойти к Шкафу инструментов ->", new Vector2(0, 15), new Vector2(304, 50), new Color(0.58f, 0.38f, 0.16f, 1f), () => {
            SetViewState("ROOM");
            MovePlayerToStation(posCabinetInteract, "CABINET");
        }, out _, out _);
        TooltipTrigger.Attach(btnGoToCabinetFromBench.gameObject, "BENCH_GO_CAB_BTN");

        benchToolsPanel = CreatePanel(rightCol.transform, "BenchToolsSub", Color.clear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -32), new Vector2(320, 310));

        Button actCleanBtn = CreateButton(benchToolsPanel.transform, "ActClean", "🧪 1. Растворитель: Очистка", new Vector2(0, 108), new Vector2(308, 42), new Color(0.24f, 0.40f, 0.50f, 1f), () => SelectBenchTool("CLEAN"), out benchToolBtnTexts[0], out benchToolBtnImgs[0]);
        TooltipTrigger.Attach(actCleanBtn.gameObject, "BENCH_ACT_CLEAN");

        Button actGrindBtn = CreateButton(benchToolsPanel.transform, "ActGrind", "🪚 2. Напильник: Шлифовка", new Vector2(0, 58), new Vector2(308, 42), new Color(0.50f, 0.34f, 0.20f, 1f), () => SelectBenchTool("GRIND"), out benchToolBtnTexts[1], out benchToolBtnImgs[1]);
        TooltipTrigger.Attach(actGrindBtn.gameObject, "BENCH_ACT_GRIND");

        Button actClickBtn = CreateButton(benchToolsPanel.transform, "ActClick", "🗜️ 3. Зажимы: Закрепка", new Vector2(0, 8), new Vector2(308, 42), new Color(0.46f, 0.28f, 0.44f, 1f), () => SelectBenchTool("CLICK_FIX"), out benchToolBtnTexts[2], out benchToolBtnImgs[2]);
        TooltipTrigger.Attach(actClickBtn.gameObject, "BENCH_ACT_CLICK");

        Button actFinishBtn = CreateButton(benchToolsPanel.transform, "ActFinish", "✨ 4. Лоск: Полировка", new Vector2(0, -42), new Vector2(308, 42), new Color(0.26f, 0.46f, 0.30f, 1f), () => SelectBenchTool("FINISH"), out benchToolBtnTexts[3], out benchToolBtnImgs[3]);
        TooltipTrigger.Attach(actFinishBtn.gameObject, "BENCH_ACT_FINISH");

        Button actDoneBtn = CreateButton(benchToolsPanel.transform, "ActDoneWalkWindow", "Завершить и отнести в Окно заказов ->", new Vector2(0, -108), new Vector2(308, 48), new Color(0.62f, 0.40f, 0.16f, 1f), () => {
            if (cleanProgress >= 75 && grindProgress >= 75 && clickFixProgress >= 100 && finishProgress >= 80f)
            {
                orderStage = "READY_TO_SUBMIT";
                SetViewState("ROOM");
                MovePlayerToStation(posWindowInteract, "WINDOW");
            }
            else
            {
                PlaySFX(clipFail, 0.45f, 1.1f);
                bottomHintText.text = "Завершите все 4 этапа: Очистку (75%+), Шлифовку (75%+), Закрепку кликами (100%) и Полировку (80%+)!";
            }
        }, out btnDoneText, out btnDoneImg);
        TooltipTrigger.Attach(actDoneBtn.gameObject, "BENCH_DONE_BTN");

        Button backRoomBtn = CreateButton(workbenchOverlay.transform, "BackRoomBtn", "← Отойти от стола в мастерскую", new Vector2(0, -240), new Vector2(350, 40), new Color(0.42f, 0.26f, 0.20f, 1f), () => {
            SetViewState("ROOM");
        }, out _, out _);
        TooltipTrigger.Attach(backRoomBtn.gameObject, "BENCH_BACK_ROOM_BTN");

        AddCornerCloseButton(workbenchOverlay.transform, () => SetViewState("ROOM"));

        for (int i = 0; i < 12; i++)
        {
            GameObject ftGO = new GameObject("FText_" + i);
            ftGO.transform.SetParent(workbenchOverlay.transform, false);
            RectTransform ftRT = ftGO.AddComponent<RectTransform>();
            ftRT.sizeDelta = new Vector2(320, 38);
            Text ftTxt = ftGO.AddComponent<Text>();
            ftTxt.font = Font.CreateDynamicFontFromOSFont("Segoe UI", 16);
            if (ftTxt.font == null) ftTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ftTxt.fontSize = 16;
            ftTxt.fontStyle = FontStyle.Bold;
            ftTxt.alignment = TextAnchor.MiddleCenter;
            ftTxt.raycastTarget = false;
            ftGO.SetActive(false);
            floatingTexts.Add(new FloatingTextItem { rt = ftRT, txt = ftTxt, timer = 0f, maxTime = 1.2f });
        }

        workbenchOverlay.SetActive(false);
    }

    public void StepBenchZoom(float delta)
    {
        targetBenchZoom = Mathf.Clamp(targetBenchZoom + delta, 1.0f, 2.0f);
        float pitch = 1.0f + (targetBenchZoom - 1.0f) * 0.5f;
        PlaySFX(clipToolSwitch, 0.45f, pitch);
    }

    private void CycleBenchZoom()
    {
        if (targetBenchZoom < 1.35f) targetBenchZoom = 1.5f;
        else if (targetBenchZoom < 1.75f) targetBenchZoom = 2.0f;
        else targetBenchZoom = 1.0f;
        float pitch = 1.0f + (targetBenchZoom - 1.0f) * 0.5f;
        PlaySFX(clipToolSwitch, 0.45f, pitch);
    }

    public void SelectBenchTool(string toolId)
    {
        if (toolLoopSource != null && toolLoopSource.isPlaying)
        {
            toolLoopSource.Stop();
        }

        activeBenchAction = toolId;
        clickNodesContainer.SetActive(toolId == "CLICK_FIX");

        Color[] defCols = {
            new Color(0.24f, 0.40f, 0.50f, 1f),
            new Color(0.50f, 0.34f, 0.20f, 1f),
            new Color(0.46f, 0.28f, 0.44f, 1f),
            new Color(0.26f, 0.46f, 0.30f, 1f)
        };
        int activeIdx = toolId == "CLEAN" ? 0 : (toolId == "GRIND" ? 1 : (toolId == "CLICK_FIX" ? 2 : 3));
        for (int i = 0; i < 4; i++)
        {
            benchToolBtnImgs[i].color = (i == activeIdx) ? new Color(0.85f, 0.65f, 0.22f, 1f) : defCols[i];
            benchToolBtnImgs[i].transform.localScale = (i == activeIdx) ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
        }

        PlaySFX(clipToolSwitch, 0.5f, activeIdx == 0 ? 0.9f : (activeIdx == 1 ? 1.1f : (activeIdx == 2 ? 1.3f : 1.5f)));

        if (toolId == "CLEAN")
        {
            speedBarLabelText.text = "Режим очистки: Двигайте мышью ПЛАВНО";
            bottomHintText.text = "Зажмите ЛКМ и ПЛАВНО протирайте пятна окислов. Резкие рывки сотрут базовый слой!";
        }
        else if (toolId == "GRIND")
        {
            speedBarLabelText.text = "Режим шлифовки: БЫСТРО ДЕРГАЙТЕ мышью по зазубринам!";
            bottomHintText.text = "Зажмите ЛКМ на рыжих зазубринах и БЫСТРО ДЕРГАЙТЕ мышью из стороны в сторону!";
        }
        else if (toolId == "CLICK_FIX")
        {
            speedBarLabelText.text = "Режим закрепки: КЛИКАЙТЕ БЕЗ ПАУЗ, иначе узел соберется обратно до 5!";
            bottomHintText.text = "Быстро кликайте 5 раз подряд по каждому узлу! Если кольцо таймера сожмется, счетчик вернется к 5!";
        }
        else if (toolId == "FINISH")
        {
            speedBarLabelText.text = "Режим полировки: Плавными кругами наведите зеркальный PBR-блеск";
            bottomHintText.text = "Зажмите ЛКМ и плавно водите по предмету до 100% сияния.";
        }
        UpdateBenchToolVisuals();
    }

    private void UpdateBenchToolVisuals()
    {
        string[] baseLabels = {
            "🧪 1. Растворитель: Очистка",
            "🪚 2. Напильник: Шлифовка",
            "🗜️ 3. Зажимы: Закрепка",
            "✨ 4. Лоск: Полировка"
        };
        bool[] finished = {
            cleanProgress >= 75f,
            grindProgress >= 75f,
            clickFixProgress >= 100f,
            finishProgress >= 80f
        };
        for (int i = 0; i < 4; i++)
        {
            if (benchToolBtnTexts[i] != null)
            {
                string mark = finished[i] ? "✓ " : "";
                string activeMarker = (activeBenchAction == (i == 0 ? "CLEAN" : (i == 1 ? "GRIND" : (i == 2 ? "CLICK_FIX" : "FINISH")))) ? "▶ " : "";
                benchToolBtnTexts[i].text = mark + activeMarker + baseLabels[i];
            }
        }
    }

    private void UpdateBenchDoneButtonVisuals()
    {
        if (btnDoneImg == null || btnDoneText == null) return;
        bool allDone = (cleanProgress >= 75 && grindProgress >= 75 && clickFixProgress >= 100 && finishProgress >= 80f);
        btnDoneImg.color = allDone ? new Color(0.24f, 0.65f, 0.32f, 1f) : new Color(0.38f, 0.28f, 0.22f, 1f);
        btnDoneText.text = allDone ? "✓ ГОТОВО! Отнести в Окно заказов →" : "Завершить и отнести в Окно заказов ->";
        UpdateBenchToolVisuals();
    }

    private void BuildModalOverlay(Transform parent)
    {
        modalOverlay = CreatePanel(parent, "ModalOverlay", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 450), spBeveledPanel, true, true);
        modalTitle = CreateText(modalOverlay.transform, "Title", "", 22, new Color(0.98f, 0.86f, 0.56f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -36), new Vector2(-40, 40), TextAnchor.MiddleCenter, true);
        modalBody = CreateText(modalOverlay.transform, "Body", "", 16, new Color(0.94f, 0.92f, 0.86f, 1f), new Vector2(0, 0.26f), new Vector2(1, 0.82f), Vector2.zero, new Vector2(-64, 0), TextAnchor.MiddleLeft);

        modalBtn1 = CreateButton(modalOverlay.transform, "Btn1", "Продолжить", new Vector2(0, -124), new Vector2(550, 44), new Color(0.28f, 0.50f, 0.32f, 1f), () => OnModalButtonClick(1), out modalBtn1Text, out _);
        TooltipTrigger.Attach(modalBtn1.gameObject, "MODAL_BTN_1");

        modalBtn2 = CreateButton(modalOverlay.transform, "Btn2", "Вариант 2", new Vector2(0, -176), new Vector2(550, 44), new Color(0.48f, 0.28f, 0.22f, 1f), () => OnModalButtonClick(2), out modalBtn2Text, out _);
        TooltipTrigger.Attach(modalBtn2.gameObject, "MODAL_BTN_2");

        modalSealGO = CreatePanel(modalOverlay.transform, "ModalSeal", Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-65f, -60f), new Vector2(74f, 74f), spWaxSealBadge, false, true);
        modalSealGO.GetComponent<Image>().raycastTarget = false;
        TooltipTrigger.Attach(modalSealGO, "MODAL_SEAL");
        modalSealText = CreateText(modalSealGO.transform, "SealTxt", "", 12, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

        AddCornerCloseButton(modalOverlay.transform, () => {
            if (viewState == "CERTIFICATE")
            {
                SetViewState("UPGRADES");
                SelectMasterStandTab(1);
            }
            else if (viewState == "RESULT")
            {
                OnModalButtonClick(1);
            }
            else if (viewState == "TWIST")
            {
                OnModalButtonClick(2);
            }
            else if (viewState == "FINALE")
            {
                OnModalButtonClick(1);
            }
            else
            {
                modalOverlay.SetActive(false);
                SetViewState("ROOM");
            }
        });

        modalOverlay.SetActive(false);
    }

    private void HighlightList(List<Image> list, int activeIdx, Color activeCol)
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i].color = (i == activeIdx) ? activeCol : new Color(0.28f, 0.23f, 0.19f, 1f);
            list[i].transform.localScale = (i == activeIdx) ? new Vector3(1.025f, 1.025f, 1f) : Vector3.one;
        }
    }

    private void EmitSparks(Vector2 localPos, int count, Color sparkCol)
    {
        int spawned = 0;
        for (int i = 0; i < sparks.Count && spawned < count; i++)
        {
            if (sparks[i].life <= 0f)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float spd = Random.Range(90f, 260f);
                sparks[i].pos = localPos;
                sparks[i].vel = new Vector2(Mathf.Cos(angle) * spd, Mathf.Sin(angle) * spd);
                sparks[i].maxLife = Random.Range(0.22f, 0.45f);
                sparks[i].life = sparks[i].maxLife;
                sparks[i].baseCol = sparkCol;
                sparks[i].rt.anchoredPosition = localPos;
                sparks[i].img.color = sparkCol;
                spawned++;
            }
        }
    }

    private void SpawnFloatingText(Vector2 localPos, string text, Color col)
    {
        foreach (var ft in floatingTexts)
        {
            if (ft.timer <= 0f)
            {
                ft.rt.gameObject.SetActive(true);
                ft.rt.anchoredPosition = localPos;
                ft.pos = localPos;
                ft.txt.text = text;
                ft.baseCol = col;
                ft.txt.color = col;
                ft.timer = 1.15f;
                ft.maxTime = 1.15f;
                return;
            }
        }
    }

    private void UpdateFloatingTexts(float dt)
    {
        foreach (var ft in floatingTexts)
        {
            if (ft.timer > 0f)
            {
                ft.timer -= dt;
                float progress = 1f - (ft.timer / ft.maxTime);
                ft.pos.y += dt * 38f;
                ft.rt.anchoredPosition = ft.pos;
                Color c = ft.baseCol;
                c.a = Mathf.Clamp01(1f - progress * progress);
                ft.txt.color = c;
                if (ft.timer <= 0f) ft.rt.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateSparks(float dt)
    {
        for (int i = 0; i < sparks.Count; i++)
        {
            if (sparks[i].life > 0f)
            {
                sparks[i].life -= dt;
                if (sparks[i].life <= 0f)
                {
                    sparks[i].img.color = Color.clear;
                }
                else
                {
                    sparks[i].vel.y -= 280f * dt;
                    sparks[i].pos += sparks[i].vel * dt;
                    sparks[i].rt.anchoredPosition = sparks[i].pos;
                    Color c = sparks[i].baseCol;
                    float lifeRatio = sparks[i].life / sparks[i].maxLife;
                    c.a = lifeRatio;
                    sparks[i].img.color = c;
                    sparks[i].rt.sizeDelta = Vector2.Lerp(new Vector2(2.5f, 2.5f), new Vector2(10f, 10f), lifeRatio);
                }
            }
        }
    }

    private void SetupClickNodesForItem(string shapeType)
    {
        foreach (var node in activeClickNodes)
        {
            if (node.uiRT != null)
            {
                if (Application.isPlaying) Destroy(node.uiRT.gameObject);
                else DestroyImmediate(node.uiRT.gameObject);
            }
        }
        activeClickNodes.Clear();

        Vector2[] pts;
        if (shapeType == "RING")
            pts = new Vector2[] { new Vector2(0, 84), new Vector2(-84, 0), new Vector2(84, 0), new Vector2(0, -84) };
        else if (shapeType == "DIAMOND")
            pts = new Vector2[] { new Vector2(-58, 58), new Vector2(58, 58), new Vector2(-58, -58), new Vector2(58, -58) };
        else if (shapeType == "CROSS")
            pts = new Vector2[] { new Vector2(0, 72), new Vector2(-62, 10), new Vector2(62, 10), new Vector2(0, -72) };
        else
            pts = new Vector2[] { new Vector2(0, 88), new Vector2(-68, 0), new Vector2(68, 0), new Vector2(0, -88) };

        for (int i = 0; i < pts.Length; i++)
        {
            int idx = i;
            GameObject nodeGO = CreatePanel(clickNodesContainer.transform, "ClickNode_" + i, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pts[i], new Vector2(56, 56), spTargetClamp);
            Image img = nodeGO.GetComponent<Image>();
            Button btn = nodeGO.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject ringGO = CreatePanel(nodeGO.transform, "TimerRing", Color.clear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84), spTimerRing);
            ringGO.GetComponent<Image>().raycastTarget = false;

            Text txt = CreateText(nodeGO.transform, "Cnt", "5", 17, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

            ClickNode cn = new ClickNode
            {
                pos = pts[i],
                clicksLeft = 5,
                maxClicks = 5,
                resetTimer = 0f,
                uiRT = nodeGO.GetComponent<RectTransform>(),
                ringRT = ringGO.GetComponent<RectTransform>(),
                uiTxt = txt,
                uiImg = img,
                ringImg = ringGO.GetComponent<Image>()
            };
            activeClickNodes.Add(cn);

            btn.onClick.AddListener(() => OnClickFixNode(idx));
        }

        clickNodesContainer.SetActive(false);
        UpdateClickFixProgress();
    }

    private void OnClickFixNode(int idx)
    {
        if (activeBenchAction != "CLICK_FIX") return;
        if (cleanProgress < 35)
        {
            PlaySFX(clipSpringReset, 0.35f, 1.4f);
            bottomHintText.text = "НАРУШЕН ПОРЯДОК: Сначала очистите хотя бы 35% окислов растворителем!";
            return;
        }

        ClickNode cn = activeClickNodes[idx];
        if (cn.clicksLeft > 0)
        {
            cn.clicksLeft--;
            cn.resetTimer = CLICK_RESET_TIME;
            cn.uiRT.localScale = new Vector2(1.22f, 1.22f);
            PlaySFX(clipClickPin, 0.65f, 0.9f + (5 - cn.clicksLeft) * 0.14f);
            EmitSparks(cn.pos, 7, new Color(0.55f, 0.92f, 1f, 1f));
            if (cn.clicksLeft > 0)
                SpawnFloatingText(cn.pos + new Vector2(0, 22), $"-1 Клик (осталось {cn.clicksLeft})", new Color(0.55f, 0.92f, 1f, 1f));
            else
                SpawnFloatingText(cn.pos + new Vector2(0, 26), "✓ Цанга закреплена!", new Color(0.40f, 1f, 0.50f, 1f));

            if (cn.clicksLeft <= 0)
            {
                cn.uiRT.gameObject.SetActive(false);
                PlaySFX(clipCrystalChime, 0.7f, 1.4f);
                EmitSparks(cn.pos, 14, new Color(0.35f, 0.95f, 0.45f, 1f));
            }
            else
            {
                cn.uiTxt.text = cn.clicksLeft.ToString();
                cn.ringImg.color = new Color(1f, 0.88f, 0.35f, 0.95f);
                cn.ringRT.sizeDelta = new Vector2(86f, 86f);
            }

            UpdateClickFixProgress();
            RefreshItemVisuals();
            UpdateTopBar();
            CheckMidOrderTwist();
        }
    }

    private void UpdateClickNodesDecay(float dt)
    {
        if (viewState != "WORKBENCH" || activeBenchAction != "CLICK_FIX") return;

        bool anyReset = false;
        foreach (var cn in activeClickNodes)
        {
            if (cn.uiRT != null) cn.uiRT.localScale = Vector2.Lerp(cn.uiRT.localScale, Vector2.one, dt * 12f);
            if (cn.clicksLeft > 0 && cn.clicksLeft < cn.maxClicks)
            {
                cn.resetTimer -= dt;
                float tRatio = Mathf.Clamp01(cn.resetTimer / CLICK_RESET_TIME);
                cn.uiImg.color = Color.Lerp(new Color(1f, 0.25f, 0.25f, 1f), Color.white, tRatio);
                cn.ringRT.sizeDelta = Vector2.Lerp(new Vector2(48f, 48f), new Vector2(88f, 88f), tRatio);
                cn.ringImg.color = new Color(1f, 0.35f + tRatio * 0.55f, 0.25f, 0.95f);

                if (cn.resetTimer <= 0f)
                {
                    cn.clicksLeft = cn.maxClicks;
                    cn.uiTxt.text = cn.maxClicks.ToString();
                    cn.uiImg.color = Color.white;
                    cn.ringImg.color = Color.clear;
                    EmitSparks(cn.pos, 8, new Color(1f, 0.30f, 0.20f, 1f));
                    anyReset = true;
                }
            }
        }

        if (anyReset)
        {
            PlaySFX(clipSpringReset, 0.75f, 1.0f);
            UpdateClickFixProgress();
            bottomHintText.text = "СЛИШКОМ МЕДЛЕННО! Цанга разжалась обратно — нужно нажать 5 раз подряд без пауз!";
        }
    }

    private void UpdateClickFixProgress()
    {
        int total = 0, done = 0;
        foreach (var n in activeClickNodes)
        {
            total += n.maxClicks;
            done += (n.maxClicks - n.clicksLeft);
        }
        int prev = clickFixProgress;
        clickFixProgress = (total > 0) ? Mathf.FloorToInt((done / (float)total) * 100f) : 100;
        if (prev < 100 && clickFixProgress >= 100)
        {
            PlaySFX(clipBell, 0.8f, 1.3f);
            bottomHintText.text = "ЗАКРЕПКА ЗАВЕРШЕНА! Перейдите к 4-му этапу: Полировка лоска.";
        }
    }

    private void GenerateItemTexture(string shapeType)
    {
        if (itemTex != null) Destroy(itemTex);
        itemTex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, false);
        itemTex.filterMode = FilterMode.Bilinear;
        itemTex.wrapMode = TextureWrapMode.Clamp;

        float cx = TEX_SIZE * 0.5f;
        float cy = TEX_SIZE * 0.5f;
        float scale = TEX_SIZE / 200f;

        for (int y = 0; y < TEX_SIZE; y++)
        {
            for (int x = 0; x < TEX_SIZE; x++)
            {
                float dx = (x - cx) / scale;
                float dy = (y - cy) / scale;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);

                float clothNoise = Mathf.Sin(x * 0.45f) * Mathf.Cos(y * 0.45f) * 0.015f;
                float radialVig = Mathf.Clamp01(1.14f - (dist / 120f) * 0.35f);
                Color albedo = new Color((0.10f + clothNoise) * radialVig, (0.26f + clothNoise) * radialVig, (0.18f + clothNoise) * radialVig, 1f);
                Vector3 norm = new Vector3(clothNoise * 2f, clothNoise * 2f, 1f);
                float metallic = 0.02f;

                bool isItem = false;
                bool isGem = false;
                bool isBurrZone = false;
                float shadowDist = Mathf.Sqrt((dx - 5f) * (dx - 5f) + (dy + 6f) * (dy + 6f));

                if (shapeType == "RING")
                {
                    if (shadowDist < 72f && shadowDist > 38f) albedo *= 0.62f;
                    float gemY = dy - 52f;
                    float gemDist = Mathf.Sqrt(dx * dx + gemY * gemY);

                    if (gemDist < 25f)
                    {
                        isItem = true;
                        if (gemDist > 19.5f)
                        {
                            norm = new Vector3(dx / 25f, gemY / 25f, 0.65f);
                            albedo = new Color(0.94f, 0.76f, 0.28f, 1f);
                            metallic = 0.92f;
                        }
                        else
                        {
                            norm = new Vector3(dx / 20f, gemY / 20f, Mathf.Sqrt(Mathf.Max(0.1f, 1f - (gemDist / 20f) * (gemDist / 20f))));
                            albedo = new Color(0.82f, 0.16f, 0.14f, 1f);
                            metallic = 0.45f;
                            isGem = true;
                        }
                    }
                    else if (dist < 69f && dist > 41f)
                    {
                        isItem = true;
                        float midR = 55f;
                        float tubePos = (dist - midR) / 14f;
                        float leafPattern = Mathf.Sin(ang * 16f) * Mathf.Cos(tubePos * 2.8f) * 0.22f;
                        Vector3 baseNorm = new Vector3((dx / dist) * tubePos, (dy / dist) * tubePos, Mathf.Sqrt(Mathf.Max(0.15f, 1f - tubePos * tubePos)));
                        norm = (baseNorm + new Vector3(Mathf.Cos(ang) * leafPattern, Mathf.Sin(ang) * leafPattern, 0.3f) * 0.35f).normalized;
                        albedo = new Color(0.96f, 0.80f, 0.32f, 1f);
                        metallic = 0.95f;

                        if (Mathf.Abs(dist - 55f) < 9.5f && Mathf.Sin(ang * 5f) > 0.35f)
                            isBurrZone = true;
                    }
                }
                else if (shapeType == "DIAMOND")
                {
                    if (shadowDist < 70f) albedo *= 0.62f;
                    float manhattan = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (dist < 68f && manhattan < 88f)
                    {
                        isItem = true;
                        if (Mathf.Abs(dx) > 35f && Mathf.Abs(dy) > 35f)
                        {
                            norm = new Vector3(dx / 70f, dy / 70f, 0.75f);
                            albedo = new Color(0.86f, 0.89f, 0.94f, 1f);
                            metallic = 0.92f;
                        }
                        else
                        {
                            int sector = Mathf.FloorToInt((ang + Mathf.PI) / (Mathf.PI * 0.25f));
                            float facetAngle = sector * (Mathf.PI * 0.25f);
                            norm = (dist < 30f)
                                ? new Vector3(0f, 0f, 1f)
                                : new Vector3(Mathf.Cos(facetAngle) * 0.65f, Mathf.Sin(facetAngle) * 0.65f, 0.76f);

                            albedo = new Color(0.76f, 0.90f, 1.0f, 1f);
                            metallic = 0.78f;
                            isGem = true;

                            if (dist > 34f && dist < 58f && Mathf.Cos(ang * 4f) > 0.45f)
                                isBurrZone = true;
                        }
                    }
                }
                else if (shapeType == "CROSS")
                {
                    bool inV = Mathf.Abs(dx) < 20f && dy > -72f && dy < 72f;
                    bool inH = Mathf.Abs(dy - 10f) < 20f && Mathf.Abs(dx) < 64f;
                    if (inV || inH)
                    {
                        isItem = true;
                        float beamDist = Mathf.Min(inV ? 20f - Mathf.Abs(dx) : 100f, inH ? 20f - Mathf.Abs(dy - 10f) : 100f);
                        if (dist < 18f)
                        {
                            norm = new Vector3(dx / 18f, dy / 18f, Mathf.Sqrt(Mathf.Max(0.1f, 1f - (dist / 18f) * (dist / 18f))));
                            albedo = new Color(0.14f, 0.38f, 0.88f, 1f);
                            metallic = 0.58f;
                            isGem = true;
                        }
                        else if (beamDist < 4f)
                        {
                            norm = new Vector3(dx / 64f, dy / 72f, 0.65f);
                            albedo = new Color(0.96f, 0.82f, 0.34f, 1f);
                            metallic = 0.94f;
                            if (dist > 45f) isBurrZone = true;
                        }
                        else
                        {
                            norm = new Vector3(dx / 50f, dy / 60f, 0.82f);
                            albedo = new Color(0.88f, 0.70f, 0.24f, 1f);
                            metallic = 0.88f;
                        }
                    }
                }
                else
                {
                    float oval = (dx * dx) / (64f * 64f) + (dy * dy) / (76f * 76f);
                    if (oval <= 1.08f && oval > 1.0f) albedo *= 0.62f;

                    if (oval <= 1.0f)
                    {
                        isItem = true;
                        if (oval > 0.78f)
                        {
                            norm = new Vector3(dx / 64f, dy / 76f, 0.65f);
                            albedo = new Color(0.94f, 0.76f, 0.28f, 1f);
                            metallic = 0.92f;
                            if (oval > 0.80f && oval < 0.94f && Mathf.Sin(ang * 6f) > 0.3f)
                                isBurrZone = true;
                        }
                        else
                        {
                            float guillocheWave = Mathf.Sin(dist * 0.45f + Mathf.Sin(ang * 12f) * 1.5f);
                            norm = new Vector3(dx / 70f + guillocheWave * 0.18f, dy / 70f + guillocheWave * 0.18f, 0.88f);
                            albedo = new Color(0.18f, 0.44f, 0.78f, 1f);
                            metallic = 0.65f;

                            if (dist < 19f)
                            {
                                albedo = (dist < 9f) ? new Color(0.82f, 0.14f, 0.28f, 1f) : new Color(0.96f, 0.80f, 0.34f, 1f);
                                metallic = 0.90f;
                                if (dist < 9f) isGem = true;
                            }
                        }
                    }
                }

                albedoMap[y, x] = albedo;
                normalMap[y, x] = norm.normalized;
                metallicMap[y, x] = metallic;
                isItemMask[y, x] = isItem ? (byte)1 : (byte)0;
                isGemMask[y, x] = isGem ? (byte)1 : (byte)0;
                dirtMap[y, x] = 0;
                burrMap[y, x] = 0f;

                if (isItem)
                {
                    float tarnish = Mathf.Sin(dx * 0.11f + Mathf.Cos(dy * 0.10f) * 1.6f) * Mathf.Cos(dy * 0.11f);
                    if (tarnish > 0.24f) dirtMap[y, x] = 1;
                    if (isBurrZone) burrMap[y, x] = 1f;
                }
            }
        }
    }

    private void RefreshItemVisuals()
    {
        if (itemTex == null) return;
        int totalDirt = 0, cleanedDirt = 0, totalBurr = 0, fixedBurr = 0;
        float scale = TEX_SIZE / 200f;

        float lightTexX = ((currentLightPos.x + 180f) / 360f) * TEX_SIZE;
        float lightTexY = ((currentLightPos.y + 180f) / 360f) * TEX_SIZE;

        for (int y = 0; y < TEX_SIZE; y++)
        {
            int rowIdx = y * TEX_SIZE;
            for (int x = 0; x < TEX_SIZE; x++)
            {
                if (isItemMask[y, x] == 0)
                {
                    pixelBuffer[rowIdx + x] = albedoMap[y, x];
                    continue;
                }

                Color albedo = albedoMap[y, x];
                Vector3 norm = normalMap[y, x];
                float metallic = metallicMap[y, x];
                float smoothness = 0.72f;

                if (finishProgress > 10f)
                {
                    float fRatio = finishProgress / 100f;
                    if (selectedStyle == "NEW")
                    {
                        smoothness = Mathf.Lerp(0.72f, 0.96f, fRatio);
                        albedo *= (1f + 0.14f * fRatio);
                        if (fRatio > 0.85f)
                        {
                            float glintStar = Mathf.Sin(x * 0.35f + y * 0.35f + Time.time * 5f);
                            if (glintStar > 0.96f) albedo += new Color(0.35f, 0.30f, 0.20f, 0f);
                        }
                    }
                    else if (selectedStyle == "PATINA")
                    {
                        smoothness = Mathf.Lerp(0.72f, 0.52f, fRatio);
                        albedo = Color.Lerp(albedo, new Color(albedo.r * 0.88f, albedo.g * 0.84f, albedo.b * 0.74f, 1f), fRatio);
                    }
                }

                byte dVal = dirtMap[y, x];
                if (dVal == 1 || dVal == 2)
                {
                    totalDirt++;
                    if (dVal == 1)
                    {
                        albedo = new Color(albedo.r * 0.42f, albedo.g * 0.46f, albedo.b * 0.34f, 1f);
                        metallic = 0.08f;
                        smoothness = 0.18f;
                    }
                    else cleanedDirt++;
                }

                float bVal = burrMap[y, x];
                if (bVal > 0f)
                {
                    totalBurr++;
                    if (bVal > 0.08f)
                    {
                        albedo = Color.Lerp(albedo, new Color(0.96f, 0.48f, 0.16f, 1f), bVal * 0.88f);
                        norm = new Vector3(-norm.y, norm.x, 0.55f);
                        smoothness = 0.35f;
                    }
                    else fixedBurr++;
                }

                if (chemicalStain)
                {
                    float dx = (x - TEX_SIZE * 0.60f) / scale;
                    float dy = (y - TEX_SIZE * 0.55f) / scale;
                    float stainDist = dx * dx + dy * dy;
                    if (stainDist < 460f)
                    {
                        float blend = Mathf.Clamp01((460f - stainDist) / 140f) * 0.80f;
                        albedo = Color.Lerp(albedo, new Color(0.45f, 0.14f, 0.42f, 1f), blend);
                        smoothness = 0.2f;
                    }
                }

                if (dVal == 3)
                {
                    albedo = new Color(0.22f, 0.20f, 0.18f, 1f);
                    metallic = 0.05f;
                    smoothness = 0.1f;
                }

                Vector3 toLight = new Vector3((lightTexX - x) * 0.00714f, (lightTexY - y) * 0.00714f, 0.85f);
                if (isGemMask[y, x] == 1)
                {
                    float angle = Mathf.Atan2(toLight.y, toLight.x);
                    float prism = Mathf.Sin(angle * 3.5f + (x + y) * 0.15f) * 0.14f;
                    albedo.r = Mathf.Clamp01(albedo.r + prism);
                    albedo.b = Mathf.Clamp01(albedo.b - prism * 0.6f);
                    smoothness = Mathf.Max(smoothness, 0.94f);
                }
                pixelBuffer[rowIdx + x] = EvaluatePBRPixelFast(albedo, norm, metallic, smoothness, toLight);
            }
        }

        itemTex.SetPixels32(pixelBuffer);
        itemTex.Apply();

        if (itemSurfaceImg.sprite == null)
        {
            Sprite sp = Sprite.Create(itemTex, new Rect(0, 0, TEX_SIZE, TEX_SIZE), new Vector2(0.5f, 0.5f));
            itemSurfaceImg.sprite = sp;
            benchItemPreviewImg.sprite = sp;
        }
        benchItemPreviewImg.color = (activeOrder != null) ? Color.white : Color.clear;

        int prevC = cleanProgress;
        int prevG = grindProgress;
        cleanProgress = (totalDirt > 0) ? Mathf.FloorToInt((cleanedDirt / (float)totalDirt) * 100f) : 100;
        grindProgress = (totalBurr > 0) ? Mathf.FloorToInt((fixedBurr / (float)totalBurr) * 100f) : 100;

        if (orderStage == "READY_TO_RESTORE")
        {
            if (prevC < 75 && cleanProgress >= 75)
            {
                PlaySFX(clipCrystalChime, 0.75f, 1.25f);
                bottomHintText.text = "ОЧИСТКА ВЫПОЛНЕНА! Теперь переключитесь на 2. Шлифовка (рывками) или продолжайте до 100%.";
                SpawnFloatingText(new Vector2(0, 80), "✓ Очистка выполнена (75%+)", new Color(0.40f, 0.85f, 1f, 1f));
            }
            if (prevG < 75 && grindProgress >= 75)
            {
                PlaySFX(clipCrystalChime, 0.75f, 1.35f);
                bottomHintText.text = "ШЛИФОВКА ВЫПОЛНЕНА! Перейдите к 3. Закрепка узлов кликами или доведите до 100%.";
                SpawnFloatingText(new Vector2(0, 80), "✓ Шлифовка выполнена (75%+)", new Color(1f, 0.85f, 0.35f, 1f));
            }
        }
    }

    private void SetViewState(string newState)
    {
        viewState = newState;
        if (toolLoopSource != null && toolLoopSource.isPlaying) toolLoopSource.Stop();
        if (toolCursorGO != null) toolCursorGO.SetActive(false);

        windowOverlay.SetActive(newState == "WINDOW_SELECT");
        workbenchOverlay.SetActive(newState == "WORKBENCH");
        cabinetOverlay.SetActive(newState == "CABINET");
        upgradesOverlay.SetActive(newState == "UPGRADES");
        modalOverlay.SetActive(newState == "TWIST" || newState == "RESULT" || newState == "FINALE" || newState == "CERTIFICATE");
        if (sunlightGodRaysImg != null) sunlightGodRaysImg.gameObject.SetActive(newState == "ROOM");
        if (roomVignetteImg != null) roomVignetteImg.gameObject.SetActive(newState == "ROOM");

        if (newState != "WORKBENCH")
        {
            targetBenchZoom = 1.0f;
            benchZoom = 1.0f;
            if (itemSurfaceRT != null) itemSurfaceRT.localScale = Vector3.one;
            if (benchZoomBtnText != null) benchZoomBtnText.text = "🔍 100%";
        }
        if (newState == "UPGRADES")
        {
            SelectMasterStandTab(0);
        }

        if (newState == "WORKBENCH")
        {
            foreach (var cn in activeClickNodes)
            {
                if (cn.clicksLeft > 0 && cn.clicksLeft < cn.maxClicks)
                    cn.resetTimer = Mathf.Max(cn.resetTimer, CLICK_RESET_TIME);
            }
        }

        UpdateRoomIndicators();
        UpdateTopBar();
    }

    private void UpdateRoomIndicators()
    {
        windowBadgeImg.gameObject.SetActive(orderStage == "NONE" || orderStage == "READY_TO_SUBMIT");
        benchBadgeImg.gameObject.SetActive(orderStage == "NEED_DIAGNOSIS" || orderStage == "READY_TO_RESTORE");
        cabinetBadgeImg.gameObject.SetActive(orderStage == "NEED_TOOLS");

        if (TimeController.Instance != null)
        {
            TimeController.Instance.SyncWithOrderCount(completedOrdersCount);
        }

        if (winGlowImg != null && benchGlowImg != null)
        {
            if (completedOrdersCount == 0)
            {
                winGlowImg.color = new Color(0.85f, 0.95f, 1f, 0.36f);
                benchGlowImg.color = new Color(1f, 0.92f, 0.70f, 0.28f);
            }
            else if (completedOrdersCount == 1)
            {
                winGlowImg.color = new Color(1f, 0.98f, 0.80f, 0.42f);
                benchGlowImg.color = new Color(1f, 0.90f, 0.65f, 0.32f);
            }
            else if (completedOrdersCount == 2)
            {
                winGlowImg.color = new Color(1f, 0.60f, 0.30f, 0.46f);
                benchGlowImg.color = new Color(1f, 0.82f, 0.48f, 0.38f);
            }
            else
            {
                winGlowImg.color = new Color(0.24f, 0.35f, 0.65f, 0.38f);
                benchGlowImg.color = new Color(1f, 0.75f, 0.32f, 0.48f);
            }
        }

        if (ambientTintImg != null)
        {
            if (completedOrdersCount == 0) ambientTintImg.color = new Color(0.72f, 0.84f, 1.0f, 0.06f);
            else if (completedOrdersCount == 1) ambientTintImg.color = new Color(1.0f, 0.98f, 0.92f, 0.03f);
            else if (completedOrdersCount == 2) ambientTintImg.color = new Color(1.0f, 0.55f, 0.22f, 0.14f);
            else ambientTintImg.color = new Color(0.12f, 0.18f, 0.40f, 0.26f);
        }

        if (orderStage == "NONE")
        {
            playerBubbleText.text = "Выбрать заказ в Окне заказов";
            bottomHintText.text = "Подойдите к ОКНУ ЗАКАЗОВ слева или загляните на СТЕНД МАСТЕРА справа внизу для покупки оборудования и разряда.";
        }
        else if (orderStage == "NEED_DIAGNOSIS")
        {
            playerBubbleText.text = "Несу предмет на Рабочий стол";
            bottomHintText.text = "Кликните ЛКМ на РАБОЧИЙ СТОЛ сверху, чтобы осмотреть украшение под лупой.";
        }
        else if (orderStage == "NEED_TOOLS")
        {
            playerBubbleText.text = "Нужен Шкаф инструментов";
            bottomHintText.text = "Диагностика завершена! Подойдите к ШКАФУ ИНСТРУМЕНТОВ справа сверху.";
        }
        else if (orderStage == "READY_TO_RESTORE")
        {
            playerBubbleText.text = "Все готово -> К Рабочему столу!";
            bottomHintText.text = "Инструменты в руках! Вернитесь к РАБОЧЕМУ СТОЛУ для выполнения 4 этапов реставрации.";
        }
        else if (orderStage == "READY_TO_SUBMIT")
        {
            playerBubbleText.text = "Готово! Несу в Окно заказов";
            bottomHintText.text = "Украшение восстановлено! Отнесите его в ОКНО ЗАКАЗОВ слева для получения оплаты.";
        }
    }

    private void UpdateTopBar()
    {
        string rankStr = qualificationRank == 1 ? "I разряд" : (qualificationRank == 2 ? "II разряд" : "III разряд (Гранд)");
        string ordName = (activeOrder != null) ? activeOrder.title : "Свободен";
        string timeOfDay = (TimeController.Instance != null) ? TimeController.Instance.CurrentPhase : (completedOrdersCount == 0 ? "Утро" : (completedOrdersCount == 1 ? "День" : (completedOrdersCount == 2 ? "Закат" : "Вечер")));
        topTitleText.text = $"Смена ({timeOfDay}) • Заказов: {completedOrdersCount}/{TOTAL_ORDERS_TO_WIN} • {rankStr} • {ordName}";
        topStatsText.text = $"Касса: {money} руб.   |   Доверие: {trust}%   |   Износ: {Mathf.FloorToInt(overScrubDamage)}%";
    }

    private void MovePlayerToStation(Vector2 stationInteractPos, string stationName)
    {
        targetPos = stationInteractPos;
        pendingStation = stationName;
        clickMarkerRT.anchoredPosition = stationInteractPos;
        markerAlpha = 1f;
    }

    private void OnPlayerArrivedAtStation(string station)
    {
        if (station == "WINDOW")
        {
            PlaySFX(clipBell, 0.7f, 1.0f);
            if (orderStage == "NONE")
            {
                PopulateWindowCardsUI();
                SetViewState("WINDOW_SELECT");
            }
            else if (orderStage == "READY_TO_SUBMIT")
            {
                EvaluateCompletedOrder();
            }
            else
            {
                bottomHintText.text = "У вас уже есть активный заказ в работе!";
            }
        }
        else if (station == "WORKBENCH")
        {
            if (orderStage == "NONE")
                bottomHintText.text = "Сначала возьмите заказ в Окне заказов слева!";
            else
                OpenWorkbenchView();
        }
        else if (station == "CABINET")
        {
            if (orderStage == "NONE")
                bottomHintText.text = "Сначала примите заказ в Окне заказов слева!";
            else if (orderStage == "NEED_DIAGNOSIS")
                bottomHintText.text = "Сначала осмотрите предмет под лупой на Рабочем столе!";
            else
                OpenCabinetView();
        }
        else if (station == "UPGRADES")
        {
            OpenUpgradesView();
        }
        else if (station == "FIREPLACE")
        {
            PlaySFX(clipHearthFire, 0.85f, Random.Range(0.95f, 1.05f));
            PlaySFX(clipBell, 0.5f, 1.25f);
            EmitFireplaceSparks(16);
            fireplaceStokeTimer = 2.5f;
            overScrubDamage = Mathf.Max(0f, overScrubDamage - 2f);
            SpawnFloatingText(new Vector2(-270f, 160f), "+15% к теплу мастерской", new Color(1f, 0.85f, 0.40f, 1f));
            bottomHintText.text = "Вы подбросили сухое березовое полено в чугунный камин. По мастерской разнеслось благодатное тепло!";
            UpdateTopBar();
        }
    }

    private void OpenUpgradesView()
    {
        SetViewState("UPGRADES");
        RefreshUpgradesUI();
    }

    private void RefreshUpgradesUI()
    {
        upgStatusText.text = $"Ваш баланс: {money} руб.   |   Текущий разряд мастера: {qualificationRank}-й разряд";

        upgBtnLabels[0].text = upgPrecisionBits ? "Куплено" : "Купить за 800 руб.";
        upgBtnImgs[0].color = upgPrecisionBits ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.28f, 0.52f, 0.32f, 1f);
        if (upgBadgeImgs[0] != null) upgBadgeImgs[0].color = upgPrecisionBits ? new Color(0.40f, 0.85f, 1f, 1f) : new Color(0.35f, 0.55f, 0.65f, 0.65f);

        upgBtnLabels[1].text = upgStabilizer ? "Куплено" : "Купить за 1200 руб.";
        upgBtnImgs[1].color = upgStabilizer ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.28f, 0.52f, 0.32f, 1f);
        if (upgBadgeImgs[1] != null) upgBadgeImgs[1].color = upgStabilizer ? new Color(0.35f, 0.90f, 0.45f, 1f) : new Color(0.35f, 0.55f, 0.40f, 0.65f);

        upgBtnLabels[2].text = (qualificationRank >= 2) ? "Получено" : "Повысить за 1600 руб.";
        upgBtnImgs[2].color = (qualificationRank >= 2) ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.55f, 0.40f, 0.16f, 1f);
        if (upgBadgeImgs[2] != null) upgBadgeImgs[2].color = (qualificationRank >= 2) ? new Color(0.95f, 0.78f, 0.25f, 1f) : new Color(0.65f, 0.50f, 0.25f, 0.65f);

        upgBtnLabels[3].text = (qualificationRank >= 3) ? "Получено" : "Повысить за 3000 руб.";
        upgBtnImgs[3].color = (qualificationRank >= 3) ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.55f, 0.40f, 0.16f, 1f);
        if (upgBadgeImgs[3] != null) upgBadgeImgs[3].color = (qualificationRank >= 3) ? new Color(0.86f, 0.35f, 0.90f, 1f) : new Color(0.55f, 0.30f, 0.58f, 0.65f);

        UpdateTopBar();
    }

    private void BuyUpgrade(int idx)
    {
        if (idx == 0 && !upgPrecisionBits)
        {
            if (money >= 800) { money -= 800; upgPrecisionBits = true; PlaySFX(clipCoin); bottomHintText.text = "Куплены Алмазные микро-насадки! Скорость и радиус работы увеличены на 40%."; }
            else { PlaySFX(clipFail); bottomHintText.text = "Недостаточно денег в кассе (нужно 800 руб.)!"; }
        }
        else if (idx == 1 && !upgStabilizer)
        {
            if (money >= 1200) { money -= 1200; upgStabilizer = true; PlaySFX(clipCoin); bottomHintText.text = "Куплен Оптический стабилизатор! Износ слоя при ошибках снижен на 60%."; }
            else { PlaySFX(clipFail); bottomHintText.text = "Недостаточно денег в кассе (нужно 1200 руб.)!"; }
        }
        else if (idx == 2 && qualificationRank < 2)
        {
            if (money >= 1600) { money -= 1600; qualificationRank = 2; PlaySFX(clipCoin); PlaySFX(clipBell, 0.75f, 1.15f); bottomHintText.text = "Поздравляем! Вы получили II разряд — теперь в Окне заказов доступны элитные реликвии!"; }
            else { PlaySFX(clipFail); bottomHintText.text = "Недостаточно денег для сертификации II разряда (нужно 1600 руб.)!"; }
        }
        else if (idx == 3 && qualificationRank < 3)
        {
            if (qualificationRank < 2) { PlaySFX(clipFail); bottomHintText.text = "Сначала получите Сертификат II разряда!"; return; }
            if (money >= 3000) { money -= 3000; qualificationRank = 3; trust = Mathf.Min(100, trust + 20); PlaySFX(clipCoin); PlaySFX(clipBell, 0.85f, 1.35f); bottomHintText.text = "Вы стали Гранд-мастером III разряда! Гонорары за все заказы увеличены на +35%!"; }
            else { PlaySFX(clipFail); bottomHintText.text = "Недостаточно денег для патента III разряда (нужно 3000 руб.)!"; }
        }
        RefreshUpgradesUI();
        UpdateRoomIndicators();
    }

    private void PopulateWindowCardsUI()
    {
        for (int i = 0; i < 3; i++)
        {
            OrderData od = currentWindowChoices[i];
            cardTitles[i].text = od.title;
            cardClients[i].text = od.clientName + $"\nБазовая оплата: {od.baseReward} руб.";
            cardDescs[i].text = od.shortDesc + "\n\n" + od.briefingText;

            bool locked = (qualificationRank < od.requiredRank);
            cardBtnLabels[i].text = locked ? $"Нужен {od.requiredRank}-й разряд" : "Принять заказ";
            cardBtnImgs[i].color = locked ? new Color(0.45f, 0.24f, 0.22f, 1f) : new Color(0.26f, 0.52f, 0.32f, 1f);

            string romanRank = od.requiredRank == 1 ? "I" : (od.requiredRank == 2 ? "II" : "III");
            cardBadgeTexts[i].text = romanRank;
            if (od.requiredRank == 1)
                cardBadgeImgs[i].color = new Color(0.75f, 0.58f, 0.35f, 1f);
            else if (od.requiredRank == 2)
                cardBadgeImgs[i].color = new Color(0.95f, 0.78f, 0.25f, 1f);
            else
                cardBadgeImgs[i].color = new Color(0.86f, 0.32f, 0.88f, 1f);
        }
    }

    private void SelectOrderFromWindow(int index)
    {
        OrderData chosen = currentWindowChoices[index];
        if (qualificationRank < chosen.requiredRank)
        {
            PlaySFX(clipFail);
            bottomHintText.text = $"Для этого заказа требуется {chosen.requiredRank}-й разряд квалификации! Повысьте разряд на СТЕНДЕ МАСТЕРА справа внизу.";
            return;
        }

        PlaySFX(clipBell, 0.8f, 1.15f);
        activeOrder = chosen;
        orderStage = "NEED_DIAGNOSIS";

        zonesDiscovered = 0;
        cleanProgress = 0;
        grindProgress = 0;
        clickFixProgress = 0;
        finishProgress = 0f;
        chemicalStain = false;
        wrongToolScratches = false;
        overScrubDamage = 0f;
        twistChecked = false;

        selectedMechTool = null;
        selectedSolvent = null;
        selectedStyle = null;

        foreach (var d in activeOrder.diagnostics) d.found = false;
        itemSurfaceImg.sprite = null;
        GenerateItemTexture(activeOrder.shapeType);
        SetupClickNodesForItem(activeOrder.shapeType);
        RefreshItemVisuals();

        SetViewState("ROOM");
        MovePlayerToStation(posBenchInteract, "WORKBENCH");
    }

    private void OpenWorkbenchView()
    {
        SetViewState("WORKBENCH");
        if (orderStage == "NEED_DIAGNOSIS")
        {
            loupeGO.SetActive(true);
            benchToolsPanel.SetActive(false);
            clickNodesContainer.SetActive(false);
            btnGoToCabinetFromBench.gameObject.SetActive(false);
            benchModeNoticeText.text = "ДИАГНОСТИКА ПОД ЛУПОЙ\n\nЗажмите ЛКМ и осмотрите 3 зоны украшения, чтобы определить состав и повреждения.";
            benchLogText.text = $"Предмет: {activeOrder.title}\n\nЖурнал диагностики:\n(Найдено зон: {zonesDiscovered}/3)";
            benchProgressText.text = "";
            bottomHintText.text = "Водите лупой с зажатой ЛКМ по украшению для поиска 3 проблемных зон.";
        }
        else if (orderStage == "NEED_TOOLS")
        {
            loupeGO.SetActive(false);
            benchToolsPanel.SetActive(false);
            clickNodesContainer.SetActive(false);
            btnGoToCabinetFromBench.gameObject.SetActive(true);
            benchModeNoticeText.text = "ДИАГНОСТИКА ЗАВЕРШЕНА!\n\nПерейдите к Шкафу инструментов справа и подберите оснастку.";
            bottomHintText.text = "Нажмите кнопку перехода к Шкафу инструментов.";
        }
        else if (orderStage == "READY_TO_RESTORE" || orderStage == "READY_TO_SUBMIT")
        {
            loupeGO.SetActive(false);
            benchToolsPanel.SetActive(true);
            btnGoToCabinetFromBench.gameObject.SetActive(false);
            benchModeNoticeText.text = "ЭТАПЫ РЕСТАВРАЦИИ:\n1. Плавная очистка\n2. Быстрая шлифовка рывками\n3. Быстрые клики без пауз\n4. Финишная полировка";

            string toolName = (selectedMechTool == "DIAMOND_FILE") ? "Алмазный надфиль" : ((selectedMechTool == "RING_GRINDER") ? "Бормашина для колец" : "Микро-шпатель");
            string solvName = (selectedSolvent == "ACID_GEL") ? "Ультра-гель" : ((selectedSolvent == "SPIRIT") ? "Спиртовой эфир" : "Технический ацетон");
            string styleName = (selectedStyle == "NEW") ? "«Как новое»" : "«Патина»";
            benchLogText.text = $"Предмет: {activeOrder.title}\nКлиент: {activeOrder.clientName}\n\nОснастка в руках:\n• {toolName}\n• {solvName}\n• Финиш: {styleName}\n\nПлан мастера:\n1. Очистка ({solvName})\n2. Шлифовка ({toolName})\n3. Закрепка штифтов (кликами)\n4. Полировка ({styleName})";

            SelectBenchTool("CLEAN");
        }
    }

    private void OpenCabinetView()
    {
        SetViewState("CABINET");
        HighlightList(toolBtnImgs, -1, Color.white);
        HighlightList(solventBtnImgs, -1, Color.white);
        HighlightList(styleBtnImgs, -1, Color.white);

        string memo = $"ДАННЫЕ ДИАГНОСТИКИ:\n{activeOrder.title}\nКлиент: {activeOrder.clientName}\n";
        foreach (var d in activeOrder.diagnostics)
            memo += "\n• " + d.text;
        cabinetDiagReminderText.text = memo;
        bottomHintText.text = "Выберите спец-инструмент, растворитель и стиль финиша.";
    }

    private void ConfirmCabinetSelection()
    {
        if (string.IsNullOrEmpty(selectedMechTool) || string.IsNullOrEmpty(selectedSolvent) || string.IsNullOrEmpty(selectedStyle))
        {
            PlaySFX(clipFail);
            bottomHintText.text = "Выберите в шкафу все 3 позиции: Инструмент, Растворитель и Подход!";
            return;
        }

        if (selectedSolvent != activeOrder.correctSolvent) chemicalStain = true;
        if (selectedMechTool != activeOrder.correctMechTool) wrongToolScratches = true;

        RefreshItemVisuals();
        orderStage = "READY_TO_RESTORE";
        SetViewState("ROOM");
        MovePlayerToStation(posBenchInteract, "WORKBENCH");
    }

    private void CheckMidOrderTwist()
    {
        return;
    }

    private void TriggerMidOrderTwist()
    {
        SetViewState("TWIST");
        modalSealGO.SetActive(false);
        modalTitle.text = "СМЕНА УСЛОВИЙ В МАСТЕРСКОЙ!";
        modalBody.text = activeOrder.twistText;
        modalBtn1.gameObject.SetActive(true);
        modalBtn2.gameObject.SetActive(true);

        if (activeOrder.twistType == "SECRET_BOX")
        {
            modalBtn1Text.text = "Вскрыть тайник без разрешения владельца";
            modalBtn2Text.text = "Не трогать тайник и продолжить работу";
        }
        else if (activeOrder.twistType == "OUT_OF_MATERIAL")
        {
            modalBtn1Text.text = "Заказать экспресс-доставку ультра-геля (-150 руб.)";
            modalBtn2Text.text = "Разбавить техническим ацетоном (Бесплатно, риск пятна!)";
        }
        else if (activeOrder.twistType == "CLIENT_CHANGES_MIND")
        {
            modalBtn1Text.text = "Переключиться на вид «Как новое» по просьбе клиента";
            modalBtn2Text.text = "Сохранить музейную патину вопреки телеграмме";
        }
    }

    private void OnModalButtonClick(int btnIdx)
    {
        if (viewState == "TWIST")
        {
            if (activeOrder.twistType == "SECRET_BOX")
            {
                if (btnIdx == 1)
                {
                    secretViolated = true;
                    trust = Mathf.Max(0, trust - 60);
                    benchLogText.text += "\n\n[ТАЙНИК ВСКРЫТ]: Пломба сорвана! Доверие упало.";
                }
                else
                {
                    trust = Mathf.Min(100, trust + 10);
                    benchLogText.text += "\n\n[ТАЙНИК СОХРАНЕН]: Вы бережно закрыли крышечку каста.";
                }
            }
            else if (activeOrder.twistType == "OUT_OF_MATERIAL")
            {
                if (btnIdx == 1)
                {
                    money = Mathf.Max(0, money - 150);
                    benchLogText.text += "\n\n[ДОСТАВКА]: Курьер доставил чистый гель (-150 руб.).";
                }
                else
                {
                    chemicalStain = true;
                    RefreshItemVisuals();
                    benchLogText.text += "\n\n[ПЯТНО]: Ацетон оставил мутный след на оправе!";
                }
            }
            else if (activeOrder.twistType == "CLIENT_CHANGES_MIND")
            {
                activeOrder.preferredStyle = "NEW";
                selectedStyle = (btnIdx == 1) ? "NEW" : "PATINA";
                RefreshItemVisuals();
            }
            SetViewState("WORKBENCH");
        }
        else if (viewState == "RESULT")
        {
            completedOrdersCount++;
            activeOrder = null;
            orderStage = "NONE";
            benchItemPreviewImg.color = Color.clear;

            if (completedOrdersCount < TOTAL_ORDERS_TO_WIN)
            {
                GenerateWindowOrderChoices();
                SetViewState("ROOM");
            }
            else
            {
                ShowShiftFinale();
            }
        }
        else if (viewState == "CERTIFICATE")
        {
            SetViewState("UPGRADES");
            SelectMasterStandTab(1);
        }
        else if (viewState == "FINALE")
        {
            trust = 100;
            money = 400;
            secretViolated = false;
            completedOrdersCount = 0;
            completedOrderTypes.Clear();
            upgPrecisionBits = false;
            upgStabilizer = false;
            qualificationRank = 1;
            historyLog.Clear();
            activeOrder = null;
            orderStage = "NONE";
            GenerateWindowOrderChoices();
            SetViewState("ROOM");
        }
    }

    private void EvaluateCompletedOrder()
    {
        SetViewState("RESULT");
        int pay = activeOrder.baseReward;
        if (qualificationRank >= 3)
            pay = Mathf.FloorToInt(pay * 1.35f);

        List<string> notes = new List<string>();
        int severeErrors = 0;

        if (chemicalStain)
        {
            severeErrors++;
            pay = Mathf.FloorToInt(pay * 0.45f);
            notes.Add("• НЕВЕРНЫЙ РАСТВОРИТЕЛЬ: Химическое пятно испортило поверхность (-55%).");
        }
        else notes.Add("• ХИМИЯ: Растворитель подобран безупречно.");

        if (wrongToolScratches)
        {
            severeErrors++;
            pay = Mathf.FloorToInt(pay * 0.55f);
            notes.Add("• НЕВЕРНЫЙ ИНСТРУМЕНТ: Неподходящая насадка оставила грубые борозды (-45%).");
        }
        else notes.Add("• ОСНАСТКА: Инструмент выбран точно по диагностике.");

        if (overScrubDamage > 25f)
        {
            severeErrors++;
            pay = Mathf.FloorToInt(pay * 0.55f);
            notes.Add($"• ПОВРЕЖДЕНИЕ СЛОЯ: Износ поверхности составил {Mathf.FloorToInt(overScrubDamage)}% (-45%).");
        }
        else notes.Add("• ТЕХНИКА РУКИ: Чистая работа без повреждения базового слоя.");

        if (selectedStyle == activeOrder.preferredStyle)
        {
            int bonus = (activeOrder.clientType == "COLLECTOR") ? 600 : 400;
            pay += bonus;
            notes.Add($"• ВЫБОР СТИЛЯ: Полное попадание в ожидания клиента (+{bonus} руб.)!");
        }
        else
        {
            pay = Mathf.FloorToInt(pay * 0.7f);
            notes.Add("• ВЫБОР СТИЛЯ: Выбранный финиш разошелся с пожеланием заказчика (-30%).");
        }

        bool isRuinedToZero = (overScrubDamage >= 65f) ||
                              (chemicalStain && wrongToolScratches) ||
                              (severeErrors >= 2 && selectedStyle != activeOrder.preferredStyle);

        if (isRuinedToZero)
        {
            pay = 0;
            trust = Mathf.Max(0, trust - 25);
            notes.Add("• КАТАСТРОФИЧЕСКИЙ БРАК: Из-за критических повреждений заказчик отказался платить! Гонорар снижен до 0 руб.");
            PlaySFX(clipFail, 0.85f, 1.0f);
        }
        else
        {
            PlaySFX(clipCoin, 0.9f, 1.05f);
            EmitSparks(Vector2.zero, 18, new Color(1f, 0.90f, 0.40f, 1f));
            SpawnFloatingText(new Vector2(0, 40), $"+{pay} руб. в кассу!", new Color(1f, 0.88f, 0.35f, 1f));
        }

        money += pay;

        string story = (selectedStyle == "NEW") ? activeOrder.histNew : activeOrder.histPatina;
        if (activeOrder.shapeType == "RING" && secretViolated) story = activeOrder.histSpecial;
        else if (isRuinedToZero || chemicalStain || wrongToolScratches || overScrubDamage > 45f) story = activeOrder.histSpecial;

        historyLog.Add($"■ {activeOrder.title}\nИтог: {story} (Выплата: {pay} руб.)");
        completedOrderTypes.Add(activeOrder.shapeType);

        modalSealGO.SetActive(true);
        if (isRuinedToZero)
        {
            modalSealGO.GetComponent<Image>().color = new Color(0.85f, 0.24f, 0.20f, 1f);
            modalSealText.text = "БРАК";
        }
        else if (qualificationRank >= 3)
        {
            modalSealGO.GetComponent<Image>().color = new Color(0.95f, 0.78f, 0.25f, 1f);
            modalSealText.text = "ЭТАЛОН";
        }
        else
        {
            modalSealGO.GetComponent<Image>().color = new Color(0.24f, 0.78f, 0.38f, 1f);
            modalSealText.text = "ПРИНЯТО";
        }

        modalTitle.text = "КАРТОЧКА ПРЕДМЕТА: " + activeOrder.title;
        modalBody.text = $"ОЦЕНКА ЗАКАЗЧИКА (Гонорар: {pay} руб. | Касса: {money} руб.):\n{string.Join("\n", notes)}\n\nИСТОРИЯ ПРЕДМЕТА:\n«{story}»";
        modalBtn1.gameObject.SetActive(true);
        modalBtn1Text.text = (completedOrdersCount + 1 < TOTAL_ORDERS_TO_WIN) ? "Продолжить работу в мастерской ->" : "Подвести итоги смены";
        modalBtn2.gameObject.SetActive(false);
        UpdateTopBar();
    }

    private void ShowShiftFinale()
    {
        SetViewState("FINALE");
        PlaySFX(clipBell, 0.9f, 1.1f);
        EmitSparks(Vector2.zero, 28, new Color(1f, 0.92f, 0.45f, 1f));
        SpawnFloatingText(new Vector2(0, 60), "★ ДИПЛОМ ГИЛЬДИИ ВРУЧЕН!", new Color(1f, 0.95f, 0.50f, 1f));
        modalSealGO.SetActive(true);
        modalSealGO.GetComponent<Image>().color = secretViolated ? new Color(0.65f, 0.35f, 0.35f, 1f) : new Color(0.95f, 0.78f, 0.25f, 1f);
        modalSealText.text = secretViolated ? "АРХИВ" : (qualificationRank >= 3 ? "ГРАНД" : "МАСТЕР");
        string rank = secretViolated ? "Любопытный ремесленник (Доверие утрачено)" :
                      (qualificationRank == 3 ? "Легендарный Гранд-мастер III разряда" : $"Мастер-реставратор {qualificationRank}-го разряда");
        int efficiencyPercent = Mathf.Clamp(Mathf.RoundToInt(trust * 0.75f + (money / 70f)), 10, 100);
        modalTitle.text = "СМЕНА ЗАВЕРШЕНА — ЗОЛОТОЙ ДИПЛОМ ГИЛЬДИИ";
        modalBody.text = $"Квалификационный статус: {rank}\nИтоговый капитал: {money} руб.  |  Репутация гильдии: {trust}%\nТочность реставрации: {efficiencyPercent}%\n\nВОССТАНОВЛЕННЫЕ МУЗЕЙНЫЕ ШЕДЕВРЫ:\n{string.Join("\n\n", historyLog)}";
        modalBtn1.gameObject.SetActive(true);
        modalBtn1Text.text = "Начать новую рабочую смену";
        modalBtn2.gameObject.SetActive(false);
    }

    private void EmitFireplaceSparks(int count)
    {
        if (fireplaceSparks == null || fireplaceSparks.Count == 0) return;
        int emitted = 0;
        for (int i = 0; i < fireplaceSparks.Count && emitted < count; i++)
        {
            FireplaceSpark spk = fireplaceSparks[i];
            if (spk.life <= 0f)
            {
                spk.life = Random.Range(1.2f, 2.2f);
                spk.maxLife = spk.life;
                spk.pos = new Vector2(Random.Range(-40f, 40f), Random.Range(-25f, -10f));
                spk.vel = new Vector2(Random.Range(-15f, 15f), Random.Range(45f, 95f));
                spk.wobblePhase = Random.Range(0f, Mathf.PI * 2f);
                spk.img.color = new Color(1f, 0.90f, 0.55f, 0.85f);
                spk.rt.anchoredPosition = spk.pos;
                emitted++;
            }
        }
    }

    private void UpdateFireplaceSparks(float dt)
    {
        if (viewState != "ROOM")
        {
            for (int i = 0; i < fireplaceSparks.Count; i++)
            {
                fireplaceSparks[i].img.color = Color.clear;
            }
            return;
        }

        if (Random.value < dt * 4.5f)
        {
            EmitFireplaceSparks(1);
        }

        float t = Time.time;
        for (int i = 0; i < fireplaceSparks.Count; i++)
        {
            FireplaceSpark spk = fireplaceSparks[i];
            if (spk.life > 0f)
            {
                spk.life -= dt;
                spk.vel.y += 35f * dt;
                spk.pos.y += spk.vel.y * dt;
                spk.pos.x += (spk.vel.x + Mathf.Sin(t * 6f + spk.wobblePhase) * 16f) * dt;
                spk.rt.anchoredPosition = spk.pos;

                float prog = 1f - (spk.life / spk.maxLife);
                Color c = Color.Lerp(new Color(1f, 0.95f, 0.75f, 0.9f), new Color(0.95f, 0.35f, 0.05f, 0f), prog);
                spk.img.color = c;
            }
            else
            {
                spk.img.color = Color.clear;
            }
        }
    }

    private void UpdateDustMotes(float dt)
    {
        if (viewState != "ROOM")
        {
            for (int i = 0; i < dustMotes.Count; i++)
                dustMotes[i].img.color = Color.clear;
            return;
        }
        float t = Time.time;
        Color phaseAmbient = (TimeController.Instance != null)
            ? TimeController.Instance.GetAmbientColor(TimeController.Instance.CurrentPhase, TimeController.Instance.PhaseProgress)
            : new Color(1f, 0.90f, 0.65f, 1f);

        for (int i = 0; i < dustMotes.Count; i++)
        {
            DustMote m = dustMotes[i];
            m.basePos.x += Mathf.Cos(t * 0.12f + m.phase) * dt * 3.2f;
            m.basePos.y += Mathf.Sin(t * 0.09f + m.phase * 1.2f) * dt * 2.2f + dt * 1.4f;
            if (m.basePos.x < -560f) m.basePos.x = 560f;
            else if (m.basePos.x > 560f) m.basePos.x = -560f;
            if (m.basePos.y < -230f) m.basePos.y = 190f;
            else if (m.basePos.y > 190f) m.basePos.y = -230f;

            float px = m.basePos.x + Mathf.Cos(t * 0.45f + m.phase) * m.radius;
            float py = m.basePos.y + Mathf.Sin(t * 0.35f + m.phase * 1.5f) * (m.radius * 0.65f);
            m.rt.anchoredPosition = new Vector2(px, py);

            float inWin = Mathf.Clamp01(1f - Mathf.Abs(px - (-390f)) / 170f) * Mathf.Clamp01((py + 60f) / 200f);
            float inLamp = Mathf.Clamp01(1f - Mathf.Abs(px) / 150f) * Mathf.Clamp01(1f - Mathf.Abs(py - 150f) / 100f);
            float glint = Mathf.Max(inWin * 1.3f, inLamp * 1.15f);

            float pulse = (Mathf.Sin(t * 1.8f + m.phase) + 1f) * 0.5f;
            float alpha = Mathf.Clamp(0.12f + pulse * 0.26f + glint * 0.28f, 0.08f, 0.68f);
            Color col = Color.Lerp(new Color(1f, 0.90f, 0.65f), phaseAmbient, 0.35f);
            m.img.color = new Color(col.r, col.g, col.b, alpha);
        }
    }

    public void StepCatBreathing(float dt)
    {
        if (catRT == null) return;
        float t = Time.time;
        catClickScale = Vector3.Lerp(catClickScale, Vector3.one, dt * 5.5f);

        if (Mathf.Abs(catRT.localScale.x - 1f) > 0.05f || Mathf.Abs(catRT.localScale.y - 1f) > 0.05f)
        {
            catRT.localScale = Vector3.Lerp(catRT.localScale, Vector3.one, dt * 6f);
        }
        else
        {
            float breathY = 1.0f + Mathf.Sin(t * 2.1f) * 0.038f;
            float breathX = 1.0f + Mathf.Cos(t * 2.1f) * 0.020f;
            catRT.localScale = new Vector3(breathX, breathY, 1f);
        }

        if (catTailRT != null)
        {
            float tailAngle = Mathf.Sin(t * 2.4f) * 15.0f + Mathf.Sin(t * 0.8f + 0.5f) * 4.5f;
            catTailRT.localRotation = Quaternion.Euler(0f, 0f, tailAngle);
        }

        if (catLeftEarRT != null)
        {
            float cycleL = (t * 0.52f) % 4.8f;
            float twitchL = (cycleL < 0.22f) ? Mathf.Sin(cycleL * Mathf.PI * 13.6f) * 11.5f : 0f;
            catLeftEarRT.localRotation = Quaternion.Euler(0f, 0f, twitchL);
        }

        if (catRightEarRT != null)
        {
            float cycleR = ((t + 2.5f) * 0.44f) % 6.1f;
            float twitchR = (cycleR < 0.18f) ? Mathf.Sin(cycleR * Mathf.PI * 16.5f) * -10.0f : 0f;
            catRightEarRT.localRotation = Quaternion.Euler(0f, 0f, twitchR);
        }
    }

    public void StepPlayerMovement(float dt)
    {
        float distToTarget = Vector2.Distance(playerPos, targetPos);
        if (distToTarget > 2f)
        {
            float stepSpeed = Mathf.Min(moveSpeed, distToTarget * 8.5f);
            playerPos = Vector2.MoveTowards(playerPos, targetPos, stepSpeed * dt);
            walkAnimTimer += dt * 14f;
            float bobY = Mathf.Abs(Mathf.Sin(walkAnimTimer)) * 5.5f;
            playerRT.anchoredPosition = new Vector2(playerPos.x, playerPos.y + bobY);

            float moveDeltaX = targetPos.x - playerPos.x;
            if (Mathf.Abs(moveDeltaX) > 1.5f)
            {
                float sign = Mathf.Sign(moveDeltaX);
                playerRT.localScale = new Vector3(sign, 1f, 1f);
                if (playerBubbleText != null && playerBubbleText.transform.parent != null)
                {
                    playerBubbleText.transform.parent.localScale = new Vector3(sign, 1f, 1f);
                }
            }

            stepSoundTimer -= dt;
            if (stepSoundTimer <= 0f)
            {
                stepSoundTimer = 0.26f;
                PlaySFX(clipStep, 0.45f, Random.Range(0.92f, 1.08f));
            }
        }
        else
        {
            bool atWorkbench = (pendingStation == null && Vector2.Distance(playerPos, posBenchInteract) < 8f);
            if (atWorkbench)
            {
                float workBobY = Mathf.Sin(Time.time * 1.85f) * 2.4f + Mathf.Cos(Time.time * 0.92f) * 1.1f;
                float workTilt = Mathf.Sin(Time.time * 1.85f) * 1.8f;
                float chestScale = 1.0f + Mathf.Sin(Time.time * 1.85f) * 0.026f;
                float chestWidth = 1.0f + Mathf.Cos(Time.time * 1.85f) * 0.014f;
                playerRT.anchoredPosition = new Vector2(playerPos.x, playerPos.y + workBobY);
                playerRT.localRotation = Quaternion.Euler(0f, 0f, workTilt);
                playerRT.localScale = new Vector3(Mathf.Sign(playerRT.localScale.x) * chestWidth, chestScale, 1f);
            }
            else
            {
                float idleBreathing = Mathf.Sin(Time.time * 2.4f) * 1.6f;
                playerRT.anchoredPosition = new Vector2(playerPos.x, playerPos.y + idleBreathing);
                playerRT.localRotation = Quaternion.identity;
                playerRT.localScale = new Vector3(Mathf.Sign(playerRT.localScale.x), 1f, 1f);
            }

            if (!string.IsNullOrEmpty(pendingStation))
            {
                string st = pendingStation;
                pendingStation = null;
                OnPlayerArrivedAtStation(st);
            }
        }
    }

    public void InspectWithLoupe(Vector2 localPt)
    {
        if (activeOrder == null || activeOrder.diagnostics == null) return;
        if (toolCursorGO != null && toolCursorGO.activeSelf) toolCursorGO.SetActive(false);
        if (loupeRT != null) loupeRT.anchoredPosition = localPt;
        float closestDist = 999f;
        foreach (var d in activeOrder.diagnostics)
        {
            if (!d.found)
            {
                float curD = Vector2.Distance(localPt, new Vector2(d.x, d.y));
                if (curD < closestDist) closestDist = curD;
            }
        }
        float proximity = Mathf.Clamp01(1f - closestDist / 70f);
        if (loupeGO != null)
        {
            Image loupeImg = loupeGO.GetComponent<Image>();
            if (loupeImg != null) loupeImg.color = Color.Lerp(Color.white, new Color(1f, 0.90f, 0.50f, 1f), proximity);
        }
        if (loupeRT != null) loupeRT.localScale = Vector3.one * (1f + proximity * 0.08f);

        foreach (var d in activeOrder.diagnostics)
        {
            if (!d.found && Vector2.Distance(localPt, new Vector2(d.x, d.y)) < d.r)
            {
                d.found = true;
                zonesDiscovered++;
                PlaySFX(clipCrystalChime, 0.8f, 0.95f + zonesDiscovered * 0.18f);
                if (benchLogText != null) benchLogText.text += "\n\n• " + d.text;
                if (bottomHintText != null) bottomHintText.text = $"Найдено повреждений под лупой: {zonesDiscovered} / 3";
                EmitSparks(localPt, 8, new Color(0.95f, 0.90f, 0.45f, 1f));
                SpawnFloatingText(localPt + new Vector2(0, 24), "+1 Дефект обнаружен!", new Color(1f, 0.90f, 0.45f, 1f));
                if (zonesDiscovered >= 3)
                {
                    PlaySFX(clipBell, 0.75f, 1.2f);
                    EmitSparks(localPt, 16, new Color(1f, 0.95f, 0.50f, 1f));
                    orderStage = "NEED_TOOLS";
                    OpenWorkbenchView();
                }
            }
        }
    }

    public void SimulateLoupeInspection(Vector2 localPt)
    {
        InspectWithLoupe(localPt);
    }

    public void ApplyScrubbing(Vector2 localPt, float speedRatio, float dt)
    {
        if (dirtMap == null) return;
        int px = Mathf.FloorToInt(((localPt.x + 180f) / 360f) * TEX_SIZE);
        int py = Mathf.FloorToInt(((localPt.y + 180f) / 360f) * TEX_SIZE);
        bool modified = false;

        float radiusMult = upgPrecisionBits ? 1.4f : 1.0f;
        float dmgMult = upgStabilizer ? 0.4f : 1.0f;
        if (barsikBuffTimer > 0f) dmgMult *= 0.5f;
        int brushR = Mathf.RoundToInt(15 * radiusMult);

        bool isTooFast = speedRatio > (upgStabilizer ? 0.86f : 0.72f);
        for (int y = Mathf.Max(0, py - brushR); y <= Mathf.Min(TEX_SIZE - 1, py + brushR); y++)
        {
            for (int x = Mathf.Max(0, px - brushR); x <= Mathf.Min(TEX_SIZE - 1, px + brushR); x++)
            {
                if ((x - px) * (x - px) + (y - py) * (y - py) <= brushR * brushR)
                {
                    if (isTooFast)
                    {
                        dirtMap[y, x] = 3;
                        modified = true;
                    }
                    else if (dirtMap[y, x] == 1 && speedRatio > 0.02f)
                    {
                        dirtMap[y, x] = 2;
                        modified = true;
                    }
                }
            }
        }

        if (modified && isTooFast)
        {
            overScrubDamage = Mathf.Min(100f, overScrubDamage + dt * 5.0f * dmgMult);
        }

        if (modified && !isTooFast)
        {
            sparkSpawnTimer += dt;
            if (sparkSpawnTimer > 0.08f)
            {
                sparkSpawnTimer = 0f;
                EmitSparks(localPt, 1, new Color(0.72f, 0.90f, 1f, 0.70f));
            }
        }

        if (modified)
        {
            RefreshItemVisuals();
            UpdateTopBar();
            CheckMidOrderTwist();
        }
    }

    public void ApplyGrinding(Vector2 localPt, float speedRatio, float dt)
    {
        if (burrMap == null) return;
        if (cleanProgress < 35)
        {
            PlaySFX(clipSpringReset, 0.35f, 1.4f);
            if (bottomHintText != null) bottomHintText.text = "НАРУШЕН ПОРЯДОК: Сначала смойте хотя бы 35% окислов растворителем!";
            return;
        }
        if (speedRatio < 0.26f)
        {
            if (bottomHintText != null) bottomHintText.text = "Слишком медленно для шлифовки! Быстро дергайте мышью из стороны в сторону по рыжим зазубринам!";
            return;
        }

        int px = Mathf.FloorToInt(((localPt.x + 180f) / 360f) * TEX_SIZE);
        int py = Mathf.FloorToInt(((localPt.y + 180f) / 360f) * TEX_SIZE);
        bool modified = false;

        float radiusMult = upgPrecisionBits ? 1.4f : 1.0f;
        float dmgMult = upgStabilizer ? 0.4f : 1.0f;
        if (barsikBuffTimer > 0f) dmgMult *= 0.5f;
        int grindR = Mathf.RoundToInt(20 * radiusMult);
        float grindPower = dt * (3.8f + speedRatio * 5.0f) * radiusMult;
        bool hitBurr = false;

        for (int y = Mathf.Max(0, py - grindR); y <= Mathf.Min(TEX_SIZE - 1, py + grindR); y++)
        {
            for (int x = Mathf.Max(0, px - grindR); x <= Mathf.Min(TEX_SIZE - 1, px + grindR); x++)
            {
                if ((x - px) * (x - px) + (y - py) * (y - py) <= grindR * grindR)
                {
                    if (burrMap[y, x] > 0.08f)
                    {
                        burrMap[y, x] = Mathf.Max(0.02f, burrMap[y, x] - grindPower);
                        hitBurr = true;
                        modified = true;
                    }
                }
            }
        }

        if (hitBurr)
        {
            if (wrongToolScratches)
                overScrubDamage = Mathf.Min(100f, overScrubDamage + dt * 2.8f * dmgMult);

            sparkSpawnTimer += dt;
            if (sparkSpawnTimer > 0.03f)
            {
                sparkSpawnTimer = 0f;
                EmitSparks(localPt, 4, new Color(1f, 0.72f, 0.22f, 1f));
            }
        }

        if (modified)
        {
            RefreshItemVisuals();
            UpdateTopBar();
            CheckMidOrderTwist();
        }
    }

    public void ApplyPolishing(float speedRatio, float dt)
    {
        ApplyPolishing(Vector2.zero, speedRatio, dt);
    }

    public void ApplyPolishing(Vector2 localPt, float speedRatio, float dt)
    {
        if (cleanProgress < 65 || grindProgress < 65 || clickFixProgress < 100)
        {
            PlaySFX(clipSpringReset, 0.35f, 1.4f);
            if (bottomHintText != null) bottomHintText.text = "НАРУШЕН ПОРЯДОК: Сначала завершите Очистку, Шлифовку и Закрепку всех 4 узлов кликами!";
            return;
        }

        if (speedRatio > 0.03f && speedRatio < 0.82f)
        {
            float radiusMult = upgPrecisionBits ? 1.4f : 1.0f;
            float prevFinish = finishProgress;
            finishProgress = Mathf.Min(100f, finishProgress + dt * 48f * radiusMult);
            if (prevFinish < 80f && finishProgress >= 80f)
            {
                SpawnFloatingText(new Vector2(0, 80), "✓ Лоск наведен (80%+)", new Color(0.40f, 1f, 0.60f, 1f));
            }
            if (prevFinish < 100f && finishProgress >= 100f)
            {
                PlaySFX(clipBell, 0.85f, 1.25f);
                EmitSparks(localPt, 14, new Color(1f, 0.95f, 0.65f, 1f));
                if (bottomHintText != null) bottomHintText.text = "РЕСТАВРАЦИЯ ЗАВЕРШЕНА! Нажмите 'Завершить и отнести в Окно заказов'.";
                SpawnFloatingText(new Vector2(0, 80), "★ ИДЕАЛЬНЫЙ ЛОСК (100%)", new Color(1f, 0.95f, 0.50f, 1f));
            }
            sparkSpawnTimer += dt;
            if (sparkSpawnTimer > 0.07f)
            {
                sparkSpawnTimer = 0f;
                EmitSparks(localPt, 2, new Color(1f, 0.96f, 0.65f, 1f));
            }

            RefreshItemVisuals();
            UpdateTopBar();
            CheckMidOrderTwist();
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        Vector3 mousePos = Input.mousePosition;

        UpdateSparks(dt);
        UpdateFireplaceSparks(dt);
        UpdateFloatingTexts(dt);
        UpdateClickNodesDecay(dt);
        UpdateDustMotes(dt);
        StepCatBreathing(dt);

        if (barsikBuffTimer > 0f)
        {
            barsikBuffTimer -= dt;
            if (barsikBuffTimer <= 0f)
            {
                barsikBuffTimer = 0f;
                if (barsikBuffRoot != null) barsikBuffRoot.SetActive(false);
            }
            else
            {
                if (barsikBuffRoot != null && !barsikBuffRoot.activeSelf) barsikBuffRoot.SetActive(true);
                if (barsikBuffText != null)
                {
                    int bm = Mathf.FloorToInt(barsikBuffTimer / 60f);
                    int bs = Mathf.FloorToInt(barsikBuffTimer % 60f);
                    barsikBuffText.text = $"🐾 Барсик: {bm:D2}:{bs:D2}";
                }
            }
        }

        if (TimeController.Instance != null)
        {
            TimeController.Instance.AdvanceTime(dt);
            float shiftSec = TimeController.Instance.ShiftTimeSeconds;

            float hourAngle = (shiftSec / 720f) * 360f;
            if (hourHandRT != null) hourHandRT.localEulerAngles = new Vector3(0f, 0f, -hourAngle);

            float minuteAngle = ((shiftSec % 60f) / 60f) * 360f;
            if (minuteHandRT != null) minuteHandRT.localEulerAngles = new Vector3(0f, 0f, -minuteAngle);

            string phase = TimeController.Instance.CurrentPhase;
            int phaseIdx = TimeController.Instance.CurrentPhaseIndex;
            float prog = TimeController.Instance.PhaseProgress;

            if (clockPhaseText != null) clockPhaseText.text = phase.ToUpper();
            if (clockDigitalText != null)
            {
                int m = Mathf.FloorToInt(shiftSec / 60f);
                int s = Mathf.FloorToInt(shiftSec % 60f);
                clockDigitalText.text = $"{m:D2}:{s:D2} / 12:00";
            }
            if (clockPhaseFillRT != null)
            {
                clockPhaseFillRT.sizeDelta = new Vector2(56f * prog, 0f);
            }

            Color[] gemCols = {
                new Color(0.45f, 0.85f, 1f, 1f),
                new Color(1f, 0.88f, 0.35f, 1f),
                new Color(1f, 0.55f, 0.25f, 1f),
                new Color(0.55f, 0.55f, 0.95f, 1f)
            };
            for (int i = 0; i < 4; i++)
            {
                if (clockPhaseGemImgs[i] != null)
                {
                    if (i == phaseIdx)
                    {
                        float pulse = 0.85f + Mathf.Sin(Time.time * 3f) * 0.15f;
                        clockPhaseGemImgs[i].color = gemCols[i] * pulse;
                        clockPhaseGemImgs[i].transform.localScale = Vector3.one * 1.15f;
                    }
                    else if (i < phaseIdx)
                    {
                        clockPhaseGemImgs[i].color = gemCols[i] * 0.65f;
                        clockPhaseGemImgs[i].transform.localScale = Vector3.one;
                    }
                    else
                    {
                        clockPhaseGemImgs[i].color = new Color(0.30f, 0.28f, 0.25f, 0.55f);
                        clockPhaseGemImgs[i].transform.localScale = Vector3.one;
                    }
                }
            }

            if (winGlowImg != null && benchGlowImg != null && prog > 0.001f)
            {
                winGlowImg.color = TimeController.Instance.GetWindowColor(phase, prog);
                benchGlowImg.color = TimeController.Instance.GetAmbientColor(phase, prog);
                if (ambientTintImg != null)
                {
                    Color t0 = (phaseIdx == 0) ? new Color(0.72f, 0.84f, 1.0f, 0.06f) :
                               (phaseIdx == 1) ? new Color(1.0f, 0.98f, 0.92f, 0.03f) :
                               (phaseIdx == 2) ? new Color(1.0f, 0.55f, 0.22f, 0.14f) :
                                                 new Color(0.12f, 0.18f, 0.40f, 0.26f);
                    int nextIdx = Mathf.Min(3, phaseIdx + 1);
                    Color t1 = (nextIdx == 0) ? new Color(0.72f, 0.84f, 1.0f, 0.06f) :
                               (nextIdx == 1) ? new Color(1.0f, 0.98f, 0.92f, 0.03f) :
                               (nextIdx == 2) ? new Color(1.0f, 0.55f, 0.22f, 0.14f) :
                                                 new Color(0.12f, 0.18f, 0.40f, 0.26f);
                    ambientTintImg.color = Color.Lerp(t0, t1, prog);
                }
            }
            if (sunlightGodRaysImg != null && sunlightGodRaysImg.gameObject.activeSelf)
            {
                float pulse = Mathf.Sin(Time.time * 1.4f) * 0.5f + 0.5f;
                Color rayCol;
                if (phaseIdx == 0) rayCol = new Color(0.96f, 0.90f, 0.72f, 0.16f + 0.04f * pulse);
                else if (phaseIdx == 1) rayCol = new Color(1f, 0.98f, 0.82f, 0.24f + 0.05f * pulse);
                else if (phaseIdx == 2) rayCol = new Color(1f, 0.56f, 0.26f, 0.26f + 0.05f * pulse);
                else rayCol = new Color(0.62f, 0.78f, 1.0f, 0.15f + 0.04f * pulse);
                sunlightGodRaysImg.color = rayCol;
            }
        }
        if (clockWidgetRT != null) clockWidgetRT.localScale = Vector3.Lerp(clockWidgetRT.localScale, Vector3.one, dt * 8f);

        if (viewState == "ROOM")
        {
            float t = Time.time;
            for (int i = 0; i < 5; i++)
            {
                if (flameTongueRTs[i] != null)
                {
                    float freq = flameFrequencies[i];
                    float phase = flamePhases[i];
                    float sy = flameBaseSizes[i].y * (1f + 0.30f * Mathf.Sin(t * freq + phase) + 0.15f * Mathf.Cos(t * freq * 1.6f));
                    float sx = flameBaseSizes[i].x * (1f + 0.16f * Mathf.Cos(t * freq * 0.8f + phase));
                    float rotZ = Mathf.Sin(t * 8f + phase) * 9f;
                    flameTongueRTs[i].sizeDelta = new Vector2(sx, sy);
                    flameTongueRTs[i].localEulerAngles = new Vector3(0f, 0f, rotZ);
                }
            }

            if (fireplaceGlowImg != null)
            {
                float flicker = Mathf.Sin(t * 6.8f) * 0.04f + Mathf.Sin(t * 14.2f) * 0.02f + (Mathf.PerlinNoise(t * 4f, 0f) - 0.5f) * 0.06f;
                float baseAlpha = (completedOrdersCount >= 3) ? 0.44f : 0.30f;
                if (fireplaceStokeTimer > 0f)
                {
                    fireplaceStokeTimer -= dt;
                    baseAlpha += 0.18f * (fireplaceStokeTimer / 2.5f);
                }
                fireplaceGlowImg.color = new Color(1f, 0.62f, 0.22f, Mathf.Clamp01(baseAlpha + flicker));
            }

            if (candleFlameBenchRT != null)
            {
                float cFlicker = Mathf.Sin(t * 19.4f) * 0.08f + Mathf.Sin(t * 7.8f) * 0.12f;
                candleFlameBenchRT.localScale = new Vector3(1f + cFlicker * 0.5f, 1f + cFlicker, 1f);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (viewState == "CERTIFICATE")
            {
                PlaySFX(clipClickPin, 0.4f, 0.9f);
                SetViewState("UPGRADES");
                SelectMasterStandTab(1);
            }
            else if (viewState == "WORKBENCH" || viewState == "CABINET" || viewState == "WINDOW_SELECT" || viewState == "UPGRADES")
            {
                PlaySFX(clipClickPin, 0.4f, 0.9f);
                SetViewState("ROOM");
            }
        }

        if (viewState == "ROOM")
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(roomAreaRT, mousePos, null, out Vector2 clickPt))
                {
                    if (Mathf.Abs(clickPt.y) < 275f)
                    {
                        float clampedX = Mathf.Clamp(clickPt.x, -365f, 520f);
                        float clampedY = Mathf.Clamp(clickPt.y, -230f, 100f);
                        targetPos = new Vector2(clampedX, clampedY);
                        pendingStation = null;
                        clickMarkerRT.anchoredPosition = targetPos;
                        markerAlpha = 1f;
                    }
                }
            }

            StepPlayerMovement(dt);

            if (markerAlpha > 0f)
            {
                markerAlpha = Mathf.Max(0f, markerAlpha - dt * 2.2f);
                clickMarkerRT.GetComponent<Image>().color = new Color(1f, 0.88f, 0.35f, markerAlpha * 0.7f);
                clickMarkerRT.localScale = Vector3.one * (1.3f - markerAlpha * 0.3f);
            }
        }
        else if (viewState == "WORKBENCH")
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.05f)
            {
                StepBenchZoom(scroll * 0.25f);
            }
            benchZoom = Mathf.Lerp(benchZoom, targetBenchZoom, dt * 10f);
            itemSurfaceRT.localScale = new Vector3(benchZoom, benchZoom, 1f);
            if (benchZoomBtnText != null)
            {
                benchZoomBtnText.text = $"🔍 {Mathf.RoundToInt(benchZoom * 100f)}%";
            }

            bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(itemSurfaceRT, mousePos, null, out Vector2 localPt);

            float localDist = 0f;
            if (hasLastMouse && dt > 0f)
            {
                localDist = Vector2.Distance(localPt, lastLocalMousePos);
                normMouseSpeed = Mathf.Lerp(normMouseSpeed, localDist / dt, dt * 14f);
            }
            lastLocalMousePos = localPt;
            hasLastMouse = true;

            if (isInside && localDist > 2.8f)
            {
                currentLightPos = localPt;
                RefreshItemVisuals();
            }

            float speedRatio = Mathf.Clamp01(normMouseSpeed / 950f);
            speedFillRT.sizeDelta = new Vector2(360f * speedRatio, 0f);

            if (activeBenchAction == "GRIND")
                speedFillImg.color = (speedRatio > 0.28f) ? new Color(1f, 0.78f, 0.20f, 1f) : new Color(0.45f, 0.45f, 0.45f, 1f);
            else
                speedFillImg.color = (speedRatio > 0.72f) ? new Color(0.92f, 0.25f, 0.20f, 1f) : new Color(0.28f, 0.82f, 0.42f, 1f);

            if (orderStage == "NEED_DIAGNOSIS" && isInside)
            {
                if (toolCursorGO != null && toolCursorGO.activeSelf) toolCursorGO.SetActive(false);
                loupeRT.anchoredPosition = localPt;
                float closestDist = 999f;
                foreach (var d in activeOrder.diagnostics)
                {
                    if (!d.found)
                    {
                        float curD = Vector2.Distance(localPt, new Vector2(d.x, d.y));
                        if (curD < closestDist) closestDist = curD;
                    }
                }
                float proximity = Mathf.Clamp01(1f - closestDist / 70f);
                loupeGO.GetComponent<Image>().color = Color.Lerp(Color.white, new Color(1f, 0.90f, 0.50f, 1f), proximity);
                loupeRT.localScale = Vector3.one * (1f + proximity * 0.08f);

                if (Input.GetMouseButton(0))
                {
                    InspectWithLoupe(localPt);
                }
            }
            else if ((orderStage == "READY_TO_RESTORE" || orderStage == "READY_TO_SUBMIT") && isInside)
            {
                if (toolCursorGO != null)
                {
                    if (activeBenchAction != "CLICK_FIX")
                    {
                        toolCursorGO.SetActive(true);
                        toolCursorRT.anchoredPosition = localPt;
                        float radiusMult = upgPrecisionBits ? 1.4f : 1.0f;
                        float ringDiam = 44f * radiusMult;
                        Color ringCol = new Color(0.40f, 0.80f, 1f, 0.65f);

                        if (activeBenchAction == "CLEAN")
                        {
                            ringDiam = 42f * radiusMult;
                            bool isTooFast = speedRatio > (upgStabilizer ? 0.86f : 0.72f);
                            ringCol = isTooFast ? new Color(0.95f, 0.25f, 0.20f, 0.85f) : new Color(0.40f, 0.80f, 1f, 0.65f);
                        }
                        else if (activeBenchAction == "GRIND")
                        {
                            ringDiam = 56f * radiusMult;
                            ringCol = (speedRatio > 0.26f) ? new Color(1f, 0.78f, 0.22f, 0.85f) : new Color(0.70f, 0.50f, 0.30f, 0.45f);
                        }
                        else if (activeBenchAction == "FINISH")
                        {
                            ringDiam = 64f * radiusMult;
                            ringCol = (cleanProgress >= 65 && grindProgress >= 65 && clickFixProgress >= 100) ? new Color(1f, 0.95f, 0.55f, 0.80f) : new Color(0.6f, 0.6f, 0.6f, 0.35f);
                        }

                        toolCursorRT.sizeDelta = new Vector2(ringDiam, ringDiam);
                        toolCursorImg.color = ringCol;
                    }
                    else
                    {
                        toolCursorGO.SetActive(false);
                    }
                }

                if (activeOrder != null)
                {
                    Vector2 gemPos = (activeOrder.shapeType == "RING") ? new Vector2(0f, 47f) : Vector2.zero;
                    float dGem = Vector2.Distance(localPt, gemPos);
                    if (dGem < 45f && cleanProgress > 30)
                    {
                        gemGleamTimer -= dt;
                        if (gemGleamTimer <= 0f)
                        {
                            gemGleamTimer = Random.Range(2.0f, 3.5f);
                            Color gleamCol = (activeOrder.shapeType == "RING") ? new Color(1f, 0.55f, 0.55f, 0.95f) : ((activeOrder.shapeType == "DIAMOND") ? new Color(0.80f, 0.95f, 1f, 0.95f) : new Color(0.55f, 0.75f, 1f, 0.95f));
                            EmitSparks(gemPos, 2, gleamCol);
                            PlaySFX(clipCrystalChime, 0.16f, Random.Range(1.6f, 2.0f));
                        }
                    }
                }

                string c1 = (cleanProgress >= 75) ? "✓" : "○";
                string c2 = (grindProgress >= 75) ? "✓" : "○";
                string c3 = (clickFixProgress >= 100) ? "✓" : "○";
                string c4 = (finishProgress >= 80f) ? "✓" : "○";
                benchProgressText.text =
                    $"{c1} 1. Очистка окислов: {cleanProgress}% (цель 75%)\n" +
                    $"{c2} 2. Шлифовка (рывками): {grindProgress}% (цель 75%)\n" +
                    $"{c3} 3. Закрепка (кликами): {clickFixProgress}% (цель 100%)\n" +
                    $"{c4} 4. Финишный лоск: {Mathf.FloorToInt(finishProgress)}% (цель 80%)";
                UpdateBenchDoneButtonVisuals();

                bool holdingMouse = Input.GetMouseButton(0) && Mathf.Abs(localPt.x) < 180f && Mathf.Abs(localPt.y) < 180f && activeBenchAction != "CLICK_FIX";

                if (holdingMouse && speedRatio > 0.04f)
                {
                    AudioClip targetLoop = (activeBenchAction == "GRIND") ? clipGrindLoop : clipCleanLoop;
                    if (toolLoopSource.clip != targetLoop) toolLoopSource.clip = targetLoop;
                    if (activeBenchAction == "GRIND")
                        toolLoopSource.pitch = Mathf.Lerp(0.85f, 1.38f, speedRatio);
                    else if (activeBenchAction == "CLEAN")
                        toolLoopSource.pitch = (speedRatio > (upgStabilizer ? 0.86f : 0.72f)) ? 1.45f : Mathf.Lerp(0.88f, 1.15f, speedRatio / 0.72f);
                    else
                        toolLoopSource.pitch = 1.0f;
                    if (!toolLoopSource.isPlaying) toolLoopSource.Play();
                }
                else if (toolLoopSource.isPlaying)
                {
                    toolLoopSource.Stop();
                }

                if (holdingMouse)
                {
                    if (activeBenchAction == "CLEAN")
                    {
                        ApplyScrubbing(localPt, speedRatio, dt);
                    }
                    else if (activeBenchAction == "GRIND")
                    {
                        ApplyGrinding(localPt, speedRatio, dt);
                    }
                    else if (activeBenchAction == "FINISH")
                    {
                        ApplyPolishing(localPt, speedRatio, dt);
                    }
                }
            }
            else
            {
                if (toolCursorGO != null && toolCursorGO.activeSelf) toolCursorGO.SetActive(false);
                if (toolLoopSource != null && toolLoopSource.isPlaying) toolLoopSource.Stop();
            }
        }
    }
}
