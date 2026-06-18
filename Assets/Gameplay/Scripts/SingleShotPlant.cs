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

    protected override void Start() {
        base.Start();
        originalScale = transform.localScale;
        
        // Auto-assign default sprites if not configured
        if (muzzleGlowSprite == null) {
            muzzleGlowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Gameplay/Textures/DefenseLineGlow.png");
        }
        if (muzzleSmokeSprite == null) {
            muzzleSmokeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Gameplay/Textures/Fog_Particle.png");
        }
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
            glow.Setup(muzzleGlowSprite, new Color(0.4f, 1f, 0.4f, 1f), new Vector3(1.0f, 1.0f, 1.0f), 0.2f);
        }

        if (muzzleSmokeSprite != null) {
            GameObject smokeGo = new GameObject("MuzzleSmoke");
            smokeGo.transform.position = spawnPos + Vector3.right * 0.1f;
            var smoke = smokeGo.AddComponent<MuzzleFlashEffect>();
            smoke.Setup(muzzleSmokeSprite, new Color(0.8f, 0.9f, 0.8f, 0.6f), new Vector3(0.6f, 0.6f, 1.0f), 0.3f);
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