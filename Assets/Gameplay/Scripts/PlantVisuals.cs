using UnityEngine;

public static class PlantVisuals {
    private static Sprite fireBloomSprite;
    private static Sprite frostFlowerSprite;
    private static Sprite thornVineSprite;
    private static Sprite bombCactusSprite;

    private static Sprite fireBloomProjSprite;
    private static Sprite frostFlowerProjSprite;
    private static Sprite thornVineProjSprite;
    private static Sprite bombCactusProjSprite;

    public static Sprite GetPlantSprite(string name) {
        if (name == null) name = "";
        
        if (name.Contains("Fire") || name.Contains("Bloom")) {
            if (fireBloomSprite != null) return fireBloomSprite;
            fireBloomSprite = CreateFireBloomSprite();
            return fireBloomSprite;
        }
        else if (name.Contains("Frost") || name.Contains("Ice") || name.Contains("Flower")) {
            if (name.Contains("Frost") || name.Contains("Ice")) {
                if (frostFlowerSprite != null) return frostFlowerSprite;
                frostFlowerSprite = CreateFrostFlowerSprite();
                return frostFlowerSprite;
            }
        }
        
        if (name.Contains("Thorn") || name.Contains("Vine")) {
            if (thornVineSprite != null) return thornVineSprite;
            thornVineSprite = CreateThornVineSprite();
            return thornVineSprite;
        }
        else if (name.Contains("Bomb") || name.Contains("Cactus")) {
            if (bombCactusSprite != null) return bombCactusSprite;
            bombCactusSprite = CreateBombCactusSprite();
            return bombCactusSprite;
        }

        // Fallback to default flower texture if nothing matched
        return null;
    }

    public static Sprite GetProjectileSprite(string name) {
        if (name == null) name = "";

        if (name.Contains("Fire") || name.Contains("Bloom")) {
            if (fireBloomProjSprite != null) return fireBloomProjSprite;
            fireBloomProjSprite = CreateFireBloomProjSprite();
            return fireBloomProjSprite;
        }
        else if (name.Contains("Frost") || name.Contains("Ice") || name.Contains("Flower")) {
            if (name.Contains("Frost") || name.Contains("Ice")) {
                if (frostFlowerProjSprite != null) return frostFlowerProjSprite;
                frostFlowerProjSprite = CreateFrostFlowerProjSprite();
                return frostFlowerProjSprite;
            }
        }
        
        if (name.Contains("Thorn") || name.Contains("Vine")) {
            if (thornVineProjSprite != null) return thornVineProjSprite;
            thornVineProjSprite = CreateThornVineProjSprite();
            return thornVineProjSprite;
        }
        else if (name.Contains("Bomb") || name.Contains("Cactus")) {
            if (bombCactusProjSprite != null) return bombCactusProjSprite;
            bombCactusProjSprite = CreateBombCactusProjSprite();
            return bombCactusProjSprite;
        }

        return Projectile.CreateFireballSprite();
    }

    private static Sprite CreateFireBloomSprite() {
        int w = 128, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        // Draw a flame flower: 6 lobes of fire-like petals
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                Vector2 dir = (pos - center).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x);

                // 6 petals pattern
                float petalVal = Mathf.Max(0f, Mathf.Cos(6f * angle));
                float maxPetalRadius = 25f + 30f * petalVal;
                float midPetalRadius = 18f + 20f * petalVal;
                float innerRadius = 15f;

                Color c = Color.clear;
                if (dist <= maxPetalRadius) {
                    // Outer Petals: Red to Orange
                    float t = dist / maxPetalRadius;
                    c = Color.Lerp(Color.red, new Color(1f, 0.4f, 0f, 1f), t);
                    
                    if (dist <= midPetalRadius) {
                        // Mid Petals: Orange to Yellow
                        float tMid = dist / midPetalRadius;
                        c = Color.Lerp(new Color(1f, 0.4f, 0f, 1f), Color.yellow, tMid);
                    }
                    if (dist <= innerRadius) {
                        // Inner Core: Yellow to White
                        float tInner = dist / innerRadius;
                        c = Color.Lerp(Color.yellow, Color.white, tInner);
                    }

                    // Soft alpha edge
                    float edgeDist = maxPetalRadius - dist;
                    if (edgeDist < 3f) {
                        c.a *= (edgeDist / 3f);
                    }
                }
                cols[y * w + x] = c;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateFrostFlowerSprite() {
        int w = 128, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        // Draw a crystalline ice flower: 6 sharp crystal points
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                Vector2 dir = (pos - center).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x);

                // Crystalline pointed petals: sharp star shape
                float crystalVal = Mathf.Pow(Mathf.Abs(Mathf.Cos(3f * angle)), 8f);
                float maxCrystalRadius = 20f + 36f * crystalVal;
                float innerRadius = 14f;

                Color c = Color.clear;
                if (dist <= maxCrystalRadius) {
                    // Outer Point: Blue to Cyan
                    float t = dist / maxCrystalRadius;
                    c = Color.Lerp(new Color(0.1f, 0.3f, 0.9f, 1f), Color.cyan, t);

                    if (dist <= innerRadius) {
                        // Inner Core: White/Ice Blue
                        float tInner = dist / innerRadius;
                        c = Color.Lerp(Color.cyan, Color.white, tInner);
                    }

                    // Sharp crystal border effect
                    float edgeDist = maxCrystalRadius - dist;
                    if (edgeDist < 2f) {
                        c.a *= (edgeDist / 2f);
                    }
                }
                cols[y * w + x] = c;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateThornVineSprite() {
        int w = 128, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        // Draw green thorn plant: dark green leafy center, 8 sharp spines sticking out
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                Vector2 dir = (pos - center).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x);

                // Leafy base + 8 very narrow spikes
                float leafyBase = 22f + 5f * Mathf.Cos(4f * angle);
                
                // Spikes at 8 angles: 0, 45, 90, 135, 180, 225, 270, 315 degrees
                float spikeVal = 0f;
                for (int i = 0; i < 8; i++) {
                    float targetAngle = (i * Mathf.PI / 4f);
                    float diff = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, targetAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad);
                    if (diff < 0.15f) {
                        // Narrow spike length
                        spikeVal = Mathf.Max(spikeVal, (1f - diff / 0.15f));
                    }
                }
                float maxSpikeRadius = leafyBase + 30f * spikeVal;

                Color c = Color.clear;
                if (dist <= maxSpikeRadius) {
                    if (dist > leafyBase) {
                        // Spike: Lime Green to Dark Forest Green at tips
                        float tSpike = (dist - leafyBase) / (maxSpikeRadius - leafyBase);
                        c = Color.Lerp(new Color(0.4f, 0.9f, 0.1f, 1f), new Color(0.15f, 0.45f, 0.1f, 1f), tSpike);
                    } else {
                        // Central leafy core: Emerald/Forest Green
                        float tBase = dist / leafyBase;
                        c = Color.Lerp(new Color(0.1f, 0.5f, 0.1f, 1f), new Color(0.3f, 0.8f, 0.2f, 1f), tBase);
                    }

                    float edgeDist = maxSpikeRadius - dist;
                    if (edgeDist < 2f) {
                        c.a *= (edgeDist / 2f);
                    }
                }
                cols[y * w + x] = c;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateBombCactusSprite() {
        int w = 128, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f - 10f); // Lower body center

        // Draw a heavy cactus shape: oval green body with a big red/orange explosive fruit on top
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                
                // 1. Explosive Fruit on top (circular)
                Vector2 fruitCenter = center + new Vector2(0f, 35f);
                float fruitDist = Vector2.Distance(pos, fruitCenter);
                
                // 2. Heavy Cactus body (oval: stretch y)
                Vector2 bodyOffset = pos - center;
                float ovalDist = Mathf.Sqrt(bodyOffset.x * bodyOffset.x * 2.5f + bodyOffset.y * bodyOffset.y * 1.1f);

                Color c = Color.clear;
                if (ovalDist <= 38f) {
                    // Cactus body: Olive/Dark Green
                    float t = ovalDist / 38f;
                    c = Color.Lerp(new Color(0.12f, 0.42f, 0.12f, 1f), new Color(0.25f, 0.65f, 0.25f, 1f), 1f - t);

                    // Add simple vertical ribbed shading lines
                    float ribX = Mathf.Abs(bodyOffset.x);
                    if (Mathf.Abs(ribX - 12f) < 2f || Mathf.Abs(ribX - 24f) < 2f || Mathf.Abs(ribX) < 2f) {
                        c = Color.Lerp(c, new Color(0.08f, 0.28f, 0.08f, 1f), 0.4f);
                    }

                    // Edge feathering
                    float edgeDist = 38f - ovalDist;
                    if (edgeDist < 2f) c.a *= (edgeDist / 2f);
                }

                // Overlay fruit on top
                if (fruitDist <= 18f) {
                    float t = fruitDist / 18f;
                    // Glowing Red to Orange explosive fruit
                    Color fruitColor = Color.Lerp(new Color(1f, 0.15f, 0.05f, 1f), new Color(1f, 0.6f, 0f, 1f), t);
                    
                    // Simple white central glow spot
                    if (fruitDist < 5f) {
                        fruitColor = Color.Lerp(Color.white, fruitColor, fruitDist / 5f);
                    }

                    c = fruitColor;
                    float edgeDist = 18f - fruitDist;
                    if (edgeDist < 2f) c.a *= (edgeDist / 2f);
                }

                cols[y * w + x] = c;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateFireBloomProjSprite() {
        // Red fireball with yellow core
        return Projectile.CreateFireballSprite();
    }

    private static Sprite CreateFrostFlowerProjSprite() {
        // Pointed blue crystal projectile
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                Vector2 offset = pos - center;
                // Diamond shape
                float dDist = Mathf.Abs(offset.x) * 1.2f + Mathf.Abs(offset.y) * 1.2f;

                Color c = Color.clear;
                if (dDist <= 18f) {
                    float t = dDist / 18f;
                    c = Color.Lerp(Color.white, new Color(0.2f, 0.7f, 1f, 1f), t);
                    float edge = 18f - dDist;
                    if (edge < 2f) c.a *= (edge / 2f);
                }
                cols[y * w + x] = c;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateThornVineProjSprite() {
        // Sharp horizontal green needle/thorn
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                Vector2 offset = pos - center;
                // Horizontal spine: thin in Y, long in X
                float needleDist = Mathf.Abs(offset.y) * 4.5f + Mathf.Abs(offset.x) * 0.7f;

                Color c = Color.clear;
                if (needleDist <= 12f) {
                    c = Color.Lerp(new Color(0.5f, 1f, 0.2f, 1f), new Color(0.1f, 0.5f, 0.1f, 1f), needleDist / 12f);
                    float edge = 12f - needleDist;
                    if (edge < 2f) c.a *= (edge / 2f);
                }
                cols[y * w + x] = c;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateBombCactusProjSprite() {
        // Round dark-grey bomb with small orange glowing spark at upper-right
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                
                Vector2 fusePos = center + new Vector2(10f, 10f);
                float fuseDist = Vector2.Distance(pos, fusePos);

                Color c = Color.clear;
                // Dark grey bomb body
                if (dist <= 15f) {
                    float t = dist / 15f;
                    c = Color.Lerp(new Color(0.2f, 0.2f, 0.25f, 1f), new Color(0.08f, 0.08f, 0.1f, 1f), t);
                    
                    float edge = 15f - dist;
                    if (edge < 2f) c.a *= (edge / 2f);
                }

                // Glowing orange fuse spark on top
                if (fuseDist <= 5f) {
                    c = Color.Lerp(Color.yellow, new Color(1f, 0.4f, 0f, 1f), fuseDist / 5f);
                    float edge = 5f - fuseDist;
                    if (edge < 1f) c.a *= edge;
                }

                cols[y * w + x] = c;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}
