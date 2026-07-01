using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Utils;
using System;
using AdsManager;
using IAPPurchasing;

public class SettingView : View
{
    private static SettingView _instance;
    public GameObject consentButton;
    public GameObject noAdsButton;
    public GameObject restoreButton;
    private string _shareMessage;
    public GameSettings gameSettings;

    public override void Awake()
    {
        base.Awake();
        _instance = this;
        SetShareMessage();
    }

    public static SettingView GetInstance()
    {
        return _instance;
    }

    private void Start()
    {
        EnableDisableConsentButton();

        ActiveRestoreButton();

        if (PreferenceHelper.IsAdRemoved())
            HideConsentAndBuyAdsButtons();
    }

    private void OnEnable()
    {
        PurchaseController.OnRemoveAd += HideConsentAndBuyAdsButtons;
        PurchaseController.OnPurchaseFail += HideLoadingView;
        PurchaseController.OnRestoreFailed += ShowMessage;
        PurchaseController.OnRestoreSuccess += RestorePurchaseSuccess;
    }



    private void OnDisable()
    {
        PurchaseController.OnRemoveAd -= HideConsentAndBuyAdsButtons;
        PurchaseController.OnPurchaseFail -= HideLoadingView;
        PurchaseController.OnRestoreFailed -= ShowMessage;
        PurchaseController.OnRestoreSuccess -= RestorePurchaseSuccess;
    }

    protected override void OnViewShow()
    {
        PreferenceHelper.SetIsWebViewOpen(false);
    }

    private void ActiveRestoreButton()
    {
#if UNITY_ANDROID
        restoreButton.SetActive(false);
#elif UNITY_IOS
        restoreButton.SetActive(true);
#endif
    }

    private void HideConsentAndBuyAdsButtons()
    {
        consentButton.SetActive(false);
        noAdsButton.SetActive(false);
        restoreButton.SetActive(false);
    }

    private void SetShareMessage()
    {
#if UNITY_ANDROID
        _shareMessage = Constants.GAME_SHARE_TEXT + gameSettings.GetPlayStoreURL();
#elif UNITY_IOS
        _shareMessage = Constants.GAME_SHARE_TEXT+gameSettings.GetAppStoreURL();
#endif
    }

    public override void OnBackeyPressed()
    {
        OnCloseButtonClick();
    }

    public void OnCloseButtonClick()
    {
        Hide();
        ShowInterstitialAndContinue(() => {
            if (MenuView.GetInstance() != null)
            {
                MenuView.GetInstance().Show();
                if (AdMobManager.GetInstance() != null) {
                    AdMobManager.GetInstance().ShowBanner();
                }
            }
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

    public void OnPrivacyPolicyButtonClick()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            MessageView.GetInstance().ShowMessageView(Constants.WARN_NO_INTERNET, "Ok");
        }
        else
        {
            PreferenceHelper.SetWebViewTittle(Constants.PRIVACY_TITTLE);
            SceneManager.LoadScene(Constants.SCENE_WEB);
        }
    }

    public void OnLicenceButtonClick()
    {
        PreferenceHelper.SetWebViewTittle(Constants.LICENCE_TITTLE);
        SceneManager.LoadScene(Constants.SCENE_WEB);
    }

    public void OnBuyAdFreeButtonClick()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            MessageView.GetInstance().ShowMessageView(Constants.WARN_NO_INTERNET, "Ok");
        }
        else
        {
            AdFreeView.GetInstance().Show();
        }
    }

    public void OnRateThisAppButtonClick()
    {
        RateAppPopUpView.GetInstance().Show();
    }

    public void OnShareAppButtonClick()
    {
        AppSharing.GetInstance().ShareApp(_shareMessage);
    }

    public void EnableDisableConsentButton()
    {
        if (AdMobManager.GetInstance().IsConsentRequired())
            consentButton.SetActive(true);
        else
            consentButton.SetActive(false);
    }

    public void OnConsentButtonClick()
    {
        AdMobManager.GetInstance().LoadAndShowConsentForm();
    }

    public void OnRestorePurchaseButtonClick()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            MessageView.GetInstance().ShowMessageView(Constants.WARN_NO_INTERNET, "Ok");
        }
        else
        {
            LoadingView.GetInstance().ShowLoading("Please wait...");

            PurchaseManager.GetInstance().RestorePurchases();
        }
    }

    private void ShowMessage()
    {
        LoadingView.GetInstance().Hide();

        MessageView.GetInstance().ShowMessageView(Constants.RESTORE_PURCHASE_WARNING, "Ok");
    }

    private void HideLoadingView()
    {
        LoadingView.GetInstance().Hide();
    }

    public void RestorePurchaseSuccess()
    {
        LoadingView.GetInstance().Hide();

        MessageView.GetInstance().ShowMessageView(Constants.RESTORE_PURCHASE_SUCCESS, "Ok");
    }

    public void ResetConcent()
    {
        AdMobManager.GetInstance().ResetConsent();
    }
}
