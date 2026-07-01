using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using AdsManager;

public class GameplayManager : MonoBehaviour {
    public static GameplayManager Instance { get; private set; }

    [Header("Base Health Settings")]
    [SerializeField] private int maxBaseHealth = 10;
    private int currentBaseHealth;

    [Header("HUD Settings")]
    [SerializeField] private int coins = 10000;
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
        if (AudioManager.Instance != null) {
            AudioManager.Instance.Play(SFXType.CoinCollected);
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
    [SerializeField] private GameObject rightBoundary;
    [SerializeField] private float rightBoundaryX = 7.2f;

    public float RightBoundaryX => rightBoundary != null ? rightBoundary.transform.position.x : rightBoundaryX;


    [Header("HUD Custom Sprites")]
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Sprite waveSprite;
    [SerializeField] private Sprite lockSprite;
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    [Header("Pause Popup Custom Assets")]
    [SerializeField] private Sprite pausePopupBgSprite;
    [SerializeField] private Sprite pauseResumeBtnSprite;
    [SerializeField] private Sprite pauseRestartBtnSprite;
    [SerializeField] private Sprite pauseMainMenuBtnSprite;

    [Header("Game Over Popup Custom Assets")]
    [SerializeField] private Sprite gameOverPopupBgSprite;
    [SerializeField] private Sprite statRowBgSprite;
    [SerializeField] private Sprite statValPlaneSprite;

    private UnityEngine.UI.Button soundToggleButtonComp;
    private UnityEngine.UI.Image soundIconImgComp;
    private Sprite activeSoundOnSprite;
    private Sprite activeSoundOffSprite;

    [Header("UI Canvas References")]
    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private GameObject gameOverPopup;
    [SerializeField] private GameObject pausePopup;
    
    [SerializeField] private GameObject healthPillInstance;
    [SerializeField] private GameObject coinsPillInstance;
    [SerializeField] private List<PlantCard> plantCards = new List<PlantCard>();

    [Header("UI Button References")]
    [SerializeField] private UnityEngine.UI.Button pauseButton;
    [SerializeField] private UnityEngine.UI.Button resumeButton;
    [SerializeField] private UnityEngine.UI.Button pauseRestartButton;
    [SerializeField] private UnityEngine.UI.Button pauseMainMenuButton;
    [SerializeField] private UnityEngine.UI.Button gameOverRestartButton;
    [SerializeField] private UnityEngine.UI.Button gameOverMainMenuButton;

    private int matchCoinsDisplayed = 0;
    private Coroutine coinPulseCoroutine;
    private Coroutine healthPulseCoroutine;
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
            return;
        }

        // Hide banner and clear callback delegate so delayed ad responses don't show the banner in gameplay
        if (AdMobManager.GetInstance() != null) {
            AdMobManager.GetInstance().HideBanner();
            var field = typeof(AdMobManager).GetField("BannerAdStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) {
                field.SetValue(AdMobManager.GetInstance(), null);
            }
        }

        AdMobManager.OnInitializeComplete += HandleAdMobInitializeComplete;
        if (AdMobManager.GetInstance() != null && AdMobManager.GetInstance().IsSdkInitialized) {
            HandleAdMobInitializeComplete();
        }
    }

    private void OnDestroy() {
        AdMobManager.OnInitializeComplete -= HandleAdMobInitializeComplete;
    }

    private void HandleAdMobInitializeComplete() {
        if (AdMobManager.GetInstance() != null) {
            var idField = typeof(AdMobManager).GetField("_adIDInterstitial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            string adId = idField?.GetValue(AdMobManager.GetInstance()) as string;
            if (string.IsNullOrEmpty(adId)) {
                AdMobManager.GetInstance().SetAdmobAdsID();
            }
            if (!AdMobManager.GetInstance().IsInterstitialAdLoaded()) {
                AdMobManager.GetInstance().RequestInterstitial();
            }
        }
    }

    private void Start() {
        currentBaseHealth = maxBaseHealth;
        Time.timeScale = 1f;

        if (GlobalProgressionManager.Instance != null) {
            GlobalProgressionManager.Instance.ResetMatchCoins();
        }

        CreateUI();
        UpdateUI();

        // Temporarily disable the Wave Number UI visual display as per requirements
        if (waveText != null && waveText.transform.parent != null) {
            waveText.transform.parent.gameObject.SetActive(false);
        }
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
        if (WaveManager.Instance != null) {
            WaveManager.Instance.CancelWaveAnnouncement();
        }

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
            if (AudioManager.Instance != null) {
                AudioManager.Instance.Play(SFXType.GameOver);
            }

            // Fetch references
            var overlayImg = gameOverPopup.transform.Find("Overlay")?.GetComponent<UnityEngine.UI.Image>();
            var panel = gameOverPopup.transform.Find("Background");
            var title = panel?.Find("Title");
            var stats = panel?.Find("Stats");
            var restartBtn = panel?.Find("RestartBtn");
            var mainMenuBtn = panel?.Find("MainMenuBtn");

            // Setup initial values & styling
            UpdateGameOverStats(isVictory);

            // Fetch newly created stat value texts so we can animate them
            TextMeshProUGUI zombiesValText = null;
            TextMeshProUGUI coinsValText = null;
            if (stats != null) {
                var zombieCard = stats.Find("StatCard_Zombies");
                if (zombieCard != null) {
                    zombiesValText = zombieCard.Find("Value")?.GetComponent<TextMeshProUGUI>();
                }
                var coinCard = stats.Find("StatCard_Coins");
                if (coinCard != null) {
                    coinsValText = coinCard.Find("Value")?.GetComponent<TextMeshProUGUI>();
                }
            }

            // Set everything to inactive / invisible for step-by-step entry
            if (overlayImg != null) overlayImg.color = new Color(0f, 0f, 0f, 0f);
            if (panel != null) panel.localScale = Vector3.zero;
            if (title != null) title.localScale = Vector3.zero;
            if (restartBtn != null) restartBtn.localScale = Vector3.zero;
            if (mainMenuBtn != null) mainMenuBtn.localScale = Vector3.zero;

            // Stats cards (we want them to scale up too)
            Transform zombiesCardTrans = stats?.Find("StatCard_Zombies");
            Transform coinsCardTrans = stats?.Find("StatCard_Coins");
            if (zombiesCardTrans != null) zombiesCardTrans.localScale = Vector3.zero;
            if (coinsCardTrans != null) coinsCardTrans.localScale = Vector3.zero;

            // Phase 1: Fade overlay and Scale panel with bounce
            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration) {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                // Fade overlay
                if (overlayImg != null) {
                    overlayImg.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.75f, t));
                }
                
                // Scale panel with nice EaseOutBack bounce
                if (panel != null) {
                    float tMinusOne = t - 1f;
                    float easeOutBack = tMinusOne * tMinusOne * ((1.70158f + 1f) * tMinusOne + 1.70158f) + 1f;
                    panel.localScale = Vector3.one * easeOutBack;
                }
                yield return null;
            }
            if (panel != null) panel.localScale = Vector3.one;
            if (overlayImg != null) overlayImg.color = new Color(0f, 0f, 0f, 0.75f);

            // Phase 2: Title & Cards cascade entry
            // Show the Title with a bounce
            elapsed = 0f;
            duration = 0.3f;
            while (elapsed < duration) {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float tMinusOne = t - 1f;
                float easeOutBack = tMinusOne * tMinusOne * ((1.70158f + 1f) * tMinusOne + 1.70158f) + 1f;

                if (title != null) title.localScale = Vector3.one * easeOutBack;
                yield return null;
            }
            if (title != null) title.localScale = Vector3.one;

            // Pop in the statistics cards
            elapsed = 0f;
            duration = 0.35f;
            while (elapsed < duration) {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float tMinusOne = t - 1f;
                float easeOutBack = tMinusOne * tMinusOne * ((1.70158f + 1f) * tMinusOne + 1.70158f) + 1f;

                if (zombiesCardTrans != null) zombiesCardTrans.localScale = Vector3.one * easeOutBack;
                if (coinsCardTrans != null) coinsCardTrans.localScale = Vector3.one * easeOutBack;
                yield return null;
            }
            if (zombiesCardTrans != null) zombiesCardTrans.localScale = Vector3.one;
            if (coinsCardTrans != null) coinsCardTrans.localScale = Vector3.one;

            // Phase 3: Start Count-up of Stats
            int finalZombies = zombiesKilled;
            int finalCoins = matchCoinsDisplayed;
            
            // Start the count up animation
            StartCoroutine(CountUpStatsRoutine(zombiesValText, coinsValText, finalZombies, finalCoins));

            // Phase 4: Buttons scale up with a slight delay
            yield return new WaitForSecondsRealtime(0.1f);
            elapsed = 0f;
            duration = 0.3f;
            while (elapsed < duration) {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float tMinusOne = t - 1f;
                float easeOutBack = tMinusOne * tMinusOne * ((1.70158f + 1f) * tMinusOne + 1.70158f) + 1f;

                if (restartBtn != null) restartBtn.localScale = Vector3.one * easeOutBack;
                if (mainMenuBtn != null) mainMenuBtn.localScale = Vector3.one * easeOutBack;
                yield return null;
            }
            if (restartBtn != null) restartBtn.localScale = Vector3.one;
            if (mainMenuBtn != null) mainMenuBtn.localScale = Vector3.one;
        }
    }

    private void ApplyGameOverBottomButtonLayout(RectTransform buttonRect, Vector2 position, Vector2 size) {
        if (buttonRect == null) return;
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = size;
    }

    private void ApplyGameOverPopupLayout(bool isVictory) {
        if (gameOverPopup == null) return;

        bool useCustomBg = gameOverPopupBgSprite != null;

        var panel = gameOverPopup.transform.Find("Background")?.GetComponent<RectTransform>();
        if (panel != null) {
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;

            var bgImageComp = panel.GetComponent<UnityEngine.UI.Image>();
            if (useCustomBg) {
                panel.sizeDelta = new Vector2(887f, 760f);
                if (bgImageComp != null) {
                    bgImageComp.sprite = gameOverPopupBgSprite;
                    bgImageComp.color = Color.white;
                }
            } else {
                panel.sizeDelta = new Vector2(680f, 540f);
                if (bgImageComp != null) {
                    Color gameOverBgBottom = new Color(0.08f, 0.16f, 0.10f, 0.98f);
                    Color gameOverBgTop = new Color(0.15f, 0.30f, 0.18f, 0.98f);
                    Color gameOverBgBorder = isVictory ? new Color(0.95f, 0.82f, 0.18f, 1f) : new Color(0.85f, 0.75f, 0.25f, 1f);
                    bgImageComp.sprite = CreateRoundedRectGradientSprite(680, 540, 36, gameOverBgBottom, gameOverBgTop, gameOverBgBorder, 6);
                }
            }
        }

        var titleRect = gameOverPopup.transform.Find("Background/Title")?.GetComponent<RectTransform>();
        if (titleRect != null) {
            if (useCustomBg) {
                titleRect.gameObject.SetActive(false);
            } else {
                titleRect.gameObject.SetActive(true);
                titleRect.anchorMin = new Vector2(0.5f, 0.5f);
                titleRect.anchorMax = new Vector2(0.5f, 0.5f);
                titleRect.pivot = new Vector2(0.5f, 0.5f);
                titleRect.anchoredPosition = new Vector2(0f, 220f);
                titleRect.sizeDelta = new Vector2(600f, 80f);

                var titleTextComp = titleRect.GetComponent<TextMeshProUGUI>();
                if (titleTextComp != null) {
                    titleTextComp.text = isVictory ? "VICTORY!" : "GAME OVER";
                    titleTextComp.color = isVictory ? new Color(0.95f, 0.82f, 0.18f, 1f) : new Color(1.00f, 0.82f, 0.22f, 1f);
                    titleTextComp.fontSize = 64;
                    titleTextComp.fontStyle = FontStyles.Bold;
                    titleTextComp.alignment = TextAlignmentOptions.Center;
                    ApplyFont(titleTextComp);
                }
            }
        }

        var restartBtnRect = gameOverPopup.transform.Find("Background/RestartBtn")?.GetComponent<RectTransform>();
        if (restartBtnRect != null) {
            var img = restartBtnRect.GetComponent<UnityEngine.UI.Image>();
            var text = restartBtnRect.Find("Text")?.GetComponent<TextMeshProUGUI>();

            if (useCustomBg && pauseRestartBtnSprite != null) {
                restartBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
                restartBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
                restartBtnRect.pivot = new Vector2(0.5f, 0.5f);
                restartBtnRect.anchoredPosition = new Vector2(-180f, -220f);
                restartBtnRect.sizeDelta = new Vector2(342f, 79f);
                if (img != null) {
                    img.sprite = pauseRestartBtnSprite;
                    img.color = Color.white;
                }
                if (text != null) {
                    text.gameObject.SetActive(false);
                }
            } else {
                ApplyGameOverBottomButtonLayout(restartBtnRect, new Vector2(-140f, 55f), new Vector2(220f, 70f));
                if (img != null) {
                    img.sprite = CreateRoundedRectGradientSprite(220, 70, 22,
                        new Color(0.6f, 0.3f, 0.05f, 1f),
                        new Color(0.85f, 0.45f, 0.1f, 1f),
                        new Color(1f, 0.85f, 0.3f, 1f), 5);
                    img.color = Color.white;
                }
                if (text != null) {
                    text.gameObject.SetActive(true);
                    text.text = "PLAY AGAIN";
                    text.color = Color.white;
                    text.fontStyle = FontStyles.Bold;
                    text.fontSize = 24;
                    text.alignment = TextAlignmentOptions.Center;
                    ApplyFont(text);
                }
            }
        }

        var mainMenuBtnRect = gameOverPopup.transform.Find("Background/MainMenuBtn")?.GetComponent<RectTransform>();
        if (mainMenuBtnRect != null) {
            var img = mainMenuBtnRect.GetComponent<UnityEngine.UI.Image>();
            var text = mainMenuBtnRect.Find("Text")?.GetComponent<TextMeshProUGUI>();

            if (useCustomBg && pauseMainMenuBtnSprite != null) {
                mainMenuBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
                mainMenuBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
                mainMenuBtnRect.pivot = new Vector2(0.5f, 0.5f);
                mainMenuBtnRect.anchoredPosition = new Vector2(180f, -220f);
                mainMenuBtnRect.sizeDelta = new Vector2(300f, 68f);
                if (img != null) {
                    img.sprite = pauseMainMenuBtnSprite;
                    img.color = Color.white;
                }
                if (text != null) {
                    text.gameObject.SetActive(false);
                }
            } else {
                ApplyGameOverBottomButtonLayout(mainMenuBtnRect, new Vector2(140f, 55f), new Vector2(220f, 70f));
                if (img != null) {
                    img.sprite = CreateRoundedRectGradientSprite(220, 70, 22,
                        new Color(0.18f, 0.4f, 0.12f, 1f),
                        new Color(0.35f, 0.65f, 0.25f, 1f),
                        new Color(0.85f, 0.75f, 0.25f, 1f), 5);
                    img.color = Color.white;
                }
                if (text != null) {
                    text.gameObject.SetActive(true);
                    text.text = "MAIN MENU";
                    text.color = Color.white;
                    text.fontStyle = FontStyles.Bold;
                    text.fontSize = 24;
                    text.alignment = TextAlignmentOptions.Center;
                    ApplyFont(text);
                }
            }
        }

        var statsRect = gameOverPopup.transform.Find("Background/Stats")?.GetComponent<RectTransform>();
        if (statsRect != null) {
            statsRect.anchorMin = new Vector2(0.5f, 0.5f);
            statsRect.anchorMax = new Vector2(0.5f, 0.5f);
            statsRect.pivot = new Vector2(0.5f, 0.5f);
            if (useCustomBg) {
                statsRect.anchoredPosition = Vector2.zero;
                statsRect.sizeDelta = new Vector2(560f, 210f);
            } else {
                statsRect.anchoredPosition = new Vector2(0f, 10f);
                statsRect.sizeDelta = new Vector2(600f, 230f);
            }
        }
    }

    private void UpdateGameOverStats(bool isVictory) {
        ApplyGameOverPopupLayout(isVictory);

        // Coins and Wallet handling (preserving logic exactly as original)
        matchCoinsDisplayed = 0;
        if (GlobalProgressionManager.Instance != null) {
            matchCoinsDisplayed = GlobalProgressionManager.Instance.MatchEarnedCoins;
            GlobalProgressionManager.Instance.ApplyMatchCoinsToWallet();
        }

        var statsRect = gameOverPopup.transform.Find("Background/Stats")?.GetComponent<RectTransform>();
        if (statsRect != null) {
            var statsImg = statsRect.GetComponent<UnityEngine.UI.Image>();
            if (statsImg == null) {
                statsImg = statsRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            }
            Color innerBgBottom = new Color(0.04f, 0.08f, 0.05f, 0.95f); // Deep forest green inner panel
            Color innerBgTop = new Color(0.07f, 0.14f, 0.09f, 0.95f);
            Color innerBorder = new Color(0.55f, 0.48f, 0.15f, 0.8f);    // Gold frame
            int statsW = gameOverPopupBgSprite != null ? 560 : 600;
            int statsH = gameOverPopupBgSprite != null ? 210 : 230;
            statsImg.sprite = CreateRoundedRectGradientSprite(statsW, statsH, 24, innerBgBottom, innerBgTop, innerBorder, 3);
 
            // Rebuild stats children inside statsRect
            for (int i = statsRect.childCount - 1; i >= 0; i--) {
                Destroy(statsRect.GetChild(i).gameObject);
            }
 
            // Create Zombies Defeated card on the left with skull sprite
            Sprite skullSprite = CreateSkullSprite(64, 64);
            float cardX = gameOverPopupBgSprite != null ? 125f : 135f;
            CreateStatCard("StatCard_Zombies", statsRect, new Vector2(-cardX, 0f), skullSprite, Color.white, "ZOMBIES KILLED", "0", new Color(0.95f, 0.35f, 0.35f, 1f));
 
            // Create Coins Earned card on the right with coin sprite
            CreateStatCard("StatCard_Coins", statsRect, new Vector2(cardX, 0f), activeCoinSprite, Color.white, "COINS EARNED", "0", new Color(1f, 0.85f, 0.15f, 1f));
        }
    }

    private void CreateStatCard(string name, Transform parent, Vector2 pos, Sprite iconSprite, Color iconColor, string headerText, string startVal, Color valColor) {
        var cardGo = new GameObject(name);
        var cardRect = cardGo.AddComponent<RectTransform>();
        cardGo.transform.SetParent(parent, false);
        
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = pos;

        float cardW = gameOverPopupBgSprite != null ? 220f : 245f;
        float cardH = gameOverPopupBgSprite != null ? 150f : 180f;
        cardRect.sizeDelta = new Vector2(cardW, cardH);

        var cardImg = cardGo.AddComponent<UnityEngine.UI.Image>();
        if (statValPlaneSprite != null) {
            cardImg.sprite = statValPlaneSprite;
            cardImg.color = Color.white;
        } else {
            Color cardBgBottom = new Color(0.02f, 0.05f, 0.03f, 0.95f); // Very dark forest green
            Color cardBgTop = new Color(0.04f, 0.09f, 0.06f, 0.95f);
            Color cardBorder = new Color(0.2f, 0.5f, 0.3f, 0.6f);
            cardImg.sprite = CreateRoundedRectGradientSprite(Mathf.RoundToInt(cardW), Mathf.RoundToInt(cardH), 18, cardBgBottom, cardBgTop, cardBorder, 2);
        }

        // Icon Image
        var iconGo = new GameObject("Icon");
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconGo.transform.SetParent(cardGo.transform, false);
        var iconImgComp = iconGo.AddComponent<UnityEngine.UI.Image>();
        iconImgComp.sprite = iconSprite;
        iconImgComp.color = iconColor;
        iconImgComp.preserveAspect = true;
        
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, gameOverPopupBgSprite != null ? 35f : 45f);
        iconRect.sizeDelta = new Vector2(48f, 48f);

        // Heading
        var headingGo = new GameObject("Heading");
        var headingRect = headingGo.AddComponent<RectTransform>();
        headingGo.transform.SetParent(cardGo.transform, false);
        var headingTextComp = headingGo.AddComponent<TextMeshProUGUI>();
        headingTextComp.text = headerText;
        headingTextComp.fontSize = gameOverPopupBgSprite != null ? 14 : 15;
        headingTextComp.fontStyle = FontStyles.Bold;
        headingTextComp.color = new Color(0.9f, 0.75f, 0.2f, 1f); // Gold heading
        headingTextComp.alignment = TextAlignmentOptions.Center;

        headingRect.anchorMin = new Vector2(0.5f, 0.5f);
        headingRect.anchorMax = new Vector2(0.5f, 0.5f);
        headingRect.pivot = new Vector2(0.5f, 0.5f);
        headingRect.anchoredPosition = new Vector2(0f, gameOverPopupBgSprite != null ? -3f : 5f);
        headingRect.sizeDelta = new Vector2(gameOverPopupBgSprite != null ? 210f : 230f, 30f);
        ApplyFont(headingTextComp);

        // Value
        var valGo = new GameObject("Value");
        var valRect = valGo.AddComponent<RectTransform>();
        valGo.transform.SetParent(cardGo.transform, false);
        var valTextComp = valGo.AddComponent<TextMeshProUGUI>();
        valTextComp.text = startVal;
        valTextComp.fontSize = gameOverPopupBgSprite != null ? 38 : 42;
        valTextComp.fontStyle = FontStyles.Bold;
        valTextComp.color = valColor;
        valTextComp.alignment = TextAlignmentOptions.Center;

        valRect.anchorMin = new Vector2(0.5f, 0.5f);
        valRect.anchorMax = new Vector2(0.5f, 0.5f);
        valRect.pivot = new Vector2(0.5f, 0.5f);
        valRect.anchoredPosition = new Vector2(0f, gameOverPopupBgSprite != null ? -42f : -38f);
        valRect.sizeDelta = new Vector2(200f, 50f);
        ApplyFont(valTextComp);
    }

    private void CreateStatRow(string name, Transform parent, Vector2 pos, Sprite iconSprite, Color iconColor, string startVal, Color valColor) {
        var rowGo = new GameObject(name);
        var rowRect = rowGo.AddComponent<RectTransform>();
        rowGo.transform.SetParent(parent, false);

        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = pos;
        rowRect.sizeDelta = new Vector2(460f, 75f);

        var rowImg = rowGo.AddComponent<UnityEngine.UI.Image>();
        rowImg.sprite = statRowBgSprite;
        rowImg.color = Color.white;

        // Icon Image
        var iconGo = new GameObject("Icon");
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconGo.transform.SetParent(rowGo.transform, false);
        var iconImgComp = iconGo.AddComponent<UnityEngine.UI.Image>();
        iconImgComp.sprite = iconSprite;
        iconImgComp.color = iconColor;
        iconImgComp.preserveAspect = true;

        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(45f, 0f);
        iconRect.sizeDelta = new Vector2(48f, 48f);

        // Value Text
        var valGo = new GameObject("Value");
        var valRect = valGo.AddComponent<RectTransform>();
        valGo.transform.SetParent(rowGo.transform, false);
        var valTextComp = valGo.AddComponent<TextMeshProUGUI>();
        valTextComp.text = startVal;
        valTextComp.fontSize = 42;
        valTextComp.fontStyle = FontStyles.Bold;
        valTextComp.color = valColor;
        valTextComp.alignment = TextAlignmentOptions.Center;

        valRect.anchorMin = new Vector2(0.5f, 0.5f);
        valRect.anchorMax = new Vector2(0.5f, 0.5f);
        valRect.pivot = new Vector2(0.5f, 0.5f);
        valRect.anchoredPosition = new Vector2(30f, 0f);
        valRect.sizeDelta = new Vector2(250f, 50f);
        ApplyFont(valTextComp);
    }

    private Sprite CreateSkullSprite(int width, int height) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        float cx = width / 2f;
        float cy = height * 0.58f;
        float rHead = width * 0.3f;
        
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float dx = x - cx;
                float dy = y - cy;
                
                // Base head circle
                bool inHead = (dx * dx + dy * dy <= rHead * rHead);
                
                // Jaw area below head
                bool inJaw = (Mathf.Abs(dx) <= width * 0.16f && y >= height * 0.16f && y <= height * 0.45f);
                
                // Combine skull base
                bool isSkullBase = inHead || inJaw;
                
                // Eye sockets
                float eyeY = height * 0.58f;
                float eyeL_X = cx - width * 0.12f;
                float eyeR_X = cx + width * 0.12f;
                float rEye = width * 0.08f;
                bool isLeftEye = ((x - eyeL_X) * (x - eyeL_X) + (y - eyeY) * (y - eyeY) <= rEye * rEye);
                bool isRightEye = ((x - eyeR_X) * (x - eyeR_X) + (y - eyeY) * (y - eyeY) <= rEye * rEye);
                
                // Nose cavity
                float noseY = height * 0.44f;
                bool isNose = (Mathf.Abs(dx) <= width * 0.04f && Mathf.Abs(y - noseY) <= height * 0.05f);
                
                // Teeth slits
                bool isTeeth = (y >= height * 0.16f && y <= height * 0.28f && (Mathf.Abs(x - cx) < 2f || Mathf.Abs(x - (cx - 6f)) < 2f || Mathf.Abs(x - (cx + 6f)) < 2f));
                
                if (isSkullBase && !isLeftEye && !isRightEye && !isNose && !isTeeth) {
                    cols[y * width + x] = new Color(0.9f, 0.9f, 0.9f, 1f); // White/light gray bone color
                } else {
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private IEnumerator CountUpStatsRoutine(TextMeshProUGUI zombiesText, TextMeshProUGUI coinsText, int targetZombies, int targetCoins) {
        float elapsed = 0f;
        float duration = 1.2f;
        
        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float tEase = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
            
            int currentZombies = Mathf.RoundToInt(Mathf.Lerp(0, targetZombies, tEase));
            int currentCoins = Mathf.RoundToInt(Mathf.Lerp(0, targetCoins, tEase));
            
            if (zombiesText != null) zombiesText.text = currentZombies.ToString();
            if (coinsText != null) coinsText.text = currentCoins.ToString();
            
            yield return null;
        }
        
        if (zombiesText != null) zombiesText.text = targetZombies.ToString();
        if (coinsText != null) coinsText.text = targetCoins.ToString();
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

    private bool waveCompleteWasActive = false;

    public void TogglePause() {
        if (currentBaseHealth <= 0) return;
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePopup != null) {
            if (isPaused) {
                if (WaveManager.Instance != null) {
                    WaveManager.Instance.CancelWaveAnnouncement();
                    waveCompleteWasActive = WaveManager.Instance.IsWaveCompletePanelActive;
                    WaveManager.Instance.SetWaveCompletePanelVisible(false);
                }
                pausePopup.SetActive(true);
                StopAllCoroutines();
                CleanupOrphanedCoinAnimations();
                StartCoroutine(PopupScaleIn(pausePopup.transform));
            } else {
                pausePopup.SetActive(false);
                if (WaveManager.Instance != null && waveCompleteWasActive) {
                    WaveManager.Instance.SetWaveCompletePanelVisible(true);
                }
            }
        }
    }

    private void CleanupOrphanedCoinAnimations() {
        if (hudCanvas == null) return;
        foreach (Transform child in hudCanvas.transform) {
            if (child.name == "CoinGainText" || child.name == "FlyingCoin") {
                Destroy(child.gameObject);
            }
        }
    }

    public void OnRestartButtonClicked() {
        Time.timeScale = 1f;
        ShowInterstitialAndContinue(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });
    }

    public void OnMainMenuButtonClicked() {
        Time.timeScale = 1f;
        ShowInterstitialAndContinue(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GardenGuardians_MainMenu");
        });
    }

    private void ShowInterstitialAndContinue(System.Action onCompleted) {
        if (AdMobManager.GetInstance() != null && AdMobManager.GetInstance().IsInterstitialAdLoaded()) {
            var field = typeof(AdMobManager).GetField("_interstitial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var interstitial = field?.GetValue(AdMobManager.GetInstance()) as GoogleMobileAds.Api.InterstitialAd;
            
            if (interstitial != null) {
                System.Action handleClosed = null;
                System.Action<GoogleMobileAds.Api.AdError> handleFailed = null;
                bool hasContinued = false;
                
                handleClosed = () => {
                    interstitial.OnAdFullScreenContentClosed -= handleClosed;
                    interstitial.OnAdFullScreenContentFailed -= handleFailed;
                    if (!hasContinued) {
                        hasContinued = true;
                        onCompleted();
                    }
                };
                
                handleFailed = (err) => {
                    interstitial.OnAdFullScreenContentClosed -= handleClosed;
                    interstitial.OnAdFullScreenContentFailed -= handleFailed;
                    if (!hasContinued) {
                        hasContinued = true;
                        onCompleted();
                    }
                };
                
                interstitial.OnAdFullScreenContentClosed += handleClosed;
                interstitial.OnAdFullScreenContentFailed += handleFailed;
                
                AdMobManager.GetInstance().ShowInterstitial();
            } else {
                onCompleted();
            }
        } else {
            onCompleted();
        }
    }

    public bool IsSlotOnCooldown(int index) {
        if (plantCards == null) return false;
        foreach (var card in plantCards) {
            if (card != null && card.realSlotIndex == index) {
                return card.IsOnCooldown();
            }
        }
        return false;
    }

    public void StartSlotCooldown(int index, float duration) {
        if (plantCards == null) return;
        foreach (var card in plantCards) {
            if (card != null && card.realSlotIndex == index) {
                card.StartCooldown(duration);
                break;
            }
        }
    }

    private void UpdateAffordability() {
        if (plantCards == null || PlantPlacementManager.Instance == null) return;
        for (int i = 0; i < plantCards.Count; i++) {
            var card = plantCards[i];
            if (card != null) {
                int realIndex = card.realSlotIndex;
                int cost = PlantPlacementManager.Instance.GetSlotCost(realIndex);
                card.SetAffordable(coins >= cost);
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

    private void SetupExistingUI() {
        if (UnityEngine.EventSystems.EventSystem.current == null) {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        activeHeartSprite = heartSprite != null ? heartSprite : CreateHeartSprite(64, 64);
        activeCoinSprite = coinSprite != null ? coinSprite : CreateCoinSprite(64, 64);
        activeWaveSprite = waveSprite != null ? waveSprite : CreateWaveSprite(64, 64);
        activeLockSprite = lockSprite != null ? lockSprite : CreateLockSprite(64, 64);
        activePauseSprite = pauseSprite != null ? pauseSprite : CreatePauseSprite(64, 64);

        if (pauseButton != null) {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(TogglePause);
        }
        if (resumeButton != null) {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(TogglePause);
        }
        if (pauseRestartButton != null) {
            pauseRestartButton.onClick.RemoveAllListeners();
            pauseRestartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        if (pauseMainMenuButton != null) {
            pauseMainMenuButton.onClick.RemoveAllListeners();
            pauseMainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }
        if (gameOverRestartButton != null) {
            gameOverRestartButton.onClick.RemoveAllListeners();
            gameOverRestartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        if (gameOverMainMenuButton != null) {
            gameOverMainMenuButton.onClick.RemoveAllListeners();
            gameOverMainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }

        if (gameOverPopup != null) {
            ApplyGameOverPopupLayout(false);
        }

        var ppm = GetComponent<PlantPlacementManager>();
        int slotsCount = ppm != null ? ppm.SlotsCount : 8;

        Transform toolbar = hudCanvas != null ? hudCanvas.transform.Find("PlantToolbar") : null;
        if (toolbar != null && ppm != null) {
            // Find all unlocked slot indices
            List<int> unlockedSlotIndices = new List<int>();
            for (int i = 0; i < slotsCount; i++) {
                if (!ppm.IsSlotLocked(i)) {
                    unlockedSlotIndices.Add(i);
                }
            }

            // Set toolbar width to show up to 7 cards (visible area size)
            int visibleCardsCount = Mathf.Clamp(unlockedSlotIndices.Count, 1, 7);
            float toolbarWidth = visibleCardsCount * 125f + 15f;

            var toolbarRect = toolbar.GetComponent<RectTransform>();
            if (toolbarRect != null) {
                toolbarRect.sizeDelta = new Vector2(toolbarWidth, 170f);
            }
            var toolbarImg = toolbar.GetComponent<UnityEngine.UI.Image>();
            if (toolbarImg != null) {
                Color toolbarBottom = new Color(0.08f, 0.15f, 0.09f, 0.85f);
                Color toolbarTop = new Color(0.18f, 0.32f, 0.20f, 0.85f);
                Color toolbarBorder = new Color(0.85f, 0.75f, 0.25f, 1f);
                toolbarImg.sprite = CreateRoundedRectGradientSprite(Mathf.RoundToInt(toolbarWidth), 170, 30, toolbarBottom, toolbarTop, toolbarBorder, 5);
            }

            // Configure RectMask2D to mask outer scroll content
            if (toolbar.gameObject.GetComponent<UnityEngine.UI.RectMask2D>() == null) {
                toolbar.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            }

            // Configure ScrollRect
            var scrollRect = toolbar.gameObject.GetComponent<UnityEngine.UI.ScrollRect>();
            if (scrollRect == null) {
                scrollRect = toolbar.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            }
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.15f;
            scrollRect.scrollSensitivity = 10f;

            // Find or create Content container
            Transform contentTrans = toolbar.Find("Content");
            GameObject contentGo;
            if (contentTrans == null) {
                contentGo = new GameObject("Content");
                contentGo.AddComponent<RectTransform>();
                contentTrans = contentGo.transform;
                contentTrans.SetParent(toolbar, false);
            } else {
                contentGo = contentTrans.gameObject;
            }

            var contentRect = contentGo.GetComponent<RectTransform>();
            if (contentRect == null) {
                contentRect = contentGo.AddComponent<RectTransform>();
            }
            contentTrans = contentRect.transform;
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
            float totalContentWidth = unlockedSlotIndices.Count * 125f + 15f;
            contentRect.sizeDelta = new Vector2(totalContentWidth, 170f);

            scrollRect.content = contentRect;
            scrollRect.viewport = toolbarRect;

            // Collect all child gameobjects to destroy to prevent MissingReferenceException from duplicate lookups
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (var card in plantCards) {
                if (card != null && card.gameObject != null) {
                    toDestroy.Add(card.gameObject);
                }
            }
            plantCards.Clear();

            if (contentTrans != null) {
                foreach (Transform child in contentTrans) {
                    if (child != null && child.gameObject != null && !toDestroy.Contains(child.gameObject)) {
                        toDestroy.Add(child.gameObject);
                    }
                }
            }

            if (toolbar != null) {
                foreach (Transform child in toolbar) {
                    if (child != null && child != contentTrans && child.gameObject != null && !toDestroy.Contains(child.gameObject)) {
                        toDestroy.Add(child.gameObject);
                    }
                }
            }

            foreach (var go in toDestroy) {
                if (go != null) {
                    Destroy(go);
                }
            }

            // Card backgrounds & styles
            Color slotActiveBottom = new Color(0.12f, 0.25f, 0.14f, 0.9f);
            Color slotActiveTop = new Color(0.24f, 0.48f, 0.28f, 0.9f);
            Color slotActiveBorder = new Color(0.85f, 0.75f, 0.25f, 1f);

            for (int k = 0; k < unlockedSlotIndices.Count; k++) {
                int realIndex = unlockedSlotIndices[k];
                bool locked = false; // Always false since we only loop unlocked slot indices
                string fullName = ppm.GetSlotName(realIndex);
                int cost = ppm.GetSlotCost(realIndex);
                Color slotTint = ppm.GetSlotTintColor(realIndex);

                string cleanName, emoji;
                ParseNameAndIcon(fullName, out cleanName, out emoji);

                var slotGo = new GameObject($"Slot_{realIndex}");
                slotGo.AddComponent<RectTransform>();
                slotGo.transform.SetParent(contentTrans, false);

                var cardRect = slotGo.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0f, 0.5f);
                cardRect.anchorMax = new Vector2(0f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(110f, 135f);
                float cardX = 15f + k * 125f + 55f;
                cardRect.anchoredPosition = new Vector2(cardX, 0f);

                var cardBgImg = slotGo.AddComponent<UnityEngine.UI.Image>();
                cardBgImg.sprite = CreateRoundedRectGradientSprite(110, 135, 22, slotActiveBottom, slotActiveTop, slotActiveBorder, 4);

                // Selection Glow
                var glowGo = new GameObject("SelectionGlow");
                glowGo.AddComponent<RectTransform>();
                glowGo.transform.SetParent(slotGo.transform, false);
                var glowImg = glowGo.AddComponent<UnityEngine.UI.Image>();
                glowImg.sprite = CreateRoundedRectGradientSprite(124, 149, 27, new Color(0,0,0,0), new Color(0,0,0,0), new Color(1f, 0.85f, 0.2f, 1f), 6);
                var glowRect = glowGo.GetComponent<RectTransform>();
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.sizeDelta = new Vector2(14f, 14f);

                // Icon
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

                // Lock Overlay (empty/inactive since locked plants aren't displayed)
                GameObject lockOverlayGo = new GameObject("LockOverlay");
                lockOverlayGo.AddComponent<RectTransform>();
                lockOverlayGo.transform.SetParent(slotGo.transform, false);
                var lockBg = lockOverlayGo.AddComponent<UnityEngine.UI.Image>();
                lockBg.sprite = CreateRoundedRectSprite(110, 135, 22, new Color(0.05f, 0.05f, 0.05f, 0.65f));
                var lockOverlayRect = lockOverlayGo.GetComponent<RectTransform>();
                lockOverlayRect.anchorMin = Vector2.zero;
                lockOverlayRect.anchorMax = Vector2.one;
                lockOverlayRect.sizeDelta = Vector2.zero;
                lockOverlayGo.SetActive(false);

                // Cooldown Overlay
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

                // Button Click Listener mapping
                var slotBtnComp = slotGo.AddComponent<UnityEngine.UI.Button>();
                slotBtnComp.interactable = true;
                int targetSlotIndex = realIndex;
                slotBtnComp.onClick.AddListener(() => {
                    if (PlantPlacementManager.Instance != null) {
                        PlantPlacementManager.Instance.SelectPlant(targetSlotIndex);
                    }
                });
                slotGo.AddComponent<UIButtonEffects>();

                // Initialize Card
                var card = slotGo.AddComponent<PlantCard>();
                card.realSlotIndex = realIndex;
                card.Initialize(cardBgImg, glowImg, cdImg, cdText, iconImg, costText, lockOverlayGo, locked, nameText);
                card.InitializeRuntime(locked, cleanName, cost, plantSprite, slotTint);
                plantCards.Add(card);
            }
        }
        CreateOrSetupSoundButton();
    }

    private void CreateUI() {
        if (hudCanvas != null) {
            SetupExistingUI();
            return;
        }

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
        waveRect.anchoredPosition = new Vector2(-230f, -40f); // Spaced nicely from Pause and Sound Toggle
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

        // 5b. Sound Toggle Button
        CreateOrSetupSoundButton();

        // 6. Bottom Plant Toolbar
        var toolbar = new GameObject("PlantToolbar");
        toolbar.AddComponent<RectTransform>();
        toolbar.transform.SetParent(hudCanvas.transform, false);
        
        var ppm = GetComponent<PlantPlacementManager>();
        int slotsCount = ppm != null ? ppm.SlotsCount : 6;

        if (ppm != null) {
            // Find all unlocked slot indices
            List<int> unlockedSlotIndices = new List<int>();
            for (int i = 0; i < slotsCount; i++) {
                if (!ppm.IsSlotLocked(i)) {
                    unlockedSlotIndices.Add(i);
                }
            }

            // Set toolbar width to show up to 7 cards (visible area size)
            int visibleCardsCount = Mathf.Clamp(unlockedSlotIndices.Count, 1, 7);
            float toolbarWidth = visibleCardsCount * 125f + 15f;

            var toolbarRect = toolbar.GetComponent<RectTransform>();
            if (toolbarRect != null) {
                toolbarRect.anchorMin = new Vector2(0.5f, 0f);
                toolbarRect.anchorMax = new Vector2(0.5f, 0f);
                toolbarRect.pivot = new Vector2(0.5f, 0f);
                toolbarRect.anchoredPosition = new Vector2(0f, 30f);
                toolbarRect.sizeDelta = new Vector2(toolbarWidth, 170f);
            }
            var toolbarImg = toolbar.AddComponent<UnityEngine.UI.Image>();
            if (toolbarImg != null) {
                toolbarImg.sprite = CreateRoundedRectGradientSprite(Mathf.RoundToInt(toolbarWidth), 170, 30, toolbarBottom, toolbarTop, toolbarBorder, 5);
            }

            // Configure RectMask2D to mask outer scroll content
            if (toolbar.gameObject.GetComponent<UnityEngine.UI.RectMask2D>() == null) {
                toolbar.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            }

            // Configure ScrollRect
            var scrollRect = toolbar.gameObject.GetComponent<UnityEngine.UI.ScrollRect>();
            if (scrollRect == null) {
                scrollRect = toolbar.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            }
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.15f;
            scrollRect.scrollSensitivity = 10f;

            // Find or create Content container
            var contentGo = new GameObject("Content");
            contentGo.AddComponent<RectTransform>();
            var contentTrans = contentGo.transform;
            contentTrans.SetParent(toolbar.transform, false);

            var contentRect = contentGo.GetComponent<RectTransform>();
            if (contentRect == null) {
                contentRect = contentGo.AddComponent<RectTransform>();
            }
            contentTrans = contentRect.transform;
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
            float totalContentWidth = unlockedSlotIndices.Count * 125f + 15f;
            contentRect.sizeDelta = new Vector2(totalContentWidth, 170f);

            scrollRect.content = contentRect;
            scrollRect.viewport = toolbarRect;

            plantCards.Clear();

            for (int k = 0; k < unlockedSlotIndices.Count; k++) {
                int realIndex = unlockedSlotIndices[k];
                bool locked = false; // Always false since we only show unlocked
                string fullName = ppm.GetSlotName(realIndex);
                int cost = ppm.GetSlotCost(realIndex);
                Color slotTint = ppm.GetSlotTintColor(realIndex);

                string cleanName, emoji;
                ParseNameAndIcon(fullName, out cleanName, out emoji);

                var slotGo = new GameObject($"Slot_{realIndex}");
                slotGo.AddComponent<RectTransform>();
                slotGo.transform.SetParent(contentTrans, false);

                var cardRect = slotGo.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0f, 0.5f);
                cardRect.anchorMax = new Vector2(0f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(110f, 135f);
                float cardX = 15f + k * 125f + 55f;
                cardRect.anchoredPosition = new Vector2(cardX, 0f);

                var cardBgImg = slotGo.AddComponent<UnityEngine.UI.Image>();
                cardBgImg.sprite = CreateRoundedRectGradientSprite(110, 135, 22, slotActiveBottom, slotActiveTop, slotActiveBorder, 4);

                // Selection Glow
                var glowGo = new GameObject("SelectionGlow");
                glowGo.AddComponent<RectTransform>();
                glowGo.transform.SetParent(slotGo.transform, false);
                var glowImg = glowGo.AddComponent<UnityEngine.UI.Image>();
                glowImg.sprite = CreateRoundedRectGradientSprite(124, 149, 27, new Color(0,0,0,0), new Color(0,0,0,0), new Color(1f, 0.85f, 0.2f, 1f), 6);
                var glowRect = glowGo.GetComponent<RectTransform>();
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.sizeDelta = new Vector2(14f, 14f);

                // Icon
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

                // Lock Overlay (empty/inactive since locked plants aren't displayed)
                GameObject lockOverlayGo = new GameObject("LockOverlay");
                lockOverlayGo.AddComponent<RectTransform>();
                lockOverlayGo.transform.SetParent(slotGo.transform, false);
                var lockBg = lockOverlayGo.AddComponent<UnityEngine.UI.Image>();
                lockBg.sprite = CreateRoundedRectSprite(110, 135, 22, new Color(0.05f, 0.05f, 0.05f, 0.65f));
                var lockOverlayRect = lockOverlayGo.GetComponent<RectTransform>();
                lockOverlayRect.anchorMin = Vector2.zero;
                lockOverlayRect.anchorMax = Vector2.one;
                lockOverlayRect.sizeDelta = Vector2.zero;
                lockOverlayGo.SetActive(false);

                // Cooldown Overlay
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

                // Button Click Listener mapping
                var slotBtnComp = slotGo.AddComponent<UnityEngine.UI.Button>();
                slotBtnComp.interactable = true;
                int targetSlotIndex = realIndex;
                slotBtnComp.onClick.AddListener(() => {
                    if (PlantPlacementManager.Instance != null) {
                        PlantPlacementManager.Instance.SelectPlant(targetSlotIndex);
                    }
                });
                slotGo.AddComponent<UIButtonEffects>();

                // Initialize Card
                var card = slotGo.AddComponent<PlantCard>();
                card.realSlotIndex = realIndex;
                card.Initialize(cardBgImg, glowImg, cdImg, cdText, iconImg, costText, lockOverlayGo, locked, nameText);
                card.InitializeRuntime(locked, cleanName, cost, plantSprite, slotTint);
                plantCards.Add(card);
            }
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
        popupBg.transform.SetParent(gameOverPopup.transform, false);
        var bgImage = popupBg.AddComponent<UnityEngine.UI.Image>();
        if (gameOverPopupBgSprite != null) {
            bgImage.sprite = gameOverPopupBgSprite;
            bgImage.color = Color.white;
        } else {
            Color gameOverBgBottom = new Color(0.08f, 0.16f, 0.10f, 0.98f); // Deep forest green/swamp
            Color gameOverBgTop = new Color(0.15f, 0.30f, 0.18f, 0.98f);    // Bright forest green
            Color gameOverBgBorder = new Color(0.85f, 0.75f, 0.25f, 1f);   // Fantasy gold/leafy border
            bgImage.sprite = CreateRoundedRectGradientSprite(680, 540, 36, gameOverBgBottom, gameOverBgTop, gameOverBgBorder, 6);
        }
        
        var bgRect = popupBg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        if (gameOverPopupBgSprite != null) {
            bgRect.sizeDelta = new Vector2(887f, 760f);
        } else {
            bgRect.sizeDelta = new Vector2(680f, 540f);
        }

        // Title
        var titleGo = new GameObject("Title");
        titleGo.AddComponent<RectTransform>();
        titleGo.transform.SetParent(popupBg.transform, false);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "GAME OVER";
        titleText.fontSize = 64;
        titleText.fontStyle = FontStyles.Bold;
        if (gameOverPopupBgSprite != null) {
            titleText.color = new Color(1.00f, 0.82f, 0.22f, 1f); // Gold Accent
        } else {
            titleText.color = new Color(0.95f, 0.15f, 0.15f, 1f);
        }
        titleText.alignment = TextAlignmentOptions.Center;
        
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        if (gameOverPopupBgSprite != null) {
            titleRect.anchoredPosition = new Vector2(0f, 240f);
        } else {
            titleRect.anchoredPosition = new Vector2(0f, 220f);
        }
        titleRect.sizeDelta = new Vector2(600f, 80f);

        if (gameOverPopupBgSprite != null) {
            titleGo.SetActive(false);
        }

        // Stats Area
        var statsGo = new GameObject("Stats");
        statsGo.AddComponent<RectTransform>();
        statsGo.transform.SetParent(popupBg.transform, false);
        var statsRect = statsGo.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.5f, 0.5f);
        statsRect.anchorMax = new Vector2(0.5f, 0.5f);
        statsRect.pivot = new Vector2(0.5f, 0.5f);
        if (gameOverPopupBgSprite != null) {
            statsRect.anchoredPosition = Vector2.zero;
            statsRect.sizeDelta = new Vector2(560f, 210f);
        } else {
            statsRect.anchoredPosition = new Vector2(0f, 10f);
            statsRect.sizeDelta = new Vector2(600f, 230f);
        }

        var statsImg = statsGo.AddComponent<UnityEngine.UI.Image>();
        Color innerBgBottom = new Color(0.04f, 0.08f, 0.05f, 0.95f);
        Color innerBgTop = new Color(0.07f, 0.14f, 0.09f, 0.95f);
        Color innerBorder = new Color(0.55f, 0.48f, 0.15f, 0.8f);
        int statsW = gameOverPopupBgSprite != null ? 560 : 600;
        int statsH = gameOverPopupBgSprite != null ? 210 : 230;
        statsImg.sprite = CreateRoundedRectGradientSprite(statsW, statsH, 24, innerBgBottom, innerBgTop, innerBorder, 3);

        // Restart and Main Menu Buttons (Children of popupBg now!)
        if (gameOverPopupBgSprite != null && pauseRestartBtnSprite != null && pauseMainMenuBtnSprite != null) {
            CreateStyledButton("RestartBtn", popupBg.transform, new Vector2(-178f, 48f), new Vector2(342f, 79f), "PLAY AGAIN",
                Color.black, Color.black, Color.black, 0, OnRestartButtonClicked, Color.white, pauseRestartBtnSprite);

            CreateStyledButton("MainMenuBtn", popupBg.transform, new Vector2(178f, 48f), new Vector2(300f, 68f), "MAIN MENU",
                Color.black, Color.black, Color.black, 0, OnMainMenuButtonClicked, Color.white, pauseMainMenuBtnSprite);
        } else {
            CreateStyledButton("RestartBtn", popupBg.transform, new Vector2(-140f, 55f), new Vector2(220f, 70f), "PLAY AGAIN",
                new Color(0.6f, 0.3f, 0.05f, 1f), new Color(0.85f, 0.45f, 0.1f, 1f), new Color(1f, 0.85f, 0.3f, 1f), 5, OnRestartButtonClicked);

            CreateStyledButton("MainMenuBtn", popupBg.transform, new Vector2(140f, 55f), new Vector2(220f, 70f), "MAIN MENU",
                new Color(0.18f, 0.4f, 0.12f, 1f), new Color(0.35f, 0.65f, 0.25f, 1f), new Color(0.85f, 0.75f, 0.25f, 1f), 5, OnMainMenuButtonClicked);
        }

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
        if (pausePopupBgSprite != null) {
            pausePopupRect.sizeDelta = new Vector2(942f, 770f);
        } else {
            pausePopupRect.sizeDelta = new Vector2(700f, 500f);
        }

        var pauseBg = new GameObject("Background");
        pauseBg.AddComponent<RectTransform>();
        pauseBg.transform.SetParent(pausePopup.transform, false);
        var pauseBgImg = pauseBg.AddComponent<UnityEngine.UI.Image>();
        if (pausePopupBgSprite != null) {
            pauseBgImg.sprite = pausePopupBgSprite;
            pauseBgImg.color = Color.white;
        } else {
            Color pauseBgBottom = new Color(0.04f, 0.12f, 0.06f, 0.96f); // Dark Green
            Color pauseBgTop = new Color(0.08f, 0.22f, 0.12f, 0.96f);    // Forest Green
            Color pauseBgBorder = new Color(0.90f, 0.75f, 0.25f, 1f);    // Gold Accent Border
            pauseBgImg.sprite = CreateRoundedRectGradientSprite(700, 500, 40, pauseBgBottom, pauseBgTop, pauseBgBorder, 5);
        }
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
        pauseTitleText.color = new Color(1.00f, 0.82f, 0.22f, 1f); // Gold Accent
        pauseTitleText.alignment = TextAlignmentOptions.Center;
        
        var pauseTitleRect = pauseTitleGo.GetComponent<RectTransform>();
        pauseTitleRect.anchorMin = new Vector2(0.5f, 1f);
        pauseTitleRect.anchorMax = new Vector2(0.5f, 1f);
        pauseTitleRect.pivot = new Vector2(0.5f, 1f);
        pauseTitleRect.anchoredPosition = new Vector2(0f, -100f);
        pauseTitleRect.sizeDelta = new Vector2(600f, 80f);

        if (pausePopupBgSprite != null) {
            pauseTitleGo.SetActive(false);
        }

        var pauseSubtitleGo = new GameObject("Subtitle");
        pauseSubtitleGo.AddComponent<RectTransform>();
        pauseSubtitleGo.transform.SetParent(pausePopup.transform, false);
        var pauseSubtitleText = pauseSubtitleGo.AddComponent<TextMeshProUGUI>();
        pauseSubtitleText.text = "Garden Guardians • Zombie Defense";
        pauseSubtitleText.fontSize = 24;
        pauseSubtitleText.fontStyle = FontStyles.Italic | FontStyles.Bold;
        pauseSubtitleText.color = new Color(0.95f, 0.92f, 0.85f, 1f); // Soft Cream Text
        pauseSubtitleText.alignment = TextAlignmentOptions.Center;

        var pauseSubtitleRect = pauseSubtitleGo.GetComponent<RectTransform>();
        pauseSubtitleRect.anchorMin = new Vector2(0.5f, 1f);
        pauseSubtitleRect.anchorMax = new Vector2(0.5f, 1f);
        pauseSubtitleRect.pivot = new Vector2(0.5f, 1f);
        pauseSubtitleRect.anchoredPosition = new Vector2(0f, -160f);
        pauseSubtitleRect.sizeDelta = new Vector2(600f, 50f);

        if (pausePopupBgSprite != null) {
            pauseSubtitleGo.SetActive(false);
        }

        // Pause buttons (Forest Green, Wood Brown, Moss/Olive, or custom sprites stacked vertically)
        if (pausePopupBgSprite != null && pauseResumeBtnSprite != null && pauseRestartBtnSprite != null && pauseMainMenuBtnSprite != null) {
            CreateStyledButton("ResumeBtn", pausePopup.transform, new Vector2(0f, 80f), new Vector2(360f, 85f), "RESUME",
                Color.black, Color.black, Color.black, 0, TogglePause, Color.white, pauseResumeBtnSprite);

            CreateStyledButton("RestartBtn", pausePopup.transform, new Vector2(0f, -30f), new Vector2(360f, 83f), "RESTART",
                Color.black, Color.black, Color.black, 0, OnRestartButtonClicked, Color.white, pauseRestartBtnSprite);

            CreateStyledButton("MainMenuBtn", pausePopup.transform, new Vector2(0f, -140f), new Vector2(315f, 71f), "MAIN MENU",
                Color.black, Color.black, Color.black, 0, OnMainMenuButtonClicked, Color.white, pauseMainMenuBtnSprite);
        } else {
            CreateStyledButton("ResumeBtn", pausePopup.transform, new Vector2(-200f, 90f), new Vector2(180f, 70f), "RESUME",
                new Color(0.12f, 0.38f, 0.16f, 1f), new Color(0.22f, 0.60f, 0.28f, 1f), new Color(0.90f, 0.75f, 0.25f, 1f), 4, TogglePause, new Color(0.95f, 0.92f, 0.85f, 1f));

            CreateStyledButton("RestartBtn", pausePopup.transform, new Vector2(0f, 90f), new Vector2(180f, 70f), "RESTART",
                new Color(0.35f, 0.18f, 0.05f, 1f), new Color(0.55f, 0.32f, 0.12f, 1f), new Color(0.90f, 0.75f, 0.25f, 1f), 4, OnRestartButtonClicked, new Color(0.95f, 0.92f, 0.85f, 1f));

            CreateStyledButton("MainMenuBtn", pausePopup.transform, new Vector2(200f, 90f), new Vector2(180f, 70f), "MAIN MENU",
                new Color(0.25f, 0.22f, 0.12f, 1f), new Color(0.42f, 0.38f, 0.22f, 1f), new Color(0.90f, 0.75f, 0.25f, 1f), 4, OnMainMenuButtonClicked, new Color(0.95f, 0.92f, 0.85f, 1f));
        }
    }

    private void CreateStyledButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color bottom, Color top, Color border, int borderWidth, UnityEngine.Events.UnityAction action, Color? textColor = null, Sprite buttonSprite = null) {
        var btnGo = new GameObject(name);
        btnGo.AddComponent<RectTransform>();
        btnGo.transform.SetParent(parent, false);
        
        var img = btnGo.AddComponent<UnityEngine.UI.Image>();
        if (buttonSprite != null) {
            img.sprite = buttonSprite;
            img.color = Color.white;
        } else {
            img.sprite = CreateRoundedRectGradientSprite(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), 22, bottom, top, border, borderWidth);
        }

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
        btnText.color = textColor ?? Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

        var btnTextRect = btnTextGo.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        if (buttonSprite != null) {
            btnTextGo.SetActive(false);
        }

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
                plantCards[i].SetSelected(plantCards[i].realSlotIndex == activeIndex);
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

        if (AudioManager.Instance != null) {
            AudioManager.Instance.Play(SFXType.CoinEarned);
        }

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
            if (img == null) yield break;
            elapsed += Time.deltaTime;
            img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0f, maxAlpha, elapsed / halfDuration));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration) {
            if (img == null) yield break;
            elapsed += Time.deltaTime;
            img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(maxAlpha, 0f, elapsed / halfDuration));
            yield return null;
        }

        if (flashGo != null) {
            Destroy(flashGo);
        }
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

    private void CreateOrSetupSoundButton() {
        if (hudCanvas == null) return;

        activeSoundOnSprite = soundOnSprite != null ? soundOnSprite : CreateSoundOnSprite(64, 64);
        activeSoundOffSprite = soundOffSprite != null ? soundOffSprite : CreateSoundOffSprite(64, 64);

        Transform existingButton = hudCanvas.transform.Find("SoundToggleButton");
        GameObject buttonGo;

        if (existingButton != null) {
            buttonGo = existingButton.gameObject;
        } else {
            buttonGo = new GameObject("SoundToggleButton");
            buttonGo.transform.SetParent(hudCanvas.transform, false);

            var rect = buttonGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-135f, -40f);
            rect.sizeDelta = new Vector2(75f, 75f);

            var img = buttonGo.AddComponent<UnityEngine.UI.Image>();
            Color pauseBottom = new Color(0.10f, 0.10f, 0.12f, 0.85f);
            Color pauseTop = new Color(0.22f, 0.22f, 0.25f, 0.85f);
            Color pauseBorder = new Color(0.70f, 0.70f, 0.75f, 1f);
            img.sprite = CreateRoundedRectGradientSprite(75, 75, 37, pauseBottom, pauseTop, pauseBorder, 4);

            var iconGo = new GameObject("SoundIcon");
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconGo.transform.SetParent(buttonGo.transform, false);
            
            var iconImg = iconGo.AddComponent<UnityEngine.UI.Image>();
            iconImg.preserveAspect = true;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(38f, 38f);

            var btnComp = buttonGo.AddComponent<UnityEngine.UI.Button>();
            buttonGo.AddComponent<UIButtonEffects>();
        }

        soundToggleButtonComp = buttonGo.GetComponent<UnityEngine.UI.Button>();

        // Set sibling index to draw behind modal popups, exactly like the PauseButton
        Transform pauseBtnTrans = hudCanvas.transform.Find("PauseButton");
        if (pauseBtnTrans != null) {
            buttonGo.transform.SetSiblingIndex(pauseBtnTrans.GetSiblingIndex() + 1);
        }
        
        var iconTrans = buttonGo.transform.Find("SoundIcon");
        if (iconTrans != null) {
            soundIconImgComp = iconTrans.GetComponent<UnityEngine.UI.Image>();
        }

        if (soundToggleButtonComp != null) {
            soundToggleButtonComp.onClick.RemoveAllListeners();
            soundToggleButtonComp.onClick.AddListener(ToggleSoundGameplay);
        }

        UpdateSoundButtonVisuals();
    }

    private void ToggleSoundGameplay() {
        if (AudioManager.Instance != null) {
            AudioManager.Instance.IsMuted = !AudioManager.Instance.IsMuted;
        }
        UpdateSoundButtonVisuals();
    }

    private void UpdateSoundButtonVisuals() {
        if (soundIconImgComp == null) return;

        bool muted = false;
        if (AudioManager.Instance != null) {
            muted = AudioManager.Instance.IsMuted;
        }

        soundIconImgComp.sprite = muted ? activeSoundOffSprite : activeSoundOnSprite;
    }

    private Sprite CreateSoundOnSprite(int width, int height) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        float cx = width / 2f;
        float cy = height / 2f;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float sx = x + width * 0.08f;
                float dx = sx - cx;
                float dy = y - cy;

                bool inBody = (dx >= -width * 0.22f && dx <= -width * 0.08f && Mathf.Abs(dy) <= height * 0.14f);

                bool inCone = false;
                if (dx >= -width * 0.08f && dx <= width * 0.08f) {
                    float t = (dx + width * 0.08f) / (width * 0.16f);
                    float h = Mathf.Lerp(height * 0.14f, height * 0.32f, t);
                    if (Mathf.Abs(dy) <= h) {
                        inCone = true;
                    }
                }

                float arcCx = cx - width * 0.08f;
                float adx = x - arcCx;
                float ady = y - cy;
                float dist = Mathf.Sqrt(adx * adx + ady * ady);
                float angle = Mathf.Atan2(ady, adx);

                bool inWaves = false;
                if (Mathf.Abs(angle) <= Mathf.PI * 0.3f) {
                    bool innerWave = (dist >= width * 0.22f && dist <= width * 0.28f);
                    bool outerWave = (dist >= width * 0.38f && dist <= width * 0.44f);
                    if (innerWave || outerWave) {
                        inWaves = true;
                    }
                }

                if (inBody || inCone || inWaves) {
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

    private Sprite CreateSoundOffSprite(int width, int height) {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        float cx = width / 2f;
        float cy = height / 2f;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float sx = x + width * 0.08f;
                float dx = sx - cx;
                float dy = y - cy;

                bool inBody = (dx >= -width * 0.22f && dx <= -width * 0.08f && Mathf.Abs(dy) <= height * 0.14f);

                bool inCone = false;
                if (dx >= -width * 0.08f && dx <= width * 0.08f) {
                    float t = (dx + width * 0.08f) / (width * 0.16f);
                    float h = Mathf.Lerp(height * 0.14f, height * 0.32f, t);
                    if (Mathf.Abs(dy) <= h) {
                        inCone = true;
                    }
                }

                float xCx = cx + width * 0.22f;
                float rdx = x - xCx;
                float rdy = y - cy;
                bool inX = false;
                if (Mathf.Abs(rdx) <= width * 0.12f && Mathf.Abs(rdy) <= height * 0.12f) {
                    float thickness = 3.5f;
                    if (Mathf.Abs(rdx - rdy) <= thickness || Mathf.Abs(rdx + rdy) <= thickness) {
                        inX = true;
                    }
                }

                if (inBody || inCone || inX) {
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

}