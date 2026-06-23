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

    public int GetCoins() {
        return PlayerPrefs.GetInt(saveKey, 0);
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

    // Event for UI displays to subscribe to
    public delegate void CoinsChangedDelegate(int newCount);
    public static event CoinsChangedDelegate OnCoinsChanged;

    private void TriggerCoinsChanged() {
        if (OnCoinsChanged != null) {
            OnCoinsChanged.Invoke(GetCoins());
        }
    }
}
