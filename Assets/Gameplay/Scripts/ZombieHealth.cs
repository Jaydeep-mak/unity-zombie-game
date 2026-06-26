using System.Collections;
using UnityEngine;

public class ZombieHealth : MonoBehaviour {
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;
    public int baseDamage = 1;
    public int coinReward = 10;

    [Header("Visuals")]
    private SpriteRenderer sr;
    private Color originalColor;
    private Coroutine flashCoroutine;
    [SerializeField] private Sprite hitSprite;
    private SpriteRenderer[] childRenderers;
    private Color[] originalColors;

    [Header("Health Bar")]
    private GameObject healthBarGroup;
    private GameObject healthBarFill;
    private float maxFillScaleX = 0.8f; // width in units

    private bool isDead = false;
    public bool IsDead => isDead;

    [HideInInspector] public string zombieType = "Basic";

    private bool isInitialized = false;

    private void Start() {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded() {
        if (isInitialized) return;
        isInitialized = true;

        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            originalColor = sr.color;
        }

        childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (childRenderers != null && childRenderers.Length > 0) {
            originalColors = new Color[childRenderers.Length];
            for (int i = 0; i < childRenderers.Length; i++) {
                originalColors[i] = childRenderers[i].color;
            }
        }

        CreateHealthBar();

        if (GameplayManager.Instance != null) {
            GameplayManager.Instance.RegisterZombie();
        }
    }

    public void Setup(int maxHp) {
        InitializeIfNeeded();
        maxHealth = maxHp;
        currentHealth = maxHp;
        UpdateHealthBar();
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

    public void SetTintColor(Color color) {
        InitializeIfNeeded();
        if (childRenderers != null) {
            for (int i = 0; i < childRenderers.Length; i++) {
                if (childRenderers[i] != null) {
                    childRenderers[i].color = color;
                    if (originalColors != null && i < originalColors.Length) {
                        originalColors[i] = color;
                    }
                }
            }
        }
    }

    public void TakeDamage(int damage) {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();

        SpawnHitEffect();

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRedRoutine());

        // Play Hurt animation
        var animator = GetComponentInChildren<Animator>();
        if (animator != null) {
            animator.speed = 1.0f; // Reset animator speed for damage feedback
            animator.Play("Hurt", 0, 0f);
        }

        if (currentHealth <= 0) {
            Die();
        } else {
            PlayHurtSound();
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
        //var hitSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Gameplay/Textures/DefenseLineGlow.png");
        if (hitSprite != null) {
            GameObject hitGo = new GameObject("HitEffect");
            hitGo.transform.position = transform.position + new Vector3(-0.1f, 0.1f, -0.1f);
            var effect = hitGo.AddComponent<MuzzleFlashEffect>();
            // Orange fire-impact glow
            effect.Setup(hitSprite, new Color(1.0f, 0.5f, 0.1f, 1.0f), new Vector3(1.2f, 1.2f, 1.0f), 0.2f);
        }
    }

    private IEnumerator FlashRedRoutine() {
        if (childRenderers != null && childRenderers.Length > 0) {
            for (int i = 0; i < childRenderers.Length; i++) {
                if (childRenderers[i] != null) {
                    childRenderers[i].color = new Color(1f, 0.4f, 0.4f, 1f);
                }
            }
            yield return new WaitForSeconds(0.1f);
            for (int i = 0; i < childRenderers.Length; i++) {
                if (childRenderers[i] != null) {
                    childRenderers[i].color = originalColors[i];
                }
            }
        } else if (sr != null) {
            sr.color = new Color(1f, 0.4f, 0.4f, 1f);
            yield return new WaitForSeconds(0.1f);
            sr.color = originalColor;
        }
    }

    private void Die() {
        isDead = true;

        if (GameplayManager.Instance != null) {
            GameplayManager.Instance.ZombieKilled(coinReward);
        }

        PlayDeathSound();

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
        var animator = GetComponentInChildren<Animator>();
        if (animator != null) {
            animator.speed = 1.0f; // Reset animator speed for death animation
            animator.Play("Die");
        }

        float elapsed = 0f;
        float duration = animator != null ? 1.2f : 0.4f;
        Vector3 startScale = transform.localScale;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(0f, 0f, 90f);

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (animator == null) {
                transform.rotation = Quaternion.Lerp(startRot, endRot, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                if (sr != null) {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    sr.color = c;
                }
            } else {
                if (childRenderers != null && originalColors != null) {
                    for (int i = 0; i < childRenderers.Length; i++) {
                        if (childRenderers[i] != null && i < originalColors.Length) {
                            Color c = childRenderers[i].color;
                            c.a = Mathf.Lerp(originalColors[i].a, 0f, t);
                            childRenderers[i].color = c;
                        }
                    }
                }
                if (t > 0.5f) {
                    float shrinkT = (t - 0.5f) / 0.5f;
                    transform.localScale = Vector3.Lerp(startScale, Vector3.zero, shrinkT);
                }
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

    private void PlayHurtSound() {
        if (AudioManager.Instance == null) return;
        
        SFXType hurtSFX;
        string type = zombieType != null ? zombieType.ToLower() : "";
        if (type.Contains("runner") || type.Contains("fast")) {
            hurtSFX = SFXType.RunnerZombieHurt;
        } else if (type.Contains("tank") || type.Contains("heavy")) {
            hurtSFX = SFXType.TankZombieHurt;
        } else if (type.Contains("berserker") || type.Contains("giant") || type.Contains("boss")) {
            hurtSFX = SFXType.BerserkerZombieHurt;
        } else {
            hurtSFX = SFXType.BasicZombieHurt;
        }
        AudioManager.Instance.Play(hurtSFX);
    }

    private void PlayDeathSound() {
        if (AudioManager.Instance == null) return;
        
        SFXType deathSFX;
        string type = zombieType != null ? zombieType.ToLower() : "";
        if (type.Contains("runner") || type.Contains("fast")) {
            deathSFX = SFXType.RunnerZombieDie;
        } else if (type.Contains("tank") || type.Contains("heavy")) {
            deathSFX = SFXType.TankZombieDie;
        } else if (type.Contains("berserker") || type.Contains("giant") || type.Contains("boss")) {
            deathSFX = SFXType.BerserkerZombieDie;
        } else {
            deathSFX = SFXType.BasicZombieDie;
        }
        AudioManager.Instance.Play(deathSFX);
    }
}