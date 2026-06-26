using System.Collections;
using UnityEngine;

public class SingleShotPlant : PlantBase {
    [Header("Visual References")]
    [SerializeField] private Transform firePoint;

    [Header("Juice & Effects")]
    [SerializeField] private Sprite muzzleGlowSprite;
    [SerializeField] private Sprite muzzleSmokeSprite;

    private Coroutine attackCoroutine;
    private float flameIdleTimer = 0f;

    protected override void Start() {
        base.Start();
        
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

        // Gentle breathing animation (distinct to Fire Bloom)
        if (attackCoroutine == null) {
            float bob = 1.0f + Mathf.Sin(Time.time * 4f) * 0.04f;
            transform.localScale = new Vector3(originalScale.x, originalScale.y * bob, originalScale.z);
        }
    }

    private void SpawnIdleFlame() {
        var partGo = new GameObject("PlantIdleFlame");
        // Spawn slightly above the center of the plant
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.2f, 0.5f), -0.1f);
        var p = partGo.AddComponent<FlameParticle>();
        // Float slowly upwards
        Vector3 velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.5f, 1.2f), 0f);
        
        bool isIce = PlantName != null && (PlantName.Contains("Ice") || PlantName.Contains("Frost"));
        Color color = isIce
            ? Color.Lerp(new Color(0.3f, 0.75f, 1.0f, 0.8f), new Color(0.8f, 0.95f, 1.0f, 0.8f), Random.value)
            : Color.Lerp(new Color(1f, 0.6f, 0f, 0.8f), new Color(1f, 0.2f, 0f, 0.8f), Random.value);
            
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
        
        bool isIce = PlantName != null && (PlantName.Contains("Ice") || PlantName.Contains("Frost"));
        Color bulletColor = isIce ? new Color(0.3f, 0.75f, 1.0f, 1.0f) : new Color(1.0f, 0.45f, 0.15f, 1.0f);

        if (projectilePrefab != null) {
            GameObject projGo = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = projGo.GetComponent<Projectile>();
            if (proj != null) {
                proj.Setup(projectileSpeed, damage, isIce, bulletColor, PlantName);
            }
        }

        if (AudioManager.Instance != null) {
            if (isIce) {
                AudioManager.Instance.Play(SFXType.FrostFlowerLaunch);
            } else {
                AudioManager.Instance.Play(SFXType.FireBloomShoot);
            }
        }

        GameObject glowGo = new GameObject("MuzzleGlow");
        glowGo.transform.position = spawnPos;
        var glow = glowGo.AddComponent<MuzzleFlashEffect>();
        Color glowColor = isIce ? new Color(0.3f, 0.75f, 1.0f, 0.95f) : new Color(1.0f, 0.6f, 0.15f, 0.95f);
        glow.Setup(GetMuzzleGlowSprite(), glowColor, new Vector3(1.5f, 1.5f, 1.0f), 0.15f);

        GameObject smokeGo = new GameObject("MuzzleSmoke");
        smokeGo.transform.position = spawnPos + Vector3.right * 0.15f;
        var smoke = smokeGo.AddComponent<MuzzleFlashEffect>();
        Color smokeColor = isIce ? new Color(0.7f, 0.85f, 1.0f, 0.55f) : new Color(0.28f, 0.28f, 0.32f, 0.55f);
        smoke.Setup(GetMuzzleSmokeSprite(), smokeColor, new Vector3(1.0f, 1.0f, 1.0f), 0.25f);
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

    private static Sprite proceduralGlowSprite;
    private static Sprite proceduralSmokeSprite;

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
                float t = dist / radius; // 0 at center, 1 at edge
                float alpha = Mathf.Clamp01(1f - t);
                alpha = alpha * alpha; // Ease out
                cols[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        proceduralGlowSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        return proceduralGlowSprite;
    }

    private Sprite GetMuzzleSmokeSprite() {
        if (proceduralSmokeSprite != null) return proceduralSmokeSprite;
        
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
                float t = dist / radius; // 0 at center, 1 at edge
                float alpha = Mathf.Clamp01(1f - t);
                alpha = Mathf.Pow(alpha, 1.5f); // Soft falloff
                cols[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        proceduralSmokeSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        return proceduralSmokeSprite;
    }
}