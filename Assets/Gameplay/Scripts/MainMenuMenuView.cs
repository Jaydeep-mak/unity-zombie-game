using System.Collections;
using UnityEngine;
using AdsManager;

public class MainMenuMenuView : MenuView
{
    private IEnumerator Start()
    {
        Show();

        // Register Remove Ads listener to dynamically shift UI down if player purchases Ad-Free
        PurchaseController.OnRemoveAd += AdjustLayoutForAds;
        AdjustLayoutForAds();

        if (AdMobManager.GetInstance() == null) {
            yield break;
        }

        yield return new WaitUntil(() => AdMobManager.GetInstance().IsSdkInitialized);

#if UNITY_EDITOR
        BannerAdSize size = BannerAdSize.Banner;
#else
        BannerAdSize size = BannerAdSize.FullWidth;
#endif
        AdMobManager.GetInstance().RequestBanner(BannerAdPosition.Bottom, size, LocalAdStatusDelegate);
    }

    private void OnDestroy()
    {
        PurchaseController.OnRemoveAd -= AdjustLayoutForAds;
    }

    private void AdjustLayoutForAds()
    {
        bool showAds = true;
        if (Utils.PreferenceHelper.IsAdRemoved()) {
            showAds = false;
        }

        var logoTrans = canvasGameObject.transform.Find("GameNameLogo")?.GetComponent<RectTransform>();
        var startTrans = canvasGameObject.transform.Find("StartButton")?.GetComponent<RectTransform>();
        var plantsTrans = canvasGameObject.transform.Find("PlantsButton")?.GetComponent<RectTransform>();

        if (logoTrans != null && startTrans != null && plantsTrans != null) {
            if (showAds) {
                // Shifted upward to leave safe bottom space for Banner Ad
                logoTrans.anchoredPosition = new Vector2(-12.46f, 236.52f);
                startTrans.anchoredPosition = new Vector2(15.86f, -23.91f);
                plantsTrans.anchoredPosition = new Vector2(10f, -252f);
            } else {
                // Centered default positions (no Ads)
                logoTrans.anchoredPosition = new Vector2(-12.46f, 146.52f);
                startTrans.anchoredPosition = new Vector2(15.86f, -113.91f);
                plantsTrans.anchoredPosition = new Vector2(10f, -342f);
            }
        }
    }

    private void LocalAdStatusDelegate(AdStatusCode adStatusCode)
    {
        if (adStatusCode == AdStatusCode.ADLoadSuccess)
        {
            var field = typeof(MenuView).GetField("_isIAPOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isIAPOpen = field != null ? (bool)field.GetValue(this) : false;

            if (isViewVisible && !isIAPOpen && AdMobManager.GetInstance() != null)
            {
                AdMobManager.GetInstance().ShowBanner();
            }
        }
    }

    protected override void OnViewShow()
    {
        AdjustLayoutForAds();
        if (AdMobManager.GetInstance() != null) {
            AdMobManager.GetInstance().ShowBanner();
        }
    }

    private void OnDisable()
    {
        if (AdMobManager.GetInstance() != null) {
            AdMobManager.GetInstance().HideBanner();
        }
    }
}
