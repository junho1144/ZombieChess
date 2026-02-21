using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;


    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("BGM")]
    [SerializeField] private BgmEntry[] bgmEntries;
    private Dictionary<BgmType, AudioClip[]> bgmDict = new();
    private AudioSource bgmPlayer;
    private Coroutine bgmLoopCoroutine;
    private BgmType currentBgmType = BgmType.None;

    [Header("SFX")]
    [SerializeField] private SfxPlayer[] sfxPlayers;
    private Dictionary<Sfx, SfxPlayer> sfxDict = new();

    public enum Sfx
    {
        // Atk
        PonAtk = 0,
        SonAtk = 1,
        KnightAtk = 2,
        BishopAtk = 3,
        LookAtk = 4,
        KQAtk = 5,

        // Sfx
        GameWin = 100,
        StageClear = 101,
        CharacterMove = 102,
        MouseClick = 103,
        StageLose = 104,
    }

    public enum BgmType
    {
        None,

        Title,

        Act1,
        Act2,
        Act3,
        Act4,
    }

    [Serializable]
    public class BgmEntry
    {
        public BgmType bgmType;
        public AudioClip[] clips;
    }

    [Serializable]
    public class SfxPlayer
    {
        [SerializeField] private Sfx sfx;
        public Sfx Sfx => sfx;

        [SerializeField] private AudioClip[] clips;
        [SerializeField] private float startTime;
        [SerializeField] private float endTime;

        [HideInInspector] public AudioSource audioSource;
        public Coroutine playCoroutine = null;

        public IEnumerator Play()
        {
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning($"No clips assigned for SFX: {sfx}");
                yield break;
            }

            AudioClip selectedClip = clips[UnityEngine.Random.Range(0, clips.Length)];
            audioSource.clip = selectedClip;

            float start = Mathf.Clamp(startTime, 0, selectedClip.length);
            float end = Mathf.Clamp(endTime > 0 ? endTime : selectedClip.length, start, selectedClip.length);

            audioSource.time = start;
            audioSource.Play();

            float duration = end - start;
            yield return new WaitForSeconds(duration);

            if (audioSource.isPlaying)
                audioSource.Stop();

            playCoroutine = null;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeBgmPlayer();
            InitializeSfxPlayers();

            InitializeBgmDict();
            InitializeSfxDict();

            // 기본 BGM 재생 (원하는 초기값으로 변경 가능)
            ChangeBgm(BgmType.Title);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeBgmPlayer()
    {
        GameObject bgmObject = new GameObject("BgmPlayer");
        bgmObject.transform.parent = transform;

        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop = false;
        bgmPlayer.volume = 1f;
        bgmPlayer.outputAudioMixerGroup = audioMixer.FindMatchingGroups("BGM")[0];
    }

    private void InitializeSfxPlayers()
    {
        GameObject sfxObject = new GameObject("SfxPlayers");
        sfxObject.transform.parent = transform;

        foreach (var sfxPlayer in sfxPlayers)
        {
            AudioSource source = sfxObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = 1f;
            source.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];

            sfxPlayer.audioSource = source;
        }
    }

    private void InitializeBgmDict()
    {
        foreach (var entry in bgmEntries)
        {
            if (!bgmDict.ContainsKey(entry.bgmType))
            {
                bgmDict.Add(entry.bgmType, entry.clips);
            }
        }
    }

    private void InitializeSfxDict()
    {
        foreach (var player in sfxPlayers)
        {
            if (!sfxDict.ContainsKey(player.Sfx))
            {
                sfxDict.Add(player.Sfx, player);
            }
        }
    }



    public void PlaySfx(Sfx targetSfx)
    {
        if (!sfxDict.TryGetValue(targetSfx, out var player))
        {
            Debug.LogError($"SFX not found: {targetSfx}");
            return;
        }

        if (player.playCoroutine != null)
        {
            StopCoroutine(player.playCoroutine);
            player.playCoroutine = null;
        }

        if (player.audioSource.isPlaying)
        {
            player.audioSource.Stop();
        }

        player.playCoroutine = StartCoroutine(player.Play());
    }
    public void AddSfxToButton(Button button, Sfx sfx)
    {
        button.onClick.AddListener(() => PlaySfx(sfx));
    }


    public void ChangeBgm(BgmType type)
    {
        if (type == currentBgmType)
            return;

        if (bgmLoopCoroutine != null)
            StopCoroutine(bgmLoopCoroutine);

        if (type == BgmType.None || !bgmDict.ContainsKey(type))
        {
            if (bgmPlayer.isPlaying)
                StartCoroutine(FadeOutAndStop(1.5f));  // 페이드 아웃 후 정지
            currentBgmType = BgmType.None;
            return;
        }

        if (bgmPlayer.isPlaying)
        {
            StartCoroutine(FadeOutAndChange(type, 1.5f)); // 페이드 아웃 후 전환
        }
        else
        {
            StartBgm(type);
        }
    }
    private IEnumerator FadeOutAndChange(BgmType nextType, float fadeDuration)
    {
        float startVolume = bgmPlayer.volume;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            bgmPlayer.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        bgmPlayer.Stop();
        bgmPlayer.volume = 0f;
        bgmPlayer.clip = null;

        StartBgm(nextType);  // 새 BGM 시작
    }

    private IEnumerator FadeOutAndStop(float fadeDuration)
    {
        float startVolume = bgmPlayer.volume;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            bgmPlayer.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        bgmPlayer.Stop();
        bgmPlayer.volume = 0f;
        bgmPlayer.clip = null;
    }
    private void StartBgm(BgmType type)
    {
        currentBgmType = type;
        bgmLoopCoroutine = StartCoroutine(LoopBgmWithFade(type, 1.5f));
    }

    private IEnumerator LoopBgmWithFade(BgmType type, float fadeDuration)
    {
        AudioClip lastPlayedClip = null;  // 직전에 재생된 클립을 추적

        while (true)
        {
            AudioClip[] clips = bgmDict[type];
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning($"No BGM clips assigned for {type}");
                yield break;
            }

            // 직전에 재생된 클립을 제외한 클립 목록을 만들기
            var availableClips = clips.Where(clip => clip != lastPlayedClip).ToArray();

            // 만약 모든 클립이 직전에 재생된 클립이라면, 마지막으로 재생된 클립을 다시 선택
            AudioClip selectedClip = availableClips.Length > 0
                ? availableClips[UnityEngine.Random.Range(0, availableClips.Length)]
                : clips[UnityEngine.Random.Range(0, clips.Length)];

            lastPlayedClip = selectedClip;  // 이번에 재생한 클립을 추적

            bgmPlayer.clip = selectedClip;
            bgmPlayer.volume = 0f;  // 페이드 인 전에 볼륨을 0으로 시작
            bgmPlayer.Play();

            // 페이드 인
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                bgmPlayer.volume = Mathf.Lerp(0f, 1f, t / fadeDuration);
                yield return null;
            }
            bgmPlayer.volume = 1f;

            // BGM이 끝날 때까지 대기 (페이드 인과 페이드 아웃 시간 제외)
            float waitTime = selectedClip.length - fadeDuration * 2;
            if (waitTime > 0)
                yield return new WaitForSeconds(waitTime);

            // 페이드 아웃
            float startVolume = bgmPlayer.volume;  // 현재 볼륨 저장
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                bgmPlayer.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
                yield return null;
            }

            // 볼륨을 0으로 설정 후 Stop() 호출
            bgmPlayer.volume = 0f;

            // Stop 호출 전에 페이드 아웃이 완료되도록 잠시 대기
            yield return new WaitForSeconds(0.1f); // 잠시 대기 (혹시나 필요하면 조정)

            bgmPlayer.Stop();

            yield return null;  // 루프 계속
        }
    }



}