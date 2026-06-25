using UnityEngine;
using TMPro;

public class MainMenuProgressionCoinsDisplay : MonoBehaviour {
    public enum ScreenAlignment {
        TopLeft,
        TopRight
    }

    [Header("UI Sprites")]
    [SerializeField] private Sprite coinIconSprite;

    [Header("Layout Settings")]
    [SerializeField] private ScreenAlignment alignment = ScreenAlignment.TopLeft;

    private GameObject displayPill;
    private TextMeshProUGUI coinsText;
    private bool isInitialized = false;

    public void SetCoinIconSprite(Sprite sprite) {
        coinIconSprite = sprite;
        if (isInitialized) {
            if (displayPill != null) Destroy(displayPill);
            CreateDisplay();
            UpdateDisplay();
        }
    }

    public void SetAlignment(ScreenAlignment align) {
        alignment = align;
        if (isInitialized) {
            if (displayPill != null) Destroy(displayPill);
            CreateDisplay();
            UpdateDisplay();
        }
    }

    private void Start() {
        if (!isInitialized) {
            CreateDisplay();
            UpdateDisplay();
            
            GlobalProgressionManager.OnCoinsChanged += OnCoinsChanged;
            isInitialized = true;
        }
    }

    private void OnDestroy() {
        GlobalProgressionManager.OnCoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int newCount) {
        UpdateDisplay();
    }

    private void UpdateDisplay() {
        if (coinsText != null) {
            int coins = GlobalProgressionManager.Instance != null ? GlobalProgressionManager.Instance.GetCoins() : 0;
            if (coinIconSprite != null) {
                coinsText.text = coins.ToString();
            } else {
                coinsText.text = $"💰 {coins}";
            }
        }
    }

    private void CreateDisplay() {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Pill Container
        displayPill = new GameObject("ProgressionCoinsPill");
        displayPill.AddComponent<RectTransform>();
        displayPill.transform.SetParent(canvas.transform, false);

        var img = displayPill.AddComponent<UnityEngine.UI.Image>();
        
        Color coinsBottom = new Color(0.15f, 0.12f, 0.05f, 0.85f);
        Color coinsTop = new Color(0.30f, 0.24f, 0.08f, 0.85f);
        Color coinsBorder = new Color(1.00f, 0.80f, 0.20f, 1f);
        img.sprite = CreateRoundedRectGradientSprite(180, 60, 30, coinsBottom, coinsTop, coinsBorder, 3);

        var rect = displayPill.GetComponent<RectTransform>();
        if (alignment == ScreenAlignment.TopRight) {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-40f, -40f);
        } else {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -40f);
        }
        rect.sizeDelta = new Vector2(180f, 60f);

        // Icon (if sprite is assigned)
        if (coinIconSprite != null) {
            var iconGo = new GameObject("Icon");
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconGo.transform.SetParent(displayPill.transform, false);
            
            var iconImg = iconGo.AddComponent<UnityEngine.UI.Image>();
            iconImg.sprite = coinIconSprite;
            iconImg.preserveAspect = true;

            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(20f, 0f);
            iconRect.sizeDelta = new Vector2(32f, 32f);

            // Text next to icon
            var textGo = new GameObject("Text");
            var textRect = textGo.AddComponent<RectTransform>();
            textGo.transform.SetParent(displayPill.transform, false);
            coinsText = textGo.AddComponent<TextMeshProUGUI>();
            coinsText.fontSize = 24;
            coinsText.fontStyle = FontStyles.Bold;
            coinsText.color = Color.white;
            coinsText.alignment = TextAlignmentOptions.MidlineLeft;

            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(1f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = new Vector2(65f, 0f);
            textRect.sizeDelta = new Vector2(-80f, 40f);
        } else {
            // Text only (with emoji)
            var textGo = new GameObject("Text");
            var textRect = textGo.AddComponent<RectTransform>();
            textGo.transform.SetParent(displayPill.transform, false);
            coinsText = textGo.AddComponent<TextMeshProUGUI>();
            coinsText.fontSize = 24;
            coinsText.fontStyle = FontStyles.Bold;
            coinsText.color = Color.white;
            coinsText.alignment = TextAlignmentOptions.Center;

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        // Apply a high-quality font if available in the scene
        ApplyFont(coinsText);
    }

    private void ApplyFont(TextMeshProUGUI tmpText) {
        if (tmpText == null) return;
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        TMP_FontAsset fontToUse = null;
        foreach (var f in fonts) {
            if (f.name.Contains("Salsa") || f.name.Contains("Liberation")) {
                fontToUse = f;
                break;
            }
        }
        if (fontToUse != null) {
            tmpText.font = fontToUse;
        }
    }

    private Sprite CreateRoundedRectGradientSprite(int width, int height, int radius, Color bottomColor, Color topColor, Color borderColor, int borderWidth) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        for (int y = 0; y < height; y++) {
            float t = (float)y / height;
            Color innerColor = Color.Lerp(bottomColor, topColor, t);
            for (int x = 0; x < width; x++) {
                bool insideOuter = IsInsideRoundedRect(x, y, width, height, radius);
                if (!insideOuter) {
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                bool insideInner = false;
                if (borderWidth > 0) {
                    int innerWidth = width - 2 * borderWidth;
                    int innerHeight = height - 2 * borderWidth;
                    int innerRadius = Mathf.Max(0, radius - borderWidth);
                    insideInner = IsInsideRoundedRect(x - borderWidth, y - borderWidth, innerWidth, innerHeight, innerRadius);
                } else {
                    insideInner = true;
                }

                cols[y * width + x] = insideInner ? innerColor : borderColor;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius) {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        if (x < radius && y < radius) {
            return (x - radius) * (x - radius) + (y - radius) * (y - radius) <= radius * radius;
        } else if (x >= width - radius && y < radius) {
            return (x - (width - radius)) * (x - (width - radius)) + (y - radius) * (y - radius) <= radius * radius;
        } else if (x < radius && y >= height - radius) {
            return (x - radius) * (x - radius) + (y - (height - radius)) * (y - (height - radius)) <= radius * radius;
        } else if (x >= width - radius && y >= height - radius) {
            return (x - (width - radius)) * (x - (width - radius)) + (y - (height - radius)) * (y - (height - radius)) <= radius * radius;
        }
        return true;
    }
}
