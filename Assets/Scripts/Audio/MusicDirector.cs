using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SignalLost.Core;
using SignalLost.Enemies;

namespace SignalLost.Audio
{
    public sealed class MusicDirector : MonoBehaviour
    {
        [SerializeField] private string initialTrack = "Ambient";
        [SerializeField] private bool followGameplay = true;
        [SerializeField, Range(0f, 1f)] private float volume = 0.3f;
        [SerializeField] private float combatPollInterval = 0.5f;

        private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        private AudioSource activeSource;
        private AudioSource reserveSource;
        private AudioSource nextSource;
        private GameManager manager;
        private SentryBotAI[] bots;
        private Coroutine fadeRoutine;
        private string currentTrack;
        private float pollTimer;

        public string CurrentTrack => currentTrack;

        private void Awake()
        {
            activeSource = CreateSource("Music A");
            reserveSource = CreateSource("Music B");
            nextSource = reserveSource;
        }

        private void Start()
        {
            if (!followGameplay)
            {
                PlayTrack(initialTrack, 1.2f);
                return;
            }

            manager = GameManager.Instance;
            if (manager == null)
            {
                manager = FindAnyObjectByType<GameManager>();
            }

            if (manager != null)
            {
                manager.StateChanged += HandleStateChanged;
            }

            bots = FindObjectsByType<SentryBotAI>(FindObjectsInactive.Exclude);
            PlayTrack(initialTrack, 1.2f);
        }

        private void Update()
        {
            if (!followGameplay || manager == null)
            {
                return;
            }

            pollTimer -= Time.unscaledDeltaTime;
            if (pollTimer > 0f)
            {
                return;
            }

            pollTimer = combatPollInterval;
            if (manager.State != GameState.Playing)
            {
                return;
            }

            bool combat = false;
            if (bots != null)
            {
                for (int i = 0; i < bots.Length; i++)
                {
                    if (bots[i] != null && bots[i].IsAlerted)
                    {
                        combat = true;
                        break;
                    }
                }
            }

            PlayTrack(combat ? "Combat" : "Ambient", 1.4f);
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.StateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Victory)
            {
                PlayTrack("Victory", 0.7f);
            }
            else if (state == GameState.Defeat)
            {
                PlayTrack("Defeat", 0.7f);
            }
            else if (state == GameState.Playing && currentTrack != "Combat")
            {
                PlayTrack("Ambient", 1.4f);
            }
        }

        private void PlayTrack(string track, float fadeDuration)
        {
            if (string.IsNullOrWhiteSpace(track) || track == currentTrack)
            {
                return;
            }

            AudioClip clip = LoadClip(track);
            if (clip == null)
            {
                return;
            }

            AudioSource leaving = activeSource;
            AudioSource entering = nextSource;

            entering.clip = clip;
            entering.volume = 0f;
            entering.Play();

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(Crossfade(leaving, entering, Mathf.Max(0.05f, fadeDuration)));

            activeSource = entering;
            nextSource = leaving;
            currentTrack = track;
        }

        private IEnumerator Crossfade(AudioSource leaving, AudioSource entering, float duration)
        {
            float elapsed = 0f;
            float startVolume = leaving != null ? leaving.volume : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (entering != null)
                {
                    entering.volume = Mathf.Lerp(0f, volume, t);
                }

                if (leaving != null)
                {
                    leaving.volume = Mathf.Lerp(startVolume, 0f, t);
                }

                yield return null;
            }

            if (entering != null)
            {
                entering.volume = volume;
            }

            if (leaving != null)
            {
                leaving.Stop();
                leaving.clip = null;
                leaving.volume = 0f;
            }

            fadeRoutine = null;
        }

        private AudioClip LoadClip(string track)
        {
            if (clipCache.TryGetValue(track, out AudioClip cached))
            {
                return cached;
            }

            AudioClip clip = Resources.Load<AudioClip>($"Audio/Music/{track}");
            if (clip == null)
            {
                Debug.LogWarning($"[MusicDirector] Missing music clip Audio/Music/{track}", this);
                return null;
            }

            clipCache[track] = clip;
            return clip;
        }

        private AudioSource CreateSource(string sourceName)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.ignoreListenerPause = true;
            return source;
        }
    }
}
