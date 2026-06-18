using UnityEngine;

public class ZombieSpawner : MonoBehaviour {
    public GameObject zombiePrefab;
    public float spawnInterval = 1.5f;
    [SerializeField] private float[] laneYPositions = { 3.0f, 1.5f, 0.0f, -1.5f, -3.0f };

    private void Start() {
        InvokeRepeating(nameof(SpawnZombie), 0.5f, spawnInterval);
    }

    private void SpawnZombie() {
        if (zombiePrefab != null && laneYPositions != null && laneYPositions.Length > 0) {
            float randomY = laneYPositions[Random.Range(0, laneYPositions.Length)];
            Vector3 spawnPosition = new Vector3(transform.position.x, randomY, transform.position.z);
            Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
        }
    }
}