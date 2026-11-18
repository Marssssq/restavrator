using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LuaBootstrapper : MonoBehaviour
{
    // =========================================================================
    // ЭКОНОМИКА, КВАЛИФИКАЦИЯ И СОСТОЯНИЕ ИГРЫ
    // =========================================================================
    private int trust = 100;
    private int money = 400;
    private bool secretViolated = false;
    private int completedOrdersCount = 0;
    private const int TOTAL_ORDERS_TO_WIN = 4;

    private bool upgPrecisionBits = false;
    private bool upgStabilizer = false;
    private int qualificationRank = 1;

    private string viewState = "ROOM"; // "ROOM", "WINDOW_SELECT", "WORKBENCH", "CABINET", "UPGRADES", "TWIST", "RESULT", "FINALE"
    private string orderStage = "NONE"; // "NONE" -> "NEED_DIAGNOSIS" -> "NEED_TOOLS" -> "READY_TO_RESTORE" -> "READY_TO_SUBMIT"

    // Перемещение персонажа (Click-to-Move)
    private Vector2 playerPos = new Vector2(0f, -55f);
    private Vector2 targetPos = new Vector2(0f, -55f);
    private float moveSpeed = 310f;
    private string pendingStation = null;
    private float walkAnimTimer = 0f;
    private float stepSoundTimer = 0f;

    // Выбранные в шкафу инструменты и материалы
    private string selectedMechTool = null;
    private string selectedSolvent = null;
    private string selectedStyle = null;
    private string activeBenchAction = "CLEAN"; // "CLEAN", "GRIND", "CLICK_FIX", "FINISH"

    // Прогресс реставрации
    private int zonesDiscovered = 0;
    private int cleanProgress = 0;
    private int grindProgress = 0;
    private int clickFixProgress = 0;
    private float finishProgress = 0f;

    // Повреждения и нормализованный датчик скорости мыши
    private bool chemicalStain = false;
    private bool wrongToolScratches = false;
    private float overScrubDamage = 0f;
    private float normMouseSpeed = 0f;
    private Vector2 lastLocalMousePos;
    private Vector2 currentLightPos = new Vector2(-60f, 80f);
    private bool hasLastMouse = false;
    private bool twistChecked = false;

    private List<string> historyLog = new List<string>();
    private List<Texture2D> createdTextures = new List<Texture2D>();
    private List<AudioClip> createdClips = new List<AudioClip>();

    // =========================================================================
    // АУДИОСИСТЕМА (ФАЙЛЫ ИЗ RESOURCES/AUDIO ИЛИ ПРОЦЕДУРНЫЙ СИНТЕЗ)
    // =========================================================================
    private AudioSource sfxSource;
    private AudioSource toolLoopSource;
    private AudioSource ambientSource;
    private AudioClip clipBell, clipStep, clipClickPin, clipSpringReset, clipGrindLoop, clipCleanLoop, clipCoin, clipFail, clipClockTick;

    // =========================================================================
    // СТРУКТУРЫ ДАННЫХ И КЛИК-УЗЛОВ С ТАЙМЕРОМ СБРОСА
    // =========================================================================
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

    private const float CLICK_RESET_TIME = 0.65f; // Если медлить > 0.65 сек, узел собирается обратно до 5
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

    // Координаты подхода к станциям
    private readonly Vector2 posWindowInteract   = new Vector2(-355f, -15f);
    private readonly Vector2 posBenchInteract    = new Vector2(0f, 84f);
    private readonly Vector2 posCabinetInteract  = new Vector2(365f, 84f);
    private readonly Vector2 posUpgradesInteract = new Vector2(365f, -135f);

    // Ссылки на UI комнаты
    private RectTransform roomAreaRT, playerRT, clickMarkerRT;
    private Text playerBubbleText, topTitleText, topStatsText, bottomHintText;
    private Image windowBadgeImg, benchBadgeImg, cabinetBadgeImg, benchItemPreviewImg;
    private float markerAlpha = 0f;

    // Оверлеи
    private GameObject windowOverlay, workbenchOverlay, cabinetOverlay, upgradesOverlay, modalOverlay;
    private Text[] cardTitles = new Text[3];
    private Text[] cardClients = new Text[3];
    private Text[] cardDescs = new Text[3];
    private Text[] cardBtnLabels = new Text[3];
    private Image[] cardBtnImgs = new Image[3];

    private List<Image> toolBtnImgs = new List<Image>();
    private List<Image> solventBtnImgs = new List<Image>();
    private List<Image> styleBtnImgs = new List<Image>();
    private Text cabinetDiagReminderText;

    private Text upgStatusText;
    private Text[] upgBtnLabels = new Text[4];
    private Image[] upgBtnImgs = new Image[4];

    // =========================================================================
    // 2D PBR-ШЕЙДЕРНЫЕ БУФЕРЫ ПРЕДМЕТА (ALBEDO + NORMAL MAP + METALLIC/ROUGHNESS)
    // =========================================================================
    private const int TEX_SIZE = 320;
    private Texture2D itemTex;
    private Color32[] pixelBuffer = new Color32[TEX_SIZE * TEX_SIZE];
    private Color[,] albedoMap = new Color[TEX_SIZE, TEX_SIZE];
    private Vector3[,] normalMap = new Vector3[TEX_SIZE, TEX_SIZE];
    private float[,] metallicMap = new float[TEX_SIZE, TEX_SIZE];
    private byte[,] isItemMask = new byte[TEX_SIZE, TEX_SIZE];
    private byte[,] dirtMap = new byte[TEX_SIZE, TEX_SIZE];    // 0-нет, 1-окислы, 2-очищено, 3-стертый слой
    private float[,] burrMap = new float[TEX_SIZE, TEX_SIZE];  // 0..1 зазубрины для быстрой шлифовки

    private RectTransform itemSurfaceRT, loupeRT, speedFillRT;
    private Image itemSurfaceImg, speedFillImg;
    private GameObject loupeGO, benchToolsPanel, clickNodesContainer;
    private Text benchLogText, benchProgressText, benchModeNoticeText, speedBarLabelText;
    private Button btnGoToCabinetFromBench;
    private Image[] benchToolBtnImgs = new Image[4];

    // Система частиц искр
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

    private Text modalTitle, modalBody, modalBtn1Text, modalBtn2Text, modalSealText;
    private Button modalBtn1, modalBtn2;
    private GameObject modalSealGO;

    // PBR-спрайты интерфейса и мебели
    private Sprite spBeveledPanel, spBeveledButton, spWaxSealBadge, spSoftLightGlow, spLoupeLens, spTargetClamp, spTimerRing;
    private Sprite spStationWindow, spStationWorkbench, spStationCabinet, spStationMasterStand, spArtisanChar;

    // =========================================================================
    // СТАРТ И ИНИЦИАЛИЗАЦИЯ
    // =========================================================================
    void Start()
    {
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

    // =========================================================================
    // АУДИОСИСТЕМА (ФАЙЛЫ ИЗ RESOURCES/AUDIO ИЛИ ПРОЦЕДУРНЫЙ СИНТЕЗ)
    // =========================================================================
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

        clipBell        = LoadOrSynthesize("Audio/bell",   SynthesizeBellClip);
        clipStep        = LoadOrSynthesize("Audio/step",   SynthesizeStepClip);
        clipClickPin    = LoadOrSynthesize("Audio/click",  SynthesizeClickClip);
        clipSpringReset = LoadOrSynthesize("Audio/reset",  SynthesizeSpringResetClip);
        clipGrindLoop   = LoadOrSynthesize("Audio/grind",  SynthesizeGrindLoopClip);
        clipCleanLoop   = LoadOrSynthesize("Audio/clean",  SynthesizeCleanLoopClip);
        clipCoin        = LoadOrSynthesize("Audio/coin",   SynthesizeCoinClip);
        clipFail        = LoadOrSynthesize("Audio/fail",   SynthesizeFailClip);
        clipClockTick   = LoadOrSynthesize("Audio/clock",  SynthesizeClockTickClip);

        if (clipClockTick != null)
        {
            ambientSource.clip = clipClockTick;
            ambientSource.Play();
        }
    }

    private AudioClip LoadOrSynthesize(string path, System.Func<AudioClip> synthFunc)
    {
        AudioClip loaded = Resources.Load<AudioClip>(path);
        if (loaded != null) return loaded;
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
        int len = sr;
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float s = 0f;
            if (t < 0.12f)
            {
                float env = Mathf.Exp(-t * 60f);
                float strike = Mathf.Sin(2f * Mathf.PI * 1150f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 270f * t) * 0.4f;
                s += strike * env * 0.35f;
            }
            if (t >= 0.5f && t < 0.62f)
            {
                float t2 = t - 0.5f;
                float env2 = Mathf.Exp(-t2 * 65f);
                float strike2 = Mathf.Sin(2f * Mathf.PI * 920f * t2) * 0.55f + Mathf.Sin(2f * Mathf.PI * 230f * t2) * 0.45f;
                s += strike2 * env2 * 0.28f;
            }
            data[i] = s;
        }
        AudioClip c = AudioClip.Create("ProcClockTick", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    // =========================================================================
    // ПРОЦЕДУРНЫЙ 2D PBR-ШЕЙДЕРНЫЙ РЕНДЕРЕР СПРАЙТОВ (NORMAL MAP + BLINN-PHONG)
    // =========================================================================
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
    }

    // PBR-Рама с объемной золотой фаской (Normal-Mapped Bevel)
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

    // Паркетный пол с PBR-отражениями от 4 теплых ламп мастерской
    private Sprite CreateWoodFloorPBRSprite(int w, int h)
    {
        Texture2D tex = CreateTrackedTexture(w, h);
        Color32[] cols = new Color32[w * h];

        Vector2[] lampPositions = {
            new Vector2(w * 0.14f, h * 0.48f), // У окна заказов
            new Vector2(w * 0.50f, h * 0.78f), // Над рабочим столом
            new Vector2(w * 0.80f, h * 0.78f), // У шкафа инструментов
            new Vector2(w * 0.84f, h * 0.28f)  // У стенда мастера
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

                // Нормаль паркетной доски (фаска на стыках)
                Vector3 normal = new Vector3(grain * 1.5f, 0f, 1f);
                if (seamY < 2.0f) { albedo *= 0.52f; normal.y = (y % 26 < 13) ? -0.6f : 0.6f; }
                if (seamX < 2.0f) { albedo *= 0.52f; normal.x = ((x + rowOffset) % 136f < 68f) ? -0.6f : 0.6f; }
                normal.Normalize();

                // Суммирование освещения и бликов лака от ламп мастерской
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

                // Мягкое затенение углов комнаты (Ambient Occlusion)
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

    // Узорчатый рельефный ковер с микро-ворсом
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

    // Станция 1 (Окно заказов): PBR-резное дерево, свинцовый витраж, бархат и латунный колокольчик
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

                    // Золотой багет вдоль арки
                    if (Mathf.Abs(archDist - (archWidth - 11f)) < 2.6f)
                    {
                        albedo = new Color(0.90f, 0.72f, 0.28f, 1f);
                        met = 0.88f; smooth = 0.85f;
                    }
                    // Стеклянный витраж с теплым контровым светом
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

                        // Объемные складки бордовых бархатных портьер
                        float curtainCurve = 38f + Mathf.Sin(y * 0.03f) * 18f;
                        if (Mathf.Abs(dx) > curtainCurve)
                        {
                            float foldWave = Mathf.Sin(dx * 0.45f + y * 0.05f);
                            norm = new Vector3(Mathf.Cos(dx * 0.45f) * 0.65f, 0.1f, 0.75f);
                            albedo = new Color(0.56f + foldWave * 0.08f, 0.11f, 0.16f, 1f);
                            met = 0.05f; smooth = 0.35f;
                        }
                    }

                    // Бархатная подушечка для приема украшений
                    float pillowDist = (dx * dx) / (46f * 46f) + ((y - 48f) * (y - 48f)) / (18f * 18f);
                    if (pillowDist < 1f)
                    {
                        norm = new Vector3(dx / 46f, (y - 48f) / 18f, Mathf.Sqrt(1f - pillowDist));
                        albedo = new Color(0.68f, 0.14f, 0.22f, 1f);
                        met = 0.1f; smooth = 0.45f;
                    }

                    // Латунный колокольчик с зеркальным бликом
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

    // Станция 2 (Рабочий стол): Верстак с кожаным финагелем, латунной лампой и стальными штихелями
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

                    // Стальные ювелирные штихели с деревянными ручками
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

                    // Изумрудный абажур лампы в латунной оправе
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

    // Станция 3 (Шкаф инструментов): Застекленный сервант с объемными колбами и стальными щипцами
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

    // Станция 4 (Стенд Мастера): Резная кафедра с золоченым багетом и сургучными печатями
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

    // Персонаж (Мастер-реставратор с нормал-маппингом кожаного фартука, ремня и бинокуляров)
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

    // =========================================================================
    // ПУЛ ЗАКАЗОВ
    // =========================================================================
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

        currentWindowChoices.Add(ringOrder);
        currentWindowChoices.Add(diamondOrder);
        currentWindowChoices.Add(qualificationRank >= 3 ? byzantineOrder : royalOrder);
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

    // =========================================================================
    // СОЗДАНИЕ ИНТЕРФЕЙСА И КОМНАТЫ МАСТЕРСКОЙ
    // =========================================================================
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
        labelTxt = CreateText(go.transform, "Label", label, 14, new Color(0.98f, 0.95f, 0.88f, 1f), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-14, -6), TextAnchor.MiddleCenter, true);
        btn.onClick.AddListener(() => {
            PlaySFX(clipClickPin, 0.4f, 1.0f);
            onClick();
        });
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
    }

    private void BuildAllUI()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
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

        GameObject rootBG = CreatePanel(canvasGO.transform, "RootBG", new Color(0.08f, 0.06f, 0.05f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Пол с PBR-освещением от ламп и узорчатый ковер
        Sprite woodSprite = CreateWoodFloorPBRSprite(512, 256);
        GameObject roomGO = CreatePanel(rootBG.transform, "RoomArea", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(1240, 580), woodSprite, false, true);
        roomAreaRT = roomGO.GetComponent<RectTransform>();

        Sprite rugSprite = CreateOrnateRugPBRSprite(320, 160);
        GameObject rug = CreatePanel(roomAreaRT, "WorkshopRug", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10, -45), new Vector2(540, 260), rugSprite, false, true);
        rug.GetComponent<Image>().raycastTarget = false;

        // Верхняя и левая стены с резным карнизом
        GameObject topWall = CreatePanel(roomAreaRT, "TopWall", new Color(0.15f, 0.12f, 0.10f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), new Vector2(0, 128), null, false, true);
        topWall.GetComponent<Image>().raycastTarget = false;
        GameObject trimTop = CreatePanel(topWall.transform, "WoodTrim", new Color(0.52f, 0.34f, 0.18f, 1f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 8));
        trimTop.GetComponent<Image>().raycastTarget = false;

        GameObject leftWall = CreatePanel(roomAreaRT, "LeftWall", new Color(0.15f, 0.12f, 0.10f, 1f), new Vector2(0, 0), new Vector2(0, 1), new Vector2(72, 0), new Vector2(144, 0), null, false, true);
        leftWall.GetComponent<Image>().raycastTarget = false;

        // --- 1. ОКНО ЗАКАЗОВ (СЛЕВА) ---
        GameObject winGlow = CreatePanel(roomAreaRT, "WinLampGlow", new Color(1f, 0.86f, 0.55f, 0.30f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-440, -15), new Vector2(380, 380), spSoftLightGlow);
        winGlow.GetComponent<Image>().raycastTarget = false;

        GameObject winStation = CreatePanel(roomAreaRT, "Station_Window", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-485, -15), new Vector2(220, 265), spStationWindow, false, true);
        AddStationClickTrigger(winStation, posWindowInteract, "WINDOW");
        CreateText(winStation.transform, "WinTitle", "ОКНО ЗАКАЗОВ", 17, new Color(1f, 0.95f, 0.78f, 1f), Vector2.zero, Vector2.one, new Vector2(0, -82), new Vector2(-12, -12), TextAnchor.MiddleCenter, true);
        GameObject winBadge = CreatePanel(winStation.transform, "Badge", new Color(0.25f, 0.92f, 0.38f, 1f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -22), new Vector2(32, 32), spWaxSealBadge, false, true);
        windowBadgeImg = winBadge.GetComponent<Image>();
        windowBadgeImg.raycastTarget = false;
        CreateText(winBadge.transform, "Excl", "!", 19, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

        // --- 2. РАБОЧИЙ СТОЛ (НА ВЕРХНЕЙ СТЕНЕ ПО ЦЕНТРУ) ---
        GameObject benchGlow = CreatePanel(roomAreaRT, "BenchLampGlow", new Color(1f, 0.90f, 0.60f, 0.36f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 175), new Vector2(490, 360), spSoftLightGlow);
        benchGlow.GetComponent<Image>().raycastTarget = false;

        GameObject benchStation = CreatePanel(roomAreaRT, "Station_Workbench", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 195), new Vector2(350, 158), spStationWorkbench, false, true);
        AddStationClickTrigger(benchStation, posBenchInteract, "WORKBENCH");
        GameObject itemOnBench = CreatePanel(benchStation.transform, "ItemOnBench", new Color(1f, 1f, 1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-50, 4), new Vector2(54, 54));
        benchItemPreviewImg = itemOnBench.GetComponent<Image>();
        benchItemPreviewImg.raycastTarget = false;

        CreateText(benchStation.transform, "BenchTitle", "РАБОЧИЙ СТОЛ", 18, new Color(1f, 0.96f, 0.82f, 1f), Vector2.zero, Vector2.one, new Vector2(0, -45), new Vector2(-16, -16), TextAnchor.MiddleCenter, true);
        GameObject benchBadge = CreatePanel(benchStation.transform, "Badge", new Color(0.25f, 0.92f, 0.38f, 1f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-22, -20), new Vector2(32, 32), spWaxSealBadge, false, true);
        benchBadgeImg = benchBadge.GetComponent<Image>();
        benchBadgeImg.raycastTarget = false;
        CreateText(benchBadge.transform, "Excl", "!", 19, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

        // --- 3. ШКАФ ИНСТРУМЕНТОВ (СПРАВА СВЕРХУ) ---
        GameObject cabGlow = CreatePanel(roomAreaRT, "CabLampGlow", new Color(0.75f, 0.92f, 1f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(375, 175), new Vector2(370, 310), spSoftLightGlow);
        cabGlow.GetComponent<Image>().raycastTarget = false;

        GameObject cabStation = CreatePanel(roomAreaRT, "Station_Cabinet", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(375, 195), new Vector2(300, 158), spStationCabinet, false, true);
        AddStationClickTrigger(cabStation, posCabinetInteract, "CABINET");
        CreateText(cabStation.transform, "CabTitle", "ШКАФ ИНСТРУМЕНТОВ", 17, new Color(0.98f, 0.92f, 0.78f, 1f), Vector2.zero, Vector2.one, new Vector2(0, -45), new Vector2(-16, -16), TextAnchor.MiddleCenter, true);
        GameObject cabBadge = CreatePanel(cabStation.transform, "Badge", new Color(0.25f, 0.92f, 0.38f, 1f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-22, -20), new Vector2(32, 32), spWaxSealBadge, false, true);
        cabinetBadgeImg = cabBadge.GetComponent<Image>();
        cabinetBadgeImg.raycastTarget = false;
        CreateText(cabBadge.transform, "Excl", "!", 19, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

        // --- 4. СТЕНД МАСТЕРА (СПРАВА ВНИЗУ) ---
        GameObject upgGlow = CreatePanel(roomAreaRT, "UpgGlow", new Color(1f, 0.82f, 0.40f, 0.24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(465, -135), new Vector2(320, 290), spSoftLightGlow);
        upgGlow.GetComponent<Image>().raycastTarget = false;

        GameObject upgStation = CreatePanel(roomAreaRT, "Station_Upgrades", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(475, -135), new Vector2(240, 170), spStationMasterStand, false, true);
        AddStationClickTrigger(upgStation, posUpgradesInteract, "UPGRADES");
        CreateText(upgStation.transform, "UpgTitle", "СТЕНД МАСТЕРА", 17, new Color(1f, 0.92f, 0.62f, 1f), Vector2.zero, Vector2.one, new Vector2(0, -52), new Vector2(-14, -14), TextAnchor.MiddleCenter, true);

        // Маркер клика ЛКМ на полу
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

        // --- ПЕРСОНАЖ (МАСТЕР-РЕСТАВРАТОР) ---
        GameObject playerGO = CreatePanel(roomAreaRT, "PlayerCharacter", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), playerPos, new Vector2(98, 126), spArtisanChar);
        playerGO.GetComponent<Image>().raycastTarget = false;
        playerRT = playerGO.GetComponent<RectTransform>();

        GameObject bubbleGO = CreatePanel(playerRT, "StatusBubble", new Color(0.14f, 0.11f, 0.09f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 78), new Vector2(256, 34), spBeveledButton, true, true);
        bubbleGO.GetComponent<Image>().raycastTarget = false;
        playerBubbleText = CreateText(bubbleGO.transform, "BubbleTxt", "Иду к окну заказов", 12, new Color(0.98f, 0.92f, 0.76f, 1f), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-10, -4), TextAnchor.MiddleCenter, true);

        // Верхняя и нижняя панели
        GameObject topBar = CreatePanel(rootBG.transform, "TopBar", new Color(0.16f, 0.12f, 0.09f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -28), new Vector2(0, 56), null, false, true);
        topTitleText = CreateText(topBar.transform, "Title", "Мастерская «Реставратор»", 17, new Color(0.98f, 0.88f, 0.64f, 1f), new Vector2(0, 0), new Vector2(0.52f, 1), new Vector2(22, 0), new Vector2(-20, 0), TextAnchor.MiddleLeft, true);
        topStatsText = CreateText(topBar.transform, "Stats", "", 15, new Color(0.86f, 0.95f, 0.82f, 1f), new Vector2(0.52f, 0), new Vector2(1, 1), new Vector2(-22, 0), new Vector2(-20, 0), TextAnchor.MiddleRight, true);

        GameObject bottomBar = CreatePanel(rootBG.transform, "BottomBar", new Color(0.16f, 0.12f, 0.09f, 1f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 24), new Vector2(0, 48));
        bottomHintText = CreateText(bottomBar.transform, "BottomHint", "", 15, new Color(0.98f, 0.92f, 0.74f, 1f), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30, 0), TextAnchor.MiddleCenter, true);

        BuildWindowSelectionOverlay(rootBG.transform);
        BuildCabinetOverlay(rootBG.transform);
        BuildWorkbenchOverlay(rootBG.transform);
        BuildUpgradesOverlay(rootBG.transform);
        BuildModalOverlay(rootBG.transform);
    }

    // =========================================================================
    // ОВЕРЛЕИ СТАНЦИЙ
    // =========================================================================
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

            CreateButton(card.transform, "TakeBtn", "Принять заказ", new Vector2(0, -140), new Vector2(280, 44), new Color(0.26f, 0.52f, 0.32f, 1f), () => {
                SelectOrderFromWindow(idx);
            }, out cardBtnLabels[i], out cardBtnImgs[i]);
        }

        CreateButton(windowOverlay.transform, "CloseWinBtn", "← Отойти от окна", new Vector2(0, -228), new Vector2(340, 42), new Color(0.42f, 0.26f, 0.20f, 1f), () => {
            SetViewState("ROOM");
        }, out _, out _);

        windowOverlay.SetActive(false);
    }

    private void BuildCabinetOverlay(Transform parent)
    {
        cabinetOverlay = CreatePanel(parent, "CabinetOverlay", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(1120, 546), spBeveledPanel, true, true);
        CreateText(cabinetOverlay.transform, "Header", "ШКАФ ИНСТРУМЕНТОВ И РЕАКТИВОВ", 20, new Color(0.98f, 0.88f, 0.60f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -32), new Vector2(-40, 36), TextAnchor.MiddleCenter, true);

        GameObject memoBox = CreatePanel(cabinetOverlay.transform, "DiagMemo", new Color(0.18f, 0.14f, 0.11f, 1f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, 14), new Vector2(366, 376), spBeveledPanel, true, true);
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
            CreateButton(cabinetOverlay.transform, "Tool_" + id, tNames[i], new Vector2(185, 148 - i * 40), new Vector2(620, 36), new Color(0.28f, 0.23f, 0.19f, 1f), () => {
                selectedMechTool = id;
                HighlightList(toolBtnImgs, idx, new Color(0.25f, 0.52f, 0.34f, 1f));
            }, out _, out Image bImg);
            toolBtnImgs.Add(bImg);
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
            CreateButton(cabinetOverlay.transform, "Sol_" + id, sNames[i], new Vector2(185, -18 - i * 40), new Vector2(620, 36), new Color(0.28f, 0.23f, 0.19f, 1f), () => {
                selectedSolvent = id;
                HighlightList(solventBtnImgs, idx, new Color(0.25f, 0.52f, 0.34f, 1f));
            }, out _, out Image bImg);
            solventBtnImgs.Add(bImg);
        }

        string[] stIds = { "PATINA", "NEW" };
        string[] stNames = { "Сохранить следы возраста", "Восстановить «Как новое»" };
        CreateText(cabinetOverlay.transform, "Lbl3", "Выберите финишный подход:", 15, new Color(0.98f, 0.85f, 0.58f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(185, -142), new Vector2(620, 24), TextAnchor.MiddleLeft, true);
        for (int i = 0; i < 2; i++)
        {
            string id = stIds[i];
            int idx = i;
            CreateButton(cabinetOverlay.transform, "Style_" + id, stNames[i], new Vector2(32 + i * 315, -178), new Vector2(302, 36), new Color(0.28f, 0.23f, 0.19f, 1f), () => {
                selectedStyle = id;
                HighlightList(styleBtnImgs, idx, new Color(0.52f, 0.40f, 0.18f, 1f));
            }, out _, out Image bImg);
            styleBtnImgs.Add(bImg);
        }

        CreateButton(cabinetOverlay.transform, "ConfirmCabBtn", "Взять набор и перейти к Рабочему столу ->", new Vector2(210, -232), new Vector2(440, 44), new Color(0.58f, 0.38f, 0.16f, 1f), () => {
            ConfirmCabinetSelection();
        }, out _, out _);

        CreateButton(cabinetOverlay.transform, "CloseCabBtn", "← Отойти в комнату", new Vector2(-280, -232), new Vector2(260, 44), new Color(0.40f, 0.25f, 0.20f, 1f), () => {
            SetViewState("ROOM");
        }, out _, out _);

        cabinetOverlay.SetActive(false);
    }

    private void BuildUpgradesOverlay(Transform parent)
    {
        upgradesOverlay = CreatePanel(parent, "UpgradesOverlay", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(1060, 520), spBeveledPanel, true, true);
        CreateText(upgradesOverlay.transform, "Header", "СТЕНД МАСТЕРА — ОБОРУДОВАНИЕ И КВАЛИФИКАЦИЯ", 20, new Color(0.98f, 0.88f, 0.60f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -34), new Vector2(-40, 40), TextAnchor.MiddleCenter, true);

        upgStatusText = CreateText(upgradesOverlay.transform, "StatusTxt", "", 15, new Color(0.84f, 0.96f, 0.84f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), new Vector2(-60, 32), TextAnchor.MiddleCenter, true);

        string[] titles = {
            "1. Алмазные микро-насадки — 800 руб.\nУвеличивают радиус очистки и скорость стачивания зазубрин на +40%.",
            "2. Оптический стабилизатор нажима — 1200 руб.\nСнижает риск повредить базовый слой при срыве руки на 60%.",
            "3. Сертификат Ювелира II разряда — 1600 руб.\nОткрывает доступ к сложным музейным заказам с гонораром 3800 руб.",
            "4. Патент Гранд-мастера III разряда — 3000 руб.\nПовышает выплаты за все заказы на +35% и укрепляет доверие клиентов."
        };

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            float yPos = 105f - i * 74f;
            GameObject row = CreatePanel(upgradesOverlay.transform, "UpgRow_" + i, new Color(0.24f, 0.18f, 0.14f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, yPos), new Vector2(960, 64), spBeveledButton, true, true);
            CreateText(row.transform, "Desc", titles[i], 14, new Color(0.95f, 0.92f, 0.86f, 1f), new Vector2(0, 0), new Vector2(0.70f, 1), new Vector2(20, 0), new Vector2(-20, -8), TextAnchor.MiddleLeft, true);

            CreateButton(row.transform, "BuyBtn_" + i, "Купить", new Vector2(330, 0), new Vector2(240, 44), new Color(0.28f, 0.52f, 0.32f, 1f), () => {
                BuyUpgrade(idx);
            }, out upgBtnLabels[i], out upgBtnImgs[i]);
        }

        CreateButton(upgradesOverlay.transform, "CloseUpgBtn", "← Вернуться в мастерскую", new Vector2(0, -212), new Vector2(340, 44), new Color(0.42f, 0.26f, 0.20f, 1f), () => {
            SetViewState("ROOM");
        }, out _, out _);

        upgradesOverlay.SetActive(false);
    }

    private void BuildWorkbenchOverlay(Transform parent)
    {
        workbenchOverlay = CreatePanel(parent, "WorkbenchOverlay", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(1170, 554), spBeveledPanel, true, true);

        GameObject leftLog = CreatePanel(workbenchOverlay.transform, "BenchLog", new Color(0.18f, 0.14f, 0.11f, 1f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(198, 12), new Vector2(344, 448), spBeveledPanel, true, true);
        benchLogText = CreateText(leftLog.transform, "LogTxt", "", 14, new Color(0.94f, 0.90f, 0.82f, 1f), new Vector2(0, 0.30f), new Vector2(1, 1), new Vector2(0, -12), new Vector2(-26, -20), TextAnchor.UpperLeft);
        benchProgressText = CreateText(leftLog.transform, "ProgTxt", "", 14, new Color(0.76f, 0.96f, 0.80f, 1f), new Vector2(0, 0), new Vector2(1, 0.30f), new Vector2(0, 14), new Vector2(-26, -12), TextAnchor.LowerLeft, true);

        GameObject centerMat = CreatePanel(workbenchOverlay.transform, "CenterMat", new Color(0.15f, 0.34f, 0.22f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(394, 394), spBeveledPanel, true, true);
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

        benchToolsPanel = CreatePanel(rightCol.transform, "BenchToolsSub", Color.clear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -32), new Vector2(320, 310));

        CreateButton(benchToolsPanel.transform, "ActClean", "1. Растворитель: Плавная очистка", new Vector2(0, 108), new Vector2(308, 42), new Color(0.24f, 0.40f, 0.50f, 1f), () => SelectBenchTool("CLEAN"), out _, out benchToolBtnImgs[0]);
        CreateButton(benchToolsPanel.transform, "ActGrind", "2. Шлифовка: Быстро дергать мышью!", new Vector2(0, 58), new Vector2(308, 42), new Color(0.50f, 0.34f, 0.20f, 1f), () => SelectBenchTool("GRIND"), out _, out benchToolBtnImgs[1]);
        CreateButton(benchToolsPanel.transform, "ActClick", "3. Закрепка: Быстро кликать (не медлить!)", new Vector2(0, 8), new Vector2(308, 42), new Color(0.46f, 0.28f, 0.44f, 1f), () => SelectBenchTool("CLICK_FIX"), out _, out benchToolBtnImgs[2]);
        CreateButton(benchToolsPanel.transform, "ActFinish", "4. Полировка: Круговое наведение лоска", new Vector2(0, -42), new Vector2(308, 42), new Color(0.26f, 0.46f, 0.30f, 1f), () => SelectBenchTool("FINISH"), out _, out benchToolBtnImgs[3]);

        CreateButton(benchToolsPanel.transform, "ActDoneWalkWindow", "Завершить и отнести в Окно заказов ->", new Vector2(0, -108), new Vector2(308, 48), new Color(0.62f, 0.40f, 0.16f, 1f), () => {
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
        }, out _, out _);

        CreateButton(workbenchOverlay.transform, "BackRoomBtn", "← Отойти от стола в мастерскую", new Vector2(0, -240), new Vector2(350, 40), new Color(0.42f, 0.26f, 0.20f, 1f), () => {
            SetViewState("ROOM");
        }, out _, out _);

        workbenchOverlay.SetActive(false);
    }

    private void SelectBenchTool(string toolId)
    {
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
            benchToolBtnImgs[i].color = (i == activeIdx) ? new Color(0.72f, 0.56f, 0.22f, 1f) : defCols[i];

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
    }

    private void BuildModalOverlay(Transform parent)
    {
        modalOverlay = CreatePanel(parent, "ModalOverlay", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 450), spBeveledPanel, true, true);
        modalTitle = CreateText(modalOverlay.transform, "Title", "", 22, new Color(0.98f, 0.86f, 0.56f, 1f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -36), new Vector2(-40, 40), TextAnchor.MiddleCenter, true);
        modalBody = CreateText(modalOverlay.transform, "Body", "", 16, new Color(0.94f, 0.92f, 0.86f, 1f), new Vector2(0, 0.26f), new Vector2(1, 0.82f), Vector2.zero, new Vector2(-64, 0), TextAnchor.MiddleLeft);

        modalBtn1 = CreateButton(modalOverlay.transform, "Btn1", "Продолжить", new Vector2(0, -124), new Vector2(550, 44), new Color(0.28f, 0.50f, 0.32f, 1f), () => OnModalButtonClick(1), out modalBtn1Text, out _);
        modalBtn2 = CreateButton(modalOverlay.transform, "Btn2", "Вариант 2", new Vector2(0, -176), new Vector2(550, 44), new Color(0.48f, 0.28f, 0.22f, 1f), () => OnModalButtonClick(2), out modalBtn2Text, out _);

        modalSealGO = CreatePanel(modalOverlay.transform, "ModalSeal", Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-65f, -60f), new Vector2(74f, 74f), spWaxSealBadge, false, true);
        modalSealGO.GetComponent<Image>().raycastTarget = false;
        modalSealText = CreateText(modalSealGO.transform, "SealTxt", "", 12, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);

        modalOverlay.SetActive(false);
    }

    private void HighlightList(List<Image> list, int activeIdx, Color activeCol)
    {
        for (int i = 0; i < list.Count; i++)
            list[i].color = (i == activeIdx) ? activeCol : new Color(0.28f, 0.23f, 0.19f, 1f);
    }

    // =========================================================================
    // СИСТЕМА ЧАСТИЦ И УЗЛОВ С ОБРАТНЫМ СБРОСОМ ДО 5 ПРИ МЕДЛЕННЫХ КЛИКАХ
    // =========================================================================
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
            if (node.uiRT != null) Destroy(node.uiRT.gameObject);
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

            if (cn.clicksLeft <= 0)
            {
                cn.uiRT.gameObject.SetActive(false);
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
        clickFixProgress = (total > 0) ? Mathf.FloorToInt((done / (float)total) * 100f) : 100;
    }

    // =========================================================================
    // ГЕНЕРАЦИЯ КАРТ НОРМАЛЕЙ И ПОВЕРХНОСТИ УКРАШЕНИЯ (2D PBR SHADER)
    // =========================================================================
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
                        }
                    }
                    else if (dist < 69f && dist > 41f)
                    {
                        isItem = true;
                        float midR = 55f;
                        float tubePos = (dist - midR) / 14f;
                        norm = new Vector3((dx / dist) * tubePos, (dy / dist) * tubePos, Mathf.Sqrt(Mathf.Max(0.15f, 1f - tubePos * tubePos)));
                        albedo = new Color(0.95f, 0.78f, 0.30f, 1f);
                        metallic = 0.94f;

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
                            // Кристаллическая карта нормалей граней бриллианта
                            int sector = Mathf.FloorToInt((ang + Mathf.PI) / (Mathf.PI * 0.25f));
                            float facetAngle = sector * (Mathf.PI * 0.25f);
                            norm = (dist < 30f)
                                ? new Vector3(0f, 0f, 1f)
                                : new Vector3(Mathf.Cos(facetAngle) * 0.65f, Mathf.Sin(facetAngle) * 0.65f, 0.76f);

                            albedo = new Color(0.76f, 0.90f, 1.0f, 1f);
                            metallic = 0.78f;

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
                            }
                        }
                    }
                }

                albedoMap[y, x] = albedo;
                normalMap[y, x] = norm.normalized;
                metallicMap[y, x] = metallic;
                isItemMask[y, x] = isItem ? (byte)1 : (byte)0;
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

    // Попиксельный PBR-шейдер предмета с динамическим источником света от курсора мыши
    private void RefreshItemVisuals()
    {
        if (itemTex == null) return;
        int totalDirt = 0, cleanedDirt = 0, totalBurr = 0, fixedBurr = 0;
        float scale = TEX_SIZE / 200f;

        // Позиция лампы в координатах текстуры 320x320
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

                // Эффект финишной полировки на параметры PBR-шейдера
                if (finishProgress > 10f)
                {
                    float fRatio = finishProgress / 100f;
                    if (selectedStyle == "NEW")
                    {
                        smoothness = Mathf.Lerp(0.72f, 0.96f, fRatio);
                        albedo *= (1f + 0.12f * fRatio);
                    }
                    else if (selectedStyle == "PATINA")
                    {
                        smoothness = Mathf.Lerp(0.72f, 0.52f, fRatio);
                        albedo = Color.Lerp(albedo, new Color(albedo.r * 0.88f, albedo.g * 0.84f, albedo.b * 0.74f, 1f), fRatio);
                    }
                }

                // Окислы (блокируют металлический блеск и увеличивают шероховатость)
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

                // Зазубрины (меняют нормаль и цвет на шероховатую бронзу)
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

        cleanProgress = (totalDirt > 0) ? Mathf.FloorToInt((cleanedDirt / (float)totalDirt) * 100f) : 100;
        grindProgress = (totalBurr > 0) ? Mathf.FloorToInt((fixedBurr / (float)totalBurr) * 100f) : 100;
    }

    // =========================================================================
    // УПРАВЛЕНИЕ СОСТОЯНИЯМИ, ЭКОНОМИКОЙ И ОЦЕНКОЙ
    // =========================================================================
    private void SetViewState(string newState)
    {
        viewState = newState;
        if (toolLoopSource != null && toolLoopSource.isPlaying) toolLoopSource.Stop();

        windowOverlay.SetActive(newState == "WINDOW_SELECT");
        workbenchOverlay.SetActive(newState == "WORKBENCH");
        cabinetOverlay.SetActive(newState == "CABINET");
        upgradesOverlay.SetActive(newState == "UPGRADES");
        modalOverlay.SetActive(newState == "TWIST" || newState == "RESULT" || newState == "FINALE");

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
        topTitleText.text = $"Заказов: {completedOrdersCount}/{TOTAL_ORDERS_TO_WIN}  |  Квалификация: {rankStr}  |  {ordName}";
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

        upgBtnLabels[1].text = upgStabilizer ? "Куплено" : "Купить за 1200 руб.";
        upgBtnImgs[1].color = upgStabilizer ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.28f, 0.52f, 0.32f, 1f);

        upgBtnLabels[2].text = (qualificationRank >= 2) ? "Получено" : "Повысить за 1600 руб.";
        upgBtnImgs[2].color = (qualificationRank >= 2) ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.55f, 0.40f, 0.16f, 1f);

        upgBtnLabels[3].text = (qualificationRank >= 3) ? "Получено" : "Повысить за 3000 руб.";
        upgBtnImgs[3].color = (qualificationRank >= 3) ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.55f, 0.40f, 0.16f, 1f);

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
            if (money >= 1600) { money -= 1600; qualificationRank = 2; PlaySFX(clipCoin); bottomHintText.text = "Поздравляем! Вы получили II разряд — теперь в Окне заказов доступны элитные реликвии!"; }
            else { PlaySFX(clipFail); bottomHintText.text = "Недостаточно денег для сертификации II разряда (нужно 1600 руб.)!"; }
        }
        else if (idx == 3 && qualificationRank < 3)
        {
            if (qualificationRank < 2) { PlaySFX(clipFail); bottomHintText.text = "Сначала получите Сертификат II разряда!"; return; }
            if (money >= 3000) { money -= 3000; qualificationRank = 3; trust = Mathf.Min(100, trust + 20); PlaySFX(clipCoin); bottomHintText.text = "Вы стали Гранд-мастером III разряда! Гонорары за все заказы увеличены на +35%!"; }
            else { PlaySFX(clipFail); bottomHintText.text = "Недостаточно денег для патента III разряда (нужно 3000 руб.)!"; }
        }
        RefreshUpgradesUI();
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
        int avgProg = (cleanProgress + grindProgress + clickFixProgress) / 3;
        if (avgProg >= 45 && !twistChecked && activeOrder.twistType != "NONE")
        {
            twistChecked = true;
            if (Random.value <= 0.60f)
            {
                PlaySFX(clipBell, 0.85f, 0.85f);
                TriggerMidOrderTwist();
            }
        }
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
        else if (viewState == "FINALE")
        {
            trust = 100;
            money = 400;
            secretViolated = false;
            completedOrdersCount = 0;
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
            PlaySFX(clipCoin, 0.8f, 1.0f);
        }

        money += pay;

        string story = (selectedStyle == "NEW") ? activeOrder.histNew : activeOrder.histPatina;
        if (activeOrder.shapeType == "RING" && secretViolated) story = activeOrder.histSpecial;
        else if (isRuinedToZero || chemicalStain || wrongToolScratches || overScrubDamage > 45f) story = activeOrder.histSpecial;

        historyLog.Add($"■ {activeOrder.title}\nИтог: {story} (Выплата: {pay} руб.)");

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
        modalSealGO.SetActive(true);
        modalSealGO.GetComponent<Image>().color = secretViolated ? new Color(0.65f, 0.35f, 0.35f, 1f) : new Color(0.95f, 0.78f, 0.25f, 1f);
        modalSealText.text = secretViolated ? "АРХИВ" : "ГРАНД";
        string rank = secretViolated ? "Любопытный ремесленник (Доверие утрачено)" :
                      (qualificationRank == 3 ? "Легендарный Гранд-мастер III разряда" : $"Мастер-реставратор {qualificationRank}-го разряда");
        modalTitle.text = "СМЕНА ЗАВЕРШЕНА — АРХИВ МАСТЕРСКОЙ";
        modalBody.text = $"Итоговый статус: {rank}\nНакоплено в кассе: {money} руб. | Доверие: {trust}%\n\nИСТОРИИ ВОССТАНОВЛЕННЫХ РЕЛИКВИЙ:\n{string.Join("\n\n", historyLog)}";
        modalBtn1.gameObject.SetActive(true);
        modalBtn1Text.text = "Начать новую игру";
        modalBtn2.gameObject.SetActive(false);
    }

    // =========================================================================
    // ПОКАДРОВОЕ ОБНОВЛЕНИЕ (С РЕАЛТАЙМОВЫМ 2D PBR-ОСВЕЩЕНИЕМ ОТ КУРСОРА)
    // =========================================================================
    private void UpdateDustMotes(float dt)
    {
        if (viewState != "ROOM")
        {
            for (int i = 0; i < dustMotes.Count; i++)
                dustMotes[i].img.color = Color.clear;
            return;
        }
        float t = Time.time;
        for (int i = 0; i < dustMotes.Count; i++)
        {
            DustMote m = dustMotes[i];
            float px = m.basePos.x + Mathf.Cos(t * 0.45f + m.phase) * m.radius;
            float py = m.basePos.y + Mathf.Sin(t * 0.35f + m.phase * 1.5f) * (m.radius * 0.65f);
            m.rt.anchoredPosition = new Vector2(px, py);
            float pulse = (Mathf.Sin(t * 1.8f + m.phase) + 1f) * 0.5f;
            m.img.color = new Color(1f, 0.90f, 0.65f, 0.10f + pulse * 0.25f);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        Vector3 mousePos = Input.mousePosition;

        UpdateSparks(dt);
        UpdateClickNodesDecay(dt);
        UpdateDustMotes(dt);

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
                    playerBubbleText.transform.parent.localScale = new Vector3(sign, 1f, 1f);
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
                playerRT.anchoredPosition = playerPos;
                if (!string.IsNullOrEmpty(pendingStation))
                {
                    string st = pendingStation;
                    pendingStation = null;
                    OnPlayerArrivedAtStation(st);
                }
            }

            if (markerAlpha > 0f)
            {
                markerAlpha = Mathf.Max(0f, markerAlpha - dt * 2.2f);
                clickMarkerRT.GetComponent<Image>().color = new Color(1f, 0.88f, 0.35f, markerAlpha * 0.7f);
            }
        }
        else if (viewState == "WORKBENCH")
        {
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
                    foreach (var d in activeOrder.diagnostics)
                    {
                        if (!d.found && Vector2.Distance(localPt, new Vector2(d.x, d.y)) < d.r)
                        {
                            d.found = true;
                            zonesDiscovered++;
                            PlaySFX(clipClickPin, 0.65f, 1.2f);
                            benchLogText.text += "\n\n• " + d.text;
                            bottomHintText.text = $"Найдено повреждений под лупой: {zonesDiscovered} / 3";
                            EmitSparks(localPt, 8, new Color(0.95f, 0.90f, 0.45f, 1f));
                            if (zonesDiscovered >= 3)
                            {
                                orderStage = "NEED_TOOLS";
                                OpenWorkbenchView();
                            }
                        }
                    }
                }
            }
            else if ((orderStage == "READY_TO_RESTORE" || orderStage == "READY_TO_SUBMIT") && isInside)
            {
                benchProgressText.text =
                    $"1. Очистка окислов: {cleanProgress}%\n" +
                    $"2. Шлифовка (рывками): {grindProgress}%\n" +
                    $"3. Закрепка (кликами): {clickFixProgress}%\n" +
                    $"4. Финишный лоск: {Mathf.FloorToInt(finishProgress)}%";

                bool holdingMouse = Input.GetMouseButton(0) && Mathf.Abs(localPt.x) < 180f && Mathf.Abs(localPt.y) < 180f && activeBenchAction != "CLICK_FIX";

                if (holdingMouse && speedRatio > 0.04f)
                {
                    AudioClip targetLoop = (activeBenchAction == "GRIND") ? clipGrindLoop : clipCleanLoop;
                    if (toolLoopSource.clip != targetLoop) toolLoopSource.clip = targetLoop;
                    toolLoopSource.pitch = (activeBenchAction == "GRIND") ? Mathf.Lerp(0.85f, 1.35f, speedRatio) : 1.0f;
                    if (!toolLoopSource.isPlaying) toolLoopSource.Play();
                }
                else if (toolLoopSource.isPlaying)
                {
                    toolLoopSource.Stop();
                }

                if (holdingMouse)
                {
                    int px = Mathf.FloorToInt(((localPt.x + 180f) / 360f) * TEX_SIZE);
                    int py = Mathf.FloorToInt(((localPt.y + 180f) / 360f) * TEX_SIZE);
                    bool modified = false;

                    float radiusMult = upgPrecisionBits ? 1.4f : 1.0f;
                    float dmgMult = upgStabilizer ? 0.4f : 1.0f;
                    int brushR = Mathf.RoundToInt(15 * radiusMult);

                    if (activeBenchAction == "CLEAN")
                    {
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
                                        overScrubDamage = Mathf.Min(100f, overScrubDamage + dt * 5.0f * dmgMult);
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

                        if (modified && !isTooFast)
                        {
                            sparkSpawnTimer += dt;
                            if (sparkSpawnTimer > 0.08f)
                            {
                                sparkSpawnTimer = 0f;
                                EmitSparks(localPt, 1, new Color(0.72f, 0.90f, 1f, 0.70f));
                            }
                        }
                    }
                    else if (activeBenchAction == "GRIND")
                    {
                        if (cleanProgress < 35)
                        {
                            PlaySFX(clipSpringReset, 0.35f, 1.4f);
                            bottomHintText.text = "НАРУШЕН ПОРЯДОК: Сначала смойте хотя бы 35% окислов растворителем!";
                        }
                        else if (speedRatio < 0.26f)
                        {
                            bottomHintText.text = "Слишком медленно для шлифовки! Быстро дергайте мышью из стороны в сторону по рыжим зазубринам!";
                        }
                        else
                        {
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
                        }
                    }
                    else if (activeBenchAction == "FINISH")
                    {
                        if (cleanProgress < 65 || grindProgress < 65 || clickFixProgress < 100)
                        {
                            PlaySFX(clipSpringReset, 0.35f, 1.4f);
                            bottomHintText.text = "НАРУШЕН ПОРЯДОК: Сначала завершите Очистку, Шлифовку и Закрепку всех 4 узлов кликами!";
                        }
                        else if (speedRatio > 0.03f && speedRatio < 0.82f)
                        {
                            float prevFinish = finishProgress;
                            finishProgress = Mathf.Min(100f, finishProgress + dt * 48f * radiusMult);
                            if (prevFinish < 100f && finishProgress >= 100f)
                            {
                                PlaySFX(clipBell, 0.85f, 1.25f);
                                EmitSparks(localPt, 14, new Color(1f, 0.95f, 0.65f, 1f));
                                bottomHintText.text = "РЕСТАВРАЦИЯ ЗАВЕРШЕНА! Нажмите 'Завершить и отнести в Окно заказов'.";
                            }
                            modified = true;
                            sparkSpawnTimer += dt;
                            if (sparkSpawnTimer > 0.07f)
                            {
                                sparkSpawnTimer = 0f;
                                EmitSparks(localPt, 2, new Color(1f, 0.96f, 0.65f, 1f));
                            }
                        }
                    }

                    if (modified)
                    {
                        RefreshItemVisuals();
                        UpdateTopBar();
                        CheckMidOrderTwist();
                    }
                }
            }
            else if (toolLoopSource != null && toolLoopSource.isPlaying)
            {
                toolLoopSource.Stop();
            }
        }
    }
}