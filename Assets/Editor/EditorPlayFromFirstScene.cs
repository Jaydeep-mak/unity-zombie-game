#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public static class EditorPlayFromFirstScene
{
    private const string MenuPath = "Edit/Play From Splash Scene";
    private const string SettingKey = "PlayFromSplashScene_Enabled";
    private static bool isRunningTests = false;

    // Hold static references to prevent garbage collection of the test API callbacks
    private static TestRunnerApi testRunnerApi;
    private static TestCallbacks testCallbacks;

    static EditorPlayFromFirstScene()
    {
        testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        testCallbacks = new TestCallbacks();
        testRunnerApi.RegisterCallbacks(testCallbacks);

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += ApplyPlayModeStartScene;
    }

    [MenuItem(MenuPath, false, 250)]
    public static void TogglePlayFromSplashScene()
    {
        bool isEnabled = !GetEnabledState();
        SetEnabledState(isEnabled);
        ApplyPlayModeStartScene();
        Debug.Log("Play From Splash Scene is now " + (isEnabled ? "ENABLED" : "DISABLED"));
    }

    [MenuItem(MenuPath, true)]
    public static bool TogglePlayFromSplashSceneValidate()
    {
        Menu.SetChecked(MenuPath, GetEnabledState());
        return true;
    }

    private static bool GetEnabledState()
    {
        return EditorPrefs.GetBool(SettingKey, true); // Enabled by default
    }

    private static void SetEnabledState(bool enabled)
    {
        EditorPrefs.SetBool(SettingKey, enabled);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (isRunningTests || IsTestSceneActive())
            {
                EditorSceneManager.playModeStartScene = null;
            }
            else
            {
                ApplyPlayModeStartScene();
            }
        }
    }

    private static bool IsTestSceneActive()
    {
        string activeScenePath = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(activeScenePath)) return false;

        return activeScenePath.Contains("InitTestScene") ||
               activeScenePath.Contains("TestScene") ||
               activeScenePath.Contains("Temp/PlaymodeTestScene");
    }

    private static void ApplyPlayModeStartScene()
    {
        if (GetEnabledState() && !isRunningTests && !IsTestSceneActive())
        {
            // Retrieve scene at index 0 from Build Settings
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes != null && scenes.Length > 0 && scenes[0].enabled)
            {
                SceneAsset splashScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenes[0].path);
                if (splashScene != null)
                {
                    EditorSceneManager.playModeStartScene = splashScene;
                    return;
                }
            }

            // Fallback search for the Splash Scene asset if build settings are empty
            SceneAsset fallbackSplash = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SplashScene.unity");
            if (fallbackSplash != null)
            {
                EditorSceneManager.playModeStartScene = fallbackSplash;
            }
        }
        else
        {
            // Reset to default Unity behavior (play the current open scene)
            EditorSceneManager.playModeStartScene = null;
        }
    }

    // Helper callbacks class to handle test start and end states
    private class TestCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
            isRunningTests = true;
            EditorSceneManager.playModeStartScene = null;
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            isRunningTests = false;
        }

        public void TestStarted(ITestAdaptor test) {}
        public void TestFinished(ITestResultAdaptor result) {}
    }
}
#endif
