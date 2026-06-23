using UnityEngine;
using TMPro;

public class MainMenuProgressionCoinsDisplay : MonoBehaviour {
    private GameObject displayPill;
    private TextMeshProUGUI coinsText;

    private void Start() {
        CreateDisplay();
        UpdateDisplay();
        
        GlobalProgressionManager.OnCoinsChanged += OnCoinsChanged;
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
            coinsText.text = $"💰 {coins}";
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
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(40f, -40f);
        rect.sizeDelta = new Vector2(180f, 60f);

        // Text
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
