using System.Collections;
using UnityEngine;

public class ZombieHealth : MonoBehaviour {
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

    [Header("Visuals")]
    private SpriteRenderer sr;
    private Color originalColor;
    private Coroutine flashCoroutine;

    [Header("Health Bar")]
    private GameObject healthBarGroup;
    private GameObject healthBarFill;
    private float maxFillScaleX = 0.8f; // width in units

    private bool isDead = false;

    private void Start() {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            originalColor = sr.color;
        }

        CreateHealthBar();

        if (GameplayManager.Instance != null) {
            GameplayManager.Instance.RegisterZombie();
        }
    }

    private Sprite CreateFlatSprite() {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f); // 1 pixel = 1 unit at scale 1
    }

    private void CreateHealthBar() {
        healthBarGroup = new GameObject("HealthBar");
        healthBarGroup.transform.SetParent(transform);
        healthBarGroup.transform.localPosition = new Vector3(0f, 0.8f, -0.1f); // Above head

        Sprite flatSprite = CreateFlatSprite();

        // Background
        var bg = new GameObject("BG");
        bg.transform.SetParent(healthBarGroup.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(0.9f, 0.15f, 1f); // 0.9 units wide, 0.15 units high
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = flatSprite;
        bgSr.color = Color.black;
        bgSr.sortingOrder = 8;

        // Fill
        healthBarFill = new GameObject("Fill");
        healthBarFill.transform.SetParent(healthBarGroup.transform);
        healthBarFill.transform.localPosition = Vector3.zero;
        healthBarFill.transform.localScale = new Vector3(maxFillScaleX, 0.09f, 1f); // 0.8 units wide, 0.09 units high
        var fillSr = healthBarFill.AddComponent<SpriteRenderer>();
        fillSr.sprite = flatSprite;
        fillSr.color = Color.red;
        fillSr.sortingOrder = 9;
    }

    public void TakeDamage(int damage) {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();

        SpawnHitEffect();

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRedRoutine());

        if (currentHealth <= 0) {
            Die();
        }
    }

    private void UpdateHealthBar() {
        if (healthBarFill != null) {
            float ratio = (float)currentHealth / maxHealth;
            healthBarFill.transform.localScale = new Vector3(ratio * maxFillScaleX, 0.09f, 1f);
            healthBarFill.transform.localPosition = new Vector3(- (1f - ratio) * maxFillScaleX * 0.5f, 0f, 0f);
        }
    }

    private void SpawnHitEffect() {
        var hitSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Gameplay/Textures/DefenseLineGlow.png");
        if (hitSprite != null) {
            GameObject hitGo = new GameObject("HitEffect");
            hitGo.transform.position = transform.position + new Vector3(-0.1f, 0.1f, -0.1f);
            var effect = hitGo.AddComponent<MuzzleFlashEffect>();
            // Orange fire-impact glow
            effect.Setup(hitSprite, new Color(1.0f, 0.5f, 0.1f, 1.0f), new Vector3(1.2f, 1.2f, 1.0f), 0.2f);
        }
    }

    private IEnumerator FlashRedRoutine() {
        if (sr != null) {
            sr.color = new Color(1f, 0.4f, 0.4f, 1f);
            yield return new WaitForSeconds(0.1f);
            sr.color = originalColor;
        }
    }

    private void Die() {
        isDead = true;

        if (GameplayManager.Instance != null) {
            GameplayManager.Instance.ZombieKilled();
        }

        // Spawn fire death burst
        for (int i = 0; i < 12; i++) {
            var partGo = new GameObject("DeathFlame");
            partGo.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.4f, 0.4f), -0.05f);
            var p = partGo.AddComponent<FlameParticle>();
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(0.5f, 2f);
            Vector3 velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 1f, 0f); // Float up
            Color color = Color.Lerp(new Color(1f, 0.6f, 0f), Color.red, Random.value);
            p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.4f, 0.7f), Random.Range(0.2f, 0.4f));
        }

        var controller = GetComponent<ZombieController>();
        if (controller != null) controller.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        if (healthBarGroup != null) Destroy(healthBarGroup);

        StartCoroutine(DeathAnimationRoutine());
    }

    private IEnumerator DeathAnimationRoutine() {
        float elapsed = 0f;
        float duration = 0.4f;
        Vector3 startScale = transform.localScale;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(0f, 0f, 90f);

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.rotation = Quaternion.Lerp(startRot, endRot, t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (sr != null) {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy() {
        if (GameplayManager.Instance != null) {
            GameplayManager.Instance.UnregisterZombie();
        }
    }
}