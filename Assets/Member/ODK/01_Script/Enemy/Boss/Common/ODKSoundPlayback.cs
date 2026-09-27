using System;
using System.Collections.Generic;
using KimLIb.SoundSystem;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    public static class ODKSoundPlayback
    {
        private const int FallbackSourceCount = 16;

        private static SoundManager soundManager;
        private static GameObject fallbackRoot;
        private static readonly List<AudioSource> fallbackSources = new();
        private static int fallbackIndex;
        private static bool managerFailed;
        private static readonly HashSet<string> warned = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            soundManager = null;
            fallbackRoot = null;
            fallbackSources.Clear();
            fallbackIndex = 0;
            managerFailed = false;
            warned.Clear();
            cachedSfx = null;
            cachedMusic = null;
            mixerSearched = false;
        }

        public static bool Play(SoundClipSO clip, Vector3 position)
        {
            if (clip == null)
                return false;

            if (clip.audioClip == null)
            {
                WarnOnce($"clip:{clip.name}", $"[ODKSound] '{clip.name}' 에 AudioClip 이 비어있음");
                return false;
            }

            if (!managerFailed && TryPlayWithManager(clip, position))
                return true;

            PlayFallback(clip, position);
            return true;
        }

        private static bool TryPlayWithManager(SoundClipSO clip, Vector3 position)
        {
            if (soundManager == null)
                soundManager = Object.FindFirstObjectByType<SoundManager>();

            if (soundManager == null)
            {
                WarnOnce("noManager", "[ODKSound] 씬에 SoundManager 가 없음 → 직접 재생으로 대체");
                return false;
            }

            if (soundManager.SoundChannel == null)
            {
                WarnOnce("noChannel", "[ODKSound] SoundManager.SoundChannel 이 비어있음 → 직접 재생으로 대체");
                return false;
            }

            try
            {
                soundManager.SoundChannel.RaiseEvent(new PlaySoundEvent().InitData(position, clip));
                return true;
            }
            catch (Exception e)
            {
                managerFailed = true;
                Debug.LogWarning($"[ODKSound] SoundManager 재생 실패 → 이후 직접 재생으로 대체\n{e}");
                return false;
            }
        }

        private static void PlayFallback(SoundClipSO clip, Vector3 position)
        {
            AudioSource source = GetFallbackSource();
            source.transform.position = position;
            source.outputAudioMixerGroup = FindMixerGroup(clip.audioTypes);
            source.clip = clip.audioClip;
            source.loop = clip.loop;
            source.volume = clip.volume;
            source.pitch = clip.pitch;
            if (clip.randomizePitch)
                source.pitch += UnityEngine.Random.Range(-clip.randomPitchModifier, clip.randomPitchModifier);
            source.Play();
        }

        private static AudioSource GetFallbackSource()
        {
            if (fallbackRoot == null)
            {
                fallbackRoot = new GameObject("ODK Sound Fallback");
                fallbackSources.Clear();
                fallbackIndex = 0;
                for (int i = 0; i < FallbackSourceCount; i++)
                {
                    AudioSource s = fallbackRoot.AddComponent<AudioSource>();
                    s.playOnAwake = false;
                    s.spatialBlend = 0f;
                    fallbackSources.Add(s);
                }
            }

            for (int i = 0; i < fallbackSources.Count; i++)
            {
                int idx = (fallbackIndex + i) % fallbackSources.Count;
                if (!fallbackSources[idx].isPlaying)
                {
                    fallbackIndex = (idx + 1) % fallbackSources.Count;
                    return fallbackSources[idx];
                }
            }

            AudioSource oldest = fallbackSources[fallbackIndex];
            fallbackIndex = (fallbackIndex + 1) % fallbackSources.Count;
            oldest.Stop();
            return oldest;
        }

        private static AudioMixerGroup cachedSfx;
        private static AudioMixerGroup cachedMusic;
        private static bool mixerSearched;

        private static AudioMixerGroup FindMixerGroup(AudioTypes type)
        {
            if (!mixerSearched)
            {
                mixerSearched = true;
                SoundPlayer player = Object.FindFirstObjectByType<SoundPlayer>(FindObjectsInactive.Include);
                if (player != null)
                {
                    AudioSource src = player.GetComponent<AudioSource>();
                    if (src != null) cachedSfx = src.outputAudioMixerGroup;
                }
            }
            return type == AudioTypes.Music ? cachedMusic : cachedSfx;
        }

        private static void WarnOnce(string key, string message)
        {
            if (warned.Add(key))
                Debug.LogWarning(message);
        }

        public static void ClearCache()
        {
            soundManager = null;
            managerFailed = false;
        }
    }
}
