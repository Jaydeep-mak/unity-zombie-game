using System.Collections;
using UnityEngine;

public class GunGuardianPlant : PlantBase {
    [Header("Visual References")]
    [SerializeField] private Transform firePoint;

    private Coroutine attackCoroutine;
    private float idleParticleTimer = 0f;

    protected override void Start() {
        base.Start();

        if (firePoint == null) {
            firePoint = transform.Find("FirePoint");
        }

        // Tint to a futuristic cyan/teal by default if not colored by slot config
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.color == Color.white) {
            sr.color = new Color(0f, 0.85f, 1f, 1f);
        }
    }

    protected override void Update() {
        base.Update();

        // Spawn persistent idle cyan energy sparks
        idleParticleTimer += Time.deltaTime;
        if (idleParticleTimer >= 0.25f) {
            SpawnIdleEnergySpark();
            idleParticleTimer = 0f;
        }

        // Mechanical jitter idle animation (distinct from organic breathing)
        if (attackCoroutine == null) {
            float jitter = 1.0f + Mathf.PingPong(Time.time * 5f, 0.02f);
            transform.localScale = new Vector3(originalScale.x * jitter, originalScale.y, originalScale.z);
        }
    }

    private void SpawnIdleEnergySpark() {
        var partGo = new GameObject("GuardianIdleEnergy");
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.2f, 0.5f), -0.1f);
        var p = partGo.AddComponent<FlameParticle>();
        Vector3 velocity = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.4f, 0.8f), 0f);
        Color color = Color.Lerp(new Color(0f, 0.8f, 1f, 0.7f), new Color(0.6f, 0.95f, 1f, 0.6f), Random.value);
        p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.3f, 0.55f), Random.Range(0.1f, 0.18f));
    }

    protected override void Attack(GameObject target) {
        if (attackCoroutine != null) {
            StopCoroutine(attackCoroutine);
        }
        attackCoroutine = StartCoroutine(AnimateAttack());
    }

    private IEnumerator AnimateAttack() {
        float time = 0f;

        // 1. Mechanical Chamber Back (Recoil)
        Vector3 recoilScale = new Vector3(originalScale.x * 0.82f, originalScale.y * 1.1f, originalScale.z);
        while (time < 0.05f) {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, recoilScale, time / 0.05f);
            yield return null;
        }

        // 2. Lunge Forward (Fire bullet)
        time = 0f;
        Vector3 lungeScale = new Vector3(originalScale.x * 1.18f, originalScale.y * 0.9f, originalScale.z);
        while (time < 0.06f) {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(recoilScale, lungeScale, time / 0.06f);
            yield return null;
        }

        // --- Spawn Bullet Projectile ---
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.right * 0.35f;
        Color bulletColor = new Color(0f, 0.85f, 1f, 1f); // Neon Cyan

        if (projectilePrefab != null) {
            GameObject projGo = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = projGo.GetComponent<Projectile>();
            if (proj != null) {
                proj.Setup(projectileSpeed, damage, false, bulletColor, PlantName);
            }
        }

        // Muzzle Flash
        GameObject glowGo = new GameObject("GuardianMuzzleFlash");
        glowGo.transform.position = spawnPos;
        var glow = glowGo.AddComponent<MuzzleFlashEffect>();
        glow.Setup(GetMuzzleGlowSprite(), new Color(0f, 0.85f, 1f, 0.9f), new Vector3(1.3f, 1.3f, 1f), 0.12f);
        // --------------------------------

        // 3. Return to standard state
        time = 0f;
        while (time < 0.12f) {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(lungeScale, originalScale, time / 0.12f);
            yield return null;
        }

        transform.localScale = originalScale;
        attackCoroutine = null;
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
