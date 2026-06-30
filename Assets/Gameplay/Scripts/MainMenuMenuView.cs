using System.Collections;
using UnityEngine;

public class MainMenuMenuView : MenuView
{
    private void Start()
    {
        // Override the mobile-specific Start method of MenuView to prevent
        // issues with uninitialized AdMob SDK or missing UI references.
        Show();
    }

    protected override void OnViewShow()
    {
        // Override and bypass base.OnViewShow() (MenuView) because it
        // attempts to call AdMobManager.GetInstance().ShowBanner() which is
        // null when running the Main Menu scene directly in the Editor.
    }
}
