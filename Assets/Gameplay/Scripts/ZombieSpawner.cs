using UnityEngine;

public class ZombieSpawner : MonoBehaviour {
    public GameObject zombiePrefab;
    public float spawnInterval = 1.5f;
    [SerializeField] private float[] laneYPositions = { 3.0f, 1.5f, 0.0f, -1.5f, -3.0f };

    // Start method is removed to prevent automatic continuous spawning. Spawning is driven by WaveManager.

    public GameObject SpawnZombie(ZombieTypeConfig config, float waveHealthMultiplier, float waveSpeedMultiplier) {
        if (zombiePrefab != null && laneYPositions != null && laneYPositions.Length > 0) {
            float randomY = laneYPositions[Random.Range(0, laneYPositions.Length)];
            Vector3 spawnPosition = new Vector3(transform.position.x, randomY, transform.position.z);
            
            // Allow prefab override (for future support of completely different models/sprites)
            GameObject prefabToUse = config.prefabOverride != null ? config.prefabOverride : zombiePrefab;
            GameObject zombie = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);

            // Apply size/scale
            zombie.transform.localScale = config.localScale;

            // Apply sprite color tint
            var sr = zombie.GetComponent<SpriteRenderer>();
            if (sr != null) {
                sr.color = config.spriteColor;
            }

            // Apply speed scaling
            var controller = zombie.GetComponent<ZombieController>();
            if (controller != null) {
                controller.speed = config.speed * waveSpeedMultiplier;
            }

            var rb = zombie.GetComponent<Rigidbody2D>();
            if (rb != null) {
                rb.useFullKinematicContacts = true;
            }

            // Apply health scaling and damage settings
            var health = zombie.GetComponent<ZombieHealth>();
            if (health != null) {
                health.baseDamage = config.baseDamage;
                health.Setup(Mathf.RoundToInt(config.maxHealth * waveHealthMultiplier));
            }

            return zombie;
        }
        return null;
    }
}