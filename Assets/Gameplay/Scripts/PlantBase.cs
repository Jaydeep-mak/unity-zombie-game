using UnityEngine;

public abstract class PlantBase : MonoBehaviour {
    [Header("Plant Configuration")]
    [SerializeField] protected int damage = 2;
    [SerializeField] protected float attackInterval = 1.5f;
    [SerializeField] protected float attackRange = 12f;
    [SerializeField] protected float projectileSpeed = 5f;
    [SerializeField] protected GameObject projectilePrefab;

    protected float attackTimer = 0f;
    protected Vector3 originalScale;

    [Header("Lifetime System")]
    [SerializeField] protected float lifetime = -1f;
    protected float remainingLifetime;
    protected bool isExpiring = false;
    protected SpriteRenderer spriteRenderer;
    private float blinkTimer = 0f;

    [Header("Health System")]
    [SerializeField] protected int maxHealth = 10;
    protected int currentHealth;

    protected virtual void Start() {
        originalScale = transform.localScale;
        remainingLifetime = lifetime;
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Offset starting timer randomly slightly so plants don't fire exactly in sync if placed at same time
        attackTimer = Random.Range(0f, 0.5f);
        StartCoroutine(PlacementAnimationRoutine());
    }

    protected virtual System.Collections.IEnumerator PlacementAnimationRoutine() {
        float elapsed = 0f;
        float duration = 0.35f;
        Vector3 targetScale = originalScale;
        transform.localScale = Vector3.zero;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Overshoot bounce scale curve
            float scaleModifier = Mathf.Sin(t * Mathf.PI * 1.5f) * 0.15f + t;
            scaleModifier = Mathf.Clamp(scaleModifier, 0f, 1.2f);
            transform.localScale = targetScale * scaleModifier;
            yield return null;
        }
        transform.localScale = targetScale;
    }

    public virtual void TakeDamage(int dmg) {
        if (isExpiring) return;
        currentHealth = Mathf.Max(0, currentHealth - dmg);
        PlayHitAnimation();
        if (currentHealth <= 0) {
            isExpiring = true;
            FreeGridCell();
            PlayDeathAnimation();
        }
    }

    public virtual void PlayHitAnimation() {
        StartCoroutine(HitAnimationRoutine());
    }

    protected virtual System.Collections.IEnumerator HitAnimationRoutine() {
        var sr = GetComponent<SpriteRenderer>();
        Color origColor = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = new Color(1f, 0.4f, 0.4f, 1f);
        
        Vector3 pos = transform.position;
        float elapsed = 0f;
        while (elapsed < 0.15f) {
            elapsed += Time.deltaTime;
            transform.position = pos + (Vector3)Random.insideUnitCircle * 0.08f;
            yield return null;
        }
        transform.position = pos;
        if (sr != null) sr.color = origColor;
    }

    public virtual void PlayDeathAnimation() {
        StartCoroutine(DeathAnimationRoutine());
    }

    protected virtual System.Collections.IEnumerator DeathAnimationRoutine() {
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        float elapsed = 0f;
        float duration = 0.3f;
        Vector3 startScale = transform.localScale;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            transform.Rotate(0f, 0f, 270f * Time.deltaTime);
            yield return null;
        }
        Destroy(gameObject);
    }

    protected virtual void Update() {
        UpdateLifetime();

        attackTimer += Time.deltaTime;
        
        if (attackTimer >= attackInterval) {
            GameObject targetZombie = DetectZombieInLane();
            if (targetZombie != null) {
                Attack(targetZombie);
                attackTimer = 0f;
            }
        }
    }

    protected virtual GameObject DetectZombieInLane() {
        // Raycast horizontally to the right to detect any zombies in this lane
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.right, attackRange);
        foreach (var hit in hits) {
            if (hit.collider != null) {
                var zombie = hit.collider.GetComponent<ZombieController>();
                if (zombie != null) {
                    return zombie.gameObject;
                }
            }
        }
        return null;
    }

    public string PlantName { get; protected set; }
    public bool IsExpiring => isExpiring;
    public int CurrentHealth => currentHealth;

    public virtual void Configure(int damageVal, float intervalVal, float speedVal, Color color, string nameVal = "", float lifetimeVal = -1f, int maxHealthVal = 10) {
        this.damage = damageVal;
        this.attackInterval = intervalVal;
        this.projectileSpeed = speedVal;
        this.PlantName = nameVal;
        this.lifetime = lifetimeVal;
        this.remainingLifetime = lifetimeVal;
        this.maxHealth = maxHealthVal;
        this.currentHealth = maxHealthVal;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) {
            Sprite customSprite = PlantVisuals.GetPlantSprite(nameVal);
            if (customSprite != null) {
                spriteRenderer.sprite = customSprite;
                spriteRenderer.color = Color.white; // Reset to white so custom sprite colors show properly
            } else {
                spriteRenderer.color = color;
            }
        }
    }

    protected void UpdateLifetime() {
        if (lifetime <= 0f || isExpiring) return;

        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 5f) {
            float blinkInterval = 0.5f; // Slow blink (5 to 2 seconds)
            if (remainingLifetime <= 2f) {
                blinkInterval = 0.2f; // Faster blink (2 to 1 seconds)
            }
            if (remainingLifetime <= 1f) {
                blinkInterval = 0.08f; // Rapid blink (1 to 0 seconds)
            }

            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval) {
                blinkTimer = 0f;
                if (spriteRenderer != null) {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                }
            }
        } else {
            if (spriteRenderer != null && !spriteRenderer.enabled) {
                spriteRenderer.enabled = true;
            }
        }

        if (remainingLifetime <= 0f) {
            isExpiring = true;
            if (spriteRenderer != null) {
                spriteRenderer.enabled = true;
            }
            FreeGridCell();
            if (AudioManager.Instance != null) {
                AudioManager.Instance.Play(SFXType.PlantFade);
            }
            PlayDeathAnimation();
        }
    }

    protected void FreeGridCell() {
        var cells = FindObjectsByType<GridCell>(FindObjectsSortMode.None);
        foreach (var cell in cells) {
            if (cell.placedPlant == gameObject) {
                cell.isOccupied = false;
                cell.placedPlant = null;
                cell.ResetHighlight();
                break;
            }
        }
    }

    public GameObject ProjectilePrefab {
        get => projectilePrefab;
        set => projectilePrefab = value;
    }

    protected abstract void Attack(GameObject target);
}