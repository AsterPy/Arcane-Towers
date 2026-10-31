using System.Collections.Generic;
using UnityEngine;

public class ArcaneAudio : MonoBehaviour
{
    public static ArcaneAudio Instance { get; private set; }

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.70f;
    [Range(0f, 1f)] public float musicVolume = 0.30f;

    [Header("Audio Clips (Configured in Inspector / Prefab)")]
    public AudioClip cardSelect;
    public AudioClip cardNoEnergy;
    public AudioClip spawnWarrior;
    public AudioClip spawnArcher;
    public AudioClip spawnGolem;
    public AudioClip spawnDrone;
    public AudioClip attackWarrior;
    public AudioClip attackArcher;
    public AudioClip attackGolem;
    public AudioClip attackDrone;
    public AudioClip towerShotMain;
    public AudioClip towerShotSide;
    public AudioClip towerDestroy;
    public AudioClip spellFireball;
    public AudioClip spellHeal;
    public AudioClip bgmBattle;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayTimes = new Dictionary<string, float>();
    private const float MinSoundInterval = 0.04f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureAudioListener();

        // Setup SFX and Music sources
        AudioSource[] sources = GetComponents<AudioSource>();
        sfxSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        musicSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;

        InitializeClipDictionary();
    }

    private void Start()
    {
        PlayMusic("bgm_battle");
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        AutoAssignClipsIfEmpty();
#endif
    }

    public void AutoAssignClipsIfEmpty()
    {
#if UNITY_EDITOR
        if (cardSelect == null) cardSelect = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/card_select.wav");
        if (cardNoEnergy == null) cardNoEnergy = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/card_no_energy.mp3");
        if (spawnWarrior == null) spawnWarrior = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/spawn_warrior.mp3");
        if (spawnArcher == null) spawnArcher = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/spawn_archer.mp3");
        if (spawnGolem == null) spawnGolem = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/spawn_golem.mp3");
        if (spawnDrone == null) spawnDrone = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/spawn_drone.mp3");
        if (attackWarrior == null) attackWarrior = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/attack_warrior.mp3");
        if (attackArcher == null) attackArcher = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/attack_archer.mp3");
        if (attackGolem == null) attackGolem = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/attack_golem.mp3");
        if (attackDrone == null) attackDrone = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/attack_drone.mp3");
        if (towerShotMain == null) towerShotMain = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/tower_shot_main.mp3");
        if (towerShotSide == null) towerShotSide = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/tower_shot_side.mp3");
        if (towerDestroy == null) towerDestroy = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/tower_destroy.mp3");
        if (spellFireball == null) spellFireball = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/spell_fireball.mp3");
        if (spellHeal == null) spellHeal = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/spell_heal.mp3");
        if (bgmBattle == null) bgmBattle = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/bgm_battle.mp3");
#endif
    }

    private void EnsureAudioListener()
    {
        AudioListener listener = FindAnyObjectByType<AudioListener>();
        if (listener == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cam.gameObject.AddComponent<AudioListener>();
            else gameObject.AddComponent<AudioListener>();
        }
    }

    private void InitializeClipDictionary()
    {
        clips.Clear();
#if UNITY_EDITOR
        AutoAssignClipsIfEmpty();
#endif
        RegisterClip("card_select", cardSelect);
        RegisterClip("card_click", cardSelect);
        RegisterClip("card_no_energy", cardNoEnergy);
        RegisterClip("no_energy", cardNoEnergy);

        RegisterClip("spawn_warrior", spawnWarrior);
        RegisterClip("spawn_archer", spawnArcher);
        RegisterClip("spawn_golem", spawnGolem);
        RegisterClip("spawn_drone", spawnDrone);
        RegisterClip("spawn_hero", spawnWarrior);
        RegisterClip("spawn_unit", spawnWarrior);

        RegisterClip("attack_warrior", attackWarrior);
        RegisterClip("sword_slash", attackWarrior);
        RegisterClip("attack_archer", attackArcher);
        RegisterClip("arrow_shot", attackArcher);
        RegisterClip("attack_golem", attackGolem);
        RegisterClip("golem_punch", attackGolem);
        RegisterClip("attack_drone", attackDrone);
        RegisterClip("drone_shot", attackDrone);

        RegisterClip("tower_shot_main", towerShotMain);
        RegisterClip("tower_shot_side", towerShotSide);
        RegisterClip("tower_destroy", towerDestroy);
        RegisterClip("tower_hit", attackWarrior);

        RegisterClip("spell_fireball", spellFireball);
        RegisterClip("fireball_cast", spellFireball);
        RegisterClip("spell_heal", spellHeal);
        RegisterClip("heal_cast", spellHeal);

        RegisterClip("bgm_battle", bgmBattle);
    }

    private void RegisterClip(string key, AudioClip clip)
    {
        if (clip != null)
        {
            clip.LoadAudioData();
            clips[key] = clip;
        }
    }

    public static ArcaneAudio EnsureInstance()
    {
        if (Instance == null)
        {
            ArcaneAudio found = FindAnyObjectByType<ArcaneAudio>();
            if (found != null)
            {
                Instance = found;
            }
            else
            {
                GameObject go = new GameObject("ArcaneAudio");
                Instance = go.AddComponent<ArcaneAudio>();
            }
        }
        Instance.EnsureAudioListener();
        return Instance;
    }

    public void Play(string soundName, float volumeMultiplier = 1f)
    {
        if (string.IsNullOrEmpty(soundName)) return;

        float now = Time.time;
        if (lastPlayTimes.TryGetValue(soundName, out float lastTime))
        {
            if (now - lastTime < MinSoundInterval)
                return;
        }
        lastPlayTimes[soundName] = now;

        if (clips.TryGetValue(soundName, out AudioClip clip) && clip != null && sfxSource != null)
        {
            sfxSource.pitch = Random.Range(0.96f, 1.04f);
            float finalVol = masterVolume * sfxVolume * volumeMultiplier;
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(finalVol));
        }
    }

    public void PlayMusic(string musicName = "bgm_battle")
    {
        if (musicSource == null) return;

        if (clips.TryGetValue(musicName, out AudioClip clip) && clip != null)
        {
            musicSource.clip = clip;
            musicSource.volume = masterVolume * musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    // --- CONVENIENCE STATIC API ---

    public static void PlayCardClick() => EnsureInstance()?.Play("card_select", 0.75f);
    public static void PlayNoEnergy() => EnsureInstance()?.Play("card_no_energy", 0.75f);
    public static void PlayDie() => EnsureInstance()?.Play("attack_warrior", 0.40f);
    public static void PlayTowerHit() => EnsureInstance()?.Play("tower_hit", 0.60f);
    public static void PlayTowerDestroy() => EnsureInstance()?.Play("tower_destroy", 0.85f);

    public static void PlayTowerShot(bool isCore)
    {
        string sName = isCore ? "tower_shot_main" : "tower_shot_side";
        EnsureInstance()?.Play(sName, 0.70f);
    }

    public static void PlaySpawn(string unitId = null)
    {
        if (string.IsNullOrEmpty(unitId))
        {
            EnsureInstance()?.Play("spawn_warrior", 0.75f);
            return;
        }

        string key = "spawn_" + unitId.ToLower().Trim();
        EnsureInstance()?.Play(key, key.Contains("golem") ? 1.0f : 0.75f);
    }

    public static void PlayAttack(string unitId)
    {
        if (string.IsNullOrEmpty(unitId)) return;
        string key = "attack_" + unitId.ToLower().Trim();
        EnsureInstance()?.Play(key, 0.70f);
    }

    public static void PlaySpell(SpellType type)
    {
        if (type == SpellType.Damage)
        {
            EnsureInstance()?.Play("spell_fireball", 0.85f);
        }
        else
        {
            EnsureInstance()?.Play("spell_heal", 0.75f);
        }
    }
}
