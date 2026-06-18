using UnityEngine;

public class LeftBoundaryTrigger : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D other) {
        var zombie = other.GetComponent<ZombieController>();
        if (zombie != null) {
            if (GameplayManager.Instance != null) {
                int baseDamage = 1;
                var health = zombie.GetComponent<ZombieHealth>();
                if (health != null) {
                    baseDamage = health.baseDamage;
                }
                GameplayManager.Instance.ZombieReachedBase(zombie.gameObject, baseDamage);
            }
        }
    }
}