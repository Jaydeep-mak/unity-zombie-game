using UnityEngine;

public class LeftBoundaryTrigger : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D other) {
        var zombie = other.GetComponent<ZombieController>();
        if (zombie != null) {
            if (GameplayManager.Instance != null) {
                GameplayManager.Instance.ZombieReachedBase(zombie.gameObject);
            }
        }
    }
}