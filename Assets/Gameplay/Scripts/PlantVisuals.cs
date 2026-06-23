using UnityEngine;

public static class PlantVisuals {
    private static Sprite fireBloomSprite;
    private static Sprite frostFlowerSprite;
    private static Sprite thornVineSprite;
    private static Sprite bombCactusSprite;
    private static Sprite magicBlossomSprite;

    private static Sprite fireBloomProjSprite;
    private static Sprite frostFlowerProjSprite;
    private static Sprite thornVineProjSprite;
    private static Sprite bombCactusProjSprite;
    private static Sprite magicBlossomProjSprite;
    private static Sprite gunGuardianSprite;
    private static Sprite gunGuardianProjSprite;
    private static Sprite sunflowerTreeSprite;

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
        else if (name.Contains("Magic") || name.Contains("Blossom")) {
            if (magicBlossomSprite != null) return magicBlossomSprite;
            magicBlossomSprite = CreateMagicBlossomSprite();
            return magicBlossomSprite;
        }
        else if (name.Contains("Gun") || name.Contains("Guardian")) {
            if (gunGuardianSprite != null) return gunGuardianSprite;
            gunGuardianSprite = CreateGunGuardianSprite();
            return gunGuardianSprite;
        }
        else if (name.Contains("Sunflower") || name.Contains("Economy")) {
            if (sunflowerTreeSprite != null) return sunflowerTreeSprite;
            sunflowerTreeSprite = CreateSunflowerTreeSprite();
            return sunflowerTreeSprite;
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
        else if (name.Contains("Magic") || name.Contains("Blossom")) {
            if (magicBlossomProjSprite != null) return magicBlossomProjSprite;
            magicBlossomProjSprite = CreateMagicBlossomProjSprite();
            return magicBlossomProjSprite;
        }
        else if (name.Contains("Gun") || name.Contains("Guardian")) {
            if (gunGuardianProjSprite != null) return gunGuardianProjSprite;
            gunGuardianProjSprite = CreateGunGuardianProjSprite();
            return gunGuardianProjSprite;
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

    private static Sprite CreateMagicBlossomSprite() {
        int w = 128, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                Vector2 dir = (pos - center).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x);

                // 5 petals pattern
                float petalVal = Mathf.Max(0f, Mathf.Cos(5f * angle));
                float maxPetalRadius = 22f + 28f * petalVal;
                float midPetalRadius = 15f + 18f * petalVal;
                float innerRadius = 12f;

                Color c = Color.clear;
                if (dist <= maxPetalRadius) {
                    float t = dist / maxPetalRadius;
                    c = Color.Lerp(new Color(0.85f, 0.1f, 0.85f, 1f), new Color(1f, 0.4f, 0.7f, 1f), t);
                    
                    if (dist <= midPetalRadius) {
                        float tMid = dist / midPetalRadius;
                        c = Color.Lerp(new Color(1f, 0.4f, 0.7f, 1f), new Color(0.6f, 0.2f, 1f, 1f), tMid);
                    }
                    if (dist <= innerRadius) {
                        float tInner = dist / innerRadius;
                        c = Color.Lerp(new Color(0.6f, 0.2f, 1f, 1f), Color.white, tInner);
                    }

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

    private static Sprite CreateMagicBlossomProjSprite() {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                Vector2 dir = (pos - center).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x);

                // 4-point star shape
                float starVal = Mathf.Pow(Mathf.Abs(Mathf.Cos(2f * angle)), 4f);
                float maxStarRadius = 8f + 16f * starVal;
                float innerRadius = 6f;

                Color c = Color.clear;
                if (dist <= maxStarRadius) {
                    float t = dist / maxStarRadius;
                    c = Color.Lerp(new Color(0.9f, 0.1f, 0.6f, 1f), new Color(0.2f, 0.8f, 1f, 1f), t);

                    if (dist <= innerRadius) {
                        float tInner = dist / innerRadius;
                        c = Color.Lerp(new Color(0.2f, 0.8f, 1f, 1f), Color.white, tInner);
                    }

                    float edgeDist = maxStarRadius - dist;
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

    private static Sprite CreateGunGuardianSprite() {
        int w = 128, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                Vector2 offset = pos - center;

                Color c = Color.clear;

                // 1. Horizontal Gun Barrel (extends from center to right)
                bool inBarrel = offset.x >= 0f && offset.x <= 48f && Mathf.Abs(offset.y) <= 8f;
                
                // 2. Turret Base (circular, radius 32)
                float baseDist = offset.magnitude;
                bool inBase = baseDist <= 32f;

                // 3. Central core glow (radius 12)
                bool inCore = baseDist <= 12f;

                if (inBarrel) {
                    float t = offset.x / 48f;
                    // Dark steel to glowing blue tip
                    c = Color.Lerp(new Color(0.3f, 0.3f, 0.35f, 1f), new Color(0f, 0.85f, 1f, 1f), t);
                }

                if (inBase) {
                    float t = baseDist / 32f;
                    Color baseCol = Color.Lerp(new Color(0.18f, 0.18f, 0.22f, 1f), new Color(0.35f, 0.35f, 0.42f, 1f), 1f - t);
                    if (baseDist >= 28f) {
                        baseCol = new Color(0.12f, 0.12f, 0.15f, 1f); // Dark rim
                    }
                    c = baseCol;
                }

                if (inCore) {
                    float t = baseDist / 12f;
                    Color coreCol = Color.Lerp(Color.cyan, Color.white, 1f - t);
                    c = coreCol;
                }

                // Smooth edges
                if (c != Color.clear) {
                    float edgeDist = 0f;
                    if (inCore) {
                        // Core overlays base
                    } else if (inBase) {
                        edgeDist = 32f - baseDist;
                    } else if (inBarrel) {
                        float distY = 8f - Mathf.Abs(offset.y);
                        float distX = 48f - offset.x;
                        edgeDist = Mathf.Min(distY, distX);
                    }

                    if (edgeDist > 0f && edgeDist < 2f) {
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

    private static Sprite CreateGunGuardianProjSprite() {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                Vector2 offset = pos - center;
                // Capsule shape: horizontally elongated
                float d = Mathf.Sqrt(offset.x * offset.x * 0.8f + offset.y * offset.y * 3.5f);

                Color c = Color.clear;
                if (d <= 14f) {
                    float t = d / 14f;
                    // White core to cyan edge
                    c = Color.Lerp(Color.white, new Color(0f, 0.85f, 1f, 1f), t);
                    float edge = 14f - d;
                    if (edge < 2f) c.a *= (edge / 2f);
                }
                cols[y * w + x] = c;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateSunflowerTreeSprite() {
        int w = 128, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[w * h];
        Vector2 center = new Vector2(w / 2f, h / 2f);

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                Vector2 dir = (pos - center).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x);

                // 12 petals (golden sunflower)
                float petalVal = Mathf.Max(0f, Mathf.Cos(12f * angle));
                float maxPetalRadius = 28f + 22f * petalVal;
                float midPetalRadius = 20f + 16f * petalVal;
                float innerRadius = 14f;

                Color c = Color.clear;
                if (dist <= maxPetalRadius) {
                    float t = dist / maxPetalRadius;
                    // Outer petals: Golden yellow
                    c = Color.Lerp(new Color(1f, 0.85f, 0.1f, 1f), new Color(1f, 0.7f, 0.05f, 1f), t);

                    if (dist <= midPetalRadius) {
                        float tMid = dist / midPetalRadius;
                        c = Color.Lerp(new Color(1f, 0.7f, 0.05f, 1f), new Color(1f, 0.9f, 0.15f, 1f), tMid);
                    }
                    if (dist <= innerRadius) {
                        float tInner = dist / innerRadius;
                        // Dark brown center for sunflower seeds
                        c = Color.Lerp(new Color(0.3f, 0.15f, 0.05f, 1f), new Color(0.6f, 0.3f, 0.05f, 1f), tInner);
                    }

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
}
