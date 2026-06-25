using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class PlantCollectionManager : MonoBehaviour {
    [System.Serializable]
    public class PlantConfig {
        public string name;
        public int cost;
        public string category;
        public float lifetime;
        [TextArea(3, 5)]
        public string description;
        public int unlockCost; // Cost in Global Coins to unlock permanently
    }

    [Header("UI References")]
    [SerializeField] private Button backButton;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Container")]
    [SerializeField] private Transform cardsGridParent;

    [Header("Plant Configurations")]
    [SerializeField] private PlantConfig[] plantConfigs;

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
        new PlantCollectionItem { name = "Gun Guardian", displayName = "Gun Guardian", isLocked = true },
        new PlantCollectionItem { name = "Guardian Oak", displayName = "Guardian Oak", isLocked = true },
        new PlantCollectionItem { name = "Sunflower Tree", displayName = "Sunflower Tree", isLocked = true }
    };

    // Popup UI references
    private GameObject detailsPopupGo;
    private TextMeshProUGUI popupTitleText;
    private TextMeshProUGUI popupStatusText;
    private UnityEngine.UI.Image popupIconImg;
    private TextMeshProUGUI popupTypeText;
    private TextMeshProUGUI popupCostText;
    private TextMeshProUGUI popupLifetimeText;
    private TextMeshProUGUI popupDescText;
    private UnityEngine.UI.Image popupPanelBg;
    private CanvasGroup popupCanvasGroup;

    // Unlock Button references
    private GameObject popupUnlockButtonGo;
    private Button popupUnlockButton;
    private TextMeshProUGUI popupUnlockText;
    private RectTransform popupCloseButtonRect;

    private void Start() {
        // Initialize lock states from progression
        for (int i = 0; i < plantItems.Length; i++) {
            plantItems[i].isLocked = IsPlantLocked(plantItems[i].name);
        }

        // Add progression coins display to the canvas
        var canvasComp = FindFirstObjectByType<Canvas>();
        if (canvasComp != null && canvasComp.gameObject.GetComponent<MainMenuProgressionCoinsDisplay>() == null) {
            canvasComp.gameObject.AddComponent<MainMenuProgressionCoinsDisplay>();
        }

        if (backButton != null) {
            backButton.onClick.AddListener(GoBackToMainMenu);
        }

        // Ensure scroll view has a transparent image to catch dragging on empty space
        if (cardsGridParent != null && cardsGridParent.parent != null && cardsGridParent.parent.parent != null) {
            var scrollView = cardsGridParent.parent.parent.gameObject;
            if (scrollView.GetComponent<UnityEngine.UI.Image>() == null) {
                var img = scrollView.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(0f, 0f, 0f, 0f);
                img.raycastTarget = true;
            }
        }

        // Fade in
        if (fadeOverlay != null) {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.alpha = 1f;
            StartCoroutine(FadeRoutine(1f, 0f));
        }

        CreateDetailsPopupUI();
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
        
        // We will layout in rows of 3 columns
        float colSpacing = 320f;
        float rowSpacing = 360f;

        float startX = -colSpacing; // -320, 0, 320

        int totalRows = Mathf.CeilToInt((float)plantItems.Length / 3f);
        float contentHeight = totalRows * rowSpacing + 40f; // 40px bottom padding

        // Dynamic content height adjusting for scroll content
        var contentRect = cardsGridParent.GetComponent<RectTransform>();
        if (contentRect != null) {
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0f);
        }

        for (int i = 0; i < plantItems.Length; i++) {
            var item = plantItems[i];
            int row = i / 3;
            int col = i % 3;

            var cardGo = new GameObject($"Card_{item.name.Replace(" ", "")}");
            var cardRect = cardGo.AddComponent<RectTransform>();
            cardGo.transform.SetParent(cardsGridParent, false);

            cardRect.anchorMin = new Vector2(0.5f, 1f); // Anchored to top-center of Content
            cardRect.anchorMax = new Vector2(0.5f, 1f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(startX + col * colSpacing, -(row * rowSpacing + 180f));
            cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);

            // Card Background Image (this represents the button background)
            var bgImg = cardGo.AddComponent<UnityEngine.UI.Image>();
            bgImg.raycastTarget = true; // Ensure clicks are received
            
            // Colors for Unlocked vs Locked card background
            Color bgBottom = item.isLocked ? new Color(0.12f, 0.12f, 0.12f, 0.9f) : new Color(0.08f, 0.18f, 0.11f, 0.95f);
            Color bgTop = item.isLocked ? new Color(0.20f, 0.20f, 0.20f, 0.9f) : new Color(0.15f, 0.30f, 0.18f, 0.95f);
            Color bgBorder = item.isLocked ? new Color(0.35f, 0.35f, 0.35f, 0.8f) : new Color(0.85f, 0.75f, 0.25f, 1f);
            
            bgImg.sprite = CreateRoundedRectGradientSprite(240, 320, 30, bgBottom, bgTop, bgBorder, 5);

            // Add Button component to the card
            var cardBtn = cardGo.AddComponent<UnityEngine.UI.Button>();
            cardBtn.transition = UnityEngine.UI.Selectable.Transition.None;
            string pName = item.name;
            bool isLocked = item.isLocked;
            cardBtn.onClick.AddListener(() => {
                OpenDetailsPopup(pName, isLocked);
            });
            cardGo.AddComponent<UIButtonEffects>();

            // Plant Icon Container (so we can scale or position it cleanly)
            var iconGo = new GameObject("Icon");
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconGo.transform.SetParent(cardGo.transform, false);
            var iconImg = iconGo.AddComponent<UnityEngine.UI.Image>();
            iconImg.raycastTarget = false; // let clicks pass through to button
            
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
            nameText.raycastTarget = false; // let clicks pass through to button
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
                lockBg.raycastTarget = false; // let clicks pass through to button
                lockBg.sprite = CreateRoundedRectSprite(240, 320, 30, new Color(0f, 0f, 0f, 0.25f));
                
                lockOverlayRect.anchorMin = Vector2.zero;
                lockOverlayRect.anchorMax = Vector2.one;
                lockOverlayRect.sizeDelta = Vector2.zero;

                var lockIconGo = new GameObject("LockIcon");
                var lockIconRect = lockIconGo.AddComponent<RectTransform>();
                lockIconGo.transform.SetParent(lockOverlayGo.transform, false);
                var lockIconImg = lockIconGo.AddComponent<UnityEngine.UI.Image>();
                lockIconImg.raycastTarget = false; // let clicks pass through to button
                lockIconImg.sprite = CreateLockSprite(64, 64);
                
                lockIconRect.anchorMin = new Vector2(0.5f, 0.5f);
                lockIconRect.anchorMax = new Vector2(0.5f, 0.5f);
                lockIconRect.pivot = new Vector2(0.5f, 0.5f);
                lockIconRect.anchoredPosition = new Vector2(0f, 30f); // align with plant icon center
                lockIconRect.sizeDelta = new Vector2(64f, 64f);
            }
        }
    }

    private void CreateDetailsPopupUI() {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        // Overlay background
        detailsPopupGo = new GameObject("DetailsPopup");
        var popupRect = detailsPopupGo.AddComponent<RectTransform>();
        detailsPopupGo.transform.SetParent(canvas.transform, false);
        popupRect.anchorMin = Vector2.zero;
        popupRect.anchorMax = Vector2.one;
        popupRect.sizeDelta = Vector2.zero;

        var overlayImg = detailsPopupGo.AddComponent<UnityEngine.UI.Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.78f);
        overlayImg.raycastTarget = true; // Block clicks to background elements

        popupCanvasGroup = detailsPopupGo.AddComponent<CanvasGroup>();
        popupCanvasGroup.alpha = 0f;
        detailsPopupGo.SetActive(false);

        // Center Panel
        var panelGo = new GameObject("Panel");
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelGo.transform.SetParent(detailsPopupGo.transform, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(640f, 720f);

        popupPanelBg = panelGo.AddComponent<UnityEngine.UI.Image>();

        // Title
        var titleGo = new GameObject("Title");
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleGo.transform.SetParent(panelGo.transform, false);
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 300f);
        titleRect.sizeDelta = new Vector2(500f, 50f);
        popupTitleText = titleGo.AddComponent<TextMeshProUGUI>();
        popupTitleText.fontSize = 38;
        popupTitleText.fontStyle = FontStyles.Bold;
        popupTitleText.color = Color.white;
        popupTitleText.alignment = TextAlignmentOptions.Center;

        // Icon Container
        var iconContGo = new GameObject("IconContainer");
        var iconContRect = iconContGo.AddComponent<RectTransform>();
        iconContGo.transform.SetParent(panelGo.transform, false);
        iconContRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconContRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconContRect.pivot = new Vector2(0.5f, 0.5f);
        iconContRect.anchoredPosition = new Vector2(0f, 160f);
        iconContRect.sizeDelta = new Vector2(160f, 160f);
        var iconContImg = iconContGo.AddComponent<UnityEngine.UI.Image>();
        iconContImg.sprite = CreateRoundedRectSprite(160, 160, 20, new Color(0f, 0f, 0f, 0.25f));

        // Icon
        var iconGo = new GameObject("Icon");
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconGo.transform.SetParent(iconContGo.transform, false);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = Vector2.zero;
        popupIconImg = iconGo.AddComponent<UnityEngine.UI.Image>();

        // Description (Abilities list - 460 width, left aligned for proper bullets layout)
        var descGo = new GameObject("Description");
        var descRect = descGo.AddComponent<RectTransform>();
        descGo.transform.SetParent(panelGo.transform, false);
        descRect.anchorMin = new Vector2(0.5f, 0.5f);
        descRect.anchorMax = new Vector2(0.5f, 0.5f);
        descRect.pivot = new Vector2(0.5f, 0.5f);
        descRect.anchoredPosition = new Vector2(0f, -35f);
        descRect.sizeDelta = new Vector2(460f, 160f);
        popupDescText = descGo.AddComponent<TextMeshProUGUI>();
        popupDescText.fontSize = 18;
        popupDescText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        popupDescText.alignment = TextAlignmentOptions.TopLeft;
        popupDescText.lineSpacing = 6f;

        // Type
        var typeGo = new GameObject("TypeLabel");
        var typeRect = typeGo.AddComponent<RectTransform>();
        typeGo.transform.SetParent(panelGo.transform, false);
        typeRect.anchorMin = new Vector2(0.5f, 0.5f);
        typeRect.anchorMax = new Vector2(0.5f, 0.5f);
        typeRect.pivot = new Vector2(0.5f, 0.5f);
        typeRect.anchoredPosition = new Vector2(0f, -145f);
        typeRect.sizeDelta = new Vector2(500f, 30f);
        popupTypeText = typeGo.AddComponent<TextMeshProUGUI>();
        popupTypeText.fontSize = 22;
        popupTypeText.fontStyle = FontStyles.Bold;
        popupTypeText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        popupTypeText.alignment = TextAlignmentOptions.Center;

        // Cost
        var costGo = new GameObject("CostLabel");
        var costRect = costGo.AddComponent<RectTransform>();
        costGo.transform.SetParent(panelGo.transform, false);
        costRect.anchorMin = new Vector2(0.5f, 0.5f);
        costRect.anchorMax = new Vector2(0.5f, 0.5f);
        costRect.pivot = new Vector2(0.5f, 0.5f);
        costRect.anchoredPosition = new Vector2(0f, -178f);
        costRect.sizeDelta = new Vector2(500f, 30f);
        popupCostText = costGo.AddComponent<TextMeshProUGUI>();
        popupCostText.fontSize = 22;
        popupCostText.fontStyle = FontStyles.Bold;
        popupCostText.color = new Color(1f, 0.85f, 0.15f, 1f);
        popupCostText.alignment = TextAlignmentOptions.Center;

        // Lifetime
        var lifeGo = new GameObject("LifetimeLabel");
        var lifeRect = lifeGo.AddComponent<RectTransform>();
        lifeGo.transform.SetParent(panelGo.transform, false);
        lifeRect.anchorMin = new Vector2(0.5f, 0.5f);
        lifeRect.anchorMax = new Vector2(0.5f, 0.5f);
        lifeRect.pivot = new Vector2(0.5f, 0.5f);
        lifeRect.anchoredPosition = new Vector2(0f, -211f);
        lifeRect.sizeDelta = new Vector2(500f, 30f);
        popupLifetimeText = lifeGo.AddComponent<TextMeshProUGUI>();
        popupLifetimeText.fontSize = 22;
        popupLifetimeText.fontStyle = FontStyles.Bold;
        popupLifetimeText.color = new Color(0.2f, 0.85f, 0.95f, 1f);
        popupLifetimeText.alignment = TextAlignmentOptions.Center;

        // Status
        var statusGo = new GameObject("Status");
        var statusRect = statusGo.AddComponent<RectTransform>();
        statusGo.transform.SetParent(panelGo.transform, false);
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.anchoredPosition = new Vector2(0f, -244f);
        statusRect.sizeDelta = new Vector2(500f, 30f);
        popupStatusText = statusGo.AddComponent<TextMeshProUGUI>();
        popupStatusText.fontSize = 22;
        popupStatusText.fontStyle = FontStyles.Bold;
        popupStatusText.alignment = TextAlignmentOptions.Center;

        // Close Button
        var closeGo = new GameObject("CloseButton");
        var closeRect = closeGo.AddComponent<RectTransform>();
        closeGo.transform.SetParent(panelGo.transform, false);
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(0f, -305f);
        closeRect.sizeDelta = new Vector2(220f, 55f);

        var closeBg = closeGo.AddComponent<UnityEngine.UI.Image>();
        closeBg.sprite = CreateRoundedRectGradientSprite(220, 55, 15, new Color(0.75f, 0.15f, 0.1f, 0.95f), new Color(0.9f, 0.3f, 0.15f, 0.95f), new Color(1f, 0.85f, 0.2f, 1f), 3);

        var closeBtn = closeGo.AddComponent<UnityEngine.UI.Button>();
        closeBtn.onClick.AddListener(CloseDetailsPopup);
        closeGo.AddComponent<UIButtonEffects>();

        var closeTextGo = new GameObject("Text");
        var closeTextRect = closeTextGo.AddComponent<RectTransform>();
        closeTextGo.transform.SetParent(closeGo.transform, false);
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.sizeDelta = Vector2.zero;
        var closeText = closeTextGo.AddComponent<TextMeshProUGUI>();
        closeText.text = "CLOSE";
        closeText.fontSize = 24;
        closeText.fontStyle = FontStyles.Bold;
        closeText.color = Color.white;
        closeText.alignment = TextAlignmentOptions.Center;

        // Unlock Button
        var unlockGo = new GameObject("UnlockButton");
        var unlockRect = unlockGo.AddComponent<RectTransform>();
        unlockGo.transform.SetParent(panelGo.transform, false);
        unlockRect.anchorMin = new Vector2(0.5f, 0.5f);
        unlockRect.anchorMax = new Vector2(0.5f, 0.5f);
        unlockRect.pivot = new Vector2(0.5f, 0.5f);
        unlockRect.anchoredPosition = new Vector2(0f, -305f);
        unlockRect.sizeDelta = new Vector2(220f, 55f);

        var unlockBg = unlockGo.AddComponent<UnityEngine.UI.Image>();
        unlockBg.sprite = CreateRoundedRectGradientSprite(220, 55, 15, new Color(0.15f, 0.45f, 0.2f, 0.95f), new Color(0.25f, 0.65f, 0.3f, 0.95f), new Color(0.25f, 0.85f, 0.35f, 1f), 3);

        var unlockBtn = unlockGo.AddComponent<UnityEngine.UI.Button>();
        unlockGo.AddComponent<UIButtonEffects>();

        var unlockTextGo = new GameObject("Text");
        var unlockTextRect = unlockTextGo.AddComponent<RectTransform>();
        unlockTextGo.transform.SetParent(unlockGo.transform, false);
        unlockTextRect.anchorMin = Vector2.zero;
        unlockTextRect.anchorMax = Vector2.one;
        unlockTextRect.sizeDelta = Vector2.zero;
        var unlockText = unlockTextGo.AddComponent<TextMeshProUGUI>();
        unlockText.text = "UNLOCK";
        unlockText.fontSize = 24;
        unlockText.fontStyle = FontStyles.Bold;
        unlockText.color = Color.white;
        unlockText.alignment = TextAlignmentOptions.Center;

        // Store references
        popupUnlockButtonGo = unlockGo;
        popupUnlockButton = unlockBtn;
        popupUnlockText = unlockText;
        popupCloseButtonRect = closeRect;

        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        TMP_FontAsset fontToUse = null;
        foreach (var f in fonts) {
            if (f.name.Contains("Salsa") || f.name.Contains("Liberation")) {
                fontToUse = f;
                break;
            }
        }
        if (fontToUse != null) {
            popupTitleText.font = fontToUse;
            popupStatusText.font = fontToUse;
            popupTypeText.font = fontToUse;
            popupCostText.font = fontToUse;
            popupLifetimeText.font = fontToUse;
            popupDescText.font = fontToUse;
            closeText.font = fontToUse;
            unlockText.font = fontToUse;
        }
    }

    private void OpenDetailsPopup(string plantName, bool isLocked) {
        PlantConfig config = null;
        if (plantConfigs != null) {
            foreach (var cfg in plantConfigs) {
                if (cfg.name == plantName) {
                    config = cfg;
                    break;
                }
            }
        }

        if (config == null) {
            Debug.LogError($"Plant config not found for: {plantName}");
            return;
        }

        popupTitleText.text = config.name.ToUpper();

        if (isLocked) {
            popupStatusText.text = "LOCKED";
            popupStatusText.color = new Color(0.85f, 0.25f, 0.2f, 1f);
        } else {
            popupStatusText.text = "UNLOCKED";
            popupStatusText.color = new Color(0.25f, 0.85f, 0.35f, 1f);
        }

        popupIconImg.sprite = PlantVisuals.GetPlantSprite(config.name);
        popupIconImg.color = isLocked ? new Color(0.25f, 0.25f, 0.25f, 0.6f) : Color.white;

        popupTypeText.text = $"Type: {GetPlantType(config.category, config.name)}";
        if (isLocked) {
            popupCostText.text = $"Cost: {config.unlockCost} Global Coins";
        } else {
            popupCostText.text = $"Cost: {config.cost} Coins";
        }
        popupLifetimeText.text = $"Lifetime: {GetLifetimeText(config.lifetime, config.category)}";

        popupDescText.text = config.description;

        // Manage buttons placement
        if (isLocked) {
            if (popupUnlockButtonGo != null) {
                popupUnlockButtonGo.SetActive(true);
                popupUnlockButton.onClick.RemoveAllListeners();
                popupUnlockButton.onClick.AddListener(() => OnClickUnlock(config));
                popupUnlockButtonGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120f, -305f);
            }
            if (popupCloseButtonRect != null) {
                popupCloseButtonRect.anchoredPosition = new Vector2(120f, -305f);
            }
        } else {
            if (popupUnlockButtonGo != null) {
                popupUnlockButtonGo.SetActive(false);
            }
            if (popupCloseButtonRect != null) {
                popupCloseButtonRect.anchoredPosition = new Vector2(0f, -305f);
            }
        }

        Color bgBottom = isLocked ? new Color(0.12f, 0.12f, 0.12f, 0.98f) : new Color(0.08f, 0.18f, 0.11f, 0.98f);
        Color bgTop = isLocked ? new Color(0.20f, 0.20f, 0.20f, 0.98f) : new Color(0.15f, 0.30f, 0.18f, 0.95f);
        Color bgBorder = isLocked ? new Color(0.35f, 0.35f, 0.35f, 0.8f) : new Color(0.85f, 0.75f, 0.25f, 1f);
        popupPanelBg.sprite = CreateRoundedRectGradientSprite(640, 720, 36, bgBottom, bgTop, bgBorder, 5);

        StopAllCoroutines();
        detailsPopupGo.SetActive(true);
        StartCoroutine(FadePopupRoutine(0f, 1f));
    }

    private void CloseDetailsPopup() {
        StopAllCoroutines();
        StartCoroutine(FadePopupRoutine(1f, 0f, () => {
            detailsPopupGo.SetActive(false);
        }));
    }

    private IEnumerator FadePopupRoutine(float fromAlpha, float toAlpha, System.Action onComplete = null) {
        float elapsed = 0f;
        float duration = 0.2f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            if (popupCanvasGroup != null) {
                popupCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            }
            yield return null;
        }
        if (popupCanvasGroup != null) {
            popupCanvasGroup.alpha = toAlpha;
        }
        if (onComplete != null) {
            onComplete();
        }
    }

    private string GetPlantType(string categoryName, string name) {
        if (categoryName.Contains("Attack")) {
            if (name.Contains("Magic")) return "Global Attack Plant";
            return "Damage Plant";
        }
        if (categoryName.Contains("Trap")) {
            if (name.Contains("Frost")) return "Freeze Plant";
            return "Trap Plant";
        }
        if (categoryName.Contains("Economy")) return "Economy Plant";
        if (categoryName.Contains("Tank") || name.Contains("Oak")) return "Defense Plant";
        return categoryName + " Plant";
    }

    private string GetLifetimeText(float lifetime, string categoryName) {
        if (lifetime <= 0) {
            if (categoryName.Contains("Trap")) return "Until Activated";
            return "Permanent";
        }
        return $"{lifetime} Seconds";
    }

    private bool IsPlantLocked(string plantName) {
        if (GlobalProgressionManager.Instance != null) {
            return GlobalProgressionManager.Instance.IsPlantLocked(plantName);
        }
        if (plantName == "Fire Bloom" || plantName == "Frost Flower") {
            return false;
        }
        return PlayerPrefs.GetInt("PlantUnlocked_" + plantName, 0) == 0;
    }

    private void OnClickUnlock(PlantConfig config) {
        if (GlobalProgressionManager.Instance == null) return;

        if (GlobalProgressionManager.Instance.GetCoins() >= config.unlockCost) {
            if (GlobalProgressionManager.Instance.RemoveCoins(config.unlockCost)) {
                GlobalProgressionManager.Instance.UnlockPlant(config.name);

                // Update lock states locally
                for (int i = 0; i < plantItems.Length; i++) {
                    if (plantItems[i].name == config.name) {
                        plantItems[i].isLocked = false;
                        break;
                    }
                }

                // Regenerate the list of cards
                GeneratePlantCards();

                // Refresh details popup to unlocked view
                OpenDetailsPopup(config.name, false);
            }
        } else {
            StartCoroutine(ShowFeedbackRoutine("NOT ENOUGH COINS"));
        }
    }

    private IEnumerator ShowFeedbackRoutine(string message) {
        if (popupUnlockText != null && popupUnlockButton != null) {
            string originalText = popupUnlockText.text;
            popupUnlockText.text = message;
            popupUnlockText.color = new Color(1.0f, 0.3f, 0.3f, 1f);
            popupUnlockButton.interactable = false;

            yield return new WaitForSeconds(1.5f);

            popupUnlockText.text = originalText;
            popupUnlockText.color = Color.white;
            popupUnlockButton.interactable = true;
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

#if UNITY_EDITOR
    [ContextMenu("Sync From Demo Scene")]
    public void SyncFromDemoScene() {
        string demoScenePath = "Assets/Scenes/demo.unity";
        var demoScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(demoScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        
        var gameManager = GameObject.Find("GameManager");
        if (gameManager == null) {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(demoScene, true);
            Debug.LogError("GameManager not found in demo.unity");
            return;
        }

        var ppm = gameManager.GetComponent<PlantPlacementManager>();
        if (ppm == null) {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(demoScene, true);
            Debug.LogError("PlantPlacementManager not found on GameManager in demo.unity");
            return;
        }

        var field = ppm.GetType().GetField("slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field == null) {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(demoScene, true);
            Debug.LogError("slots field not found in PlantPlacementManager");
            return;
        }

        var slotsList = field.GetValue(ppm) as System.Collections.IList;
        if (slotsList == null) {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(demoScene, true);
            Debug.LogError("slots list is null in PlantPlacementManager");
            return;
        }

        var list = new System.Collections.Generic.List<PlantConfig>();
        
        var descMap = new System.Collections.Generic.Dictionary<string, string> {
            { "Fire Bloom", "<align=center><b>Abilities:</b></align>\n\n<align=left><indent=60px>• Launches fire projectiles at zombies in its lane.\n\n• Reliable damage dealer.\n\n• Good starter attack plant.</indent></align>" },
            { "Frost Flower", "<align=center><b>Abilities:</b></align>\n\n<align=left><indent=60px>• Freezes zombies that enter its tile.\n\n• Stops enemy movement temporarily.\n\n• Consumed after activation.</indent></align>" },
            { "Thorn Vine", "<align=center><b>Abilities:</b></align>\n\n<align=left><indent=60px>• Very fast attack speed.\n\n• Continuously damages zombies.\n\n• Effective against weaker enemies.</indent></align>" },
            { "Bomb Cactus", "<align=center><b>Abilities:</b></align>\n\n<align=left><indent=60px>• Explodes when zombies enter its tile.\n\n• Deals area damage.\n\n• Removed after detonation.</indent></align>" },
            { "Magic Blossom", "<align=center><b>Abilities:</b></align>\n\n<align=left><indent=60px>• Attacks zombies anywhere on the battlefield.\n\n• Uses rapid magical attacks.\n\n• High damage over time.</indent></align>" },
            { "Gun Guardian", "<align=center><b>Abilities:</b></align>\n\n<align=left><indent=60px>• Shoots rapid physical projectiles at zombies.\n\n• High rate of fire.\n\n• Excellent for sustaining damage lanes.</indent></align>" },
            { "Guardian Oak", "<align=center><b>Abilities:</b></align>\n\n<align=left><indent=60px>• Extremely high health.\n\n• Blocks zombies from advancing.\n\n• Protects nearby attack plants.</indent></align>" },
            { "Sunflower Tree", "<align=center><b>Abilities:</b></align>\n\n<align=left><indent=60px>• Generates coins over time.\n\n• Supports long-term economy growth.\n\n• Helps build stronger defenses.</indent></align>" }
        };

        foreach (var slot in slotsList) {
            if (slot == null) continue;
            string fullName = slot.GetType().GetField("name").GetValue(slot) as string;
            int cost = (int)slot.GetType().GetField("cost").GetValue(slot);
            var categoryVal = slot.GetType().GetField("category").GetValue(slot);
            string category = categoryVal.ToString();
            float lifetime = (float)slot.GetType().GetField("lifetime").GetValue(slot);

            string cleanName = fullName;
            string[] emojis = { "🔥", "❄️", "🧪", "💣", "⚡", "🌻", "🌸", "🌿", "🔫", "🌳", "🛡️" };
            foreach (var em in emojis) {
                cleanName = cleanName.Replace(em, "").Trim();
            }

            if (descMap.ContainsKey(cleanName)) {
                var config = new PlantConfig {
                    name = cleanName,
                    cost = cost,
                    category = category,
                    lifetime = lifetime,
                    description = descMap[cleanName]
                };
                list.Add(config);
            }
        }

        plantConfigs = list.ToArray();
        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(demoScene, true);
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("Successfully synced plant configurations from demo scene! Count: " + plantConfigs.Length);
    }
#endif
}
