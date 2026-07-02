using UnityEngine;

public class GlobalProgressionManager : MonoBehaviour {
    private static GlobalProgressionManager instance;
    public static GlobalProgressionManager Instance {
        get {
            if (instance == null) {
                instance = FindFirstObjectByType<GlobalProgressionManager>();
                if (instance == null) {
                    var go = new GameObject("GlobalProgressionManager");
                    instance = go.AddComponent<GlobalProgressionManager>();
                }
            }
            return instance;
        }
    }

    [Header("Storage Configuration")]
    [SerializeField] private string saveKey = "GlobalProgressionCoins";

    // Match Session Coins (Temporary)
    private int matchEarnedCoins = 0;
    public int MatchEarnedCoins => matchEarnedCoins;

    private void Awake() {
        if (instance == null) {
            instance = this;
            if (transform.parent == null) {
                DontDestroyOnLoad(gameObject);
            }
        } else if (instance != this) {
            Destroy(this);
        }
    }

    public bool IsPlantLocked(string plantName) {
        if (string.IsNullOrEmpty(plantName)) return false;
        if (plantName == "Fire Bloom" || plantName == "Frost Flower" || plantName == "Thorn Vine" || plantName == "Bomb Cactus" || plantName == "Guardian Oak") {
            return false;
        }
        return PlayerPrefs.GetInt("PlantUnlocked_" + plantName, 0) == 0;
    }

    public void UnlockPlant(string plantName) {
        if (string.IsNullOrEmpty(plantName)) return;
        PlayerPrefs.SetInt("PlantUnlocked_" + plantName, 1);
        PlayerPrefs.Save();
        if (AudioManager.Instance != null) {
            AudioManager.Instance.Play(SFXType.UIPlantUnlock);
        }
        TriggerCoinsChanged();
    }

    public int GetCoins() {
        return PlayerPrefs.GetInt(saveKey, 50000);
    }

    public void AddCoins(int amount) {
        int current = GetCoins();
        PlayerPrefs.SetInt(saveKey, current + amount);
        PlayerPrefs.Save();
        TriggerCoinsChanged();
    }

    public bool RemoveCoins(int amount) {
        int current = GetCoins();
        if (current >= amount) {
            PlayerPrefs.SetInt(saveKey, current - amount);
            PlayerPrefs.Save();
            TriggerCoinsChanged();
            return true;
        }
        return false;
    }

    // Match Session API
    public void ResetMatchCoins() {
        matchEarnedCoins = 0;
    }

    public void AddMatchCoins(int amount) {
        matchEarnedCoins += amount;
    }

    public void ApplyMatchCoinsToWallet() {
        if (matchEarnedCoins > 0) {
            AddCoins(matchEarnedCoins);
            matchEarnedCoins = 0; // Clear to prevent double addition
        }
    }

    // Event for UI displays to subscribe to
    public delegate void CoinsChangedDelegate(int newCount);
    public static event CoinsChangedDelegate OnCoinsChanged;

    private void TriggerCoinsChanged() {
        if (OnCoinsChanged != null) {
            OnCoinsChanged.Invoke(GetCoins());
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Reset Progression (Clear PlayerPrefs)")]
    public void ResetProgression() {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs cleared! All progression has been reset.");
        TriggerCoinsChanged();
    }
#endif
}
