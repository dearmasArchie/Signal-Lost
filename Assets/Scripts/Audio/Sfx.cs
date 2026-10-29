using System.Collections.Generic;
using UnityEngine;

namespace SignalLost.Audio
{
    public static class Sfx
    {
        private const string SfxRoot = "Audio/SFX/";
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip Load(string clipName)
        {
            if (string.IsNullOrWhiteSpace(clipName))
            {
                return null;
            }

            if (Cache.TryGetValue(clipName, out AudioClip clip))
            {
                return clip;
            }

            clip = Resources.Load<AudioClip>(SfxRoot + clipName);
            Cache[clipName] = clip;

            if (clip == null)
            {
                Debug.LogWarning($"[SignalLostAudio] Missing SFX clip: {SfxRoot}{clipName}");
            }

            return clip;
        }

        public static void Play2D(string clipName, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip = Load(clipName);

            if (clip == null)
            {
                return;
            }

            GameObject sourceObject = new GameObject($"SFX_{clipName}");
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.Play();
            Object.Destroy(sourceObject, clip.length / Mathf.Max(0.1f, source.pitch) + 0.25f);
        }

        public static void PlayAt(string clipName, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip = Load(clipName);

            if (clip == null)
            {
                return;
            }

            GameObject sourceObject = new GameObject($"SFX_{clipName}");
            sourceObject.transform.position = position;
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1f;
            source.maxDistance = 30f;
            source.dopplerLevel = 0f;
            source.playOnAwake = false;
            source.Play();
            Object.Destroy(sourceObject, clip.length / Mathf.Max(0.1f, source.pitch) + 0.25f);
        }
    }
}
