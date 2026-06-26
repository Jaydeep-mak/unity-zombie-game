using System.Collections;
using UnityEngine;

public class ThornVinePlant : PlantBase {
    [Header("Thorn Vine Settings")]
    [SerializeField] private float doubleAttackChance = 0.2f;

    [Header("Visual References")]
    [SerializeField] private Transform firePoint;

    private Coroutine attackCoroutine;
    private float idleParticleTimer = 0f;

    protected override void Start() {
        base.Start();

        if (firePoint == null) {
            firePoint = transform.Find("FirePoint");
        }

        // Tint the plant green (only if not already colored by dynamic placement)
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.color == Color.white) {
            sr.color = new Color(0.2f, 0.75f, 0.15f, 1.0f);
        }
    }

    protected override void Update() {
        base.Update();

        // Spawn persistent idle thorn particles
        idleParticleTimer += Time.deltaTime;
        if (idleParticleTimer >= 0.2f) {
            SpawnIdleParticle();
            idleParticleTimer = 0f;
        }

        // Rapid wiggling animation (distinct to Thorn Vine)
        if (attackCoroutine == null) {
            float wiggle = Mathf.Sin(Time.time * 8f) * 0.03f;
            transform.rotation = Quaternion.Euler(0f, 0f, wiggle * Mathf.Rad2Deg);
            transform.localScale = originalScale;
        } else {
            transform.rotation = Quaternion.identity;
        }
    }

    private void SpawnIdleParticle() {
        var partGo = new GameObject("ThornIdleParticle");
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.15f, 0.45f), -0.1f);
        var p = partGo.AddComponent<FlameParticle>();
        Vector3 velocity = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0.3f, 0.8f), 0f);
        Color color = Color.Lerp(new Color(0.3f, 0.7f, 0.1f, 0.7f), new Color(0.5f, 0.4f, 0.15f, 0.6f), Random.value);
        p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.35f, 0.6f), Random.Range(0.1f, 0.2f));
    }

    protected override void Attack(GameObject target) {
        if (attackCoroutine != null) {
            StopCoroutine(attackCoroutine);
        }
        bool doDouble = Random.value < doubleAttackChance;
        attackCoroutine = StartCoroutine(AnimateAttack(doDouble));
    }

    private IEnumerator AnimateAttack(bool doubleAttack) {
        float time = 0f;

        // 1. Quick recoil (faster than Fire Bloom since we attack faster)
        Vector3 recoilScale = new Vector3(originalScale.x * 0.85f, originalScale.y * 1.12f, originalScale.z);
        while (time < 0.04f) {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, recoilScale, time / 0.04f);
            yield return null;
        }

        // 2. Quick lunge
        time = 0f;
        Vector3 lungeScale = new Vector3(originalScale.x * 1.2f, originalScale.y * 0.88f, originalScale.z);
        while (time < 0.05f) {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(recoilScale, lungeScale, time / 0.05f);
            yield return null;
        }

        // --- Spawn Thorn Projectile ---
        SpawnThornProjectile();

        // 3. If double attack, fire second after short delay
        if (doubleAttack) {
            yield return new WaitForSeconds(0.1f);
            SpawnThornProjectile();
        }

        // 4. Quick return to original scale
        time = 0f;
        while (time < 0.1f) {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(lungeScale, originalScale, time / 0.1f);
            yield return null;
        }

        transform.localScale = originalScale;
        attackCoroutine = null;
    }

    private void SpawnThornProjectile() {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.right * 0.3f;
        Color thornColor = new Color(0.35f, 0.8f, 0.2f, 1.0f);

        if (projectilePrefab != null) {
            GameObject projGo = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj = projGo.GetComponent<Projectile>();
            if (proj != null) {
                proj.Setup(projectileSpeed, damage, false, thornColor, PlantName);
            }
        }

        if (AudioManager.Instance != null) {
            AudioManager.Instance.Play(SFXType.ThornVineShoot);
        }

        // Muzzle flash — green glow
        GameObject glowGo = new GameObject("ThornMuzzleGlow");
        glowGo.transform.position = spawnPos;
        var glow = glowGo.AddComponent<MuzzleFlashEffect>();
        glow.Setup(GetMuzzleGlowSprite(), new Color(0.3f, 0.85f, 0.15f, 0.9f), new Vector3(1.2f, 1.2f, 1.0f), 0.1f);
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
