using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Firebase.Analytics;
using AdsManager;

public class SplashManager : MonoBehaviour
{
    public float delay;
    public CanvasGroup canvasGroup;
    public GameSettings gameSettings;

    private void Start()
    {
        FadeEffect();

        StartCoroutine(LoadScene());

        FirebaseManager.CheckFireBaseDependency();
    }

    private void GameLaunchFirebaseEvent()
    {
       FirebaseManager.LogEvent(Constants.EVENT_GAME_LAUNCH);
    }

    public IEnumerator LoadScene()
    {
        yield return new WaitForSeconds(delay);
        if (AdMobManager.GetInstance() != null) {
            AdMobManager.GetInstance().SetAdmobAdsID();
            if (!AdMobManager.GetInstance().IsInterstitialAdLoaded()) {
                AdMobManager.GetInstance().RequestInterstitial();
            }
        }
        SceneManager.LoadScene(Constants.SCENE_MENU);
        GameLaunchFirebaseEvent();
    }

    public void FadeEffect()
    {
        canvasGroup.DOFade(1, 0.5f);
    }
}
