using System.Collections;
using UnityEngine;

public class GuardianOakPlant : PlantBase {
    private float particleTimer = 0f;



    protected override void Update() {
        base.Update();

        // Gentle breathing scale motion
        float breathing = Mathf.Sin(Time.time * 2f) * 0.03f;
        transform.localScale = new Vector3(
            originalScale.x + breathing,
            originalScale.y + breathing * 0.5f,
            originalScale.z
        );

        // Leaf/root rotation sway
        float rotSway = Mathf.Cos(Time.time * 1.5f) * 1.5f;
        transform.rotation = Quaternion.Euler(0f, 0f, rotSway);

        // Magical subtle green glow particles
        particleTimer += Time.deltaTime;
        if (particleTimer >= 0.4f) {
            particleTimer = 0f;
            SpawnSubtleGlowParticle();
        }
    }

    private void SpawnSubtleGlowParticle() {
        var glow = new GameObject("OakGlowParticle");
        glow.transform.position = transform.position + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.7f), -0.1f);
        var p = glow.AddComponent<FlameParticle>();
        Vector3 vel = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.15f, 0.35f), 0f);
        Color color = new Color(0.4f, 0.9f, 0.3f, 0.5f);
        p.Setup(Projectile.CreateFireballSprite(), color, vel, Random.Range(0.15f, 0.3f), Random.Range(0.6f, 1.0f));
    }

    protected override IEnumerator DeathAnimationRoutine() {
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        float elapsed = 0f;
        float duration = 0.8f;
        Vector3 startScale = transform.localScale;
        Color startCol = spriteRenderer != null ? spriteRenderer.color : Color.white;

        // Spawn falling leaves
        for (int i = 0; i < 15; i++) {
            SpawnLeafParticle();
        }

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (spriteRenderer != null) {
                Color c = startCol;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }
            transform.localScale = startScale * (1f - t * 0.1f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void SpawnLeafParticle() {
        var leaf = new GameObject("OakLeafParticle");
        leaf.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.1f, 0.6f), -0.1f);
        var p = leaf.AddComponent<FlameParticle>();
        Vector3 velocity = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.8f, -0.2f), 0f);
        Color color = Color.Lerp(new Color(0.15f, 0.45f, 0.15f, 0.8f), new Color(0.35f, 0.65f, 0.25f, 0.6f), Random.value);
        p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.25f, 0.45f), Random.Range(0.4f, 0.7f));
    }

    protected override void Attack(GameObject target) {
        // Blocker does not attack
    }

    protected override GameObject DetectZombieInLane() {
        // Blocker does not detect/attack zombies at range
        return null;
    }
}
