using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningLotusPlant : PlantBase {
    [Header("Lightning Config")]
    [SerializeField] private float chainRange = 4.0f;
    [SerializeField] private int[] chainDamages = new int[] { 30, 15, 15 };
    [SerializeField] private float lightningDuration = 0.14f;
    [SerializeField] private int lightningSegments = 7;
    [SerializeField] private float lightningJaggedness = 0.22f;

    [Header("Visual Colors")]
    [SerializeField] private Color mainLightningColor = new Color(0.0f, 0.85f, 1.0f, 1.0f); // Neon Cyan
    [SerializeField] private Color innerLightningColor = Color.white;

    private Coroutine attackCoroutine;
    private float idleParticleTimer = 0f;

    protected override void Start() {
        base.Start();

        // Give the plant its signature lightning-neon color if not already set
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.color == Color.white) {
            sr.color = new Color(0.0f, 0.85f, 1.0f, 1.0f);
        }
    }

    public override void Configure(int damageVal, float intervalVal, float speedVal, Color color, string nameVal = "", float lifetimeVal = -1f, int maxHealthVal = 10) {
        base.Configure(damageVal, intervalVal, speedVal, color, nameVal, lifetimeVal, maxHealthVal);
        
        // Dynamically compute chain damages from the inspector-configured base damage.
        // Primary gets 100%, adjacent lanes get 50%
        chainDamages = new int[] {
            damageVal,
            Mathf.Max(1, Mathf.RoundToInt(damageVal * 0.5f)),
            Mathf.Max(1, Mathf.RoundToInt(damageVal * 0.5f))
        };
    }

    protected override void Update() {
        base.Update();

        // 1. Idle electric spark effects (small spark particles)
        idleParticleTimer += Time.deltaTime;
        if (idleParticleTimer >= 0.2f) {
            SpawnIdleSpark();
            idleParticleTimer = 0f;
        }

        // 2. Idle electrical energy pulse animation
        if (attackCoroutine == null) {
            // Pulse scale up/down slightly at 8Hz (faster pulse than standard breathing)
            float pulse = 1.0f + Mathf.PingPong(Time.time * 8f, 0.05f);
            transform.localScale = new Vector3(originalScale.x * pulse, originalScale.y * pulse, originalScale.z);
        }
    }

    private void SpawnIdleSpark() {
        var partGo = new GameObject("LotusIdleSpark");
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.1f, 0.6f), -0.1f);
        var p = partGo.AddComponent<FlameParticle>();
        // Electric neon blue/white velocity sparks
        Vector3 velocity = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.3f, 0.8f), 0f);
        Color color = Color.Lerp(mainLightningColor, innerLightningColor, Random.value);
        p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.2f, 0.35f), Random.Range(0.08f, 0.15f));
    }

    protected override void Attack(GameObject target) {
        if (attackCoroutine != null) {
            StopCoroutine(attackCoroutine);
        }
        attackCoroutine = StartCoroutine(AnimateAttack(target));
    }

    private IEnumerator AnimateAttack(GameObject primaryTarget) {
        if (AudioManager.Instance != null) {
            AudioManager.Instance.Play(SFXType.LightningLotusCharge);
        }

        // --- 1. Charge Phase (Anticipation) ---
        float chargeTime = 0.25f;
        float elapsed = 0f;
        Vector3 startScale = originalScale;
        Vector3 chargeScale = originalScale * 1.15f; // Scale up slightly as it charges
        
        // Spawn particles converging into the head during charge
        while (elapsed < chargeTime) {
            elapsed += Time.deltaTime;
            float t = elapsed / chargeTime;
            
            // Wobble/Shake scale to show unstable energy building up
            float wobble = Mathf.Sin(Time.time * 40f) * 0.02f;
            transform.localScale = Vector3.Lerp(startScale, chargeScale, t) + new Vector3(wobble, wobble, 0f);
            
            // Spawn electric charging sparks
            if (Random.value < 0.4f) {
                SpawnChargeEnergySpark();
            }
            yield return null;
        }

        // --- 2. Fire/Strike Phase ---
        // Quick release squash
        transform.localScale = new Vector3(originalScale.x * 1.25f, originalScale.y * 0.75f, originalScale.z);
        yield return new WaitForSeconds(0.04f);

        // Calculate targets: Primary, Upper adjacent, Lower adjacent
        List<GameObject> chainTargets = new List<GameObject>();
        if (primaryTarget != null) {
            var health = primaryTarget.GetComponent<ZombieHealth>();
            if (health != null && !health.IsDead) {
                chainTargets.Add(primaryTarget);
            }
        }

        if (chainTargets.Count > 0) {
            GameObject primary = chainTargets[0];
            float primaryY = primary.transform.position.y;

            // Find upper adjacent candidate (Y is +1.5 units)
            GameObject upperTarget = FindAdjacentLaneTarget(primary, primaryY + 1.5f);
            // Find lower adjacent candidate (Y is -1.5 units)
            GameObject lowerTarget = FindAdjacentLaneTarget(primary, primaryY - 1.5f);

            if (upperTarget != null) chainTargets.Add(upperTarget);
            if (lowerTarget != null) chainTargets.Add(lowerTarget);
        }

        // Shoot lightning bolts in a cascading chain
        Vector3 currentSource = transform.position + new Vector3(0f, 0.4f, -0.1f);
        
        for (int i = 0; i < chainTargets.Count; i++) {
            GameObject currentTarget = chainTargets[i];
            if (currentTarget == null) continue;

            // Small delay for subsequent chain jumps (visual propagation)
            if (i > 0) {
                yield return new WaitForSeconds(0.06f);
                if (AudioManager.Instance != null) {
                    AudioManager.Instance.Play(SFXType.LightningLotusChain);
                }
            } else {
                if (AudioManager.Instance != null) {
                    AudioManager.Instance.Play(SFXType.LightningLotusStrike);
                }
            }

            var health = currentTarget.GetComponent<ZombieHealth>();
            if (health != null && !health.IsDead) {
                int dmg = i < chainDamages.Length ? chainDamages[i] : 15;
                health.TakeDamage(dmg);
                SpawnElectricImpactEffect(currentTarget.transform.position);
            }

            Vector3 targetPos = currentTarget.transform.position + new Vector3(0f, 0.3f, -0.1f);
            CreateLightningVisual(currentSource, targetPos);

            // In our chain propagation, the lightning jumps from the primary target to the adjacent lanes
            if (i == 0) {
                currentSource = targetPos;
            }
        }

        // --- 3. Return to Idle ---
        elapsed = 0f;
        float returnDuration = 0.12f;
        Vector3 currentS = transform.localScale;
        while (elapsed < returnDuration) {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(currentS, originalScale, elapsed / returnDuration);
            yield return null;
        }
        transform.localScale = originalScale;
        attackCoroutine = null;
    }

    private void SpawnChargeEnergySpark() {
        // Spawn a spark in a radius around the lotus and move it towards the head
        Vector3 spawnOffset = (Vector3)Random.insideUnitCircle.normalized * Random.Range(0.6f, 1.2f);
        spawnOffset.y += 0.4f; // Focus on head
        Vector3 startPos = transform.position + spawnOffset;
        
        var partGo = new GameObject("LotusChargeSpark");
        partGo.transform.position = startPos;
        var p = partGo.AddComponent<FlameParticle>();
        
        Vector3 targetPos = transform.position + new Vector3(0f, 0.4f, 0f);
        Vector3 velocity = (targetPos - startPos).normalized * Random.Range(2.0f, 3.5f);
        Color color = Color.Lerp(mainLightningColor, innerLightningColor, Random.value);
        
        p.Setup(Projectile.CreateFireballSprite(), color, velocity, 0.22f, Random.Range(0.08f, 0.14f));
    }

    private GameObject FindAdjacentLaneTarget(GameObject primary, float targetY) {
        var zombies = FindObjectsByType<ZombieController>(FindObjectsSortMode.None);
        GameObject bestTarget = null;
        float minDistance = float.MaxValue;

        Vector3 sourcePos = primary.transform.position;

        foreach (var zombie in zombies) {
            if (zombie == null) continue;
            GameObject go = zombie.gameObject;
            if (go == primary) continue;

            var health = go.GetComponent<ZombieHealth>();
            if (health == null || health.IsDead) continue;

            // Check if zombie is in target Y lane (within 0.3f tolerance)
            if (Mathf.Abs(go.transform.position.y - targetY) < 0.3f) {
                float dist = Vector3.Distance(sourcePos, go.transform.position);
                if (dist <= chainRange && dist < minDistance) {
                    minDistance = dist;
                    bestTarget = go;
                }
            }
        }

        return bestTarget;
    }

    private void CreateLightningVisual(Vector3 start, Vector3 end) {
        // Create an empty GameObject for the lightning line renderer
        GameObject lightningGo = new GameObject("LightningArc");
        var lineRenderer = lightningGo.AddComponent<LineRenderer>();

        // Set up properties
        lineRenderer.startWidth = 0.09f;
        lineRenderer.endWidth = 0.04f;
        lineRenderer.positionCount = lightningSegments;
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingOrder = 9;

        // Set material / colors
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = innerLightningColor;
        lineRenderer.endColor = mainLightningColor;

        // Generate jagged positions
        Vector3[] points = new Vector3[lightningSegments];
        points[0] = start;
        points[lightningSegments - 1] = end;

        Vector3 direction = end - start;
        Vector3 normal = new Vector3(-direction.y, direction.x, 0f).normalized;

        for (int i = 1; i < lightningSegments - 1; i++) {
            float fraction = (float)i / (lightningSegments - 1);
            Vector3 midPoint = Vector3.Lerp(start, end, fraction);
            // Add perpendicular jitter
            float jitter = Random.Range(-lightningJaggedness, lightningJaggedness);
            points[i] = midPoint + normal * jitter;
        }

        lineRenderer.SetPositions(points);

        // Create a second glow line renderer on a child GameObject for a core-glow effect
        GameObject glowGo = new GameObject("LightningGlow");
        glowGo.transform.SetParent(lightningGo.transform);
        var glowRenderer = glowGo.AddComponent<LineRenderer>();
        glowRenderer.startWidth = 0.24f;
        glowRenderer.endWidth = 0.12f;
        glowRenderer.positionCount = lightningSegments;
        glowRenderer.useWorldSpace = true;
        glowRenderer.sortingOrder = 8;
        glowRenderer.material = lineRenderer.material;
        Color outerColor = mainLightningColor;
        outerColor.a = 0.45f;
        glowRenderer.startColor = outerColor;
        glowRenderer.endColor = outerColor;
        glowRenderer.SetPositions(points);

        // Add cleanup script for instantiated materials
        lightningGo.AddComponent<TemporaryLineRenderer>();

        // Destroy after lightningDuration
        Destroy(lightningGo, lightningDuration);
    }

    private void SpawnElectricImpactEffect(Vector3 position) {
        // Flash glow at impact point
        GameObject flashGo = new GameObject("ElectricHitFlash");
        flashGo.transform.position = position + new Vector3(0f, 0.3f, -0.05f);
        var flash = flashGo.AddComponent<MuzzleFlashEffect>();
        flash.Setup(GetMuzzleGlowSprite(), mainLightningColor, new Vector3(1.1f, 1.1f, 1f), 0.12f);

        // Spark particles
        for (int i = 0; i < 9; i++) {
            var partGo = new GameObject("ElectricSpark");
            partGo.transform.position = position + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.2f, 0.4f), -0.05f);
            var p = partGo.AddComponent<FlameParticle>();
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(1.8f, 4.2f);
            Vector3 velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
            Color color = Color.Lerp(mainLightningColor, innerLightningColor, Random.value);
            p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.18f, 0.32f), Random.Range(0.08f, 0.16f));
        }
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

public class TemporaryLineRenderer : MonoBehaviour {
    private void OnDestroy() {
        var lr = GetComponent<LineRenderer>();
        if (lr != null && lr.material != null) {
            Destroy(lr.material);
        }
        foreach (Transform child in transform) {
            var childLr = child.GetComponent<LineRenderer>();
            if (childLr != null && childLr.material != null) {
                Destroy(childLr.material);
            }
        }
    }
}
