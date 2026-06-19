using System.Collections;
using UnityEngine;

public class SingleShotPlant : PlantBase {
    [Header("Visual References")]
    [SerializeField] private Transform firePoint;

    [Header("Juice & Effects")]
    [SerializeField] private Sprite muzzleGlowSprite;
    [SerializeField] private Sprite muzzleSmokeSprite;

    private Vector3 originalScale;
    private Coroutine attackCoroutine;
    private float flameIdleTimer = 0f;

    protected override void Start() {
        base.Start();
        originalScale = transform.localScale;
        
        // Tint the plant to represent a Fire Plant (only if not already colored by dynamic placement)
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.color == Color.white) {
            sr.color = new Color(1.0f, 0.45f, 0.15f, 1.0f); // Warm fire-orange tint
        }
    }

    protected override void Update() {
        base.Update();

        // Spawn persistent idle flame particles rising from the head of the plant
        flameIdleTimer += Time.deltaTime;
        if (flameIdleTimer >= 0.15f) {
            SpawnIdleFlame();
            flameIdleTimer = 0f;
        }
    }

    private void SpawnIdleFlame() {
        var partGo = new GameObject("PlantIdleFlame");
        // Spawn slightly above the center of the plant
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.2f, 0.5f), -0.1f);
        var p = partGo.AddComponent<FlameParticle>();
        // Float slowly upwards
        Vector3 velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.5f, 1.2f), 0f);
        Color color = Color.Lerp(new Color(1f, 0.6f, 0f, 0.8f), new Color(1f, 0.2f, 0f, 0.8f), Random.value);
        p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.4f, 0.7f), Random.Range(0.15f, 0.3f));
    }

    protected override void Attack(GameObject target) {
        if (attackCoroutine != null) {
            StopCoroutine(attackCoroutine);
        }
        attackCoroutine = StartCoroutine(AnimateAttack());
    }

    private IEnumerator AnimateAttack() {
        float time = 0f;
        
        // 1. Recoil (Squash down and prep to lunge)
        Vector3 recoilScale = new Vector3(originalScale.x * 0.8f, originalScale.y * 1.15f, originalScale.z);
        while (time < 0.06f) {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, recoilScale, time / 0.06f);
            yield return null;
        }

        // 2. Lunge (Spit projectile)
        time = 0f;
        Vector3 lungeScale = new Vector3(originalScale.x * 1.25f, originalScale.y * 0.85f, originalScale.z);
        while (time < 0.08f) {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(recoilScale, lungeScale, time / 0.08f);
            yield return null;
        }

        // --- Spawn Projectile & Effects ---
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        
        if (projectilePrefab != null) {
            GameObject projGo = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = projGo.GetComponent<Projectile>();
            if (proj != null) {
                proj.Setup(projectileSpeed, damage);
            }
        }

        if (muzzleGlowSprite != null) {
            GameObject glowGo = new GameObject("MuzzleGlow");
            glowGo.transform.position = spawnPos;
            var glow = glowGo.AddComponent<MuzzleFlashEffect>();
            // Fiery orange muzzle glow
            glow.Setup(muzzleGlowSprite, new Color(1.0f, 0.5f, 0.1f, 1.0f), new Vector3(1.2f, 1.2f, 1.0f), 0.2f);
        }

        if (muzzleSmokeSprite != null) {
            GameObject smokeGo = new GameObject("MuzzleSmoke");
            smokeGo.transform.position = spawnPos + Vector3.right * 0.1f;
            var smoke = smokeGo.AddComponent<MuzzleFlashEffect>();
            // Dark gray fire smoke
            smoke.Setup(muzzleSmokeSprite, new Color(0.25f, 0.25f, 0.28f, 0.6f), new Vector3(0.7f, 0.7f, 1.0f), 0.35f);
        }
        // ----------------------------------

        // 3. Return (Recover to original scale)
        time = 0f;
        while (time < 0.15f) {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(lungeScale, originalScale, time / 0.15f);
            yield return null;
        }

        transform.localScale = originalScale;
        attackCoroutine = null;
    }
}