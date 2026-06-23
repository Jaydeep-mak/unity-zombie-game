using UnityEngine;

public class SunflowerTreePlant : EconomyPlantBase {
    private float leafAnimTimer = 0f;

    protected override void Start() {
        base.Start();

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.color == Color.white) {
            sr.color = new Color(1f, 0.9f, 0.2f, 1f);
        }
    }

    protected override void IdleSway() {
        float sway = Mathf.Sin(Time.time * 2f) * 0.05f;
        float bob = Mathf.Sin(Time.time * 3f + 1f) * 0.03f;
        transform.localScale = new Vector3(
            originalScale.x + sway,
            originalScale.y + bob,
            originalScale.z
        );
    }

    protected override void SpawnIdleParticles() {
        leafAnimTimer += Time.deltaTime;
        if (leafAnimTimer < 0.2f) return;
        leafAnimTimer = 0f;

        var partGo = new GameObject("SunflowerGlow");
        partGo.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.1f, 0.45f), -0.1f);
        var p = partGo.AddComponent<FlameParticle>();
        Vector3 velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.3f, 0.7f), 0f);
        Color color = Color.Lerp(
            new Color(1f, 0.9f, 0.1f, 0.6f),
            new Color(1f, 0.7f, 0.1f, 0.4f),
            Random.value
        );
        Sprite glowSprite = CreateSunflowerGlowSprite();
        p.Setup(glowSprite, color, velocity, Random.Range(0.5f, 0.8f), Random.Range(0.1f, 0.2f));
    }

    private static Sprite sunflowerGlowSprite;

    private static Sprite CreateSunflowerGlowSprite() {
        if (sunflowerGlowSprite != null) return sunflowerGlowSprite;
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
                cols[y * w + x] = new Color(1f, 0.85f, 0.1f, alpha * alpha * 0.6f);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        sunflowerGlowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100f);
        return sunflowerGlowSprite;
    }
}
