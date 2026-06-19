using System.Collections;
using UnityEngine;

public class BombCactusPlant : TrapPlantBase {
    [Header("Bomb Cactus Settings")]
    [SerializeField] private float explosionRadius = 2.0f;
    [SerializeField] private float splashDamagePercent = 0.5f;
    [SerializeField] private float triggerDistance = 1.0f;

    private float idleParticleTimer = 0f;

    protected override void Start() {
        base.Start();

        // Adjust collider based on trigger distance
        var bc = GetComponent<BoxCollider2D>();
        if (bc != null) {
            bc.size = new Vector2(triggerDistance * 2f, 1f);
            bc.isTrigger = true;
        }

        // Tint the plant red-orange (only if not already colored by dynamic placement)
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.color == Color.white) {
            sr.color = new Color(0.85f, 0.25f, 0.15f, 1.0f);
        }
    }

    protected override void Update() {
        base.Update();

        // Heavy slow breathing animation (distinct to Bomb Cactus)
        if (!isTriggered) {
            float pulse = 1.0f + Mathf.Sin(Time.time * 2f) * 0.05f;
            transform.localScale = new Vector3(originalScale.x * pulse, originalScale.y * pulse, originalScale.z);
        }

        // Spawn persistent idle spark particles
        idleParticleTimer += Time.deltaTime;
        if (idleParticleTimer >= 0.3f) {
            SpawnIdleSpark();
            idleParticleTimer = 0f;
        }
    }

    private void SpawnIdleSpark() {
        var partGo = new GameObject("CactusIdleSpark");
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.2f, 0.5f), -0.1f);
        var p = partGo.AddComponent<FlameParticle>();
        Vector3 velocity = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.4f, 0.9f), 0f);
        Color color = Color.Lerp(new Color(1f, 0.4f, 0.1f, 0.7f), new Color(1f, 0.7f, 0.0f, 0.6f), Random.value);
        p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.3f, 0.55f), Random.Range(0.1f, 0.18f));
    }

    protected override void OnTrapTriggered(ZombieController zombie) {
        StartCoroutine(TriggerSequence(zombie));
    }

    private IEnumerator TriggerSequence(ZombieController zombie) {
        Vector3 triggerPos = transform.position;

        // 1. Direct damage to the triggering zombie
        if (zombie != null) {
            var health = zombie.GetComponent<ZombieHealth>();
            if (health != null) {
                health.TakeDamage(damage);
            }
        }

        // 2. Splash damage to nearby zombies
        Collider2D[] nearby = Physics2D.OverlapCircleAll(triggerPos, explosionRadius);
        foreach (var col in nearby) {
            if (zombie != null && col.gameObject == zombie.gameObject) continue; // Skip direct target
            var nearbyHealth = col.GetComponent<ZombieHealth>();
            if (nearbyHealth != null) {
                int splashDmg = Mathf.Max(1, Mathf.RoundToInt(damage * splashDamagePercent));
                nearbyHealth.TakeDamage(splashDmg);
            }
        }

        // 3. Explosion effect
        SpawnExplosionEffect(triggerPos);

        // 4. Quick scale-down/dissolve visual
        float elapsed = 0f;
        float duration = 0.25f;
        Vector3 startScale = transform.localScale;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        // 5. Consume trap (destroys the cactus and frees the cell)
        ConsumeTrap();
    }

    private void SpawnExplosionEffect(Vector3 position) {
        int particleCount = Random.Range(20, 30);
        for (int i = 0; i < particleCount; i++) {
            var partGo = new GameObject("ExplosionParticle");
            partGo.transform.position = position;
            var p = partGo.AddComponent<FlameParticle>();
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float spd = Random.Range(2.0f, 5.5f);
            Vector3 velocity = new Vector3(Mathf.Cos(angle) * spd, Mathf.Sin(angle) * spd, 0f);

            Color color;
            float colorRandom = Random.value;
            if (colorRandom < 0.33f) {
                color = Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), Random.value);
            } else if (colorRandom < 0.66f) {
                color = Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, Random.value);
            } else {
                color = Color.Lerp(new Color(0.3f, 0.3f, 0.3f, 0.8f), new Color(0.5f, 0.5f, 0.5f, 0.6f), Random.value); // Smoke
            }
            p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.3f, 0.65f), Random.Range(0.25f, 0.5f));
        }

        // Large central flash
        var flashGo = new GameObject("ExplosionFlash");
        flashGo.transform.position = position;
        var flash = flashGo.AddComponent<MuzzleFlashEffect>();
        flash.Setup(Projectile.CreateFireballSprite(), new Color(1f, 0.8f, 0.2f, 1f), new Vector3(3.5f, 3.5f, 1f), 0.3f);
    }
}
