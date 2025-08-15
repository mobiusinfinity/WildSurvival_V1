using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all fire-related audio
/// </summary>
public class FireAudioManager : MonoBehaviour
{
    [System.Serializable]
    public class FireSound
    {
        public string name;
        public AudioClip clip;
        public float volume = 1f;
        public float pitch = 1f;
        public bool loop = false;
    }

    [Header("Fire Sounds")]
    [SerializeField]
    private List<FireSound> fireSounds = new List<FireSound>
    {
        new FireSound { name = "fire_ignite", volume = 0.8f },
        new FireSound { name = "fire_burn_small", volume = 0.5f, loop = true },
        new FireSound { name = "fire_burn_medium", volume = 0.7f, loop = true },
        new FireSound { name = "fire_burn_large", volume = 1f, loop = true },
        new FireSound { name = "fire_crackle", volume = 0.6f },
        new FireSound { name = "fire_extinguish", volume = 0.7f },
        new FireSound { name = "wood_add", volume = 0.6f },
        new FireSound { name = "cooking_sizzle", volume = 0.5f, loop = true }
    };

    private Dictionary<string, FireSound> soundDictionary;
    private AudioSource musicSource;
    private List<AudioSource> effectSources = new List<AudioSource>();

    private static FireAudioManager instance;
    public static FireAudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<FireAudioManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("FireAudioManager");
                    instance = go.AddComponent<FireAudioManager>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        instance = this;
        SetupAudioSources();
        BuildSoundDictionary();
    }

    private void SetupAudioSources()
    {
        // Create audio source pool
        for (int i = 0; i < 10; i++)
        {
            GameObject sourceObj = new GameObject($"FireAudioSource_{i}");
            sourceObj.transform.SetParent(transform);
            var source = sourceObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            effectSources.Add(source);
        }
    }

    private void BuildSoundDictionary()
    {
        soundDictionary = new Dictionary<string, FireSound>();
        foreach (var sound in fireSounds)
        {
            if (!string.IsNullOrEmpty(sound.name))
            {
                soundDictionary[sound.name] = sound;
            }
        }
    }

    public void PlayFireSound(string soundName, Vector3 position)
    {
        if (!soundDictionary.ContainsKey(soundName))
        {
            Debug.LogWarning($"Fire sound not found: {soundName}");
            return;
        }

        var sound = soundDictionary[soundName];
        var source = GetAvailableSource();

        if (source != null && sound.clip != null)
        {
            source.transform.position = position;
            source.clip = sound.clip;
            source.volume = sound.volume;
            source.pitch = sound.pitch + Random.Range(-0.1f, 0.1f);
            source.loop = sound.loop;
            source.spatialBlend = 1f; // 3D sound
            source.Play();
        }
    }

    public AudioSource PlayLoopingFireSound(string soundName, Transform parent)
    {
        if (!soundDictionary.ContainsKey(soundName))
            return null;

        var sound = soundDictionary[soundName];

        GameObject sourceObj = new GameObject($"FireLoop_{soundName}");
        sourceObj.transform.SetParent(parent);
        sourceObj.transform.localPosition = Vector3.zero;

        var source = sourceObj.AddComponent<AudioSource>();
        source.clip = sound.clip;
        source.volume = sound.volume;
        source.pitch = sound.pitch;
        source.loop = true;
        source.spatialBlend = 1f;
        source.minDistance = 1f;
        source.maxDistance = 10f;
        source.Play();

        return source;
    }

    private AudioSource GetAvailableSource()
    {
        foreach (var source in effectSources)
        {
            if (!source.isPlaying)
                return source;
        }

        // Create new source if all are busy
        GameObject sourceObj = new GameObject($"FireAudioSource_Extra");
        sourceObj.transform.SetParent(transform);
        var newSource = sourceObj.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        effectSources.Add(newSource);

        return newSource;
    }

    public void SetFireAudioState(FireInstance fire, FireInstance.FireState state)
    {
        string soundName = state switch
        {
            FireInstance.FireState.Igniting => "fire_ignite",
            FireInstance.FireState.Smoldering => "fire_burn_small",
            FireInstance.FireState.Burning => "fire_burn_medium",
            FireInstance.FireState.Blazing => "fire_burn_large",
            FireInstance.FireState.Extinguished => "fire_extinguish",
            _ => null
        };

        if (soundName != null)
        {
            PlayFireSound(soundName, fire.transform.position);
        }
    }
}