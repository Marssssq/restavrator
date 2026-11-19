using UnityEngine;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    private RectTransform canvasRect;
    private RectTransform panelRect;
    private CanvasGroup canvasGroup;
    private Text titleText;
    private Text descText;
    private GameObject hotkeyContainer;
    private Text hotkeyText;
    private ContentSizeFitter sizeFitter;
    private VerticalLayoutGroup layoutGroup;

    private bool isVisible;
    private float targetAlpha;

    private const float ReferenceWidth = 1280f;
    private const float ReferenceHeight = 720f;
    private const float ScreenMargin = 14f;
    private const float CursorOffset = 18f;
    private const float MaxTooltipWidth = 320f;
    private const float MinTooltipWidth = 200f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureCanvasHierarchy();
    }

    private void EnsureCanvasHierarchy()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = GameObject.Find("RestorerWorkshopCanvas");
            if (canvasGO != null)
            {
                transform.SetParent(canvasGO.transform, false);
                canvas = canvasGO.GetComponent<Canvas>();
            }
        }

        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }

        BuildVisualStructure();
        HideTooltipImmediate();
    }

    private void BuildVisualStructure()
    {
        panelRect = GetComponent<RectTransform>();
        if (panelRect == null) panelRect = gameObject.AddComponent<RectTransform>();
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(MaxTooltipWidth, 100f);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0f;

        Image panelImg = GetComponent<Image>();
        if (panelImg == null) panelImg = gameObject.AddComponent<Image>();
        panelImg.sprite = CreatePBRTooltipFrameSprite(128, 128, 14);
        panelImg.type = Image.Type.Sliced;
        panelImg.color = Color.white;
        panelImg.raycastTarget = false;

        layoutGroup = GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null) layoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(14, 14, 12, 12);
        layoutGroup.spacing = 6f;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        sizeFitter = GetComponent<ContentSizeFitter>();
        if (sizeFitter == null) sizeFitter = gameObject.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject headerRow = new GameObject("HeaderRow");
        headerRow.transform.SetParent(transform, false);
        headerRow.AddComponent<RectTransform>();
        HorizontalLayoutGroup headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        headerLayout.spacing = 8f;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(headerRow.transform, false);
        titleText = titleGO.AddComponent<Text>();
        titleText.font = Font.CreateDynamicFontFromOSFont("Georgia", 15);
        if (titleText.font == null) titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 15;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1.0f, 0.88f, 0.55f, 1f);
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.raycastTarget = false;

        ContentSizeFitter titleFitter = titleGO.AddComponent<ContentSizeFitter>();
        titleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        titleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        hotkeyContainer = new GameObject("HotkeyBadge");
        hotkeyContainer.transform.SetParent(headerRow.transform, false);
        Image badgeImg = hotkeyContainer.AddComponent<Image>();
        badgeImg.color = new Color(0.36f, 0.26f, 0.14f, 0.95f);
        badgeImg.raycastTarget = false;

        HorizontalLayoutGroup badgeLayout = hotkeyContainer.AddComponent<HorizontalLayoutGroup>();
        badgeLayout.padding = new RectOffset(6, 6, 2, 2);
        badgeLayout.childControlWidth = true;
        badgeLayout.childControlHeight = true;
        badgeLayout.childForceExpandWidth = false;
        badgeLayout.childForceExpandHeight = false;

        ContentSizeFitter badgeFitter = hotkeyContainer.AddComponent<ContentSizeFitter>();
        badgeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        badgeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject hotkeyTxtGO = new GameObject("HotkeyText");
        hotkeyTxtGO.transform.SetParent(hotkeyContainer.transform, false);
        hotkeyText = hotkeyTxtGO.AddComponent<Text>();
        hotkeyText.font = Font.CreateDynamicFontFromOSFont("Segoe UI", 11);
        if (hotkeyText.font == null) hotkeyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hotkeyText.fontSize = 11;
        hotkeyText.fontStyle = FontStyle.Bold;
        hotkeyText.color = new Color(0.98f, 0.86f, 0.45f, 1f);
        hotkeyText.alignment = TextAnchor.MiddleCenter;
        hotkeyText.raycastTarget = false;

        GameObject sepGO = new GameObject("Separator");
        sepGO.transform.SetParent(transform, false);
        RectTransform sepRT = sepGO.AddComponent<RectTransform>();
        sepRT.sizeDelta = new Vector2(0f, 2f);
        Image sepImg = sepGO.AddComponent<Image>();
        sepImg.color = new Color(0.68f, 0.52f, 0.24f, 0.65f);
        sepImg.raycastTarget = false;
        LayoutElement sepLE = sepGO.AddComponent<LayoutElement>();
        sepLE.minHeight = 2f;
        sepLE.preferredHeight = 2f;

        GameObject descGO = new GameObject("Description");
        descGO.transform.SetParent(transform, false);
        descText = descGO.AddComponent<Text>();
        descText.font = Font.CreateDynamicFontFromOSFont("Segoe UI", 13);
        if (descText.font == null) descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 13;
        descText.lineSpacing = 1.15f;
        descText.color = new Color(0.92f, 0.90f, 0.84f, 1f);
        descText.alignment = TextAnchor.UpperLeft;
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Overflow;
        descText.raycastTarget = false;
    }

    private Sprite CreatePBRTooltipFrameSprite(int w, int h, float radius)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] cols = new Color32[w * h];
        Vector3 lightDir = new Vector3(-0.45f, 0.65f, 0.75f).normalized;
        Color brassCol = new Color(0.85f, 0.68f, 0.28f, 1f);
        Color parchmentCol = new Color(0.14f, 0.10f, 0.08f, 0.96f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - w * 0.5f) - (w * 0.5f - radius - 1f), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - h * 0.5f) - (h * 0.5f - radius - 1f), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);

                float edgeDist = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                bool isBorder = (dist > radius - 3.5f) || (edgeDist < 3.5f);

                if (isBorder)
                {
                    Vector3 norm = new Vector3((x - w * 0.5f) / (w * 0.5f), (y - h * 0.5f) / (h * 0.5f), 0.7f).normalized;
                    float diff = Mathf.Clamp01(Vector3.Dot(norm, lightDir));
                    float spec = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(Vector3.Reflect(-lightDir, norm), Vector3.forward)), 16f) * 0.7f;
                    Color c = brassCol * (0.45f + diff * 0.55f) + Color.white * spec;
                    c.a = alpha;
                    cols[y * w + x] = c;
                }
                else
                {
                    float grain = (Mathf.PerlinNoise(x * 0.1f, y * 0.2f) - 0.5f) * 0.03f;
                    Color c = new Color(
                        Mathf.Clamp01(parchmentCol.r + grain),
                        Mathf.Clamp01(parchmentCol.g + grain),
                        Mathf.Clamp01(parchmentCol.b + grain),
                        alpha * parchmentCol.a
                    );
                    cols[y * w + x] = c;
                }
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    public void ShowTooltip(string title, string description, string hotkeyHint = null)
    {
        if (panelRect == null) EnsureCanvasHierarchy();
        transform.SetAsLastSibling();

        titleText.text = title ?? string.Empty;
        descText.text = description ?? string.Empty;

        bool hasHotkey = !string.IsNullOrEmpty(hotkeyHint);
        hotkeyContainer.SetActive(hasHotkey);
        if (hasHotkey) hotkeyText.text = hotkeyHint;

        float preferredWidth = Mathf.Max(titleText.preferredWidth + (hasHotkey ? hotkeyText.preferredWidth + 30f : 0f) + 32f, MinTooltipWidth);
        float targetWidth = Mathf.Min(preferredWidth, MaxTooltipWidth);
        panelRect.sizeDelta = new Vector2(targetWidth, panelRect.sizeDelta.y);

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

        isVisible = true;
        targetAlpha = 1f;
        UpdatePosition();
    }

    public void HideTooltip()
    {
        isVisible = false;
        targetAlpha = 0f;
    }

    public void HideTooltipImmediate()
    {
        isVisible = false;
        targetAlpha = 0f;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        if (canvasGroup != null && Mathf.Abs(canvasGroup.alpha - targetAlpha) > 0.01f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 14f);
        }

        if (isVisible)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        if (canvasRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out localPoint);

        float tooltipWidth = panelRect.rect.width;
        float tooltipHeight = panelRect.rect.height;

        float halfW = ReferenceWidth * 0.5f;
        float halfH = ReferenceHeight * 0.5f;

        float posX = localPoint.x + CursorOffset;
        float posY = localPoint.y - CursorOffset;

        if (posX + tooltipWidth > halfW - ScreenMargin)
        {
            posX = localPoint.x - tooltipWidth - CursorOffset;
        }

        if (posY - tooltipHeight < -halfH + ScreenMargin)
        {
            posY = localPoint.y + tooltipHeight + CursorOffset;
        }

        posX = Mathf.Clamp(posX, -halfW + ScreenMargin, halfW - tooltipWidth - ScreenMargin);
        posY = Mathf.Clamp(posY, -halfH + tooltipHeight + ScreenMargin, halfH - ScreenMargin);

        panelRect.anchoredPosition = new Vector2(posX, posY);
    }
}
