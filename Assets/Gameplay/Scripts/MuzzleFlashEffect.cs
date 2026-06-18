using UnityEngine;

public class MuzzleFlashEffect : MonoBehaviour {
    private SpriteRenderer sr;
    private float duration = 0.25f;
    private float elapsed = 0f;
    private Vector3 targetScale;

    public void Setup(Sprite sprite, Color tint, Vector3 targetScale, float duration = 0.25f) {
        this.duration = duration;
        this.targetScale = targetScale;
        
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingOrder = 7; // Render in front of plant and projectile
        
        transform.localScale = Vector3.zero;
    }

    private void Update() {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        
        if (t >= 1f) {
            Destroy(gameObject);
            return;
        }

        // Scale up quickly
        transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
        
        // Fade out
        Color c = sr.color;
        c.a = Mathf.Lerp(1f, 0f, t);
        sr.color = c;
    }
}