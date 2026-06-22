using System.Collections.Generic;
using UnityEngine;

public class MagicProjectile : MonoBehaviour {
    private float speed;
    private int damage;
    private GameObject target;
    private Color color;

    private SpriteRenderer sr;
    private Vector2 lastDirection = Vector2.right;
    private float trailTimer = 0f;
    private float trailInterval = 0.05f;

    private float lifeTime = 5.0f;
    private float lifeTimer = 0f;

    private static Sprite projectileSprite;
    private static Sprite particleSprite;

    private void Awake() {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Add Rigidbody2D and Collider2D if not already present
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }

        var col = GetComponent<Collider2D>();
        if (col == null) {
            col = gameObject.AddComponent<PolygonCollider2D>();
            col.isTrigger = true;
        }
    }

    public void Setup(float speed, int damage, GameObject target, Color color) {
        this.speed = speed;
        this.damage = damage;
        this.target = target;
        this.color = color;
        this.lifeTimer = 0f;
        this.trailTimer = 0f;

        if (sr != null) {
            if (projectileSprite == null) {
                projectileSprite = PlantVisuals.GetProjectileSprite("Magic Blossom");
            }
            sr.sprite = projectileSprite;
            sr.color = color;
        }

        transform.localScale = new Vector3(0.6f, 0.6f, 1f);
    }

    private void Update() {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime) {
            ReturnToPool();
            return;
        }

        // Homing movement
        Vector2 direction;
        if (target != null && target.activeInHierarchy) {
            var targetHealth = target.GetComponent<ZombieHealth>();
            if (targetHealth != null && !targetHealth.IsDead) {
                direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
                lastDirection = direction;
            } else {
                direction = lastDirection;
            }
        } else {
            direction = lastDirection;
        }

        // Move projectile smoothly
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // Rotate to face movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Spawn trailing magic particles
        trailTimer += Time.deltaTime;
        if (trailTimer >= trailInterval) {
            SpawnTrailParticle();
            trailTimer = 0f;
        }
    }

    private void SpawnTrailParticle() {
        if (particleSprite == null) {
            particleSprite = GetParticleSprite();
        }

        var partGo = new GameObject("MagicTrailParticle");
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), 0.05f);
        var p = partGo.AddComponent<MagicParticle>();
        
        // Drift slightly backwards and upwards
        Vector3 velocity = new Vector3(-lastDirection.x * Random.Range(0.5f, 1.2f), Random.Range(-0.3f, 0.3f), 0f);
        Color trailColor = Color.Lerp(color, Color.white, Random.Range(0.2f, 0.6f));
        p.Setup(particleSprite, trailColor, velocity, Random.Range(0.25f, 0.4f), Random.Range(0.12f, 0.25f));
    }

    private void SpawnHitImpactEffect(Vector3 position) {
        if (particleSprite == null) {
            particleSprite = GetParticleSprite();
        }

        for (int i = 0; i < 8; i++) {
            var partGo = new GameObject("MagicImpactParticle");
            partGo.transform.position = position;
            var p = partGo.AddComponent<MagicParticle>();
            
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(1f, 3f);
            Vector3 velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
            
            Color impactColor = Color.Lerp(color, Color.white, Random.Range(0.1f, 0.7f));
            p.Setup(particleSprite, impactColor, velocity, Random.Range(0.2f, 0.35f), Random.Range(0.15f, 0.3f));
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        var zombieHealth = other.GetComponent<ZombieHealth>();
        if (zombieHealth != null && !zombieHealth.IsDead) {
            zombieHealth.TakeDamage(damage);
            SpawnHitImpactEffect(transform.position);
            ReturnToPool();
        }
    }

    private void ReturnToPool() {
        gameObject.SetActive(false);
        MagicProjectilePool.ReturnProjectile(this);
    }

    public static Sprite GetParticleSprite() {
        if (particleSprite != null) return particleSprite;
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);
        float radius = w / 2f;
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > radius) {
                    cols[y * w + x] = Color.clear;
                    continue;
                }
                float t = dist / radius;
                float alpha = Mathf.Clamp01(1f - t);
                alpha = alpha * alpha; // Quad ease out for smooth soft edge
                cols[y * w + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        particleSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        return particleSprite;
    }
}

public class MagicParticle : MonoBehaviour {
    private SpriteRenderer sr;
    private Vector3 moveVelocity;
    private float lifeTime;
    private float elapsed = 0f;
    private Color startColor;

    public void Setup(Sprite sprite, Color color, Vector3 velocity, float fadeTime, float startScale) {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 8; // Render on top of characters
        
        startColor = color;
        moveVelocity = velocity;
        lifeTime = fadeTime;
        transform.localScale = Vector3.one * startScale;
    }

    private void Update() {
        elapsed += Time.deltaTime;
        if (elapsed >= lifeTime) {
            Destroy(gameObject);
            return;
        }

        transform.position += moveVelocity * Time.deltaTime;

        float t = elapsed / lifeTime;
        sr.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
        transform.localScale = Vector3.Lerp(Vector3.one * transform.localScale.x, Vector3.zero, t);
    }
}

public static class MagicProjectilePool {
    private static List<MagicProjectile> pool = new List<MagicProjectile>();
    private static GameObject poolParent;

    public static MagicProjectile GetProjectile(GameObject prefab, Vector3 position, Quaternion rotation) {
        // Look for inactive projectile in pool
        for (int i = 0; i < pool.Count; i++) {
            if (pool[i] != null && !pool[i].gameObject.activeInHierarchy) {
                MagicProjectile proj = pool[i];
                proj.transform.position = position;
                proj.transform.rotation = rotation;
                proj.gameObject.SetActive(true);
                return proj;
            }
        }

        // Initialize pool parent if needed
        if (poolParent == null) {
            poolParent = new GameObject("MagicProjectilePool");
            Object.DontDestroyOnLoad(poolParent);
        }

        // Instantiate new projectile
        GameObject go;
        if (prefab != null) {
            go = Object.Instantiate(prefab, position, rotation, poolParent.transform);
        } else {
            go = new GameObject("MagicProjectilePooled");
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.SetParent(poolParent.transform);
        }

        MagicProjectile newProj = go.GetComponent<MagicProjectile>();
        if (newProj == null) {
            newProj = go.AddComponent<MagicProjectile>();
        }
        
        pool.Add(newProj);
        return newProj;
    }

    public static void ReturnProjectile(MagicProjectile proj) {
        // Deactivated automatically, handled in pool list
    }
    
    public static void ClearPool() {
        foreach (var proj in pool) {
            if (proj != null) {
                Object.Destroy(proj.gameObject);
            }
        }
        pool.Clear();
        if (poolParent != null) {
            Object.Destroy(poolParent);
            poolParent = null;
        }
    }
}
