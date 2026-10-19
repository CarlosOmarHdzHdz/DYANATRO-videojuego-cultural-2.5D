using UnityEngine;
using UnityEngine.UI;

// Conserva el bordado original; solo anima de forma continua el interior.
public sealed class XunjuuHealthBarVisual
{
    private const int Columns = 4;
    private const int Rows = 4;

    private readonly Image image;
    private readonly Sprite[] frames;
    private readonly Texture2D packedTexture;
    private Image continuousFill;
    private Sprite backgroundSprite, fillSprite;
    private Texture2D backgroundTexture, fillTexture;

    private XunjuuHealthBarVisual(Image image, Sprite[] frames, Texture2D texture)
    {
        this.image = image;
        this.frames = frames;
        packedTexture = texture;
    }

    public static XunjuuHealthBarVisual Create(BarraVidaFrames owner)
    {
        Texture2D sheet = Resources.Load<Texture2D>("Health/MazahuaHealthSheet");
        if (sheet == null || owner == null || owner.imagenBarra == null)
            return null;

        sheet.filterMode = FilterMode.Point;
        sheet.wrapMode = TextureWrapMode.Clamp;

        if (!sheet.isReadable) return null;
        int sourceWidth = sheet.width;
        int cell = sourceWidth / 4;
        int packedHeight = Mathf.RoundToInt(cell * 164f / 300f);
        int[] sourceFrames = { 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        Color32[] source = sheet.GetPixels32();
        var packed = new Color32[cell * 4 * packedHeight * 4];
        for (int frame = 0; frame < 16; frame++)
        {
            int sourceColumn = sourceFrames[frame] % 4, sourceRow = sourceFrames[frame] / 4;
            int top = Mathf.RoundToInt(cell * (64f + sourceRow * 150f) / 300f);
            for (int y = 0; y < packedHeight; y++)
                for (int x = 0; x < cell; x++)
                {
                    int sourceY = sheet.height - 1 - top - y;
                    if (sourceY < 0 || sourceY >= sheet.height) continue;
                    if (y < cell * 0.15f && (x < cell * 0.25f || x > cell * 0.77f)) continue;
                    // Adjacent rows overlap only beneath the central skull, not the side ornaments.
                    if (y > cell * 0.5f && x > cell * 0.23f && x < cell * 0.77f) continue;
                    int destinationY = (3 - frame / 4) * packedHeight + packedHeight - 1 - y;
                    packed[destinationY * sourceWidth + frame % 4 * cell + x] = source[sourceY * sourceWidth + sourceColumn * cell + x];
                }
        }
        sheet = new Texture2D(sourceWidth, packedHeight * 4, TextureFormat.RGBA32, false);
        sheet.name = "HealthAlignedRuntime";
        sheet.SetPixels32(packed);
        sheet.Apply();
        sheet.filterMode = FilterMode.Point;
        sheet.wrapMode = TextureWrapMode.Clamp;

        int cellWidth = sheet.width / Columns;
        int cellHeight = sheet.height / Rows;
        var frames = new Sprite[Columns * Rows];
        int index = 0;
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                Rect rect = new Rect(column * cellWidth, sheet.height - (row + 1) * cellHeight, cellWidth, cellHeight);
                Sprite frame = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                frame.name = "MazahuaHealth_" + index.ToString("00");
                frame.hideFlags = HideFlags.DontSave;
                frames[index++] = frame;
            }
        }

        RectTransform root = owner.GetComponent<RectTransform>();
        if (root != null)
        {
            Place(root, new Vector2(24f, -24f), new Vector2(cellWidth + 24f, cellHeight + 46f));
            Image background = root.GetComponent<Image>();
            if (background != null && background != owner.imagenBarra)
            {
                background.enabled = false;
                background.sprite = null;
                background.type = Image.Type.Simple;
                background.color = new Color(0.025f, 0.045f, 0.05f, 0.96f);
                background.raycastTarget = false;
                Outline outline = root.GetComponent<Outline>();
                if(outline!=null)outline.enabled=false;
            }
            Transform oldFrame = root.Find("Fondo");
            if (oldFrame != null && oldFrame != owner.imagenBarra.transform)
                oldFrame.gameObject.SetActive(false);
        }

        Image image = owner.imagenBarra;
        image.overrideSprite = null;
        image.sprite = frames[0];
        image.material = null;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        Place(image.rectTransform, new Vector2(12f, -10f), new Vector2(cellWidth, cellHeight));

        if (owner.textoVida != null)
        {
            Text text = owner.textoVida;
            if (root != null)
                text.transform.SetParent(root, false);
            Place(text.rectTransform, new Vector2(12f, -cellHeight - 12f), new Vector2(cellWidth, 28f));
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            Shadow shadow = text.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        var visual = new XunjuuHealthBarVisual(image, frames, sheet);
        visual.BuildContinuousFill(cellWidth, cellHeight);
        return visual;
    }

    private void BuildContinuousFill(int width, int height)
    {
        // El primer estado contiene el bordado lleno y el último el fondo vacío.
        Color[] full = packedTexture.GetPixels((int)frames[0].rect.x, (int)frames[0].rect.y, width, height);
        Color[] empty = packedTexture.GetPixels((int)frames[15].rect.x, (int)frames[15].rect.y, width, height);
        Color[] background = (Color[])full.Clone();
        float scale = width / 300f;
        int left = Mathf.RoundToInt(14f * scale), top = Mathf.RoundToInt(88f * scale);
        int right = Mathf.RoundToInt(279f * scale), bottom = Mathf.RoundToInt(130f * scale);
        int fillWidth = right - left, fillHeight = bottom - top;
        var foreground = new Color[fillWidth * fillHeight];
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                float artX = x / scale, artY = y / scale + 64f;
                bool skull = artX >= 110f && artY < 167f && artX < 166f - (artY - 152f) * 1.5f;
                bool medallion = Vector2.Distance(new Vector2(artX, artY), new Vector2(268f, 153f)) < 16f;
                if (skull || medallion) continue;
                int pixel = (height - 1 - y) * width + x;
                background[pixel] = empty[pixel];
                foreground[(fillHeight - 1 - (y - top)) * fillWidth + x - left] = full[pixel];
            }
        }
        backgroundTexture = MakeTexture(width, height, background, "HealthBorderRuntime");
        fillTexture = MakeTexture(fillWidth, fillHeight, foreground, "HealthFillRuntime");
        backgroundSprite = MakeSprite(backgroundTexture, "MazahuaHealth_Continuous");
        fillSprite = MakeSprite(fillTexture, "MazahuaHealth_Fill");
        image.sprite = backgroundSprite;

        var fillObject = new GameObject("RellenoContinuo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(image.transform, false);
        continuousFill = fillObject.GetComponent<Image>();
        continuousFill.sprite = fillSprite;
        continuousFill.color = Color.white;
        continuousFill.type = Image.Type.Filled;
        continuousFill.fillMethod = Image.FillMethod.Horizontal;
        continuousFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        continuousFill.raycastTarget = false;
        Place(continuousFill.rectTransform, new Vector2(left, -top), new Vector2(fillWidth, fillHeight));
    }

    private static Texture2D MakeTexture(int width, int height, Color[] pixels, string name)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Sprite MakeSprite(Texture2D texture, string name)
    {
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void PlaceTopRight(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public void SetHealth(float normalized)
    {
        if (continuousFill != null) continuousFill.fillAmount = Mathf.Clamp01(normalized);
    }

    public void Dispose()
    {
        if (continuousFill != null) Release(continuousFill.gameObject);
        Release(backgroundSprite);
        Release(fillSprite);
        Release(backgroundTexture);
        Release(fillTexture);
        if (frames == null)
            return;

        foreach (Sprite frame in frames)
        {
            if (frame == null)
                continue;
            if (Application.isPlaying)
                Object.Destroy(frame);
            else
                Object.DestroyImmediate(frame);
        }
        if (packedTexture != null)
        {
            if (Application.isPlaying) Object.Destroy(packedTexture);
            else Object.DestroyImmediate(packedTexture);
        }
    }

    private static void Release(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
    }
}
