using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; } 

    [Header("Field")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;

    [SerializeField] private int hitSoundPoolSize = 5;
    private AudioSource[] hitSoundPool;
    private int currentHitSoundIndex = 0;

    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> bgmDictionary = new Dictionary<string, AudioClip>();

    private Dictionary<string, float> soundCooldowns = new Dictionary<string, float>();

    [SerializeField] private float defaultCooldownTime = 0.2f; // 기본 쿨타임 
    private float lastHitSoundTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.outputAudioMixerGroup = sfxMixerGroup; // 믹서 그룹 할당

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
            bgmSource.outputAudioMixerGroup = bgmMixerGroup; // 믹서 그룹 할당

        }
        else
        {
            Destroy(gameObject);
        }

        LoadVolumeSettings();
        CreateHitSoundPool();
    }

    private void CreateHitSoundPool()
    {
        hitSoundPool = new AudioSource[hitSoundPoolSize];
        for (int i = 0; i < hitSoundPoolSize; i++)
        {
            var audioObj = new GameObject($"HitSoundSource_{i}");
            audioObj.transform.SetParent(transform);

            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = sfxMixerGroup; // 믹서 그룹 할당
            audioSource.playOnAwake = false;

            hitSoundPool[i] = audioSource;
        }
    }

    private void PlayHitSoundFromPool(string clipName)
    {
        AudioSource availableSource = null;

        for(int i = 0; i < hitSoundPoolSize; i++)
        {
            int index = (currentHitSoundIndex + i) % hitSoundPoolSize;
            if (!hitSoundPool[index].isPlaying)
            {
                availableSource = hitSoundPool[index];
                currentHitSoundIndex = (index + 1) % hitSoundPoolSize;
                break;  
            }
        }
        // 모든 AudioSource가 사용 중이면 가장 오래된 것을 교체
        if (availableSource == null)
        {
            availableSource = hitSoundPool[currentHitSoundIndex];
            currentHitSoundIndex = (currentHitSoundIndex + 1) % hitSoundPoolSize;
        }

        var clip = GetSFXClip(clipName);
        if (clip != null && availableSource != null)
        {
            availableSource.clip = clip;
            availableSource.Play();
        }
    }

    public void PlaySFX(string clipName, float volume = 1f, float cooldownTime = -1f)
    {
        // 쿨타임 확인
        if (soundCooldowns.ContainsKey(clipName))
        {
            if (Time.time < soundCooldowns[clipName])
                return;
        }

        var clip = GetSFXClip(clipName);
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);

            float actualCooldown = cooldownTime > 0 ? cooldownTime : defaultCooldownTime;
            soundCooldowns[clipName] = Time.time + actualCooldown;
        }
    }

    public void PlayBGM(string clipName, bool loop = true)
    {
        AudioClip clip = GetBGMClip(clipName);
        if (clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }
    }

    private AudioClip GetSFXClip(string clipName)
    {
        if(sfxDictionary.TryGetValue(clipName, out AudioClip cachedClip))
        {
            return cachedClip;  
        }

        var clip = ResourceManager.Instance.Get<AudioClip>(clipName);
        if (clip != null)
        {
            sfxDictionary[clipName] = clip;
            return clip;
        }

        Debug.LogWarning($"[SoundManager] SFX {clipName}를 찾을 수 없습니다.");
        return null;
    }

    private AudioClip GetBGMClip(string clipName)
    {
        if (bgmDictionary.TryGetValue(clipName, out AudioClip cachedClip))
            return cachedClip;

        AudioClip clip = ResourceManager.Instance.Get<AudioClip>(clipName);
        if (clip != null)
        {
            bgmDictionary[clipName] = clip;
            return clip;
        }

        Debug.LogWarning($"[SoundManager] BGM {clipName}를 찾을 수 없습니다.");
        return null;
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetSFXVolume(float volume) // 개인 오디오 볼륨 조절
    {
        sfxSource.volume = Mathf.Clamp01(volume);
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume);
    }

    public void PlayRandomSFX(string[] clipNames) // 여러개 클립 중 랜덤으로 재생
    {
        if (clipNames == null || clipNames.Length == 0)
        {
            Debug.LogWarning("[SoundManager] 재생할 사운드 클립이 없습니다.");
            return;
        }

        int randomIndex = Random.Range(0, clipNames.Length);
        string selectedClip = clipNames[randomIndex];

        PlaySFX(selectedClip);
    }

    public void PlayMonsterHitSound()
    {
        if (Time.time - lastHitSoundTime < defaultCooldownTime)
            return;

        string[] hitSounds = 
        {
            "mon_hit_01",
            "mon_hit_02",
            "mon_hit_03"
        };

        int randomIndex = Random.Range(0, hitSounds.Length);
        string selectedClip = hitSounds[randomIndex];
        PlayHitSoundFromPool(selectedClip);

        lastHitSoundTime = Time.time;
    }


    // AudioMixer를 통한 볼륨 조절
    public void SetSFXVolumeByMixer(float volume)
    {
        // 0~1 값을 dB로 변환 (-80dB ~ 0dB)
        float dB = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
        audioMixer.SetFloat("SFXVolume", dB);

        SaveLoadManager.Data.sfxVolume = volume;
        SaveLoadManager.SaveToServer().Forget();
    }

    public void SetBGMVolumeByMixer(float volume)
    {
        float dB = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
        audioMixer.SetFloat("BGMVolume", dB);

        SaveLoadManager.Data.bgmVolume = volume;
        SaveLoadManager.SaveToServer().Forget();
    }

    // 게임 시작 시 저장된 볼륨 설정 불러오기
    public void LoadVolumeSettings()
    {
        float bgmVolume = SaveLoadManager.Data.bgmVolume;
        float sfxVolume = SaveLoadManager.Data.sfxVolume;

        // AudioMixer에만 적용 (서버 저장 안함)
        float bgmDB = bgmVolume > 0 ? Mathf.Log10(bgmVolume) * 20 : -80f;
        float sfxDB = sfxVolume > 0 ? Mathf.Log10(sfxVolume) * 20 : -80f;

        audioMixer.SetFloat("BGMVolume", bgmDB);
        audioMixer.SetFloat("SFXVolume", sfxDB);
    }

    public void PlayUIButtonClickSound()
    {
        PlaySFX(SoundName.SFX_UI_Button_Click, 1f, 0.15f);
    }

    public void PlayUICloseClickSound()
    {
        PlaySFX(SoundName.SFX_UI_Exit_Button_Click, 1f, 0.15f);
    }
}
