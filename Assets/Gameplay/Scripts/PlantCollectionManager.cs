using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class PlantCollectionManager : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private Button backButton;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Container")]
    [SerializeField] private Transform cardsGridParent;

    private struct PlantCollectionItem {
        public string name;
        public string displayName;
        public bool isLocked;
    }

    private PlantCollectionItem[] plantItems = new PlantCollectionItem[] {
        new PlantCollectionItem { name = "Fire Bloom", displayName = "Fire Bloom", isLocked = false },
        new PlantCollectionItem { name = "Frost Flower", displayName = "Frost Flower", isLocked = false },
        new PlantCollectionItem { name = "Thorn Vine", displayName = "Thorn Vine", isLocked = true },
        new PlantCollectionItem { name = "Bomb Cactus", displayName = "Bomb Cactus", isLocked = true },
        new PlantCollectionItem { name = "Magic Blossom", displayName = "Magic Blossom", isLocked = true },
        new PlantCollectionItem { name = "Sunflower Tree", displayName = "Sunflower Tree", isLocked = true }
    };

    private void Start() {
        if (backButton != null) {
            backButton.onClick.AddListener(GoBackToMainMenu);
        }

        // Fade in
        if (fadeOverlay != null) {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.alpha = 1f;
            StartCoroutine(FadeRoutine(1f, 0f));
        }

        GeneratePlantCards();
    }

    private void GoBackToMainMenu() {
        StartCoroutine(GoBackRoutine());
    }

    private IEnumerator GoBackRoutine() {
        if (fadeOverlay != null) {
            fadeOverlay.gameObject.SetActive(true);
            yield return StartCoroutine(FadeRoutine(0f, 1f));
        } else {
            yield return new WaitForSeconds(0.3f);
        }
        SceneManager.LoadScene("GardenGuardians_MainMenu");
    }

    private IEnumerator FadeRoutine(float fromAlpha, float toAlpha) {
        float elapsed = 0f;
        while (elapsed < fadeDuration) {
            elapsed += Time.deltaTime;
            if (fadeOverlay != null) {
                fadeOverlay.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / fadeDuration);
            }
            yield return null;
        }
        if (fadeOverlay != null) {
            fadeOverlay.alpha = toAlpha;
            if (toAlpha == 0f) {
                fadeOverlay.gameObject.SetActive(false);
            }
        }
    }

    private void GeneratePlantCards() {
        if (cardsGridParent == null) return;

        // Clear existing children if any
        foreach (Transform child in cardsGridParent) {
            Destroy(child.gameObject);
        }

        float cardWidth = 240f;
        float cardHeight = 320f;
        
        // We will layout in 2 rows of 3 columns
        float colSpacing = 320f;
        float rowSpacing = 380f;

        float startX = -colSpacing; // -320, 0, 320
        float startY = 160f;        // Row 1 at 160, Row 2 at -220

        for (int i = 0; i < plantItems.Length; i++) {
            var item = plantItems[i];
            int row = i / 3;
            int col = i % 3;

            var cardGo = new GameObject($"Card_{item.name.Replace(" ", "")}");
            var cardRect = cardGo.AddComponent<RectTransform>();
            cardGo.transform.SetParent(cardsGridParent, false);

            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(startX + col * colSpacing, startY - row * rowSpacing);
            cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);

            // Card Background Image
            var bgImg = cardGo.AddComponent<UnityEngine.UI.Image>();
            
            // Colors for Unlocked vs Locked card background
            Color bgBottom = item.isLocked ? new Color(0.12f, 0.12f, 0.12f, 0.9f) : new Color(0.08f, 0.18f, 0.11f, 0.95f);
            Color bgTop = item.isLocked ? new Color(0.20f, 0.20f, 0.20f, 0.9f) : new Color(0.15f, 0.30f, 0.18f, 0.95f);
            Color bgBorder = item.isLocked ? new Color(0.35f, 0.35f, 0.35f, 0.8f) : new Color(0.85f, 0.75f, 0.25f, 1f);
            
            bgImg.sprite = CreateRoundedRectGradientSprite(240, 320, 30, bgBottom, bgTop, bgBorder, 5);

            // Plant Icon Container (so we can scale or position it cleanly)
            var iconGo = new GameObject("Icon");
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconGo.transform.SetParent(cardGo.transform, false);
            var iconImg = iconGo.AddComponent<UnityEngine.UI.Image>();
            
            Sprite plantSprite = PlantVisuals.GetPlantSprite(item.name);
            iconImg.sprite = plantSprite;
            
            // Locked appearance: darken the icon
            iconImg.color = item.isLocked ? new Color(0.25f, 0.25f, 0.25f, 0.6f) : Color.white;

            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 30f);
            iconRect.sizeDelta = new Vector2(160f, 160f); // Make plant size look large and visible

            // Plant Name Label
            var nameGo = new GameObject("NameLabel");
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameGo.transform.SetParent(cardGo.transform, false);
            var nameText = nameGo.AddComponent<TextMeshProUGUI>();
            nameText.text = item.displayName;
            nameText.fontSize = 24;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = item.isLocked ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;
            nameText.alignment = TextAlignmentOptions.Center;

            nameRect.anchorMin = new Vector2(0.5f, 0f);
            nameRect.anchorMax = new Vector2(0.5f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.anchoredPosition = new Vector2(0f, 40f);
            nameRect.sizeDelta = new Vector2(220f, 40f);

            // Locked Overlay (Padlock Icon and Dark Overlay)
            if (item.isLocked) {
                var lockOverlayGo = new GameObject("LockOverlay");
                var lockOverlayRect = lockOverlayGo.AddComponent<RectTransform>();
                lockOverlayGo.transform.SetParent(cardGo.transform, false);
                
                var lockBg = lockOverlayGo.AddComponent<UnityEngine.UI.Image>();
                lockBg.sprite = CreateRoundedRectSprite(240, 320, 30, new Color(0f, 0f, 0f, 0.25f));
                
                lockOverlayRect.anchorMin = Vector2.zero;
                lockOverlayRect.anchorMax = Vector2.one;
                lockOverlayRect.sizeDelta = Vector2.zero;

                var lockIconGo = new GameObject("LockIcon");
                var lockIconRect = lockIconGo.AddComponent<RectTransform>();
                lockIconGo.transform.SetParent(lockOverlayGo.transform, false);
                var lockIconImg = lockIconGo.AddComponent<UnityEngine.UI.Image>();
                lockIconImg.sprite = CreateLockSprite(64, 64);
                
                lockIconRect.anchorMin = new Vector2(0.5f, 0.5f);
                lockIconRect.anchorMax = new Vector2(0.5f, 0.5f);
                lockIconRect.pivot = new Vector2(0.5f, 0.5f);
                lockIconRect.anchoredPosition = new Vector2(0f, 30f); // align with plant icon center
                lockIconRect.sizeDelta = new Vector2(64f, 64f);
            }
        }
    }

    // --- PROCEDURAL SPRITE GENERATORS ---

    private Sprite CreateRoundedRectGradientSprite(int width, int height, int radius, Color colBottom, Color colTop, Color borderColor, int borderWidth) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];

        for (int y = 0; y < height; y++) {
            float t = (float)y / (height - 1);
            Color rowColor = Color.Lerp(colBottom, colTop, t);

            for (int x = 0; x < width; x++) {
                int dist = GetDistanceFromRoundedRectBorder(x, y, width, height, radius);
                if (dist < 0) {
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                } else if (dist < borderWidth) {
                    cols[y * width + x] = borderColor;
                } else {
                    cols[y * width + x] = rowColor;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRoundedRectSprite(int width, int height, int radius, Color color) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int dist = GetDistanceFromRoundedRectBorder(x, y, width, height, radius);
                if (dist < 0) {
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                } else {
                    cols[y * width + x] = color;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private int GetDistanceFromRoundedRectBorder(int x, int y, int width, int height, int radius) {
        int left = radius;
        int right = width - radius - 1;
        int bottom = radius;
        int top = height - radius - 1;

        if (x >= left && x <= right && y >= bottom && y <= top) {
            return radius;
        }

        if (x < left && y > bottom && y < top) return x;
        if (x > right && y > bottom && y < top) return width - 1 - x;
        if (y < bottom && x >= left && x <= right) return y;
        if (y > top && x >= left && x <= right) return height - 1 - y;

        float cx = 0, cy = 0;
        if (x < left && y < bottom) { cx = left; cy = bottom; }
        else if (x > right && y < bottom) { cx = right; cy = bottom; }
        else if (x < left && y > top) { cx = left; cy = top; }
        else if (x > right && y > top) { cx = right; cy = top; }

        float dx = x - cx;
        float dy = y - cy;
        float d = Mathf.Sqrt(dx * dx + dy * dy);

        if (d > radius) return -1;
        return Mathf.RoundToInt(radius - d);
    }

    private Sprite CreateLockSprite(int width, int height) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        float cx = width / 2f;
        float cy = height / 2.3f;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float dx = x - cx;
                float dy = y - cy;
                bool isBody = (Mathf.Abs(dx) <= width * 0.28f && y >= height * 0.18f && y <= height * 0.52f);
                float distToArchCenter = Mathf.Sqrt(dx * dx + (y - height * 0.52f) * (y - height * 0.52f));
                bool isArch = (y >= height * 0.52f && distToArchCenter >= width * 0.16f && distToArchCenter <= width * 0.26f);
                if (isBody || isArch) {
                    cols[y * width + x] = new Color(0.85f, 0.85f, 0.85f, 1f);
                } else {
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}
