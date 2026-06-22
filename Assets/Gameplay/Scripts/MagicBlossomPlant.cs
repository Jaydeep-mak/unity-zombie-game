using System.Collections;
using UnityEngine;

public class MagicBlossomPlant : PlantBase {
    [Header("Visual References")]
    [SerializeField] private Transform firePoint;

    private PlantAttackSystem attackSystem;
    private Coroutine wiggleCoroutine;
    private float idleParticleTimer = 0f;

    protected override void Start() {
        base.Start();

        if (firePoint == null) {
            firePoint = transform.Find("FirePoint");
        }

        // Tint the plant to a magical violet/magenta (only if not already colored by dynamic placement)
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.color == Color.white) {
            sr.color = new Color(0.85f, 0.2f, 0.85f, 1.0f);
        }

        attackSystem = GetComponent<PlantAttackSystem>();
        if (attackSystem == null) {
            attackSystem = gameObject.AddComponent<PlantAttackSystem>();
        }
        attackSystem.CurrentAttackType = AttackType.MachineGun;
    }

    protected override void Update() {
        base.Update();

        // Spawn persistent idle magical particles rising from the head of the plant
        idleParticleTimer += Time.deltaTime;
        if (idleParticleTimer >= 0.15f) {
            SpawnIdleMagicParticle();
            idleParticleTimer = 0f;
        }

        // Idle breathing animation if not firing wiggles
        if (wiggleCoroutine == null) {
            float bob = 1.0f + Mathf.Sin(Time.time * 6f) * 0.03f;
            transform.localScale = new Vector3(originalScale.x, originalScale.y * bob, originalScale.z);
        }
    }

    private void SpawnIdleMagicParticle() {
        var partGo = new GameObject("MagicIdleParticle");
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.2f, 0.5f), -0.1f);
        var p = partGo.AddComponent<MagicParticle>();
        Vector3 velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.4f, 0.9f), 0f);
        
        // Blend purple and cyan colors
        Color color = Color.Lerp(new Color(0.85f, 0.1f, 0.85f, 0.7f), new Color(0.2f, 0.8f, 1f, 0.6f), Random.value);
        p.Setup(MagicProjectile.GetParticleSprite(), color, velocity, Random.Range(0.35f, 0.6f), Random.Range(0.12f, 0.25f));
    }

    protected override GameObject DetectZombieInLane() {
        // Global targeting: Find the leftmost (most advanced) active zombie anywhere on the battlefield
        var zombies = FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
        ZombieController leftmostZombie = null;
        float minX = float.MaxValue;

        foreach (var zombie in zombies) {
            if (zombie == null) continue;
            var health = zombie.GetComponent<ZombieHealth>();
            if (health != null && !health.IsDead) {
                if (zombie.transform.position.x < minX) {
                    minX = zombie.transform.position.x;
                    leftmostZombie = zombie;
                }
            }
        }
        return leftmostZombie != null ? leftmostZombie.gameObject : null;
    }

    protected override void Attack(GameObject target) {
        if (attackSystem == null) {
            attackSystem = GetComponent<PlantAttackSystem>();
            if (attackSystem == null) {
                attackSystem = gameObject.AddComponent<PlantAttackSystem>();
            }
        }

        Color bulletColor = new Color(0.9f, 0.2f, 0.9f, 1.0f); // Magenta/Violet magic tint
        attackSystem.ExecuteAttack(this, firePoint, target, damage, projectileSpeed, projectilePrefab, bulletColor, PlantName);

        // Spawn muzzle flash
        SpawnMagicMuzzleFlash();

        // Rapid recoil/vibration animation
        if (wiggleCoroutine != null) StopCoroutine(wiggleCoroutine);
        wiggleCoroutine = StartCoroutine(WiggleRoutine());
    }

    private void SpawnMagicMuzzleFlash() {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.right * 0.35f;

        GameObject flashGo = new GameObject("MagicMuzzleFlash");
        flashGo.transform.position = spawnPos;
        var flash = flashGo.AddComponent<MuzzleFlashEffect>();
        
        Color flashColor = Color.Lerp(new Color(0.95f, 0.1f, 0.85f, 0.9f), new Color(0.2f, 0.85f, 1f, 0.9f), Random.value);
        flash.Setup(GetMuzzleGlowSprite(), flashColor, new Vector3(1.2f, 1.2f, 1f), 0.1f);
    }

    private IEnumerator WiggleRoutine() {
        float elapsed = 0f;
        float duration = 0.04f; // Extremely fast squash
        Vector3 wiggleScale = new Vector3(originalScale.x * 1.1f, originalScale.y * 0.9f, originalScale.z);

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, wiggleScale, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(wiggleScale, originalScale, elapsed / duration);
            yield return null;
        }

        transform.localScale = originalScale;
        wiggleCoroutine = null;
    }

    private static Sprite proceduralGlowSprite;
    private Sprite GetMuzzleGlowSprite() {
        if (proceduralGlowSprite != null) return proceduralGlowSprite;

        int width = 64;
        int height = 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[width * height];
        Vector2 center = new Vector2(width / 2f, height / 2f);
        float radius = width / 2f;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > radius) {
                    cols[y * width + x] = Color.clear;
                    continue;
                }
                float t = dist / radius;
                float alpha = Mathf.Clamp01(1f - t);
                alpha = alpha * alpha;
                cols[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        proceduralGlowSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        return proceduralGlowSprite;
    }
}
