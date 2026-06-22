using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackType {
    SingleShot,
    Burst,
    MachineGun,
    Beam,
    Chain
}

public class PlantAttackSystem : MonoBehaviour {
    [Header("Attack Mode")]
    [SerializeField] private AttackType attackType = AttackType.SingleShot;

    [Header("Burst Settings")]
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.12f;

    [Header("Beam Settings")]
    [SerializeField] private float beamDamageInterval = 0.1f;
    private LineRenderer beamLine;
    private float beamDamageTimer = 0f;

    [Header("Chain Settings")]
    [SerializeField] private int chainBounceLimit = 3;
    [SerializeField] private float chainBounceRange = 5f;

    private Coroutine attackCoroutine;

    public AttackType CurrentAttackType {
        get => attackType;
        set => attackType = value;
    }

    public void ExecuteAttack(PlantBase plant, Transform firePoint, GameObject target, int damage, float projectileSpeed, GameObject projectilePrefab, Color color, string plantName) {
        if (target == null) return;

        switch (attackType) {
            case AttackType.SingleShot:
            case AttackType.MachineGun: // MachineGun fires rapidly, called externally at small intervals
                FireSingle(plant, firePoint, target, damage, projectileSpeed, projectilePrefab, color, plantName);
                break;
            case AttackType.Burst:
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(FireBurstRoutine(plant, firePoint, target, damage, projectileSpeed, projectilePrefab, color, plantName));
                break;
            case AttackType.Beam:
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(FireBeamRoutine(plant, firePoint, target, damage));
                break;
            case AttackType.Chain:
                FireChain(plant, firePoint, target, damage, projectileSpeed, projectilePrefab, color, plantName);
                break;
        }
    }

    private void FireSingle(PlantBase plant, Transform firePoint, GameObject target, int damage, float projectileSpeed, GameObject projectilePrefab, Color color, string plantName) {
        Vector3 spawnPos = firePoint != null ? firePoint.position : plant.transform.position + Vector3.right * 0.3f;

        if (plantName != null && (plantName.Contains("Magic") || plantName.Contains("Blossom"))) {
            // Spawn pooled homing MagicProjectile
            MagicProjectile proj = MagicProjectilePool.GetProjectile(projectilePrefab, spawnPos, Quaternion.identity);
            if (proj != null) {
                proj.Setup(projectileSpeed, damage, target, color);
            }
        } else {
            // Standard instantiation for other plants to ensure backward compatibility
            if (projectilePrefab != null) {
                GameObject projGo = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                var proj = projGo.GetComponent<Projectile>();
                if (proj != null) {
                    bool isIce = plantName.Contains("Ice") || plantName.Contains("Frost");
                    proj.Setup(projectileSpeed, damage, isIce, color, plantName);
                }
            }
        }
    }

    private IEnumerator FireBurstRoutine(PlantBase plant, Transform firePoint, GameObject target, int damage, float projectileSpeed, GameObject projectilePrefab, Color color, string plantName) {
        for (int i = 0; i < burstCount; i++) {
            if (target == null || !target.activeInHierarchy) yield break;
            var health = target.GetComponent<ZombieHealth>();
            if (health != null && health.IsDead) yield break;

            FireSingle(plant, firePoint, target, damage, projectileSpeed, projectilePrefab, color, plantName);
            yield return new WaitForSeconds(burstDelay);
        }
    }

    private IEnumerator FireBeamRoutine(PlantBase plant, Transform firePoint, GameObject target, int damage) {
        if (beamLine == null) {
            GameObject beamGo = new GameObject("PlantBeamLine");
            beamLine = beamGo.AddComponent<LineRenderer>();
            beamLine.startWidth = 0.12f;
            beamLine.endWidth = 0.12f;
            
            // Set up basic sprites default material for standard rendering
            beamLine.material = new Material(Shader.Find("Sprites/Default"));
            beamLine.startColor = new Color(0.2f, 0.8f, 1f, 0.8f);
            beamLine.endColor = new Color(0.9f, 0.2f, 0.9f, 0.8f);
            beamLine.sortingOrder = 8;
        }

        beamLine.gameObject.SetActive(true);

        while (target != null && target.activeInHierarchy) {
            var health = target.GetComponent<ZombieHealth>();
            if (health != null && health.IsDead) break;

            Vector3 startPos = firePoint != null ? firePoint.position : plant.transform.position;
            Vector3 endPos = target.transform.position;

            beamLine.SetPosition(0, startPos);
            beamLine.SetPosition(1, endPos);

            beamDamageTimer += Time.deltaTime;
            if (beamDamageTimer >= beamDamageInterval) {
                if (health != null) health.TakeDamage(damage);
                beamDamageTimer = 0f;
            }

            yield return null;
        }

        if (beamLine != null) {
            beamLine.gameObject.SetActive(false);
        }
    }

    private void FireChain(PlantBase plant, Transform firePoint, GameObject target, int damage, float projectileSpeed, GameObject projectilePrefab, Color color, string plantName) {
        Vector3 spawnPos = firePoint != null ? firePoint.position : plant.transform.position + Vector3.right * 0.3f;
        GameObject chainProjGo = new GameObject("ChainProjectile");
        chainProjGo.transform.position = spawnPos;
        var chainProj = chainProjGo.AddComponent<ChainProjectile>();
        chainProj.Setup(projectileSpeed, damage, target, chainBounceLimit, chainBounceRange, color);
    }

    private void OnDestroy() {
        if (beamLine != null) {
            Destroy(beamLine.gameObject);
        }
    }
}

public class ChainProjectile : MonoBehaviour {
    private float speed;
    private int damage;
    private int maxBounces;
    private float bounceRange;
    private Color color;
    private List<GameObject> hitZombies = new List<GameObject>();
    private GameObject currentTarget;

    public void Setup(float speed, int damage, GameObject initialTarget, int maxBounces, float bounceRange, Color color) {
        this.speed = speed;
        this.damage = damage;
        this.currentTarget = initialTarget;
        this.maxBounces = maxBounces;
        this.bounceRange = bounceRange;
        this.color = color;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = Projectile.CreateFireballSprite();
        sr.color = color;
        transform.localScale = new Vector3(0.7f, 0.7f, 1f);
    }

    private void Update() {
        if (currentTarget == null || !currentTarget.activeInHierarchy) {
            FindNextTarget();
            if (currentTarget == null) {
                Destroy(gameObject);
                return;
            }
        }

        Vector2 dir = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).normalized;
        transform.Translate(dir * speed * Time.deltaTime, Space.World);

        if (Vector2.Distance(transform.position, currentTarget.transform.position) < 0.25f) {
            HitTarget();
        }
    }

    private void HitTarget() {
        if (currentTarget == null) return;
        var health = currentTarget.GetComponent<ZombieHealth>();
        if (health != null && !health.IsDead) {
            health.TakeDamage(damage);
        }

        hitZombies.Add(currentTarget);
        maxBounces--;

        if (maxBounces <= 0) {
            Destroy(gameObject);
        } else {
            FindNextTarget();
            if (currentTarget == null) {
                Destroy(gameObject);
            }
        }
    }

    private void FindNextTarget() {
        var zombies = FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
        GameObject bestTarget = null;
        float minDist = bounceRange;

        foreach (var z in zombies) {
            if (z == null || hitZombies.Contains(z.gameObject)) continue;
            var health = z.GetComponent<ZombieHealth>();
            if (health != null && !health.IsDead) {
                float dist = Vector2.Distance(transform.position, z.transform.position);
                if (dist < minDist) {
                    minDist = dist;
                    bestTarget = z.gameObject;
                }
            }
        }

        currentTarget = bestTarget;
    }
}
