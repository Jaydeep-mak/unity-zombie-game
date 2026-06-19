using UnityEngine;

public class ZombieSlowEffect : MonoBehaviour {
    private ZombieController zombie;
    private SpriteRenderer sr;
    private float originalSpeed;
    private Color originalColor;
    private float durationTimer = 0f;
    private bool isSlowed = false;

    private void Awake() {
        zombie = GetComponent<ZombieController>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void ApplySlow(float speedMultiplier, float duration) {
        if (zombie == null) return;

        // If not already slowed, store the current speed
        if (!isSlowed) {
            originalSpeed = zombie.speed;
            if (sr != null) {
                originalColor = sr.color;
            }
            isSlowed = true;
        }

        // Apply speed reduction
        zombie.speed = originalSpeed * speedMultiplier;

        // Visual feedback: blue/cyan tint for frozen effect
        if (sr != null) {
            sr.color = new Color(0.3f, 0.65f, 1f, originalColor.a);
        }

        durationTimer = duration;
    }

    private void Update() {
        if (!isSlowed) return;

        durationTimer -= Time.deltaTime;
        
        // Pulse the freeze color slightly for visual feedback
        if (sr != null) {
            float pulse = 0.85f + Mathf.PingPong(Time.time * 2f, 0.15f);
            sr.color = new Color(0.3f * pulse, 0.65f * pulse, 1f * pulse, originalColor.a);
        }

        if (durationTimer <= 0f) {
            RemoveSlow();
        }
    }

    private void RemoveSlow() {
        if (zombie != null) {
            zombie.speed = originalSpeed;
        }
        if (sr != null) {
            sr.color = originalColor;
        }
        Destroy(this);
    }

    private void OnDestroy() {
        // Clean up speed and color just in case
        if (isSlowed) {
            if (zombie != null) zombie.speed = originalSpeed;
            if (sr != null) sr.color = originalColor;
        }
    }
}
