using UnityEngine;

public class Projectile : MonoBehaviour {
    private float speed;
    private int damage;
    private bool isIce = false;
    private Color projectileColor = Color.white;

    private SpriteRenderer sr;
    private static Sprite fireballSprite;

    private float trailTimer = 0f;
    private float trailInterval = 0.04f;
    private float rightBoundaryX = 7.2f;

    public static Sprite CreateFireballSprite() {
        if (fireballSprite != null) return fireballSprite;

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
                    cols[y * width + x] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }
                float t = dist / radius; // 0 at center, 1 at edge
                Color c;
                if (t < 0.2f) {
                    c = Color.Lerp(Color.white, Color.yellow, t / 0.2f);
                } else if (t < 0.5f) {
                    c = Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f, 1f), (t - 0.2f) / 0.3f);
                } else {
                    c = Color.Lerp(new Color(1f, 0.5f, 0f, 1f), new Color(1f, 0.1f, 0f, 0f), (t - 0.5f) / 0.5f);
                }
                cols[y * width + x] = c;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        fireballSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        return fireballSprite;
    }

    private string plantName = "";

    public void Setup(float speed, int damage, bool isIce = false, Color color = default, string plantName = "") {
        this.speed = speed;
        this.damage = damage;
        this.isIce = isIce;
        this.projectileColor = color == default ? Color.white : color;
        this.plantName = plantName;
    }

    private void Start() {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            Sprite customSprite = PlantVisuals.GetProjectileSprite(plantName);
            if (customSprite != null) {
                sr.sprite = customSprite;
            } else {
                sr.sprite = CreateFireballSprite();
            }
            sr.color = projectileColor;
            transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        }
        
        if (GameplayManager.Instance != null) {
            rightBoundaryX = GameplayManager.Instance.RightBoundaryX;
        }
        
        // Auto-destruct after 5 seconds to prevent memory leaks if it leaves screen
        Destroy(gameObject, 5f);
    }

    private void Update() {
        // Check boundary limit before moving
        if (transform.position.x >= rightBoundaryX) {
            SpawnBoundaryImpactEffect(transform.position);
            Destroy(gameObject);
            return;
        }

        // Projectile travels from LEFT to RIGHT smoothly
        transform.Translate(Vector2.right * speed * Time.deltaTime);

        // Spawn trailing flames
        trailTimer += Time.deltaTime;
        if (trailTimer >= trailInterval) {
            SpawnTrailParticle();
            trailTimer = 0f;
        }
    }

    private void SpawnBoundaryImpactEffect(Vector3 position) {
        for (int i = 0; i < 4; i++) {
            var partGo = new GameObject("BoundarySpark");
            partGo.transform.position = position;
            var p = partGo.AddComponent<FlameParticle>();
            float angle = Random.Range(-Mathf.PI * 0.5f, Mathf.PI * 0.5f) + Mathf.PI;
            float speed = Random.Range(0.6f, 1.8f);
            Vector3 velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
            Color color = isIce 
                ? Color.Lerp(Color.cyan, Color.white, Random.value) 
                : Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), Random.value);
            p.Setup(CreateFireballSprite(), color, velocity, Random.Range(0.15f, 0.25f), Random.Range(0.08f, 0.15f));
        }
    }

    private void SpawnTrailParticle() {
        var partGo = new GameObject("FlameTrail");
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(-0.08f, 0.08f), 0.05f);
        var p = partGo.AddComponent<FlameParticle>();
        Vector3 velocity = new Vector3(Random.Range(-1.2f, -0.4f), Random.Range(0.2f, 0.8f), 0f);
        Color color = isIce 
            ? Color.Lerp(Color.cyan, Color.white, Random.value) 
            : Color.Lerp(Color.yellow, Color.red, Random.value);
        p.Setup(CreateFireballSprite(), color, velocity, Random.Range(0.18f, 0.32f), Random.Range(0.15f, 0.3f));
    }

    private void SpawnHitImpactEffect(Vector3 position) {
        for (int i = 0; i < 10; i++) {
            var partGo = new GameObject("FlameImpact");
            partGo.transform.position = position;
            var p = partGo.AddComponent<FlameParticle>();
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(1.2f, 3.5f);
            Vector3 velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
            Color color = isIce 
                ? Color.Lerp(Color.cyan, Color.white, Random.value) 
                : Color.Lerp(Color.yellow, new Color(1f, 0.2f, 0f), Random.value);
            p.Setup(CreateFireballSprite(), color, velocity, Random.Range(0.22f, 0.42f), Random.Range(0.2f, 0.4f));
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // Check collision with a Zombie
        var zombieHealth = other.GetComponent<ZombieHealth>();
        if (zombieHealth != null) {
            zombieHealth.TakeDamage(damage);
            if (isIce) {
                var zombie = other.GetComponent<ZombieController>();
                if (zombie != null) {
                    var slowEffect = zombie.GetComponent<ZombieSlowEffect>();
                    if (slowEffect == null) {
                        slowEffect = zombie.gameObject.AddComponent<ZombieSlowEffect>();
                    }
                    slowEffect.ApplySlow(0.4f, 4.0f); // Slow down to 40% speed for 4 seconds
                }
            }
            
            if (AudioManager.Instance != null) {
                if (isIce || (plantName != null && (plantName.Contains("Ice") || plantName.Contains("Frost")))) {
                    AudioManager.Instance.Play(SFXType.FrostFlowerHit);
                    AudioManager.Instance.Play(SFXType.FrostFlowerFreeze);
                } else if (plantName != null && (plantName.Contains("Thorn") || plantName.Contains("Vine"))) {
                    AudioManager.Instance.Play(SFXType.ThornVineHit);
                } else {
                    AudioManager.Instance.Play(SFXType.FireBloomHit);
                }
            }
            
            SpawnHitImpactEffect(transform.position);
            Destroy(gameObject); // Destroy projectile on hit
        }
    }
}

public class FlameParticle : MonoBehaviour {
    private SpriteRenderer sr;
    private Vector3 moveVelocity;
    private float lifeTime;
    private float elapsed = 0f;
    private Color startColor;

    public void Setup(Sprite sprite, Color color, Vector3 velocity, float fadeTime, float startScale) {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 7; // Render in front of characters
        
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

        // Move particle
        transform.position += moveVelocity * Time.deltaTime;

        // Shrink and fade
        float t = elapsed / lifeTime;
        sr.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
        transform.localScale = Vector3.Lerp(Vector3.one * transform.localScale.x, Vector3.zero, t);
    }
}