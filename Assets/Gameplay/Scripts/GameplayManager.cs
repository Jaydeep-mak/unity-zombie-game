using UnityEngine;
using TMPro;

public class GameplayManager : MonoBehaviour {
    public static GameplayManager Instance { get; private set; }

    [Header("Base Health Settings")]
    [SerializeField] private int maxBaseHealth = 10;
    private int currentBaseHealth;

    [Header("HUD Settings")]
    [SerializeField] private int coins = 1000;
    public int Coins => coins;

    public bool UseCoins(int amount) {
        if (coins >= amount) {
            coins -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    [SerializeField] private int currentWave = 3;

    [Header("Stats")]
    private int zombiesKilled = 0;
    private int zombiesReached = 0;

    [Header("References")]
    [SerializeField] private ZombieSpawner spawner;

    [Header("UI Canvas References")]
    private GameObject hudCanvas;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI coinsText;
    private TextMeshProUGUI waveText;
    private GameObject gameOverPopup;
    private TextMeshProUGUI statsText;
    private System.Collections.Generic.List<UnityEngine.UI.Image> slotImages = new System.Collections.Generic.List<UnityEngine.UI.Image>();

    public int activeZombieCount = 0;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        currentBaseHealth = maxBaseHealth;
        Time.timeScale = 1f;

        CreateUI();
        UpdateUI();
    }

    public void RegisterZombie() {
        activeZombieCount++;
    }

    public void UnregisterZombie() {
        activeZombieCount = Mathf.Max(0, activeZombieCount - 1);
    }

    public void ZombieKilled() {
        zombiesKilled++;
    }

    public void ZombieReachedBase(GameObject zombie, int baseDamage = 1) {
        if (currentBaseHealth <= 0) return;

        zombiesReached++;
        currentBaseHealth = Mathf.Max(0, currentBaseHealth - baseDamage);
        UpdateUI();

        if (zombie != null) {
            Destroy(zombie);
        }

        if (currentBaseHealth <= 0) {
            TriggerGameOver();
        }
    }

    private void UpdateUI() {
        if (healthText != null) {
            healthText.text = $"❤️  {currentBaseHealth}/{maxBaseHealth}";
        }
        if (coinsText != null) {
            coinsText.text = $"💰  {coins}";
        }
        if (waveText != null) {
            waveText.text = $"🌊  Wave {currentWave}";
        }
    }

    public void SetCurrentWave(int wave) {
        currentWave = wave;
        UpdateUI();
    }

    private void TriggerGameOver() {
        // Stop WaveManager
        foreach (var wm in FindObjectsByType<WaveManager>(FindObjectsSortMode.None)) {
            wm.enabled = false;
        }

        // Stop spawning on all spawners
        foreach (var s in FindObjectsByType<ZombieSpawner>(FindObjectsSortMode.None)) {
            s.CancelInvoke();
            s.enabled = false;
        }

        // Stop zombie movement
        foreach (var z in FindObjectsByType<ZombieController>(FindObjectsSortMode.None)) {
            z.enabled = false;
            var rb = z.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;
        }

        // Stop plant attacks
        foreach (var p in FindObjectsByType<PlantBase>(FindObjectsSortMode.None)) {
            p.StopAllCoroutines();
            p.enabled = false;
        }

        // Show Game Over Popup
        if (gameOverPopup != null) {
            gameOverPopup.SetActive(true);
            if (statsText != null) {
                statsText.text = $"Zombies Killed: {zombiesKilled}\nZombies Reached Base: {zombiesReached}";
            }
        }
    }

    public void OnOkButtonClicked() {
        Time.timeScale = 0f;
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

    private Sprite CreateRoundedRectSprite(int width, int height, int radius, Color color) {
        return CreateRoundedRectGradientSprite(width, height, radius, color, color, color, 0);
    }

    private void CreateUI() {
        // Ensure EventSystem exists so that clicks are registered
        if (UnityEngine.EventSystems.EventSystem.current == null) {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 1. Create HUD Canvas
        hudCanvas = new GameObject("GameplayHUD");
        var canvas = hudCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        var scaler = hudCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        hudCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Curated Palette Colors (Glassmorphic)
        Color healthBottom = new Color(0.15f, 0.05f, 0.05f, 0.85f);
        Color healthTop = new Color(0.32f, 0.10f, 0.10f, 0.85f);
        Color healthBorder = new Color(0.90f, 0.30f, 0.30f, 1f);

        Color coinsBottom = new Color(0.15f, 0.12f, 0.05f, 0.85f);
        Color coinsTop = new Color(0.30f, 0.24f, 0.08f, 0.85f);
        Color coinsBorder = new Color(1.00f, 0.80f, 0.20f, 1f);

        Color waveBottom = new Color(0.05f, 0.10f, 0.18f, 0.85f);
        Color waveTop = new Color(0.10f, 0.22f, 0.35f, 0.85f);
        Color waveBorder = new Color(0.30f, 0.70f, 1.00f, 1f);

        Color settingsBottom = new Color(0.10f, 0.10f, 0.12f, 0.85f);
        Color settingsTop = new Color(0.22f, 0.22f, 0.25f, 0.85f);
        Color settingsBorder = new Color(0.70f, 0.70f, 0.75f, 1f);

        Color toolbarBottom = new Color(0.06f, 0.06f, 0.08f, 0.90f);
        Color toolbarTop = new Color(0.15f, 0.15f, 0.18f, 0.90f);
        Color toolbarBorder = new Color(0.25f, 0.60f, 0.25f, 1f);

        Color slotActiveBottom = new Color(0.10f, 0.25f, 0.10f, 0.85f);
        Color slotActiveTop = new Color(0.20f, 0.50f, 0.20f, 0.85f);
        Color slotActiveBorder = new Color(0.40f, 0.90f, 0.40f, 1f);

        Color slotLockedBottom = new Color(0.08f, 0.08f, 0.10f, 0.70f);
        Color slotLockedTop = new Color(0.16f, 0.16f, 0.20f, 0.70f);
        Color slotLockedBorder = new Color(0.35f, 0.35f, 0.40f, 0.70f);

        // 2. Base Health Pill (Top Left)
        var healthPill = new GameObject("HealthPill");
        healthPill.AddComponent<RectTransform>();
        healthPill.transform.SetParent(hudCanvas.transform, false);
        var hpImg = healthPill.AddComponent<UnityEngine.UI.Image>();
        hpImg.sprite = CreateRoundedRectGradientSprite(320, 80, 40, healthBottom, healthTop, healthBorder, 4);
        
        var hpRect = healthPill.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0f, 1f);
        hpRect.anchorMax = new Vector2(0f, 1f);
        hpRect.pivot = new Vector2(0f, 1f);
        hpRect.anchoredPosition = new Vector2(40f, -40f);
        hpRect.sizeDelta = new Vector2(320f, 80f);

        var hpTextGo = new GameObject("Text");
        hpTextGo.AddComponent<RectTransform>();
        hpTextGo.transform.SetParent(healthPill.transform, false);
        healthText = hpTextGo.AddComponent<TextMeshProUGUI>();
        healthText.fontSize = 36;
        healthText.fontStyle = FontStyles.Bold;
        healthText.color = Color.white;
        healthText.alignment = TextAlignmentOptions.Center;
        var hpTextRect = hpTextGo.GetComponent<RectTransform>();
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.sizeDelta = Vector2.zero;

        // 3. Coins Pill (Top Center Left)
        var coinsPill = new GameObject("CoinsPill");
        coinsPill.AddComponent<RectTransform>();
        coinsPill.transform.SetParent(hudCanvas.transform, false);
        var coinsImg = coinsPill.AddComponent<UnityEngine.UI.Image>();
        coinsImg.sprite = CreateRoundedRectGradientSprite(240, 80, 40, coinsBottom, coinsTop, coinsBorder, 4);

        var coinsRect = coinsPill.GetComponent<RectTransform>();
        coinsRect.anchorMin = new Vector2(0.5f, 1f);
        coinsRect.anchorMax = new Vector2(0.5f, 1f);
        coinsRect.pivot = new Vector2(0.5f, 1f);
        coinsRect.anchoredPosition = new Vector2(-150f, -40f); // Center offset left
        coinsRect.sizeDelta = new Vector2(240f, 80f);

        var coinsTextGo = new GameObject("Text");
        coinsTextGo.AddComponent<RectTransform>();
        coinsTextGo.transform.SetParent(coinsPill.transform, false);
        coinsText = coinsTextGo.AddComponent<TextMeshProUGUI>();
        coinsText.fontSize = 36;
        coinsText.fontStyle = FontStyles.Bold;
        coinsText.color = Color.white;
        coinsText.alignment = TextAlignmentOptions.Center;
        var coinsTextRect = coinsTextGo.GetComponent<RectTransform>();
        coinsTextRect.anchorMin = Vector2.zero;
        coinsTextRect.anchorMax = Vector2.one;
        coinsTextRect.sizeDelta = Vector2.zero;

        // 4. Wave Pill (Top Center Right)
        var wavePill = new GameObject("WavePill");
        wavePill.AddComponent<RectTransform>();
        wavePill.transform.SetParent(hudCanvas.transform, false);
        var waveImg = wavePill.AddComponent<UnityEngine.UI.Image>();
        waveImg.sprite = CreateRoundedRectGradientSprite(280, 80, 40, waveBottom, waveTop, waveBorder, 4);

        var waveRect = wavePill.GetComponent<RectTransform>();
        waveRect.anchorMin = new Vector2(0.5f, 1f);
        waveRect.anchorMax = new Vector2(0.5f, 1f);
        waveRect.pivot = new Vector2(0.5f, 1f);
        waveRect.anchoredPosition = new Vector2(150f, -40f); // Center offset right
        waveRect.sizeDelta = new Vector2(280f, 80f);

        var waveTextGo = new GameObject("Text");
        waveTextGo.AddComponent<RectTransform>();
        waveTextGo.transform.SetParent(wavePill.transform, false);
        waveText = waveTextGo.AddComponent<TextMeshProUGUI>();
        waveText.fontSize = 36;
        waveText.fontStyle = FontStyles.Bold;
        waveText.color = Color.white;
        waveText.alignment = TextAlignmentOptions.Center;
        var waveTextRect = waveTextGo.GetComponent<RectTransform>();
        waveTextRect.anchorMin = Vector2.zero;
        waveTextRect.anchorMax = Vector2.one;
        waveTextRect.sizeDelta = Vector2.zero;

        // 5. Settings Button (Top Right)
        var settingsBtn = new GameObject("SettingsButton");
        settingsBtn.AddComponent<RectTransform>();
        settingsBtn.transform.SetParent(hudCanvas.transform, false);
        var settingsImg = settingsBtn.AddComponent<UnityEngine.UI.Image>();
        settingsImg.sprite = CreateRoundedRectGradientSprite(80, 80, 40, settingsBottom, settingsTop, settingsBorder, 4);

        var settingsRect = settingsBtn.GetComponent<RectTransform>();
        settingsRect.anchorMin = new Vector2(1f, 1f);
        settingsRect.anchorMax = new Vector2(1f, 1f);
        settingsRect.pivot = new Vector2(1f, 1f);
        settingsRect.anchoredPosition = new Vector2(-40f, -40f);
        settingsRect.sizeDelta = new Vector2(80f, 80f);

        var settingsTextGo = new GameObject("Text");
        settingsTextGo.AddComponent<RectTransform>();
        settingsTextGo.transform.SetParent(settingsBtn.transform, false);
        var settingsText = settingsTextGo.AddComponent<TextMeshProUGUI>();
        settingsText.text = "⚙️";
        settingsText.fontSize = 36;
        settingsText.alignment = TextAlignmentOptions.Center;
        var settingsTextRect = settingsTextGo.GetComponent<RectTransform>();
        settingsTextRect.anchorMin = Vector2.zero;
        settingsTextRect.anchorMax = Vector2.one;
        settingsTextRect.sizeDelta = Vector2.zero;

        // 6. Bottom Plant Toolbar
        var toolbar = new GameObject("PlantToolbar");
        toolbar.AddComponent<RectTransform>();
        toolbar.transform.SetParent(hudCanvas.transform, false);
        var toolbarImg = toolbar.AddComponent<UnityEngine.UI.Image>();
        toolbarImg.sprite = CreateRoundedRectGradientSprite(900, 140, 30, toolbarBottom, toolbarTop, toolbarBorder, 5);

        var toolbarRect = toolbar.GetComponent<RectTransform>();
        toolbarRect.anchorMin = new Vector2(0.5f, 0f);
        toolbarRect.anchorMax = new Vector2(0.5f, 0f);
        toolbarRect.pivot = new Vector2(0.5f, 0f);
        toolbarRect.anchoredPosition = new Vector2(0f, 45f);
        toolbarRect.sizeDelta = new Vector2(900f, 140f);

        // Generate Plant Selection Slots: 🔥 ❄️ 🧪 💣 ⚡ 🌻
        string[] plantIcons = { "🔥", "❄️", "🧪", "💣", "⚡", "🌻" };
        bool[] isLocked = { false, true, true, true, true, true };
        float buttonSize = 110f;
        float spacing = 20f;
        float startX = -((plantIcons.Length - 1) * (buttonSize + spacing)) / 2f;

        slotImages.Clear();

        for (int i = 0; i < plantIcons.Length; i++) {
            var slotGo = new GameObject($"Slot_{i}");
            slotGo.AddComponent<RectTransform>();
            slotGo.transform.SetParent(toolbar.transform, false);
            var slotImg = slotGo.AddComponent<UnityEngine.UI.Image>();
            
            if (isLocked[i]) {
                slotImg.sprite = CreateRoundedRectGradientSprite(110, 110, 22, slotLockedBottom, slotLockedTop, slotLockedBorder, 4);
            } else {
                slotImg.sprite = CreateRoundedRectGradientSprite(110, 110, 22, slotActiveBottom, slotActiveTop, slotActiveBorder, 4);
            }

            slotImages.Add(slotImg);

            // Add Button component and click listener
            var slotBtn = slotGo.AddComponent<UnityEngine.UI.Button>();
            int index = i;
            slotBtn.onClick.AddListener(() => {
                if (PlantPlacementManager.Instance != null) {
                    PlantPlacementManager.Instance.SelectPlant(index);
                }
            });

            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(startX + i * (buttonSize + spacing), 0f);
            slotRect.sizeDelta = new Vector2(buttonSize, buttonSize);

            var iconTextGo = new GameObject("Icon");
            iconTextGo.AddComponent<RectTransform>();
            iconTextGo.transform.SetParent(slotGo.transform, false);
            var iconText = iconTextGo.AddComponent<TextMeshProUGUI>();
            iconText.text = plantIcons[i];
            iconText.fontSize = 52;
            iconText.alignment = TextAlignmentOptions.Center;
            
            var iconTextRect = iconTextGo.GetComponent<RectTransform>();
            iconTextRect.anchorMin = Vector2.zero;
            iconTextRect.anchorMax = Vector2.one;
            iconTextRect.sizeDelta = Vector2.zero;

            if (isLocked[i]) {
                iconText.alpha = 0.4f;
                var lockLabelGo = new GameObject("LockLabel");
                lockLabelGo.AddComponent<RectTransform>();
                lockLabelGo.transform.SetParent(slotGo.transform, false);
                var lockLabel = lockLabelGo.AddComponent<TextMeshProUGUI>();
                lockLabel.text = "🔒";
                lockLabel.fontSize = 24;
                lockLabel.alignment = TextAlignmentOptions.BottomRight;
                
                var lockRect = lockLabelGo.GetComponent<RectTransform>();
                lockRect.anchorMin = Vector2.zero;
                lockRect.anchorMax = Vector2.one;
                lockRect.anchoredPosition = new Vector2(-8f, 8f);
                lockRect.sizeDelta = Vector2.zero;
            }
        }

        // 7. Create Game Over Popup (Centered, inactive at start)
        gameOverPopup = new GameObject("GameOverPopup");
        gameOverPopup.AddComponent<RectTransform>();
        gameOverPopup.transform.SetParent(hudCanvas.transform, false);
        gameOverPopup.SetActive(false);

        var popupRect = gameOverPopup.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = new Vector2(700f, 500f);

        // Popup Background with gradient and red border
        var popupBg = new GameObject("Background");
        popupBg.AddComponent<RectTransform>();
        popupBg.transform.SetParent(gameOverPopup.transform, false);
        var bgImage = popupBg.AddComponent<UnityEngine.UI.Image>();
        
        Color popupBgBottom = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        Color popupBgTop = new Color(0.18f, 0.18f, 0.25f, 0.95f);
        Color popupBgBorder = new Color(0.90f, 0.20f, 0.20f, 1f);
        bgImage.sprite = CreateRoundedRectGradientSprite(700, 500, 40, popupBgBottom, popupBgTop, popupBgBorder, 6);
        
        var bgRect = popupBg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Game Over Title
        var titleGo = new GameObject("Title");
        titleGo.AddComponent<RectTransform>();
        titleGo.transform.SetParent(gameOverPopup.transform, false);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "GAME OVER";
        titleText.fontSize = 56;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.9f, 0.2f, 0.2f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;
        
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -50f);
        titleRect.sizeDelta = new Vector2(0f, 80f);

        // Stats Text
        var statsGo = new GameObject("Stats");
        statsGo.AddComponent<RectTransform>();
        statsGo.transform.SetParent(gameOverPopup.transform, false);
        statsText = statsGo.AddComponent<TextMeshProUGUI>();
        statsText.fontSize = 32;
        statsText.color = Color.white;
        statsText.lineSpacing = 15;
        statsText.alignment = TextAlignmentOptions.Center;
        
        var statsRect = statsGo.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0f, 0.5f);
        statsRect.anchorMax = new Vector2(1f, 0.5f);
        statsRect.pivot = new Vector2(0.5f, 0.5f);
        statsRect.anchoredPosition = new Vector2(0f, 20f);
        statsRect.sizeDelta = new Vector2(0f, 160f);

        // OK Button
        var buttonGo = new GameObject("OkButton");
        buttonGo.AddComponent<RectTransform>();
        buttonGo.transform.SetParent(gameOverPopup.transform, false);
        var buttonImage = buttonGo.AddComponent<UnityEngine.UI.Image>();
        
        Color okBtnBottom = new Color(0.60f, 0.10f, 0.10f, 1f);
        Color okBtnTop = new Color(0.90f, 0.20f, 0.20f, 1f);
        Color okBtnBorder = new Color(1.00f, 0.50f, 0.50f, 1f);
        buttonImage.sprite = CreateRoundedRectGradientSprite(240, 80, 25, okBtnBottom, okBtnTop, okBtnBorder, 4);
        
        var button = buttonGo.AddComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(OnOkButtonClicked);

        var btnRect = buttonGo.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 50f);
        btnRect.sizeDelta = new Vector2(240f, 80f);

        // OK Button Text
        var btnTextGo = new GameObject("Text");
        btnTextGo.AddComponent<RectTransform>();
        btnTextGo.transform.SetParent(buttonGo.transform, false);
        var btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnText.text = "OK";
        btnText.fontSize = 32;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        
        var btnTextRect = btnTextGo.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
    }

    public void SetSlotHighlight(int activeIndex) {
        for (int i = 0; i < slotImages.Count; i++) {
            if (slotImages[i] == null) continue;
            if (i == activeIndex) {
                slotImages[i].color = new Color(1f, 1f, 1f, 1f);
                var outline = slotImages[i].gameObject.GetComponent<UnityEngine.UI.Outline>();
                if (outline == null) {
                    outline = slotImages[i].gameObject.AddComponent<UnityEngine.UI.Outline>();
                    outline.effectColor = new Color(1f, 0.9f, 0.2f, 0.8f);
                    outline.effectDistance = new Vector2(4f, 4f);
                }
                outline.enabled = true;
            } else {
                slotImages[i].color = new Color(0.85f, 0.85f, 0.85f, 1f);
                var outline = slotImages[i].gameObject.GetComponent<UnityEngine.UI.Outline>();
                if (outline != null) {
                    outline.enabled = false;
                }
            }
        }
    }
}