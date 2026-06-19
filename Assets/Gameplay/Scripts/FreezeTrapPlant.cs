using UnityEngine;
using System.Collections;

public class FreezeTrapPlant : TrapPlantBase {
    [Header("Freeze Trap Settings")]
    [SerializeField] private float freezeDuration = 3.0f;
    [SerializeField] private float idleParticleInterval = 0.2f;

    private float idleParticleTimer = 0f;

    protected override void Start() {
        base.Start();

        // Idle Visuals: Tint the trap plant to frost blue
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            sr.color = new Color(0.3f, 0.75f, 1.0f, 1.0f);
        }
    }

    protected override void Update() {
        base.Update();

        // Idle Visuals: Spawn persistent idle ice particles rising from the trap
        idleParticleTimer += Time.deltaTime;
        if (idleParticleTimer >= idleParticleInterval) {
            SpawnIdleIceParticle();
            idleParticleTimer = 0f;
        }

        // Crystalline side-to-side shivering (distinct to Frost Flower)
        if (!isTriggered) {
            float shiver = 1.0f + Mathf.Sin(Time.time * 15f) * 0.02f;
            transform.localScale = new Vector3(originalScale.x * shiver, originalScale.y, originalScale.z);
        }
    }

    private void SpawnIdleIceParticle() {
        var partGo = new GameObject("TrapIdleIce");
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f), -0.1f);
        var p = partGo.AddComponent<FlameParticle>();
        Vector3 velocity = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.2f, 0.5f), 0f);
        Color color = Color.Lerp(new Color(0.5f, 0.85f, 1.0f, 0.7f), new Color(0.9f, 0.95f, 1.0f, 0.6f), Random.value);
        p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.5f, 1.0f), Random.Range(0.1f, 0.22f));
    }

    protected override void OnTrapTriggered(ZombieController zombie) {
        StartCoroutine(TriggerSequence(zombie));
    }

    private IEnumerator TriggerSequence(ZombieController zombie) {
        Vector3 triggerPos = transform.position;

        // 1. Ice Burst and Blue Flash Effects
        SpawnIceBurstEffect(triggerPos);

        // 2. Apply Freeze slow to the zombie
        if (zombie != null) {
            var slowEffect = zombie.GetComponent<ZombieSlowEffect>();
            if (slowEffect == null) {
                slowEffect = zombie.gameObject.AddComponent<ZombieSlowEffect>();
            }
            // Freeze completely (0 speed multiplier) for freezeDuration
            slowEffect.ApplySlow(0f, freezeDuration);
        }

        // 3. Quick shrink/dissolve scale effect on the trap itself before destruction
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 startScale = transform.localScale;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        // 4. Consume trap and free cell
        ConsumeTrap();
    }

    private void SpawnIceBurstEffect(Vector3 position) {
        int particleCount = Random.Range(15, 22);
        for (int i = 0; i < particleCount; i++) {
            var partGo = new GameObject("IceBurstParticle");
            partGo.transform.position = position;
            var p = partGo.AddComponent<FlameParticle>();
            
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(1.5f, 4.5f);
            Vector3 velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
            
            Color color = Color.Lerp(Color.cyan, Color.white, Random.value);
            p.Setup(Projectile.CreateFireballSprite(), color, velocity, Random.Range(0.3f, 0.6f), Random.Range(0.2f, 0.4f));
        }
    }
}
