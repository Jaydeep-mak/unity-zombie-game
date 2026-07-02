using UnityEngine;
using System.Collections.Generic;

public enum SFXType {
    FireBloomShoot,
    FireBloomHit,
    FrostFlowerLaunch,
    FrostFlowerHit,
    FrostFlowerFreeze,
    ThornVineShoot,
    ThornVineHit,
    BombCactusThrow,
    BombCactusExplode,
    SunflowerTreeGenerate,
    MagicBlossomRapidStart,
    MagicBlossomShoot,
    MagicBlossomHit,
    GuardianOakActivate,
    GuardianOakShield,
    LightningLotusCharge,
    LightningLotusStrike,
    LightningLotusChain,
    BasicZombieHurt,
    BasicZombieDie,
    RunnerZombieHurt,
    RunnerZombieDie,
    TankZombieHurt,
    TankZombieDie,
    BerserkerZombieHurt,
    BerserkerZombieDie,
    PlantPlaceSuccess,
    PlantPlaceInvalid,
    PlantFade,
    CoinEarned,
    CoinCollected,
    GameOver,
    UIClickStart,
    UIClickPlants,
    UIClickBack,
    UIPopupOpen,
    UIPopupClose,
    UIPlantUnlock,
    UIPurchaseSuccess,
    UIPurchaseFailed
}

public class AudioManager : MonoBehaviour {
    private static AudioManager instance;
    public static AudioManager Instance {
        get {
            if (instance == null) {
                instance = FindFirstObjectByType<AudioManager>();
                if (instance == null) {
                    var go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                }
            }
            return instance;
        }
    }

    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float masterSFXVolume = 0.8f;
    public float MasterSFXVolume {
        get => masterSFXVolume;
        set => masterSFXVolume = Mathf.Clamp01(value);
    }

    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;
    public float MusicVolume {
        get => musicVolume;
        set {
            musicVolume = Mathf.Clamp01(value);
            if (musicAudioSource != null) {
                musicAudioSource.volume = musicVolume;
            }
        }
    }

    private bool isMuted = false;
    public bool IsMuted {
        get => isMuted;
        set {
            isMuted = value;
            PlayerPrefs.SetInt("SFX_Muted", isMuted ? 1 : 0);
            PlayerPrefs.Save();
            AudioListener.volume = isMuted ? 0f : 1f; // Sync AudioListener as well
        }
    }

    [Header("Plant Sounds")]
    [SerializeField] private AudioClip fireBloomShoot;
    [SerializeField] private AudioClip fireBloomHit;
    [SerializeField] private AudioClip frostFlowerLaunch;
    [SerializeField] private AudioClip frostFlowerHit;
    [SerializeField] private AudioClip frostFlowerFreeze;
    [SerializeField] private AudioClip thornVineShoot;
    [SerializeField] private AudioClip thornVineHit;
    [SerializeField] private AudioClip bombCactusThrow;
    [SerializeField] private AudioClip bombCactusExplode;
    [SerializeField] private AudioClip sunflowerTreeGenerate;
    [SerializeField] private AudioClip magicBlossomRapidStart;
    [SerializeField] private AudioClip magicBlossomShoot;
    [SerializeField] private AudioClip magicBlossomHit;
    [SerializeField] private AudioClip guardianOakActivate;
    [SerializeField] private AudioClip guardianOakShield;
    [SerializeField] private AudioClip lightningLotusCharge;
    [SerializeField] private AudioClip lightningLotusStrike;
    [SerializeField] private AudioClip lightningLotusChain;

    [Header("Zombie Sounds")]
    [SerializeField] private AudioClip basicZombieHurt;
    [SerializeField] private AudioClip basicZombieDie;
    [SerializeField] private AudioClip runnerZombieHurt;
    [SerializeField] private AudioClip runnerZombieDie;
    [SerializeField] private AudioClip tankZombieHurt;
    [SerializeField] private AudioClip tankZombieDie;
    [SerializeField] private AudioClip berserkerZombieHurt;
    [SerializeField] private AudioClip berserkerZombieDie;

    [Header("Gameplay Events")]
    [SerializeField] private AudioClip plantPlaceSuccess;
    [SerializeField] private AudioClip plantPlaceInvalid;
    [SerializeField] private AudioClip plantFade;
    [SerializeField] private AudioClip coinEarned;
    [SerializeField] private AudioClip coinCollected;
    [SerializeField] private AudioClip gameOver;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip uiClickStart;
    [SerializeField] private AudioClip uiClickPlants;
    [SerializeField] private AudioClip uiClickBack;
    [SerializeField] private AudioClip uiPopupOpen;
    [SerializeField] private AudioClip uiPopupClose;
    [SerializeField] private AudioClip uiPlantUnlock;
    [SerializeField] private AudioClip uiPurchaseSuccess;
    [SerializeField] private AudioClip uiPurchaseFailed;

    [Header("Background Music")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private const int MAX_SOURCES = 16;

    private Dictionary<SFXType, float> lastPlayTimes = new Dictionary<SFXType, float>();
    private const float THROTTLE_INTERVAL = 0.08f; // 80ms throttle to prevent overlapping sounds clipping

    private Dictionary<SFXType, AudioClip> placeholderClips = new Dictionary<SFXType, AudioClip>();
    private AudioClip uiClickSoundClip;

    private void Awake() {
        if (instance == null) {
            instance = this;
            if (transform.parent == null) {
                DontDestroyOnLoad(gameObject);
            }
            isMuted = PlayerPrefs.GetInt("SFX_Muted", 0) == 1;
            AudioListener.volume = isMuted ? 0f : 1f; // Initialize AudioListener volume

            if (musicAudioSource == null) {
                musicAudioSource = gameObject.AddComponent<AudioSource>();
                musicAudioSource.loop = true;
                musicAudioSource.playOnAwake = false;
            }

            // Load common UI click sound from Resources
            uiClickSoundClip = Resources.Load<AudioClip>("ui_click_sound");
        } else if (instance != this) {
            Destroy(gameObject);
        }
    }

    public void Play(SFXType type, float volumeScale = 1.0f) {
        AudioClip clip = GetAudioClip(type);
        if (clip == null) {
            clip = GetPlaceholderClip(type);
        }
        if (type == SFXType.GameOver) {
            StopMusic();
        }
        if (clip != null) {
            PlaySFXInternal(clip, volumeScale, type);
        }
    }

    private AudioClip GetAudioClip(SFXType type) {
        switch (type) {
            case SFXType.FireBloomShoot: return fireBloomShoot;
            case SFXType.FireBloomHit: return fireBloomHit;
            case SFXType.FrostFlowerLaunch: return frostFlowerLaunch;
            case SFXType.FrostFlowerHit: return frostFlowerHit;
            case SFXType.FrostFlowerFreeze: return frostFlowerFreeze;
            case SFXType.ThornVineShoot: return thornVineShoot;
            case SFXType.ThornVineHit: return thornVineHit;
            case SFXType.BombCactusThrow: return bombCactusThrow;
            case SFXType.BombCactusExplode: return bombCactusExplode;
            case SFXType.SunflowerTreeGenerate: return sunflowerTreeGenerate;
            case SFXType.MagicBlossomRapidStart: return magicBlossomRapidStart;
            case SFXType.MagicBlossomShoot: return magicBlossomShoot;
            case SFXType.MagicBlossomHit: return magicBlossomHit;
            case SFXType.GuardianOakActivate: return guardianOakActivate;
            case SFXType.GuardianOakShield: return guardianOakShield;
            case SFXType.LightningLotusCharge: return lightningLotusCharge;
            case SFXType.LightningLotusStrike: return lightningLotusStrike;
            case SFXType.LightningLotusChain: return lightningLotusChain;
            case SFXType.BasicZombieHurt: return basicZombieHurt;
            case SFXType.BasicZombieDie: return basicZombieDie;
            case SFXType.RunnerZombieHurt: return runnerZombieHurt;
            case SFXType.RunnerZombieDie: return runnerZombieDie;
            case SFXType.TankZombieHurt: return tankZombieHurt;
            case SFXType.TankZombieDie: return tankZombieDie;
            case SFXType.BerserkerZombieHurt: return berserkerZombieHurt;
            case SFXType.BerserkerZombieDie: return berserkerZombieDie;
            case SFXType.PlantPlaceSuccess: return plantPlaceSuccess;
            case SFXType.PlantPlaceInvalid: return plantPlaceInvalid;
            case SFXType.PlantFade: return plantFade;
            case SFXType.CoinEarned: return coinEarned;
            case SFXType.CoinCollected: return coinCollected;
            case SFXType.GameOver: return gameOver;
            case SFXType.UIClickStart: return uiClickSoundClip != null ? uiClickSoundClip : uiClickStart;
            case SFXType.UIClickPlants: return uiClickSoundClip != null ? uiClickSoundClip : uiClickPlants;
            case SFXType.UIClickBack: return uiClickSoundClip != null ? uiClickSoundClip : uiClickBack;
            case SFXType.UIPopupOpen: return uiClickSoundClip != null ? uiClickSoundClip : uiPopupOpen;
            case SFXType.UIPopupClose: return uiClickSoundClip != null ? uiClickSoundClip : uiPopupClose;
            case SFXType.UIPlantUnlock: return uiClickSoundClip != null ? uiClickSoundClip : uiPlantUnlock;
            case SFXType.UIPurchaseSuccess: return uiClickSoundClip != null ? uiClickSoundClip : uiPurchaseSuccess;
            case SFXType.UIPurchaseFailed: return uiClickSoundClip != null ? uiClickSoundClip : uiPurchaseFailed;
            default: return null;
        }
    }

    private float GetThrottleInterval(SFXType type) {
        switch (type) {
            case SFXType.MagicBlossomShoot:
                return 0.08f;
            case SFXType.MagicBlossomHit:
            case SFXType.FireBloomHit:
            case SFXType.FrostFlowerHit:
            case SFXType.ThornVineHit:
                return 0.18f;
            case SFXType.BasicZombieHurt:
            case SFXType.RunnerZombieHurt:
            case SFXType.TankZombieHurt:
            case SFXType.BerserkerZombieHurt:
                return 0.35f;
            case SFXType.LightningLotusChain:
                return 0.12f;
            default:
                return 0f;
        }
    }

    private void PlaySFXInternal(AudioClip clip, float volumeScale, SFXType type) {
        if (isMuted) return;

        float throttleInterval = GetThrottleInterval(type);
        if (throttleInterval > 0f) {
            if (lastPlayTimes.TryGetValue(type, out float lastTime)) {
                if (Time.time - lastTime < throttleInterval) {
                    return; // Ignored to prevent overlapping noise
                }
            }
            lastPlayTimes[type] = Time.time;
        }

        AudioSource source = GetIdleAudioSource();
        if (source != null) {
            source.PlayOneShot(clip, volumeScale * masterSFXVolume);
        }
    }

    private AudioSource GetIdleAudioSource() {
        for (int i = 0; i < audioSourcePool.Count; i++) {
            if (audioSourcePool[i] != null && !audioSourcePool[i].isPlaying) {
                return audioSourcePool[i];
            }
        }

        if (audioSourcePool.Count < MAX_SOURCES) {
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            audioSourcePool.Add(newSource);
            return newSource;
        }

        return audioSourcePool[Random.Range(0, audioSourcePool.Count)];
    }

    private AudioClip GetPlaceholderClip(SFXType type) {
        if (placeholderClips.TryGetValue(type, out var clip)) {
            return clip;
        }
        AudioClip newClip = GenerateProceduralClip(type);
        if (newClip != null) {
            placeholderClips[type] = newClip;
        }
        return newClip;
    }

    private AudioClip GenerateProceduralClip(SFXType type) {
        int sampleRate = 22050;
        float duration = 0.15f;
        float[] samples;

        switch (type) {
            case SFXType.FireBloomShoot:
                duration = 0.12f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(400f, 150f, t / duration);
                    float sine = Mathf.Sin(2f * Mathf.PI * frequency * t);
                    float noise = Random.Range(-1.0f, 1.0f);
                    samples[i] = (sine * 0.6f + noise * 0.4f) * (1.0f - t / duration);
                }
                break;
            case SFXType.FireBloomHit:
                duration = 0.05f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float freq = Mathf.Lerp(300f, 100f, t / duration);
                    float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float noise = Random.Range(-1.0f, 1.0f) * 0.12f;
                    samples[i] = (sine * 0.88f + noise) * 0.2f * (1.0f - t / duration);
                }
                break;
            case SFXType.FrostFlowerLaunch:
                duration = 0.18f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(300f, 800f, t / duration);
                    float angle = 2f * Mathf.PI * frequency * t;
                    float triangle = Mathf.PingPong(angle / Mathf.PI, 2f) - 1f;
                    samples[i] = triangle * 0.4f * (1.0f - t / duration);
                }
                break;
            case SFXType.FrostFlowerHit:
            case SFXType.FrostFlowerFreeze:
                duration = 0.1f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float freq = Mathf.Lerp(1000f, 600f, t / duration);
                    float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float crackle = Random.Range(-1.0f, 1.0f) * 0.1f;
                    samples[i] = (sine * 0.9f + crackle) * 0.15f * (1.0f - t / duration);
                }
                break;
            case SFXType.ThornVineShoot:
                duration = 0.06f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(800f, 400f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f * (1.0f - t / duration);
                }
                break;
            case SFXType.ThornVineHit:
                duration = 0.03f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float freq = Mathf.Lerp(600f, 200f, t / duration);
                    float angle = 2f * Mathf.PI * freq * t;
                    float triangle = Mathf.PingPong(angle / Mathf.PI, 2f) - 1f;
                    samples[i] = triangle * 0.12f * (1.0f - t / duration);
                }
                break;
            case SFXType.BombCactusThrow:
                duration = 0.15f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(200f, 400f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f * (1.0f - t / duration);
                }
                break;
            case SFXType.BombCactusExplode:
                duration = 0.5f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float noise = Random.Range(-1.0f, 1.0f);
                    float rumble = Mathf.Sin(2f * Mathf.PI * 45f * t);
                    samples[i] = (noise * 0.6f + rumble * 0.4f) * (1.0f - t / duration);
                }
                break;
            case SFXType.SunflowerTreeGenerate:
            case SFXType.CoinEarned:
            case SFXType.CoinCollected:
                duration = 0.25f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float f1 = 987.77f; // B5
                    float f2 = 1318.51f; // E6
                    float wave = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.5f;
                    samples[i] = wave * 0.4f * (1.0f - t / duration);
                }
                break;
            case SFXType.MagicBlossomRapidStart:
                duration = 0.3f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(400f, 900f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.3f * (1.0f - t / duration);
                }
                break;
            case SFXType.MagicBlossomShoot:
                duration = 0.08f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(600f, 1000f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.4f * (1.0f - t / duration);
                }
                break;
            case SFXType.MagicBlossomHit:
                duration = 0.04f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float freq = Mathf.Lerp(1200f, 800f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.1f * (1.0f - t / duration);
                }
                break;
            case SFXType.GuardianOakActivate:
            case SFXType.GuardianOakShield:
                duration = 0.4f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(150f, 250f, t / duration);
                    float sine = Mathf.Sin(2f * Mathf.PI * frequency * t);
                    float noise = Random.Range(-1.0f, 1.0f);
                    samples[i] = (sine * 0.7f + noise * 0.3f) * (1.0f - t / duration);
                }
                break;
            case SFXType.LightningLotusCharge:
                duration = 0.3f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(100f, 600f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.3f;
                }
                break;
            case SFXType.LightningLotusStrike:
            case SFXType.LightningLotusChain:
                duration = 0.2f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float noise = Random.Range(-1.0f, 1.0f);
                    float electric = Mathf.Sin(2f * Mathf.PI * 60f * t);
                    samples[i] = (noise * 0.8f + electric * 0.2f) * (1.0f - t / duration);
                }
                break;
            case SFXType.BasicZombieHurt:
                duration = 0.12f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float baseFreq = 90f;
                    float gargle = 1f + 0.4f * Mathf.Sin(2f * Mathf.PI * 35f * t);
                    float wave = Mathf.Sin(2f * Mathf.PI * baseFreq * t * gargle);
                    float breath = Random.Range(-1.0f, 1.0f) * 0.15f;
                    samples[i] = (wave * 0.85f + breath) * 0.12f * (1.0f - t / duration);
                }
                break;
            case SFXType.RunnerZombieHurt:
                duration = 0.08f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float baseFreq = 120f;
                    float gargle = 1f + 0.5f * Mathf.Sin(2f * Mathf.PI * 45f * t);
                    float wave = Mathf.Sin(2f * Mathf.PI * baseFreq * t * gargle);
                    float breath = Random.Range(-1.0f, 1.0f) * 0.15f;
                    samples[i] = (wave * 0.85f + breath) * 0.1f * (1.0f - t / duration);
                }
                break;
            case SFXType.TankZombieHurt:
                duration = 0.18f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float baseFreq = 65f;
                    float gargle = 1f + 0.3f * Mathf.Sin(2f * Mathf.PI * 25f * t);
                    float wave = Mathf.Sin(2f * Mathf.PI * baseFreq * t * gargle);
                    float breath = Random.Range(-1.0f, 1.0f) * 0.25f;
                    samples[i] = (wave * 0.75f + breath) * 0.18f * (1.0f - t / duration);
                }
                break;
            case SFXType.BerserkerZombieHurt:
                duration = 0.22f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float baseFreq = 50f;
                    float gargle = 1f + 0.3f * Mathf.Sin(2f * Mathf.PI * 20f * t);
                    float wave = Mathf.Sin(2f * Mathf.PI * baseFreq * t * gargle);
                    float breath = Random.Range(-1.0f, 1.0f) * 0.3f;
                    samples[i] = (wave * 0.7f + breath) * 0.22f * (1.0f - t / duration);
                }
                break;
            case SFXType.BasicZombieDie:
            case SFXType.RunnerZombieDie:
            case SFXType.TankZombieDie:
            case SFXType.BerserkerZombieDie:
                duration = 0.35f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(100f, 40f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.6f * (1.0f - t / duration);
                }
                break;
            case SFXType.PlantPlaceSuccess:
                duration = 0.15f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(250f, 500f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.4f * (1.0f - t / duration);
                }
                break;
            case SFXType.PlantPlaceInvalid:
                duration = 0.2f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = 120f;
                    float buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t));
                    samples[i] = buzz * 0.3f * (1.0f - t / duration);
                }
                break;
            case SFXType.PlantFade:
                duration = 0.25f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(300f, 150f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.3f * (1.0f - t / duration);
                }
                break;
            case SFXType.UIClickStart:
            case SFXType.UIClickPlants:
            case SFXType.UIClickBack:
                duration = 0.08f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = 600f;
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.4f * (1.0f - t / duration);
                }
                break;
            case SFXType.UIPopupOpen:
                duration = 0.2f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(300f, 500f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.4f * (1.0f - t / duration);
                }
                break;
            case SFXType.UIPopupClose:
                duration = 0.15f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(500f, 300f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.4f * (1.0f - t / duration);
                }
                break;
            case SFXType.UIPlantUnlock:
            case SFXType.UIPurchaseSuccess:
                duration = 0.4f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float f1 = 523.25f; // C5
                    float f2 = 659.25f; // E5
                    float f3 = 783.99f; // G5
                    float wave = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.33f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.33f + Mathf.Sin(2f * Mathf.PI * f3 * t) * 0.33f;
                    samples[i] = wave * 0.5f * (1.0f - t / duration);
                }
                break;
            case SFXType.UIPurchaseFailed:
                duration = 0.3f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(150f, 100f, t / duration);
                    float buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t));
                    samples[i] = buzz * 0.4f * (1.0f - t / duration);
                }
                break;
            case SFXType.GameOver:
                duration = 0.8f;
                samples = new float[Mathf.RoundToInt(sampleRate * duration)];
                for (int i = 0; i < samples.Length; i++) {
                    float t = (float)i / sampleRate;
                    float frequency = Mathf.Lerp(300f, 100f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f * (1.0f - t / duration);
                }
                break;
            default:
                return null;
        }

        AudioClip clip = AudioClip.Create("Procedural_" + type.ToString(), samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnEnable() {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start() {
        HandleSceneMusic(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) {
        HandleSceneMusic(scene.name);
    }

    private void HandleSceneMusic(string sceneName) {
        if (sceneName == "GardenGuardians_MainMenu" || sceneName == "PlantCollectionScene") {
            AudioClip clip = mainMenuMusic != null ? mainMenuMusic : GetPlaceholderMusic("menu");
            PlayMusic(clip);
        } else if (sceneName == "demo") {
            AudioClip clip = gameplayMusic != null ? gameplayMusic : GetPlaceholderMusic("battle");
            PlayMusic(clip);
        } else {
            StopMusic();
        }
    }

    private AudioClip GetPlaceholderMusic(string type) {
        if (type == "menu" && proceduralMenuMusic != null) return proceduralMenuMusic;
        if (type == "battle" && proceduralBattleMusic != null) return proceduralBattleMusic;

        AudioClip clip = GenerateProceduralMusic(type);
        if (type == "menu") proceduralMenuMusic = clip;
        else proceduralBattleMusic = clip;

        return clip;
    }

    private AudioClip proceduralMenuMusic;
    private AudioClip proceduralBattleMusic;

    private AudioClip GenerateProceduralMusic(string type) {
        int sampleRate = 22050;
        float noteDuration = 0.25f;
        int numNotes = 32;
        float duration = noteDuration * numNotes;
        float[] samples = new float[Mathf.RoundToInt(sampleRate * duration)];

        float[] menuScale = { 220.00f, 246.94f, 261.63f, 293.66f, 329.63f, 392.00f, 440.00f };
        float[] battleScale = { 110.00f, 130.81f, 146.83f, 164.81f, 196.00f, 220.00f };

        int[] menuSequence = { 0, 2, 4, 3, 2, 4, 6, 5, 4, 2, 0, 1, 2, 3, 2, 0 };
        int[] battleSequence = { 0, 0, 3, 0, 5, 0, 4, 3, 0, 0, 3, 0, 5, 0, 4, 5 };

        float[] scale = type == "menu" ? menuScale : battleScale;
        int[] sequence = type == "menu" ? menuSequence : battleSequence;

        for (int i = 0; i < samples.Length; i++) {
            float t = (float)i / sampleRate;
            int noteIndex = Mathf.FloorToInt(t / noteDuration) % sequence.Length;
            float freq = scale[sequence[noteIndex] % scale.Length];
            
            float noteTime = t % noteDuration;
            float envelope = Mathf.Exp(-4f * (noteTime / noteDuration));

            float wave = 0f;
            if (type == "menu") {
                wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.3f;
            } else {
                float phase = (t * freq) % 1f;
                wave = 2f * phase - 1f;
                wave = wave * 0.6f + Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.4f;
            }

            samples[i] = wave * envelope * 0.15f;
        }

        AudioClip clip = AudioClip.Create("ProceduralBGM_" + type, samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private bool isMusicPausedInternal = false;

    public void PauseMusic() {
        if (musicAudioSource != null && musicAudioSource.isPlaying) {
            musicAudioSource.Pause();
            isMusicPausedInternal = true;
        }
    }

    public void ResumeMusic() {
        if (musicAudioSource != null && !musicAudioSource.isPlaying && isMusicPausedInternal) {
            if (!isMuted) {
                musicAudioSource.UnPause();
            }
            isMusicPausedInternal = false;
        }
    }

    private void OnApplicationFocus(bool hasFocus) {
        if (!hasFocus) {
            PauseMusic();
        } else {
            ResumeMusic();
        }
    }

    private void OnApplicationPause(bool isPaused) {
        if (isPaused) {
            PauseMusic();
        } else {
            ResumeMusic();
        }
    }

    public void PlayMusic(AudioClip clip) {
        if (clip == null) return;
        if (musicAudioSource == null) {
            musicAudioSource = gameObject.AddComponent<AudioSource>();
            musicAudioSource.loop = true;
            musicAudioSource.playOnAwake = false;
        }

        if (musicAudioSource.clip == clip && musicAudioSource.isPlaying) {
            return;
        }

        musicAudioSource.clip = clip;
        musicAudioSource.volume = musicVolume;
        musicAudioSource.Play();
        isMusicPausedInternal = false;
    }

    public void StopMusic() {
        if (musicAudioSource != null) {
            musicAudioSource.Stop();
        }
        isMusicPausedInternal = false;
    }
}
