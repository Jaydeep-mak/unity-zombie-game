using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour {
    [Header("Settings UI")]
    [SerializeField] private GameObject settingsPrefab;
    [SerializeField] private UnityEngine.UI.Image mainMenuBackground;

    [Header("UI References")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button plantsButton;
    private bool _isIAPOpen = false;
    private bool isStarting = false;

    private void Start() {
        // Ensure EventSystem exists so that UI clicks are registered
        if (UnityEngine.EventSystems.EventSystem.current == null) {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Register button listeners
        if (startButton != null) {
            startButton.onClick.AddListener(StartGame);
        }
        if (settingsButton != null) {
            settingsButton.onClick.AddListener(OpenSettings);
        }
        if (plantsButton != null) {
            plantsButton.onClick.AddListener(OpenPlantCollection);
        }

        // Force initialize AudioManager and apply sound settings
        if (AudioManager.Instance != null) {
            // This initializes the singleton and loads the muted setting
        }

        // Fade in at start
        if (fadeOverlay != null) {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.alpha = 1f;
            StartCoroutine(FadeRoutine(1f, 0f));
        }

        ApplySettingsBackground();
    }

    public void StartGame() {
        if (isStarting) return;
        isStarting = true;
        
        // Log game start event to Firebase Analytics
        FirebaseManager.LogEvent(Constants.EVENT_START_GAME);

        if (AudioManager.Instance != null) {
            AudioManager.Instance.Play(SFXType.UIClickStart);
        }
        StartCoroutine(StartGameRoutine());
    }

    private void OpenSettings() {
        if (AudioManager.Instance != null) {
            AudioManager.Instance.Play(SFXType.UIPopupOpen);
        }
        if (SettingView.GetInstance() == null && settingsPrefab != null) {
            Instantiate(settingsPrefab);
        }

        ApplySettingsBackground();

        if (MenuView.GetInstance() != null) {
            MenuView.GetInstance().Hide();
        }
        if (SettingView.GetInstance() != null) {
            SettingView.GetInstance().Show();
        }
    }

    private void ApplySettingsBackground() {
        SettingView existing = SettingView.GetInstance();
        if (existing != null && mainMenuBackground != null) {
            Transform bgTransform = existing.transform.Find("Canvas/ViewContent/BG");
            if (bgTransform != null) {
                UnityEngine.UI.Image bgImage = bgTransform.GetComponent<UnityEngine.UI.Image>();
                if (bgImage != null) {
                    bgImage.sprite = mainMenuBackground.sprite;
                    bgImage.color = mainMenuBackground.color;
                }
            }
        }
    }




    private IEnumerator StartGameRoutine() {
        // Fade out
        if (fadeOverlay != null) {
            fadeOverlay.gameObject.SetActive(true);
            yield return StartCoroutine(FadeRoutine(0f, 1f));
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        // Load the Demo Scene
        SceneManager.LoadScene("demo");
    }

    public void OpenPlantCollection() {
        if (isStarting) return;
        isStarting = true;
        if (AudioManager.Instance != null) {
            AudioManager.Instance.Play(SFXType.UIClickPlants);
        }
        StartCoroutine(OpenPlantCollectionRoutine());
    }

    private IEnumerator OpenPlantCollectionRoutine() {
        // Fade out
        if (fadeOverlay != null) {
            fadeOverlay.gameObject.SetActive(true);
            yield return StartCoroutine(FadeRoutine(0f, 1f));
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        // Load the PlantCollectionScene
        SceneManager.LoadScene("PlantCollectionScene");
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
    public void OnSettingButtonClick()
    {
        if (AudioManager.Instance != null) {
            AudioManager.Instance.Play(SFXType.UIPopupOpen);
        }
        SettingView.GetInstance().Show();
    }

    public void OnRemoveAdsButton()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            MessageView.GetInstance().ShowMessageView(Constants.WARN_NO_INTERNET, "Ok");
        }
        else
        {
            _isIAPOpen = true;

            AdFreeView.GetInstance().ShowAdFreeView(OnIAPViewHide);
        }
    }

    public void OnIAPViewHide()
    {
        _isIAPOpen = false;
    }
}
