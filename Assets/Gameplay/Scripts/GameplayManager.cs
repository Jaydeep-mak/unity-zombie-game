using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
            if (coinsPillInstance != null) {
                if (coinPulseCoroutine != null) StopCoroutine(coinPulseCoroutine);
                coinPulseCoroutine = StartCoroutine(PulsePill(coinsPillInstance.transform, 1.15f));
            }
            return true;
        }
        return false;
    }

    public void AddCoins(int amount) {
        coins += amount;
        coinsEarned += amount;
        UpdateUI();
        if (coinsPillInstance != null) {
            if (coinPulseCoroutine != null) StopCoroutine(coinPulseCoroutine);
            coinPulseCoroutine = StartCoroutine(PulsePill(coinsPillInstance.transform, 1.15f));
        }
        ShowCoinGain(amount);
    }

    [SerializeField] private int currentWave = 1;

    [Header("Stats")]
    private int zombiesKilled = 0;
    private int zombiesReached = 0;
    private int plantsPlaced = 0;
    private int coinsEarned = 0;

    public int PlantsPlaced => plantsPlaced;
    public int CoinsEarned => coinsEarned;

    public void IncrementPlantsPlaced() {
        plantsPlaced++;
    }

    [Header("References")]
    [SerializeField] private ZombieSpawner spawner;
    [SerializeField] private GameObject plantPrefab;

    [Header("HUD Custom Sprites")]
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Sprite waveSprite;
    [SerializeField] private Sprite lockSprite;
    [SerializeField] private Sprite pauseSprite;

    [Header("UI Canvas References")]
    private GameObject hudCanvas;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI coinsText;
    private TextMeshProUGUI waveText;
    private GameObject gameOverPopup;
    private GameObject pausePopup;
    
    private GameObject healthPillInstance;
    private GameObject coinsPillInstance;
    private Coroutine coinPulseCoroutine;
    private Coroutine healthPulseCoroutine;
    
    private List<PlantCard> plantCards = new List<PlantCard>();
    private bool isPaused = false;

    public int activeZombieCount = 0;

    private Sprite activeHeartSprite;
    private Sprite activeCoinSprite;
    private Sprite activeWaveSprite;
    private Sprite activeLockSprite;
    private Sprite activePauseSprite;

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

    private void Update() {
        UpdateAffordability();

        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Pause)) {
            TogglePause();
        }
    }

    public void RegisterZombie() {
        activeZombieCount++;
    }

    public void UnregisterZombie() {
        activeZombieCount = Mathf.Max(0, activeZombieCount - 1);
    }

    public void ZombieKilled(int coinReward = 10) {
        zombiesKilled++;
        AddCoins(coinReward);
    }

    public void ZombieReachedBase(GameObject zombie, int baseDamage = 1) {
        if (currentBaseHealth <= 0) return;

        zombiesReached++;
        currentBaseHealth = Mathf.Max(0, currentBaseHealth - baseDamage);
        UpdateUI();

        // Flash health pill red and pulse it
        if (healthPillInstance != null) {
            if (healthPulseCoroutine != null) StopCoroutine(healthPulseCoroutine);
            healthPulseCoroutine = StartCoroutine(PulsePill(healthPillInstance.transform, 1.25f));
            StartCoroutine(FlashPillRed(healthPillInstance.GetComponent<UnityEngine.UI.Image>()));
        }

        // Screen flash & camera shake juice
        StartCoroutine(ScreenFlashRoutine(0.25f, new Color(0.9f, 0.1f, 0.1f), 0.35f));
        StartCoroutine(CameraShakeRoutine(0.25f, 0.12f));

        if (zombie != null) {
            Destroy(zombie);
        }

        if (currentBaseHealth <= 0) {
            TriggerGameOver();
        }
    }

    private void UpdateUI() {
        if (healthText != null) {
            healthText.text = $"{currentBaseHealth}/{maxBaseHealth}";
        }
        if (coinsText != null) {
            coinsText.text = $"{coins}";
        }
        if (waveText != null) {
            waveText.text = $"Wave {currentWave}";
        }
    }

    public void SetCurrentWave(int wave) {
        currentWave = wave;
        UpdateUI();
    }

    public void TriggerVictory() {
        StartCoroutine(GameOverSequenceRoutine(true));
    }

    private void TriggerGameOver() {
        StartCoroutine(GameOverSequenceRoutine(false));
    }

    private IEnumerator GameOverSequenceRoutine(bool isVictory = false) {
        // Stop active gameplay components, but don't pause yet
        foreach (var wm in FindObjectsByType<WaveManager>(FindObjectsSortMode.None)) {
            wm.enabled = false;
        }

        foreach (var s in FindObjectsByType<ZombieSpawner>(FindObjectsSortMode.None)) {
            s.CancelInvoke();
            s.enabled = false;
        }

        foreach (var p in FindObjectsByType<PlantBase>(FindObjectsSortMode.None)) {
            p.StopAllCoroutines();
            p.enabled = false;
        }

        yield return new WaitForSeconds(1.0f); // 1 sec delay to show hit flash & impact

        // Stop zombie movement
        foreach (var z in FindObjectsByType<ZombieController>(FindObjectsSortMode.None)) {
            z.enabled = false;
            var rb = z.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;
        }

        Time.timeScale = 0f;

        if (gameOverPopup != null) {
            gameOverPopup.SetActive(true);

            // Fetch references
            var overlayImg = gameOverPopup.transform.Find("Overlay")?.GetComponent<UnityEngine.UI.Image>();
            var panel = gameOverPopup.transform.Find("Background");
            var title = panel?.Find("Title");
            var stats = panel?.Find("Stats");
            var restartBtn = panel?.Find("RestartBtn");
            var mainMenuBtn = panel?.Find("MainMenuBtn");

            // Setup initial values
            UpdateGameOverStats(isVictory);

            // Set everything to inactive / invisible for step-by-step entry
            if (overlayImg != null) overlayImg.color = new Color(0f, 0f, 0f, 0f);
            if (panel != null) panel.localScale = Vector3.zero;
            if (title != null) title.localScale = Vector3.zero;
            if (restartBtn != null) restartBtn.localScale = Vector3.zero;
            if (mainMenuBtn != null) mainMenuBtn.localScale = Vector3.zero;

            List<Transform> statBlocks = new List<Transform>();
            if (stats != null) {
                foreach (Transform child in stats) {
                    child.localScale = Vector3.zero;
                    statBlocks.Add(child);
                }
            }

            // Phase 1: Fade overlay and Scale panel
            float elapsed = 0f;
            float duration = 0.45f;
            while (elapsed < duration) {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float tEase = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
                
                if (overlayImg != null) {
                    overlayImg.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.75f, t));
                }
                if (panel != null) {
                    panel.localScale = Vector3.one * Mathf.Lerp(0f, 1.03f, tEase);
                }
                yield return null;
            }
            if (panel != null) panel.localScale = Vector3.one;
            if (overlayImg != null) overlayImg.color = new Color(0f, 0f, 0f, 0.75f);

            // Phase 2: Title Pop In
            elapsed = 0f;
            duration = 0.3f;
            while (elapsed < duration) {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float tBounce = Mathf.Sin(t * Mathf.PI * 0.75f) * 0.15f + t;
                tBounce = Mathf.Clamp01(tBounce);

                if (title != null) title.localScale = Vector3.one * tBounce;
                yield return null;
            }
            if (title != null) title.localScale = Vector3.one;

            // Phase 3: Stat Blocks Cascade Pop
            for (int i = 0; i < statBlocks.Count; i++) {
                var block = statBlocks[i];
                elapsed = 0f;
                duration = 0.18f;
                while (elapsed < duration) {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / duration;
                    float tBounce = Mathf.Sin(t * Mathf.PI * 0.75f) * 0.15f + t;
                    tBounce = Mathf.Clamp01(tBounce);
                    if (block != null) block.localScale = Vector3.one * tBounce;
                    yield return null;
                }
                if (block != null) block.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(0.06f);
            }

            // Phase 4: Buttons scale up
            elapsed = 0f;
            duration = 0.25f;
            while (elapsed < duration) {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float tEase = 1f - Mathf.Pow(1f - t, 2f); // Ease out quad
                if (restartBtn != null) restartBtn.localScale = Vector3.one * tEase;
                if (mainMenuBtn != null) mainMenuBtn.localScale = Vector3.one * tEase;
                yield return null;
            }
            if (restartBtn != null) restartBtn.localScale = Vector3.one;
            if (mainMenuBtn != null) mainMenuBtn.localScale = Vector3.one;
        }
    }

    private void UpdateGameOverStats(bool isVictory) {
        var titleTextComp = gameOverPopup.transform.Find("Background/Title")?.GetComponent<TextMeshProUGUI>();
        if (titleTextComp != null) {
            titleTextComp.text = isVictory ? "VICTORY!" : "GAME OVER";
            titleTextComp.color = isVictory ? new Color(0.3f, 1f, 0.4f, 1f) : new Color(1f, 0.3f, 0.2f, 1f);
        }

        var bgImageComp = gameOverPopup.transform.Find("Background")?.GetComponent<UnityEngine.UI.Image>();
        if (bgImageComp != null) {
            Color gameOverBgBottom = new Color(0.08f, 0.16f, 0.10f, 0.98f);
            Color gameOverBgTop = new Color(0.15f, 0.30f, 0.18f, 0.98f);
            Color gameOverBgBorder = isVictory ? new Color(0.3f, 1f, 0.4f, 1f) : new Color(0.85f, 0.75f, 0.25f, 1f);
            bgImageComp.sprite = CreateRoundedRectGradientSprite(640, 520, 36, gameOverBgBottom, gameOverBgTop, gameOverBgBorder, 5);
        }

        int survived = isVictory ? currentWave : (currentWave - 1);
        survived = Mathf.Max(0, survived);

        var statsTransform = gameOverPopup.transform.Find("Background/Stats");
        if (statsTransform != null) {
            for (int i = statsTransform.childCount - 1; i >= 0; i--) {
                Destroy(statsTransform.GetChild(i).gameObject);
            }

            string[] lines = {
                $"🌊 Waves Completed: <color=#50E3C2>{survived}</color>",
                $"☠ Zombies Killed: <color=#FF4A4A>{zombiesKilled}</color>",
                $"💰 Coins Earned: <color=#FFD700>{coinsEarned}</color>"
            };

            for (int j = 0; j < 3; j++) {
                var blockGo = new GameObject($"StatRow_{j}");
                var blockRect = blockGo.AddComponent<RectTransform>();
                blockGo.transform.SetParent(statsTransform, false);
                blockRect.anchorMin = new Vector2(0.5f, 0.5f);
                blockRect.anchorMax = new Vector2(0.5f, 0.5f);
                blockRect.pivot = new Vector2(0.5f, 0.5f);
                blockRect.anchoredPosition = new Vector2(0f, 60f - j * 60f); // Spaced vertically
                blockRect.sizeDelta = new Vector2(500f, 50f);

                var text = blockGo.AddComponent<TextMeshProUGUI>();
                text.text = lines[j];
                text.fontSize = 28;
                text.fontStyle = FontStyles.Bold;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    public void TogglePause() {
        if (currentBaseHealth <= 0) return; // Can't pause if dead
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePopup != null) {
            if (isPaused) {
                pausePopup.SetActive(true);
                StopAllCoroutines();
                StartCoroutine(PopupScaleIn(pausePopup.transform));
            } else {
                pausePopup.SetActive(false);
            }
        }
    }

    public void OnRestartButtonClicked() {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuButtonClicked() {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("GardenGuardians_MainMenu");
    }

    public bool IsSlotOnCooldown(int index) {
        if (index >= 0 && index < plantCards.Count && plantCards[index] != null) {
            return plantCards[index].IsOnCooldown();
        }
        return false;
    }

    public void StartSlotCooldown(int index, float duration) {
        if (index >= 0 && index < plantCards.Count && plantCards[index] != null) {
            plantCards[index].StartCooldown(duration);
        }
    }

    private void UpdateAffordability() {
        if (plantCards == null || PlantPlacementManager.Instance == null) return;
        for (int i = 0; i < plantCards.Count; i++) {
            if (plantCards[i] != null && !PlantPlacementManager.Instance.IsSlotLocked(i)) {
                int cost = PlantPlacementManager.Instance.GetSlotCost(i);
                plantCards[i].SetAffordable(coins >= cost);
            }
        }
    }

    public Sprite CreateRoundedRectGradientSprite(int width, int height, int radius, Color bottomColor, Color topColor, Color borderColor, int borderWidth) {
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

    private Sprite CreateHeartSprite(int width, int height) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        float cx = width / 2f;
        float cy = height / 2.3f;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float nx = (x - cx) / (width * 0.42f);
                float ny = (y - cy) / (height * 0.42f);
                float formula = nx * nx + ny * ny - 1f;
                float lhs = formula * formula * formula - nx * nx * ny * ny * ny;
                if (lhs <= 0f) {
                    cols[y * width + x] = new Color(0.9f, 0.15f, 0.15f, 1f);
                } else {
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateCoinSprite(int width, int height) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        float cx = width / 2f;
        float cy = height / 2f;
        float rOuter = width * 0.45f;
        float rInner = width * 0.32f;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= rInner) {
                    cols[y * width + x] = new Color(1f, 0.85f, 0.18f, 1f);
                } else if (dist <= rOuter) {
                    cols[y * width + x] = new Color(0.82f, 0.62f, 0.08f, 1f);
                } else {
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateWaveSprite(int width, int height) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        float cx = width / 2f;
        float cy = height / 2f;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float dx = x - cx;
                float dy = y - cy;
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= width * 0.42f) {
                    cols[y * width + x] = new Color(0.2f, 0.65f, 0.95f, 1f);
                } else {
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
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
                    cols[y * width + x] = new Color(0.78f, 0.78f, 0.78f, 1f);
                } else {
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreatePauseSprite(int width, int height) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        float cx = width / 2f;
        float cy = height / 2f;
        float barWidth = width * 0.12f;
        float barHeight = height * 0.42f;
        float spacing = width * 0.12f;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float dx = x - cx;
                float dy = y - cy;
                bool inLeftBar = Mathf.Abs(dx + (barWidth + spacing) / 2f) <= barWidth / 2f && Mathf.Abs(dy) <= barHeight / 2f;
                bool inRightBar = Mathf.Abs(dx - (barWidth + spacing) / 2f) <= barWidth / 2f && Mathf.Abs(dy) <= barHeight / 2f;

                if (inLeftBar || inRightBar) {
                    cols[y * width + x] = Color.white;
                } else {
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void CreateUI() {
        if (UnityEngine.EventSystems.EventSystem.current == null) {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Initialize HUD sprites (prioritizing Inspector)
        activeHeartSprite = heartSprite != null ? heartSprite : CreateHeartSprite(64, 64);
        activeCoinSprite = coinSprite != null ? coinSprite : CreateCoinSprite(64, 64);
        activeWaveSprite = waveSprite != null ? waveSprite : CreateWaveSprite(64, 64);
        activeLockSprite = lockSprite != null ? lockSprite : CreateLockSprite(64, 64);
        activePauseSprite = pauseSprite != null ? pauseSprite : CreatePauseSprite(64, 64);

        // 1. Create HUD Canvas
        var oldHud = GameObject.Find("GameplayHUD");
        if (oldHud != null) {
            if (Application.isPlaying) {
                Destroy(oldHud);
            } else {
                DestroyImmediate(oldHud);
            }
        }
        hudCanvas = new GameObject("GameplayHUD");
        var canvas = hudCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        var scaler = hudCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        hudCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Glassmorphic Palette Colors
        Color healthBottom = new Color(0.15f, 0.05f, 0.05f, 0.85f);
        Color healthTop = new Color(0.32f, 0.10f, 0.10f, 0.85f);
        Color healthBorder = new Color(0.90f, 0.30f, 0.30f, 1f);

        Color coinsBottom = new Color(0.15f, 0.12f, 0.05f, 0.85f);
        Color coinsTop = new Color(0.30f, 0.24f, 0.08f, 0.85f);
        Color coinsBorder = new Color(1.00f, 0.80f, 0.20f, 1f);

        Color waveBottom = new Color(0.05f, 0.10f, 0.18f, 0.85f);
        Color waveTop = new Color(0.10f, 0.22f, 0.35f, 0.85f);
        Color waveBorder = new Color(0.30f, 0.70f, 1.00f, 1f);

        Color pauseBottom = new Color(0.10f, 0.10f, 0.12f, 0.85f);
        Color pauseTop = new Color(0.22f, 0.22f, 0.25f, 0.85f);
        Color pauseBorder = new Color(0.70f, 0.70f, 0.75f, 1f);

        Color toolbarBottom = new Color(0.04f, 0.08f, 0.05f, 0.90f);
        Color toolbarTop = new Color(0.10f, 0.20f, 0.12f, 0.90f);
        Color toolbarBorder = new Color(0.25f, 0.60f, 0.30f, 1f);

        Color slotActiveBottom = new Color(0.12f, 0.16f, 0.12f, 0.85f);
        Color slotActiveTop = new Color(0.24f, 0.32f, 0.24f, 0.85f);
        Color slotActiveBorder = new Color(0.40f, 0.75f, 0.40f, 1f);

        Color slotLockedBottom = new Color(0.08f, 0.08f, 0.10f, 0.70f);
        Color slotLockedTop = new Color(0.16f, 0.16f, 0.20f, 0.70f);
        Color slotLockedBorder = new Color(0.35f, 0.35f, 0.40f, 0.70f);

        // 2. Base Health Pill (Top Left)
        healthPillInstance = new GameObject("HealthPill");
        healthPillInstance.AddComponent<RectTransform>();
        healthPillInstance.transform.SetParent(hudCanvas.transform, false);
        var hpImg = healthPillInstance.AddComponent<UnityEngine.UI.Image>();
        hpImg.sprite = CreateRoundedRectGradientSprite(260, 75, 37, healthBottom, healthTop, healthBorder, 4);
        
        var hpRect = healthPillInstance.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0f, 1f);
        hpRect.anchorMax = new Vector2(0f, 1f);
        hpRect.pivot = new Vector2(0f, 1f);
        hpRect.anchoredPosition = new Vector2(40f, -40f);
        hpRect.sizeDelta = new Vector2(260f, 75f);

        var hpIconGo = new GameObject("HeartIcon");
        var hpIconRect = hpIconGo.AddComponent<RectTransform>();
        hpIconGo.transform.SetParent(healthPillInstance.transform, false);
        var hpIconImg = hpIconGo.AddComponent<UnityEngine.UI.Image>();
        hpIconImg.sprite = activeHeartSprite;
        hpIconRect.anchorMin = new Vector2(0f, 0.5f);
        hpIconRect.anchorMax = new Vector2(0f, 0.5f);
        hpIconRect.pivot = new Vector2(0f, 0.5f);
        hpIconRect.anchoredPosition = new Vector2(20f, 0f);
        hpIconRect.sizeDelta = new Vector2(42f, 42f);

        var hpTextGo = new GameObject("Text");
        var hpTextRect = hpTextGo.AddComponent<RectTransform>();
        hpTextGo.transform.SetParent(healthPillInstance.transform, false);
        healthText = hpTextGo.AddComponent<TextMeshProUGUI>();
        healthText.fontSize = 32;
        healthText.fontStyle = FontStyles.Bold;
        healthText.color = Color.white;
        healthText.alignment = TextAlignmentOptions.MidlineLeft;
        
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.pivot = new Vector2(0.5f, 0.5f);
        hpTextRect.offsetMin = new Vector2(75f, 0f);
        hpTextRect.offsetMax = new Vector2(-20f, 0f);

        // 3. Coins Pill (Top Center)
        coinsPillInstance = new GameObject("CoinsPill");
        coinsPillInstance.AddComponent<RectTransform>();
        coinsPillInstance.transform.SetParent(hudCanvas.transform, false);
        var coinsImg = coinsPillInstance.AddComponent<UnityEngine.UI.Image>();
        coinsImg.sprite = CreateRoundedRectGradientSprite(260, 75, 37, coinsBottom, coinsTop, coinsBorder, 4);

        var coinsRect = coinsPillInstance.GetComponent<RectTransform>();
        coinsRect.anchorMin = new Vector2(0.5f, 1f);
        coinsRect.anchorMax = new Vector2(0.5f, 1f);
        coinsRect.pivot = new Vector2(0.5f, 1f);
        coinsRect.anchoredPosition = new Vector2(0f, -40f);
        coinsRect.sizeDelta = new Vector2(260f, 75f);

        var coinsIconGo = new GameObject("CoinIcon");
        var coinsIconRect = coinsIconGo.AddComponent<RectTransform>();
        coinsIconGo.transform.SetParent(coinsPillInstance.transform, false);
        var coinsIconImg = coinsIconGo.AddComponent<UnityEngine.UI.Image>();
        coinsIconImg.sprite = activeCoinSprite;
        coinsIconRect.anchorMin = new Vector2(0f, 0.5f);
        coinsIconRect.anchorMax = new Vector2(0f, 0.5f);
        coinsIconRect.pivot = new Vector2(0f, 0.5f);
        coinsIconRect.anchoredPosition = new Vector2(20f, 0f);
        coinsIconRect.sizeDelta = new Vector2(42f, 42f);

        var coinsTextGo = new GameObject("Text");
        var coinsTextRect = coinsTextGo.AddComponent<RectTransform>();
        coinsTextGo.transform.SetParent(coinsPillInstance.transform, false);
        coinsText = coinsTextGo.AddComponent<TextMeshProUGUI>();
        coinsText.fontSize = 32;
        coinsText.fontStyle = FontStyles.Bold;
        coinsText.color = Color.white;
        coinsText.alignment = TextAlignmentOptions.MidlineLeft;
        
        coinsTextRect.anchorMin = Vector2.zero;
        coinsTextRect.anchorMax = Vector2.one;
        coinsTextRect.pivot = new Vector2(0.5f, 0.5f);
        coinsTextRect.offsetMin = new Vector2(75f, 0f);
        coinsTextRect.offsetMax = new Vector2(-20f, 0f);

        // 4. Wave Pill (Top Right, shifted left for pause button)
        var wavePill = new GameObject("WavePill");
        wavePill.AddComponent<RectTransform>();
        wavePill.transform.SetParent(hudCanvas.transform, false);
        var waveImg = wavePill.AddComponent<UnityEngine.UI.Image>();
        waveImg.sprite = CreateRoundedRectGradientSprite(220, 75, 37, waveBottom, waveTop, waveBorder, 4);

        var waveRect = wavePill.GetComponent<RectTransform>();
        waveRect.anchorMin = new Vector2(1f, 1f);
        waveRect.anchorMax = new Vector2(1f, 1f);
        waveRect.pivot = new Vector2(1f, 1f);
        waveRect.anchoredPosition = new Vector2(-190f, -40f); // Spaced nicely from Pause
        waveRect.sizeDelta = new Vector2(220f, 75f);

        var waveIconGo = new GameObject("WaveIcon");
        var waveIconRect = waveIconGo.AddComponent<RectTransform>();
        waveIconGo.transform.SetParent(wavePill.transform, false);
        var waveIconImg = waveIconGo.AddComponent<UnityEngine.UI.Image>();
        waveIconImg.sprite = activeWaveSprite;
        waveIconRect.anchorMin = new Vector2(0f, 0.5f);
        waveIconRect.anchorMax = new Vector2(0f, 0.5f);
        waveIconRect.pivot = new Vector2(0f, 0.5f);
        waveIconRect.anchoredPosition = new Vector2(20f, 0f);
        waveIconRect.sizeDelta = new Vector2(42f, 42f);

        var waveTextGo = new GameObject("Text");
        var waveTextRect = waveTextGo.AddComponent<RectTransform>();
        waveTextGo.transform.SetParent(wavePill.transform, false);
        waveText = waveTextGo.AddComponent<TextMeshProUGUI>();
        waveText.fontSize = 30;
        waveText.fontStyle = FontStyles.Bold;
        waveText.color = Color.white;
        waveText.alignment = TextAlignmentOptions.MidlineLeft;
        
        waveTextRect.anchorMin = Vector2.zero;
        waveTextRect.anchorMax = Vector2.one;
        waveTextRect.pivot = new Vector2(0.5f, 0.5f);
        waveTextRect.offsetMin = new Vector2(75f, 0f);
        waveTextRect.offsetMax = new Vector2(-20f, 0f);

        // 5. Pause Button (Top Right)
        var pauseBtn = new GameObject("PauseButton");
        pauseBtn.AddComponent<RectTransform>();
        pauseBtn.transform.SetParent(hudCanvas.transform, false);
        var pauseImg = pauseBtn.AddComponent<UnityEngine.UI.Image>();
        pauseImg.sprite = CreateRoundedRectGradientSprite(75, 75, 37, pauseBottom, pauseTop, pauseBorder, 4);

        var pauseRect = pauseBtn.GetComponent<RectTransform>();
        pauseRect.anchorMin = new Vector2(1f, 1f);
        pauseRect.anchorMax = new Vector2(1f, 1f);
        pauseRect.pivot = new Vector2(1f, 1f);
        pauseRect.anchoredPosition = new Vector2(-40f, -40f);
        pauseRect.sizeDelta = new Vector2(75f, 75f);

        var pauseIconGo = new GameObject("PauseIcon");
        var pauseIconRect = pauseIconGo.AddComponent<RectTransform>();
        pauseIconGo.transform.SetParent(pauseBtn.transform, false);
        var pauseIconImg = pauseIconGo.AddComponent<UnityEngine.UI.Image>();
        pauseIconImg.sprite = activePauseSprite;
        pauseIconRect.anchorMin = new Vector2(0.5f, 0.5f);
        pauseIconRect.anchorMax = new Vector2(0.5f, 0.5f);
        pauseIconRect.pivot = new Vector2(0.5f, 0.5f);
        pauseIconRect.anchoredPosition = Vector2.zero;
        pauseIconRect.sizeDelta = new Vector2(32f, 32f);

        var btn = pauseBtn.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(TogglePause);
        pauseBtn.AddComponent<UIButtonEffects>();

        // 6. Bottom Plant Toolbar
        var toolbar = new GameObject("PlantToolbar");
        toolbar.AddComponent<RectTransform>();
        toolbar.transform.SetParent(hudCanvas.transform, false);
        
        var ppm = GetComponent<PlantPlacementManager>();
        int slotsCount = ppm != null ? ppm.SlotsCount : 6;
        float toolbarWidth = slotsCount * 125f + 35f;
        var toolbarImg = toolbar.AddComponent<UnityEngine.UI.Image>();
        toolbarImg.sprite = CreateRoundedRectGradientSprite(Mathf.RoundToInt(toolbarWidth), 170, 30, toolbarBottom, toolbarTop, toolbarBorder, 5);

        var toolbarRect = toolbar.GetComponent<RectTransform>();
        toolbarRect.anchorMin = new Vector2(0.5f, 0f);
        toolbarRect.anchorMax = new Vector2(0.5f, 0f);
        toolbarRect.pivot = new Vector2(0.5f, 0f);
        toolbarRect.anchoredPosition = new Vector2(0f, 30f);
        toolbarRect.sizeDelta = new Vector2(toolbarWidth, 170f);

        // Generate Plant Cards dynamically
        float cardWidth = 110f;
        float cardHeight = 135f;
        float spacing = 15f;
        float startX = -((slotsCount - 1) * (cardWidth + spacing)) / 2f;

        plantCards.Clear();

        for (int i = 0; i < slotsCount; i++) {
            bool locked = ppm != null ? ppm.IsSlotLocked(i) : false;
            string fullName = ppm != null ? ppm.GetSlotName(i) : "Plant";
            int cost = ppm != null ? ppm.GetSlotCost(i) : 100;
            Color slotTint = ppm != null ? ppm.GetSlotTintColor(i) : Color.white;

            string cleanName, emoji;
            ParseNameAndIcon(fullName, out cleanName, out emoji);

            var slotGo = new GameObject($"Slot_{i}");
            slotGo.AddComponent<RectTransform>();
            slotGo.transform.SetParent(toolbar.transform, false);
            
            var cardRect = slotGo.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), 0f);
            cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);

            // Card BG
            var cardBgImg = slotGo.AddComponent<UnityEngine.UI.Image>();
            if (locked) {
                cardBgImg.sprite = CreateRoundedRectGradientSprite(110, 135, 22, slotLockedBottom, slotLockedTop, slotLockedBorder, 4);
            } else {
                cardBgImg.sprite = CreateRoundedRectGradientSprite(110, 135, 22, slotActiveBottom, slotActiveTop, slotActiveBorder, 4);
            }

            // Selection Glow Outline (radius 27, sizing 124x149)
            var glowGo = new GameObject("SelectionGlow");
            glowGo.AddComponent<RectTransform>();
            glowGo.transform.SetParent(slotGo.transform, false);
            var glowImg = glowGo.AddComponent<UnityEngine.UI.Image>();
            glowImg.sprite = CreateRoundedRectGradientSprite(124, 149, 27, new Color(0,0,0,0), new Color(0,0,0,0), new Color(1f, 0.85f, 0.2f, 1f), 6);
            var glowRect = glowGo.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.sizeDelta = new Vector2(14f, 14f);

            // Icon Image (procedurally using the actual custom plant sprite)
            var iconGo = new GameObject("Icon");
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconGo.transform.SetParent(slotGo.transform, false);
            var iconImg = iconGo.AddComponent<UnityEngine.UI.Image>();
            
            Sprite plantSprite = PlantVisuals.GetPlantSprite(fullName);
            if (plantSprite == null && plantPrefab != null) {
                var sr = plantPrefab.GetComponent<SpriteRenderer>();
                if (sr != null) {
                    plantSprite = sr.sprite;
                }
            }
            iconImg.sprite = plantSprite;
            iconImg.color = plantSprite != null ? Color.white : slotTint;

            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, -40f);
            iconRect.sizeDelta = new Vector2(60f, 60f);

            // Name Label
            var nameGo = new GameObject("NameLabel");
            nameGo.AddComponent<RectTransform>();
            nameGo.transform.SetParent(slotGo.transform, false);
            var nameText = nameGo.AddComponent<TextMeshProUGUI>();
            nameText.text = cleanName;
            nameText.fontSize = 14;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Center;

            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.anchoredPosition = new Vector2(0f, -25f);
            nameRect.sizeDelta = new Vector2(100f, 25f);

            // Cost Label
            var costGo = new GameObject("CostLabel");
            costGo.AddComponent<RectTransform>();
            costGo.transform.SetParent(slotGo.transform, false);
            var costText = costGo.AddComponent<TextMeshProUGUI>();
            costText.text = $"Cost: {cost}";
            costText.fontSize = 18;
            costText.fontStyle = FontStyles.Bold;
            costText.color = new Color(1f, 0.85f, 0.15f, 1f);
            costText.alignment = TextAlignmentOptions.Center;

            var costRect = costGo.GetComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0.5f, 0f);
            costRect.anchorMax = new Vector2(0.5f, 0f);
            costRect.pivot = new Vector2(0.5f, 0.5f);
            costRect.anchoredPosition = new Vector2(0f, 15f);
            costRect.sizeDelta = new Vector2(100f, 25f);

            // Lock Overlay (utilizing procedural lock image)
            GameObject lockOverlayGo = new GameObject("LockOverlay");
            lockOverlayGo.AddComponent<RectTransform>();
            lockOverlayGo.transform.SetParent(slotGo.transform, false);
            var lockBg = lockOverlayGo.AddComponent<UnityEngine.UI.Image>();
            lockBg.sprite = CreateRoundedRectSprite(110, 135, 22, new Color(0.05f, 0.05f, 0.05f, 0.65f));
            var lockOverlayRect = lockOverlayGo.GetComponent<RectTransform>();
            lockOverlayRect.anchorMin = Vector2.zero;
            lockOverlayRect.anchorMax = Vector2.one;
            lockOverlayRect.sizeDelta = Vector2.zero;

            var lockIconGo = new GameObject("LockIcon");
            var lockIconRect = lockIconGo.AddComponent<RectTransform>();
            lockIconGo.transform.SetParent(lockOverlayGo.transform, false);
            var lockIconImg = lockIconGo.AddComponent<UnityEngine.UI.Image>();
            lockIconImg.sprite = activeLockSprite;
            lockIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockIconRect.pivot = new Vector2(0.5f, 0.5f);
            lockIconRect.anchoredPosition = Vector2.zero;
            lockIconRect.sizeDelta = new Vector2(36f, 36f);

            // Cooldown Overlay (translucent vertical wipe)
            var cdOvlGo = new GameObject("CooldownOverlay");
            cdOvlGo.AddComponent<RectTransform>();
            cdOvlGo.transform.SetParent(slotGo.transform, false);
            var cdImg = cdOvlGo.AddComponent<UnityEngine.UI.Image>();
            cdImg.sprite = CreateRoundedRectSprite(110, 135, 22, new Color(0f, 0f, 0f, 0.65f));
            var cdRect = cdOvlGo.GetComponent<RectTransform>();
            cdRect.anchorMin = Vector2.zero;
            cdRect.anchorMax = Vector2.one;
            cdRect.sizeDelta = Vector2.zero;

            var cdTextGo = new GameObject("CooldownText");
            cdTextGo.transform.SetParent(cdOvlGo.transform, false);
            var cdText = cdTextGo.AddComponent<TextMeshProUGUI>();
            cdText.fontSize = 38;
            cdText.fontStyle = FontStyles.Bold;
            cdText.color = Color.white;
            cdText.alignment = TextAlignmentOptions.Center;
            var cdTextRect = cdTextGo.GetComponent<RectTransform>();
            cdTextRect.anchorMin = Vector2.zero;
            cdTextRect.anchorMax = Vector2.one;
            cdTextRect.sizeDelta = Vector2.zero;

            // Register card listeners
            var slotBtnComp = slotGo.AddComponent<UnityEngine.UI.Button>();
            slotBtnComp.interactable = !locked;
            int index = i;
            slotBtnComp.onClick.AddListener(() => {
                if (PlantPlacementManager.Instance != null) {
                    PlantPlacementManager.Instance.SelectPlant(index);
                }
            });
            slotGo.AddComponent<UIButtonEffects>();

            // Initialize Card component
            var card = slotGo.AddComponent<PlantCard>();
            card.Initialize(cardBgImg, glowImg, cdImg, cdText, iconImg, costText, lockOverlayGo, locked, nameText);
            plantCards.Add(card);
        }

        // 7. Create Game Over Popup (Initially Inactive)
        gameOverPopup = new GameObject("GameOverPopup");
        var popupRect = gameOverPopup.AddComponent<RectTransform>();
        gameOverPopup.transform.SetParent(hudCanvas.transform, false);
        gameOverPopup.SetActive(false);

        popupRect.anchorMin = Vector2.zero;
        popupRect.anchorMax = Vector2.one;
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = Vector2.zero;

        // Dark modal overlay
        var overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(gameOverPopup.transform, false);
        var overlayImg = overlayGo.AddComponent<UnityEngine.UI.Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.75f);
        var overlayRect = overlayGo.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        // Main Background Panel
        var popupBg = new GameObject("Background");
        popupBg.AddComponent<RectTransform>();
        popupBg.transform.SetParent(gameOverPopup.transform, false);
        var bgImage = popupBg.AddComponent<UnityEngine.UI.Image>();
        Color gameOverBgBottom = new Color(0.08f, 0.16f, 0.10f, 0.98f); // Deep forest green/swamp
        Color gameOverBgTop = new Color(0.15f, 0.30f, 0.18f, 0.98f);    // Bright forest green
        Color gameOverBgBorder = new Color(0.85f, 0.75f, 0.25f, 1f);   // Fantasy gold/leafy border
        bgImage.sprite = CreateRoundedRectGradientSprite(640, 520, 36, gameOverBgBottom, gameOverBgTop, gameOverBgBorder, 5);
        
        var bgRect = popupBg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(640f, 520f);

        // Title
        var titleGo = new GameObject("Title");
        titleGo.AddComponent<RectTransform>();
        titleGo.transform.SetParent(popupBg.transform, false);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "GAME OVER";
        titleText.fontSize = 64;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.95f, 0.15f, 0.15f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;
        
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -40f);
        titleRect.sizeDelta = new Vector2(500f, 80f);

        // Stats Area
        var statsGo = new GameObject("Stats");
        statsGo.AddComponent<RectTransform>();
        statsGo.transform.SetParent(popupBg.transform, false);
        var statsRect = statsGo.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.5f, 0.5f);
        statsRect.anchorMax = new Vector2(0.5f, 0.5f);
        statsRect.pivot = new Vector2(0.5f, 0.5f);
        statsRect.anchoredPosition = new Vector2(0f, 15f);
        statsRect.sizeDelta = new Vector2(500f, 210f);

        // Restart and Main Menu Buttons (Children of popupBg now!)
        CreateStyledButton("RestartBtn", popupBg.transform, new Vector2(-135f, 40f), new Vector2(220f, 70f), "PLAY AGAIN",
            new Color(0.6f, 0.3f, 0.05f, 1f), new Color(0.85f, 0.45f, 0.1f, 1f), new Color(1f, 0.85f, 0.3f, 1f), 5, OnRestartButtonClicked);

        CreateStyledButton("MainMenuBtn", popupBg.transform, new Vector2(135f, 40f), new Vector2(220f, 70f), "MAIN MENU",
            new Color(0.18f, 0.4f, 0.12f, 1f), new Color(0.35f, 0.65f, 0.25f, 1f), new Color(0.7f, 0.9f, 0.5f, 1f), 5, OnMainMenuButtonClicked);

        // 8. Create Pause Popup (Initially Inactive)
        pausePopup = new GameObject("PausePopup");
        pausePopup.AddComponent<RectTransform>();
        pausePopup.transform.SetParent(hudCanvas.transform, false);
        pausePopup.SetActive(false);

        var pausePopupRect = pausePopup.GetComponent<RectTransform>();
        pausePopupRect.anchorMin = new Vector2(0.5f, 0.5f);
        pausePopupRect.anchorMax = new Vector2(0.5f, 0.5f);
        pausePopupRect.pivot = new Vector2(0.5f, 0.5f);
        pausePopupRect.anchoredPosition = Vector2.zero;
        pausePopupRect.sizeDelta = new Vector2(700f, 500f);

        var pauseBg = new GameObject("Background");
        pauseBg.AddComponent<RectTransform>();
        pauseBg.transform.SetParent(pausePopup.transform, false);
        var pauseBgImg = pauseBg.AddComponent<UnityEngine.UI.Image>();
        Color pauseBgBottom = new Color(0.04f, 0.06f, 0.10f, 0.96f);
        Color pauseBgTop = new Color(0.10f, 0.15f, 0.22f, 0.96f);
        Color pauseBgBorder = new Color(0.20f, 0.70f, 0.90f, 1f);
        pauseBgImg.sprite = CreateRoundedRectGradientSprite(700, 500, 40, pauseBgBottom, pauseBgTop, pauseBgBorder, 5);
        var pauseBgRect = pauseBg.GetComponent<RectTransform>();
        pauseBgRect.anchorMin = Vector2.zero;
        pauseBgRect.anchorMax = Vector2.one;
        pauseBgRect.sizeDelta = Vector2.zero;

        var pauseTitleGo = new GameObject("Title");
        pauseTitleGo.AddComponent<RectTransform>();
        pauseTitleGo.transform.SetParent(pausePopup.transform, false);
        var pauseTitleText = pauseTitleGo.AddComponent<TextMeshProUGUI>();
        pauseTitleText.text = "PAUSED";
        pauseTitleText.fontSize = 62;
        pauseTitleText.fontStyle = FontStyles.Bold;
        pauseTitleText.color = new Color(0.20f, 0.75f, 0.95f, 1f);
        pauseTitleText.alignment = TextAlignmentOptions.Center;
        
        var pauseTitleRect = pauseTitleGo.GetComponent<RectTransform>();
        pauseTitleRect.anchorMin = new Vector2(0.5f, 1f);
        pauseTitleRect.anchorMax = new Vector2(0.5f, 1f);
        pauseTitleRect.pivot = new Vector2(0.5f, 1f);
        pauseTitleRect.anchoredPosition = new Vector2(0f, -60f);
        pauseTitleRect.sizeDelta = new Vector2(600f, 80f);

        var pauseSubtitleGo = new GameObject("Subtitle");
        pauseSubtitleGo.AddComponent<RectTransform>();
        pauseSubtitleGo.transform.SetParent(pausePopup.transform, false);
        var pauseSubtitleText = pauseSubtitleGo.AddComponent<TextMeshProUGUI>();
        pauseSubtitleText.text = "Garden Guardians • Zombie Defense";
        pauseSubtitleText.fontSize = 24;
        pauseSubtitleText.fontStyle = FontStyles.Italic | FontStyles.Bold;
        pauseSubtitleText.color = Color.white;
        pauseSubtitleText.alignment = TextAlignmentOptions.Center;

        var pauseSubtitleRect = pauseSubtitleGo.GetComponent<RectTransform>();
        pauseSubtitleRect.anchorMin = new Vector2(0.5f, 1f);
        pauseSubtitleRect.anchorMax = new Vector2(0.5f, 1f);
        pauseSubtitleRect.pivot = new Vector2(0.5f, 1f);
        pauseSubtitleRect.anchoredPosition = new Vector2(0f, -135f);
        pauseSubtitleRect.sizeDelta = new Vector2(600f, 50f);

        // Pause buttons arranged side-by-side
        CreateStyledButton("ResumeBtn", pausePopup.transform, new Vector2(-200f, 90f), new Vector2(180f, 70f), "RESUME",
            new Color(0.10f, 0.35f, 0.10f, 1f), new Color(0.20f, 0.65f, 0.20f, 1f), new Color(0.40f, 0.90f, 0.40f, 1f), 4, TogglePause);

        CreateStyledButton("RestartBtn", pausePopup.transform, new Vector2(0f, 90f), new Vector2(180f, 70f), "RESTART",
            new Color(0.45f, 0.08f, 0.08f, 1f), new Color(0.75f, 0.15f, 0.15f, 1f), new Color(0.95f, 0.35f, 0.35f, 1f), 4, OnRestartButtonClicked);

        CreateStyledButton("MainMenuBtn", pausePopup.transform, new Vector2(200f, 90f), new Vector2(180f, 70f), "MAIN MENU",
            new Color(0.12f, 0.15f, 0.20f, 1f), new Color(0.25f, 0.32f, 0.42f, 1f), new Color(0.60f, 0.70f, 0.85f, 1f), 4, OnMainMenuButtonClicked);
    }

    private void CreateStyledButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color bottom, Color top, Color border, int borderWidth, UnityEngine.Events.UnityAction action) {
        var btnGo = new GameObject(name);
        btnGo.AddComponent<RectTransform>();
        btnGo.transform.SetParent(parent, false);
        
        var img = btnGo.AddComponent<UnityEngine.UI.Image>();
        img.sprite = CreateRoundedRectGradientSprite(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), 22, bottom, top, border, borderWidth);

        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = size;

        var btnTextGo = new GameObject("Text");
        btnTextGo.AddComponent<RectTransform>();
        btnTextGo.transform.SetParent(btnGo.transform, false);
        var btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnText.text = text;
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

        var btnTextRect = btnTextGo.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        var btnComp = btnGo.AddComponent<UnityEngine.UI.Button>();
        btnComp.onClick.AddListener(action);
        btnGo.AddComponent<UIButtonEffects>();
    }

    private void ParseNameAndIcon(string fullName, out string cleanName, out string emoji) {
        emoji = "🌱";
        cleanName = fullName;
        
        string[] emojis = { "🔥", "❄️", "🧪", "💣", "⚡", "🌻" };
        foreach (var em in emojis) {
            if (fullName.Contains(em)) {
                emoji = em;
                cleanName = fullName.Replace(em, "").Trim();
                break;
            }
        }
    }

    public void SetSlotHighlight(int activeIndex) {
        for (int i = 0; i < plantCards.Count; i++) {
            if (plantCards[i] != null) {
                plantCards[i].SetSelected(i == activeIndex);
            }
        }
    }

    private void ShowCoinGain(int amount) {
        if (hudCanvas == null) return;
        var coinGo = new GameObject("CoinGainText");
        coinGo.AddComponent<RectTransform>();
        coinGo.transform.SetParent(hudCanvas.transform, false);
        var text = coinGo.AddComponent<TextMeshProUGUI>();
        text.text = $"+{amount}";
        text.fontSize = 42;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.88f, 0.15f, 1f);
        text.alignment = TextAlignmentOptions.Center;

        var rect = coinGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -100f);
        rect.sizeDelta = new Vector2(200f, 60f);

        StartCoroutine(FloatCoinText(coinGo));
    }

    private IEnumerator FloatCoinText(GameObject go) {
        var rect = go.GetComponent<RectTransform>();
        var text = go.GetComponent<TextMeshProUGUI>();
        float duration = 0.85f;
        float elapsed = 0f;
        Vector2 startPos = rect.anchoredPosition;
        rect.localScale = Vector3.one * 0.4f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            rect.anchoredPosition = startPos + new Vector2(0f, t * 75f);
            
            if (t > 0.4f) {
                text.alpha = 1f - ((t - 0.4f) / 0.6f);
            }

            if (elapsed < 0.2f) {
                rect.localScale = Vector3.Lerp(Vector3.one * 0.4f, Vector3.one * 1.3f, elapsed / 0.2f);
            } else if (elapsed < 0.4f) {
                rect.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, (elapsed - 0.2f) / 0.2f);
            } else {
                rect.localScale = Vector3.one;
            }

            yield return null;
        }
        Destroy(go);
    }

    public void SpawnFlyingCoin(Vector3 screenStartPos, int amount) {
        if (hudCanvas == null) return;
        StartCoroutine(AnimateFlyingCoin(screenStartPos, amount));
    }

    private IEnumerator AnimateFlyingCoin(Vector3 screenStartPos, int amount) {
        var coinGo = new GameObject("FlyingCoin");
        coinGo.AddComponent<RectTransform>();
        coinGo.transform.SetParent(hudCanvas.transform, false);
        var coinImg = coinGo.AddComponent<UnityEngine.UI.Image>();
        coinImg.sprite = activeCoinSprite;
        coinImg.color = Color.white;

        var rect = coinGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = screenStartPos;
        rect.sizeDelta = new Vector2(48f, 48f);

        Vector2 targetPos = new Vector2(Screen.width * 0.5f - 110f, Screen.height - 40f);
        Vector2 midPoint = new Vector2(
            (screenStartPos.x + targetPos.x) * 0.5f,
            screenStartPos.y + 120f
        );

        float duration = 0.5f;
        float elapsed = 0f;
        Vector2 startScale = Vector2.one;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float tSmooth = t * t * (3f - 2f * t);

            Vector2 a = Vector2.Lerp(screenStartPos, midPoint, tSmooth);
            Vector2 b = Vector2.Lerp(midPoint, targetPos, tSmooth);
            rect.anchoredPosition = Vector2.Lerp(a, b, tSmooth);

            float scale = Mathf.Lerp(1f, 0.5f, tSmooth);
            rect.localScale = Vector3.one * scale;

            coinImg.color = new Color(1f, 1f, 1f, 1f - tSmooth * 0.3f);

            yield return null;
        }

        Destroy(coinGo);

        AddCoins(amount);
        ShowCoinGain(amount);
    }

    private IEnumerator PulsePill(Transform pillTransform, float targetScale) {
        float elapsed = 0f;
        float duration = 0.15f;
        Vector3 originalScale = Vector3.one;
        
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            pillTransform.localScale = Vector3.Lerp(originalScale, originalScale * targetScale, elapsed / duration);
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            pillTransform.localScale = Vector3.Lerp(originalScale * targetScale, originalScale, elapsed / duration);
            yield return null;
        }
        pillTransform.localScale = originalScale;
    }

    private IEnumerator FlashPillRed(UnityEngine.UI.Image img) {
        if (img == null) yield break;
        Color origColor = img.color;
        img.color = new Color(1f, 0.15f, 0.15f, 0.95f);
        yield return new WaitForSeconds(0.15f);
        img.color = origColor;
    }

    private IEnumerator ScreenFlashRoutine(float duration, Color color, float maxAlpha) {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) yield break;

        var flashGo = new GameObject("ScreenFlash");
        flashGo.transform.SetParent(canvas.transform, false);
        var img = flashGo.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(color.r, color.g, color.b, 0f);
        
        var rect = flashGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        float halfDuration = duration / 2f;
        float elapsed = 0f;

        while (elapsed < halfDuration) {
            elapsed += Time.deltaTime;
            img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0f, maxAlpha, elapsed / halfDuration));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration) {
            elapsed += Time.deltaTime;
            img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(maxAlpha, 0f, elapsed / halfDuration));
            yield return null;
        }

        Destroy(flashGo);
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude) {
        var camera = Camera.main;
        if (camera == null) yield break;

        Vector3 originalPos = camera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration) {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            camera.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        camera.transform.position = originalPos;
    }

    private IEnumerator PopupScaleIn(Transform trans) {
        trans.localScale = Vector3.zero;
        float duration = 0.35f;
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1.05f, t);
            if (t > 0.8f) {
                float tSettle = (t - 0.8f) / 0.2f;
                scale = Mathf.Lerp(1.05f, 1f, tSettle);
            }
            trans.localScale = Vector3.one * scale;
            yield return null;
        }
        trans.localScale = Vector3.one;
    }

}