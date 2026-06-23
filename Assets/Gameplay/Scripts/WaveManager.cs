using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public struct ZombieSpawnWeight {
    public string typeName;
    public float weight;
}

[System.Serializable]
public class ZombieTypeConfig {
    public string typeName;
    public GameObject prefabOverride; // Allows linking completely custom assets in the future
    public float speed = 1.5f;
    public int maxHealth = 10;
    public int baseDamage = 1;
    public int coinReward = 10;
    public Vector3 localScale = Vector3.one;
    public Color spriteColor = Color.white;
}

[System.Serializable]
public class WaveConfig {
    public int zombieCount;
    public float spawnInterval = 1.5f;
    public float healthMultiplier = 1f;
    public float speedMultiplier = 1f;
    public List<ZombieSpawnWeight> spawnWeights = new List<ZombieSpawnWeight>();

    public string GetRandomZombieType() {
        if (spawnWeights == null || spawnWeights.Count == 0) {
            return "Normal";
        }
        float totalWeight = 0f;
        foreach (var sw in spawnWeights) {
            totalWeight += sw.weight;
        }
        if (totalWeight <= 0f) return "Normal";

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var sw in spawnWeights) {
            cumulative += sw.weight;
            if (roll <= cumulative) {
                return sw.typeName;
            }
        }
        return "Normal";
    }
}

public class WaveManager : MonoBehaviour {
    public static WaveManager Instance { get; private set; }

    [Header("End Condition")]
    [SerializeField] private int totalWaves = 10;

    [Header("Zombie Type Configurations")]
    [SerializeField] private List<ZombieTypeConfig> zombieTypes = new List<ZombieTypeConfig>();

    [Header("Wave Configurations")]
    [SerializeField] private List<WaveConfig> waveConfigs = new List<WaveConfig>();

    [Header("Procedural Fallback (Difficulty Scaling)")]
    [SerializeField] private float countScaleFactor = 1.25f;
    [SerializeField] private float intervalScaleFactor = 0.9f;
    [SerializeField] private float healthScaleFactor = 1.15f;
    [SerializeField] private float speedScaleFactor = 1.05f;

    [Header("References")]
    [SerializeField] private ZombieSpawner spawner;

    // Current State
    private int currentWaveNumber = 1;
    private int zombiesToSpawn = 0;
    private float spawnCooldown = 0f;
    private WaveConfig currentWaveConfig;
    private bool isSpawning = false;
    private bool isWaitingForClear = false;

    // UI
    private GameObject waveCompletePanel;
    private TextMeshProUGUI waveCompleteText;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        if (spawner == null) {
            spawner = FindFirstObjectByType<ZombieSpawner>();
        }

        // Initialize default assets if empty
        PopulateDefaultConfigs();

        CreateWaveCompleteUI();

        // Start Wave 1
        StartCoroutine(StartWaveRoutine(1));
    }

    private void Reset() {
        PopulateDefaultConfigs();
    }

    private void PopulateDefaultConfigs() {
        if (zombieTypes.Count == 0) {
            zombieTypes.Add(new ZombieTypeConfig {
                typeName = "Normal",
                speed = 1.5f,
                maxHealth = 4,
                baseDamage = 1,
                localScale = new Vector3(-1.0f, 1.2f, 1.0f),
                spriteColor = Color.white
            });
            zombieTypes.Add(new ZombieTypeConfig {
                typeName = "Runner",
                speed = 2.5f,
                maxHealth = 3,
                baseDamage = 1,
                localScale = new Vector3(-0.8f, 0.96f, 1.0f),
                spriteColor = new Color(0.9f, 0.9f, 0.5f, 1f)
            });
            zombieTypes.Add(new ZombieTypeConfig {
                typeName = "Tank",
                speed = 0.8f,
                maxHealth = 30,
                baseDamage = 3,
                localScale = new Vector3(-1.5f, 1.8f, 1.0f),
                spriteColor = new Color(0.5f, 0.6f, 0.7f, 1f)
            });
            zombieTypes.Add(new ZombieTypeConfig {
                typeName = "Berserker",
                speed = 1.3f,
                maxHealth = 15,
                baseDamage = 5,
                localScale = new Vector3(-1.1f, 1.3f, 1.0f),
                spriteColor = new Color(1.0f, 0.4f, 0.4f, 1f)
            });
        }

        if (waveConfigs.Count == 0) {
            // Wave 1: 15 zombies (100% Normal)
            waveConfigs.Add(new WaveConfig {
                zombieCount = 15,
                spawnInterval = 1.8f,
                spawnWeights = new List<ZombieSpawnWeight> { new ZombieSpawnWeight { typeName = "Normal", weight = 100 } }
            });
            // Wave 2: 25 zombies (100% Normal)
            waveConfigs.Add(new WaveConfig {
                zombieCount = 25,
                spawnInterval = 1.5f,
                spawnWeights = new List<ZombieSpawnWeight> { new ZombieSpawnWeight { typeName = "Normal", weight = 100 } }
            });
            // Wave 3: 40 zombies (70% Normal, 30% Runner)
            waveConfigs.Add(new WaveConfig {
                zombieCount = 40,
                spawnInterval = 1.2f,
                spawnWeights = new List<ZombieSpawnWeight> {
                    new ZombieSpawnWeight { typeName = "Normal", weight = 70 },
                    new ZombieSpawnWeight { typeName = "Runner", weight = 30 }
                }
            });
            // Wave 4: 55 zombies (60% Normal, 40% Runner)
            waveConfigs.Add(new WaveConfig {
                zombieCount = 55,
                spawnInterval = 1.0f,
                spawnWeights = new List<ZombieSpawnWeight> {
                    new ZombieSpawnWeight { typeName = "Normal", weight = 60 },
                    new ZombieSpawnWeight { typeName = "Runner", weight = 40 }
                }
            });
            // Wave 5: 75 zombies (50% Normal, 35% Runner, 15% Tank)
            waveConfigs.Add(new WaveConfig {
                zombieCount = 75,
                spawnInterval = 0.8f,
                spawnWeights = new List<ZombieSpawnWeight> {
                    new ZombieSpawnWeight { typeName = "Normal", weight = 50 },
                    new ZombieSpawnWeight { typeName = "Runner", weight = 35 },
                    new ZombieSpawnWeight { typeName = "Tank", weight = 15 }
                }
            });
            // Wave 6: 100 zombies (45% Normal, 35% Runner, 20% Tank)
            waveConfigs.Add(new WaveConfig {
                zombieCount = 100,
                spawnInterval = 0.7f,
                healthMultiplier = 1.1f,
                spawnWeights = new List<ZombieSpawnWeight> {
                    new ZombieSpawnWeight { typeName = "Normal", weight = 45 },
                    new ZombieSpawnWeight { typeName = "Runner", weight = 35 },
                    new ZombieSpawnWeight { typeName = "Tank", weight = 20 }
                }
            });
            // Wave 7: 120 zombies (40% Normal, 30% Runner, 15% Tank, 15% Berserker)
            waveConfigs.Add(new WaveConfig {
                zombieCount = 120,
                spawnInterval = 0.6f,
                healthMultiplier = 1.15f,
                spawnWeights = new List<ZombieSpawnWeight> {
                    new ZombieSpawnWeight { typeName = "Normal", weight = 40 },
                    new ZombieSpawnWeight { typeName = "Runner", weight = 30 },
                    new ZombieSpawnWeight { typeName = "Tank", weight = 15 },
                    new ZombieSpawnWeight { typeName = "Berserker", weight = 15 }
                }
            });
            // Wave 8: 150 zombies (30% Normal, 30% Runner, 20% Tank, 20% Berserker)
            waveConfigs.Add(new WaveConfig {
                zombieCount = 150,
                spawnInterval = 0.5f,
                healthMultiplier = 1.2f,
                speedMultiplier = 1.05f,
                spawnWeights = new List<ZombieSpawnWeight> {
                    new ZombieSpawnWeight { typeName = "Normal", weight = 30 },
                    new ZombieSpawnWeight { typeName = "Runner", weight = 30 },
                    new ZombieSpawnWeight { typeName = "Tank", weight = 20 },
                    new ZombieSpawnWeight { typeName = "Berserker", weight = 20 }
                }
            });
            // Wave 9: 180 zombies (25% Normal, 25% Runner, 25% Tank, 25% Berserker)
            waveConfigs.Add(new WaveConfig {
                zombieCount = 180,
                spawnInterval = 0.45f,
                healthMultiplier = 1.25f,
                speedMultiplier = 1.05f,
                spawnWeights = new List<ZombieSpawnWeight> {
                    new ZombieSpawnWeight { typeName = "Normal", weight = 25 },
                    new ZombieSpawnWeight { typeName = "Runner", weight = 25 },
                    new ZombieSpawnWeight { typeName = "Tank", weight = 25 },
                    new ZombieSpawnWeight { typeName = "Berserker", weight = 25 }
                }
            });
            // Wave 10: 220 zombies (20% Normal, 25% Runner, 25% Tank, 30% Berserker)
            waveConfigs.Add(new WaveConfig {
                zombieCount = 220,
                spawnInterval = 0.4f,
                healthMultiplier = 1.3f,
                speedMultiplier = 1.1f,
                spawnWeights = new List<ZombieSpawnWeight> {
                    new ZombieSpawnWeight { typeName = "Normal", weight = 20 },
                    new ZombieSpawnWeight { typeName = "Runner", weight = 25 },
                    new ZombieSpawnWeight { typeName = "Tank", weight = 25 },
                    new ZombieSpawnWeight { typeName = "Berserker", weight = 30 }
                }
            });
        }
    }

    private void CreateWaveCompleteUI() {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) {
            waveCompletePanel = new GameObject("WaveCompletePanel");
            waveCompletePanel.transform.SetParent(canvas.transform, false);
            waveCompletePanel.SetActive(false);

            var rect = waveCompletePanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 150f);
            rect.sizeDelta = new Vector2(800f, 120f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(waveCompletePanel.transform, false);
            waveCompleteText = textGo.AddComponent<TextMeshProUGUI>();
            waveCompleteText.text = "WAVE COMPLETE!";
            waveCompleteText.fontSize = 64;
            waveCompleteText.fontStyle = FontStyles.Bold;
            waveCompleteText.color = new Color(1.0f, 0.8f, 0.2f, 1f); // Shiny gold
            waveCompleteText.alignment = TextAlignmentOptions.Center;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }
    }

    private IEnumerator StartWaveRoutine(int waveNum) {
        currentWaveNumber = waveNum;

        if (waveNum <= waveConfigs.Count) {
            currentWaveConfig = waveConfigs[waveNum - 1];
        } else {
            // Scale dynamically for endless progression (mix of all unlocked types)
            var lastConfig = waveConfigs[waveConfigs.Count - 1];
            int waveDiff = waveNum - waveConfigs.Count;
            currentWaveConfig = new WaveConfig {
                zombieCount = Mathf.RoundToInt(lastConfig.zombieCount * Mathf.Pow(countScaleFactor, waveDiff)),
                spawnInterval = Mathf.Max(0.4f, lastConfig.spawnInterval * Mathf.Pow(intervalScaleFactor, waveDiff)),
                healthMultiplier = lastConfig.healthMultiplier * Mathf.Pow(healthScaleFactor, waveDiff),
                speedMultiplier = lastConfig.speedMultiplier * Mathf.Pow(speedScaleFactor, waveDiff),
                spawnWeights = new List<ZombieSpawnWeight> {
                    new ZombieSpawnWeight { typeName = "Normal", weight = 20f },
                    new ZombieSpawnWeight { typeName = "Runner", weight = 25f },
                    new ZombieSpawnWeight { typeName = "Tank", weight = 25f },
                    new ZombieSpawnWeight { typeName = "Berserker", weight = 30f }
                }
            };
        }

        // Update top HUD display
        if (GameplayManager.Instance != null) {
            GameplayManager.Instance.SetCurrentWave(currentWaveNumber);
        }

        // Play the Wave Start Announcement UI Animation first
        yield return StartCoroutine(AnimateWaveAnnouncement(currentWaveNumber));

        zombiesToSpawn = currentWaveConfig.zombieCount;
        spawnCooldown = 0.5f; // Short delay before spawning starts
        isSpawning = true;
        isWaitingForClear = false;
    }

    private IEnumerator AnimateWaveAnnouncement(int waveNum) {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) yield break;

        // Create Panel GameObject
        var panelGo = new GameObject("WaveAnnouncementPanel");
        panelGo.transform.SetParent(canvas.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1000f, 250f);
        panelRect.localScale = Vector3.one * 0.2f;

        Color goldColor = new Color(1.0f, 0.82f, 0.15f, 1.0f);

        // Top Line Border
        var topLineGo = new GameObject("TopLine");
        topLineGo.transform.SetParent(panelGo.transform, false);
        var topLineImg = topLineGo.AddComponent<UnityEngine.UI.Image>();
        topLineImg.color = goldColor;
        var topRect = topLineGo.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 0.5f);
        topRect.anchorMax = new Vector2(0.5f, 0.5f);
        topRect.pivot = new Vector2(0.5f, 0.5f);
        topRect.anchoredPosition = new Vector2(0f, 90f);
        topRect.sizeDelta = new Vector2(600f, 8f);

        // Announcement Text
        var textGo = new GameObject("AnnouncementText");
        textGo.transform.SetParent(panelGo.transform, false);
        var textMesh = textGo.AddComponent<TextMeshProUGUI>();
        textMesh.text = $"WAVE {waveNum}";
        textMesh.fontSize = 96;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.color = goldColor;
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Premium outline shadow
        textMesh.outlineWidth = 0.2f;
        textMesh.outlineColor = Color.black;

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // Bottom Line Border
        var bottomLineGo = new GameObject("BottomLine");
        bottomLineGo.transform.SetParent(panelGo.transform, false);
        var bottomLineImg = bottomLineGo.AddComponent<UnityEngine.UI.Image>();
        bottomLineImg.color = goldColor;
        var bottomRect = bottomLineGo.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.5f, 0.5f);
        bottomRect.anchorMax = new Vector2(0.5f, 0.5f);
        bottomRect.pivot = new Vector2(0.5f, 0.5f);
        bottomRect.anchoredPosition = new Vector2(0f, -90f);
        bottomRect.sizeDelta = new Vector2(600f, 8f);

        // Spawns screen flash and screen shake juice
        StartCoroutine(ScreenFlashRoutine(0.25f, Color.white, 0.15f));
        StartCoroutine(CameraShakeRoutine(0.2f, 0.08f));

        // 1. Scale In (Bounce)
        float elapsed = 0f;
        float scaleInDuration = 0.35f;
        while (elapsed < scaleInDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleInDuration;
            float scale = Mathf.Lerp(0.2f, 1.15f, t);
            if (t > 0.8f) {
                float tSettle = (t - 0.8f) / 0.2f;
                scale = Mathf.Lerp(1.15f, 1.0f, tSettle);
            }
            panelRect.localScale = Vector3.one * scale;
            yield return null;
        }
        panelRect.localScale = Vector3.one;

        // 2. Hold (Pulse)
        elapsed = 0f;
        float holdDuration = 1.0f;
        while (elapsed < holdDuration) {
            elapsed += Time.deltaTime;
            float pulse = 1.0f + Mathf.Sin(elapsed * Mathf.PI * 2f) * 0.03f;
            panelRect.localScale = Vector3.one * pulse;
            yield return null;
        }

        // 3. Fade Out
        elapsed = 0f;
        float fadeDuration = 0.4f;
        var textGroup = panelGo.AddComponent<CanvasGroup>();
        while (elapsed < fadeDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            textGroup.alpha = Mathf.Lerp(1f, 0f, t);
            panelRect.localScale = Vector3.one * Mathf.Lerp(1.0f, 0.8f, t);
            yield return null;
        }

        Destroy(panelGo);
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
            elapsed += Time.deltaTime;
            img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0f, maxAlpha, elapsed / halfDuration));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration) {
            elapsed += Time.deltaTime;
            img.color = new Color(color.r, color.g, color.b, Mathf.Lerp(maxAlpha, 0f, elapsed / halfDuration));
            yield return null;
        }

        Destroy(flashGo);
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

    private void Update() {
        if (!enabled) return;

        if (isSpawning) {
            spawnCooldown -= Time.deltaTime;
            if (spawnCooldown <= 0f) {
                SpawnZombieFromConfig();
                spawnCooldown = currentWaveConfig.spawnInterval;
            }
        }

        if (isWaitingForClear) {
            if (GameplayManager.Instance != null && GameplayManager.Instance.activeZombieCount == 0) {
                isWaitingForClear = false;
                StartCoroutine(WaveCompleteRoutine());
            }
        }
    }

    private void SpawnZombieFromConfig() {
        if (spawner != null) {
            string selectedType = currentWaveConfig.GetRandomZombieType();
            ZombieTypeConfig config = GetZombieTypeConfig(selectedType);
            spawner.SpawnZombie(config, currentWaveConfig.healthMultiplier, currentWaveConfig.speedMultiplier);
        }
        zombiesToSpawn--;

        if (zombiesToSpawn <= 0) {
            isSpawning = false;
            isWaitingForClear = true;
        }
    }

    private ZombieTypeConfig GetZombieTypeConfig(string typeName) {
        foreach (var config in zombieTypes) {
            if (config.typeName == typeName) {
                return config;
            }
        }
        if (zombieTypes.Count > 0) return zombieTypes[0];
        return new ZombieTypeConfig {
            typeName = "Normal",
            speed = 1.5f,
            maxHealth = 4,
            baseDamage = 1,
            localScale = Vector3.one,
            spriteColor = Color.white
        };
    }

    private IEnumerator WaveCompleteRoutine() {
        if (waveCompletePanel != null) {
            waveCompletePanel.SetActive(true);
        }

        // Reward Global Progression Coins on wave completion
        if (GlobalProgressionManager.Instance != null) {
            int rewardAmount = 50 + (currentWaveNumber - 1) * 25;
            GlobalProgressionManager.Instance.AddCoins(rewardAmount);
            Debug.Log($"Rewarded {rewardAmount} Global Progression Coins for completing Wave {currentWaveNumber}.");
        }

        yield return new WaitForSeconds(3.0f);

        if (waveCompletePanel != null) {
            waveCompletePanel.SetActive(false);
        }

        if (currentWaveNumber >= totalWaves) {
            if (GameplayManager.Instance != null) {
                GameplayManager.Instance.TriggerVictory();
            }
        } else {
            StartCoroutine(StartWaveRoutine(currentWaveNumber + 1));
        }
    }
}
