using UnityEngine;
using System.Collections;

public abstract class EconomyPlantBase : PlantBase {
    [Header("Economy Settings")]
    [SerializeField] protected float coinGenerationInterval = 8f;
    [SerializeField] protected int coinAmount = 10;

    protected float coinTimer = 0f;
    protected Coroutine coinAnimationRoutine;
    protected SpriteRenderer spriteRenderer;
    protected Color originalTint = Color.white;

    protected override void Start() {
        base.Start();
        coinTimer = coinGenerationInterval;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalTint = spriteRenderer.color;
    }

    public virtual void ConfigureEconomy(int damageVal, float intervalVal, float speedVal, Color color, string nameVal = "", float lifetimeVal = -1f, float coinInterval = 8f, int coinAmt = 10) {
        Configure(damageVal, intervalVal, speedVal, color, nameVal, lifetimeVal);
        coinGenerationInterval = coinInterval;
        coinAmount = coinAmt;
        coinTimer = coinGenerationInterval;
    }

    protected override void Update() {
        UpdateLifetime();

        coinTimer += Time.deltaTime;

        if (coinTimer >= coinGenerationInterval) {
            coinTimer = 0f;
            GenerateCoins();
        }

        IdleSway();
        SpawnIdleParticles();
    }

    protected virtual void IdleSway() {
        float sway = Mathf.Sin(Time.time * 2.5f) * 0.04f;
        float bob = Mathf.Sin(Time.time * 3f + 1f) * 0.025f;
        transform.localScale = new Vector3(
            originalScale.x + sway,
            originalScale.y + bob,
            originalScale.z
        );
    }

    protected virtual void SpawnIdleParticles() {
        if (coinAnimationRoutine != null) return;

        var partGo = new GameObject("EconomyIdleParticle");
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.15f, 0.35f), -0.1f);
        var p = partGo.AddComponent<FlameParticle>();
        Vector3 velocity = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.3f, 0.6f), 0f);
        Color color = Color.Lerp(
            new Color(1f, 0.85f, 0.2f, 0.6f),
            new Color(1f, 0.95f, 0.4f, 0.4f),
            Random.value
        );
        Sprite glowSprite = CreateCoinGlowSprite();
        p.Setup(glowSprite, color, velocity, Random.Range(0.4f, 0.7f), Random.Range(0.1f, 0.2f));
    }

    protected virtual void GenerateCoins() {
        if (coinAnimationRoutine != null) StopCoroutine(coinAnimationRoutine);
        coinAnimationRoutine = StartCoroutine(CoinGenerationAnimation());
    }

    private IEnumerator CoinGenerationAnimation() {
        Vector3 basePos = transform.position;

        // Phase 1: Golden glow pulse on the plant (0.3s)
        float elapsed = 0f;
        float glowDuration = 0.3f;
        while (elapsed < glowDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / glowDuration;
            float glow = Mathf.Sin(t * Mathf.PI);
            float scalePulse = 1f + glow * 0.12f;
            transform.localScale = originalScale * scalePulse;

            if (spriteRenderer != null) {
                spriteRenderer.color = Color.Lerp(originalTint, new Color(1f, 0.95f, 0.4f, 1f), glow);
            }
            yield return null;
        }
        if (spriteRenderer != null) spriteRenderer.color = originalTint;
        transform.localScale = originalScale;

        // Phase 2: Golden sparkle burst (3 particles)
        for (int i = 0; i < 3; i++) {
            var go = new GameObject("CoinSparkle");
            go.transform.position = basePos + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.4f), -0.2f);
            var p = go.AddComponent<FlameParticle>();
            Vector3 vel = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.6f, 1.2f), 0f);
            Color c = new Color(1f, 0.88f, 0.15f, 1f);
            p.Setup(CreateCoinBurstSprite(), c, vel, Random.Range(0.3f, 0.5f), Random.Range(0.15f, 0.25f));
            yield return new WaitForSeconds(0.04f);
        }

        // Phase 3: Coin rises upward (0.4s)
        var coinWorld = new GameObject("FloatingCoinWorld");
        coinWorld.transform.position = basePos + Vector3.up * 0.5f;
        var coinSr = coinWorld.AddComponent<SpriteRenderer>();
        coinSr.sprite = CreateCoinSprite();
        coinSr.sortingOrder = 15;
        coinSr.color = new Color(1f, 0.9f, 0.1f, 1f);

        elapsed = 0f;
        float riseDuration = 0.4f;
        Vector3 startCoinPos = coinWorld.transform.position;
        while (elapsed < riseDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / riseDuration;
            coinWorld.transform.position = startCoinPos + new Vector3(0f, t * 0.8f, 0f);
            coinSr.color = new Color(1f, 0.9f, 0.1f, 1f - t * 0.3f);
            coinWorld.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one * 1.1f, t);
            yield return null;
        }

        // Phase 4: Coin flies toward UI counter (0.5s)
        if (GameplayManager.Instance != null) {
            Vector3 screenStart = Camera.main.WorldToScreenPoint(coinWorld.transform.position);
            GameplayManager.Instance.SpawnFlyingCoin(screenStart, coinAmount);
        }

        if (coinWorld != null) Destroy(coinWorld);

        // Phase 5: Scale pulse return
        elapsed = 0f;
        float returnDuration = 0.2f;
        while (elapsed < returnDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            float over = 1f + (1f - t) * 0.08f;
            transform.localScale = originalScale * over;
            yield return null;
        }
        transform.localScale = originalScale;

        coinAnimationRoutine = null;
    }

    private static Sprite coinGlowSprite;
    private static Sprite coinBurstSprite;
    private static Sprite coinSprite;

    private static Sprite CreateCoinGlowSprite() {
        if (coinGlowSprite != null) return coinGlowSprite;
        int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var cols = new Color[w * h];
        Vector2 c = new Vector2(w / 2f, h / 2f);
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float r = w / 2f;
                float alpha = Mathf.Clamp01(1f - (d / r));
                cols[y * w + x] = new Color(1f, 0.9f, 0.2f, alpha * alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        coinGlowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100f);
        return coinGlowSprite;
    }

    private static Sprite CreateCoinBurstSprite() {
        if (coinBurstSprite != null) return coinBurstSprite;
        int w = 12, h = 12;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var cols = new Color[w * h];
        Vector2 c = new Vector2(w / 2f, h / 2f);
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 p = new Vector2(x, y);
                float d = Vector2.Distance(p, c);
                float a = Mathf.Atan2(p.y - c.y, p.x - c.x);
                float star = Mathf.Pow(Mathf.Abs(Mathf.Cos(4f * a)), 6f);
                float r = 4f + 3f * star;
                float alpha = 0f;
                if (d <= r) alpha = Mathf.Clamp01(1f - (d / r));
                cols[y * w + x] = new Color(1f, 0.85f, 0.1f, alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        coinBurstSprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100f);
        return coinBurstSprite;
    }

    private static Sprite CreateCoinSprite() {
        if (coinSprite != null) return coinSprite;
        int w = 24, h = 24;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var cols = new Color[w * h];
        Vector2 c = new Vector2(w / 2f, h / 2f);
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                float d = Vector2.Distance(new Vector2(x, y), c);
                Color col = Color.clear;
                if (d <= 10f) {
                    float t = d / 10f;
                    col = Color.Lerp(new Color(1f, 0.95f, 0.2f, 1f), new Color(1f, 0.7f, 0.05f, 1f), t);
                    float edge = 10f - d;
                    if (edge < 2f) col.a *= (edge / 2f);
                }
                cols[y * w + x] = col;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        coinSprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100f);
        return coinSprite;
    }

    protected override void Attack(GameObject target) { }
    protected override GameObject DetectZombieInLane() => null;
}
