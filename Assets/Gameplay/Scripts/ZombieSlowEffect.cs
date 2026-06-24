using UnityEngine;

public class ZombieSlowEffect : MonoBehaviour {
    private ZombieController zombie;
    private SpriteRenderer[] childRenderers;
    private Color[] originalColors;
    private float originalSpeed;
    private float durationTimer = 0f;
    private bool isSlowed = false;

    private void Awake() {
        zombie = GetComponent<ZombieController>();
        childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void ApplySlow(float speedMultiplier, float duration) {
        if (zombie == null) return;

        // If not already slowed, store the current speed
        if (!isSlowed) {
            originalSpeed = zombie.speed;
            if (childRenderers != null && childRenderers.Length > 0) {
                originalColors = new Color[childRenderers.Length];
                for (int i = 0; i < childRenderers.Length; i++) {
                    originalColors[i] = childRenderers[i].color;
                }
            }
            isSlowed = true;
        }

        // Apply speed reduction
        zombie.speed = originalSpeed * speedMultiplier;

        // Visual feedback: blue/cyan tint for frozen effect
        if (childRenderers != null) {
            for (int i = 0; i < childRenderers.Length; i++) {
                if (childRenderers[i] != null && originalColors != null && i < originalColors.Length) {
                    childRenderers[i].color = new Color(0.3f, 0.65f, 1f, originalColors[i].a);
                }
            }
        }

        durationTimer = duration;
    }

    private void Update() {
        if (!isSlowed) return;

        durationTimer -= Time.deltaTime;
        
        // Pulse the freeze color slightly for visual feedback
        float pulse = 0.85f + Mathf.PingPong(Time.time * 2f, 0.15f);
        if (childRenderers != null) {
            for (int i = 0; i < childRenderers.Length; i++) {
                if (childRenderers[i] != null && originalColors != null && i < originalColors.Length) {
                    childRenderers[i].color = new Color(0.3f * pulse, 0.65f * pulse, 1f * pulse, originalColors[i].a);
                }
            }
        }

        if (durationTimer <= 0f) {
            RemoveSlow();
        }
    }

    private void RemoveSlow() {
        if (zombie != null) {
            zombie.speed = originalSpeed;
        }
        RestoreColors();
        Destroy(this);
    }

    private void OnDestroy() {
        // Clean up speed and color just in case
        if (isSlowed) {
            if (zombie != null) zombie.speed = originalSpeed;
            RestoreColors();
        }
    }

    private void RestoreColors() {
        if (childRenderers != null && originalColors != null) {
            for (int i = 0; i < childRenderers.Length; i++) {
                if (childRenderers[i] != null && i < originalColors.Length) {
                    childRenderers[i].color = originalColors[i];
                }
            }
        }
    }
}
