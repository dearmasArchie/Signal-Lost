using System;
using SignalLost.Audio;
using SignalLost.Interactables;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SignalLost.Core
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField, Min(30f)] private float missionDuration = 900f;
        [SerializeField, Min(1)] private int requiredFuses = MissionProgress.DefaultRequiredFuses;
        [SerializeField] private bool autoStartMission = true;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string islandExteriorSceneName = "IslandExterior";
        [SerializeField] private string undergroundSceneName = "UndergroundFacility";

        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Boot;

        public MissionTimer Timer { get; private set; }

        public MissionProgress Progress { get; private set; }

        public string TimerText => Timer != null ? Timer.Clock : "00:00";

        public event Action<GameState> StateChanged;

        public event Action<float> TimerChanged;

        public event Action<MissionProgress> ProgressChanged;

        public event Action<string> NotificationRaised;

        public event Action<string, string> DialogueRequested;

        public event Action<KeypadInteractable> KeypadRequested;

        public event Action MissionCompleted;

        public event Action MissionFailed;

        public string IslandExteriorSceneName => islandExteriorSceneName;
        public string UndergroundSceneName => undergroundSceneName;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Timer = new MissionTimer(missionDuration);
            Progress = new MissionProgress(requiredFuses);
            Timer.Changed += HandleTimerChanged;
            Timer.Expired += HandleTimerExpired;
            Progress.Changed += HandleProgressChanged;
            ApplyState(GameState.Boot);
        }

        private void Start()
        {
            if (autoStartMission)
            {
                StartMission();
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (State == GameState.Playing)
                {
                    PauseMission();
                }
                else if (State == GameState.Paused)
                {
                    ResumeMission();
                }
            }

            if (State == GameState.Playing)
            {
                Timer.Tick(Time.deltaTime);
            }
        }

        public void StartMission()
        {
            Time.timeScale = 1f;
            Progress.Reset();
            Timer.Reset(missionDuration);
            Timer.Start();
            ApplyState(GameState.Playing);
            ShowNotification("Restore the radio beacon and escape before lockdown.");
        }

        public void LoadIslandExterior()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(islandExteriorSceneName);
        }

        public void LoadUnderground()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(undergroundSceneName);
        }

        public void PauseMission()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            Timer.Pause();
            ApplyState(GameState.Paused);
        }

        public void ResumeMission()
        {
            if (State != GameState.Paused)
            {
                return;
            }

            Timer.Start();
            ApplyState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                PauseMission();
            }
            else if (State == GameState.Paused)
            {
                ResumeMission();
            }
        }

        public void CompleteMission()
        {
            if (State == GameState.Victory || State == GameState.Defeat)
            {
                return;
            }

            ApplyState(GameState.Victory);
            MissionCompleted?.Invoke();
        }

        public void FailMission()
        {
            if (State == GameState.Victory || State == GameState.Defeat)
            {
                return;
            }

            ApplyState(GameState.Defeat);
            MissionFailed?.Invoke();
        }

        public void NotifyPlayerDied()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            ShowNotification("Signal lost. Engineer vital signs flatlined.");
            FailMission();
        }

        public void NotifyBriefingReceived()
        {
            if (Progress.RecordBriefing())
            {
                ShowNotification("Armory access code received: 4471");
            }
        }

        public void NotifyArcPistolCollected()
        {
            if (Progress.RecordArcPistolCollected())
            {
                ShowNotification("Arc Pistol acquired.");
            }
        }

        public void NotifyFuseCollected()
        {
            if (Progress.CollectFuse())
            {
                ShowNotification($"Override fuse recovered ({Progress.FusesCollected}/{Progress.RequiredFuses}).");
                Sfx.Play2D("UI_Pickup", 0.7f);
            }
        }

        public void NotifyFuseInstalled()
        {
            if (Progress.InstallFuse())
            {
                ShowNotification($"Override fuse installed ({Progress.FusesInstalled}/{Progress.RequiredFuses}).");
                Sfx.Play2D("Console_Use", 0.75f);

                if (Progress.FusesInstalled >= Progress.RequiredFuses)
                {
                    ShowNotification("Power restored. Activate the emergency transmitter.");
                }
            }
        }

        public void NotifySignalSent()
        {
            if (Progress.TrySendSignal())
            {
                ShowNotification("Beacon online. Return to the extraction boat.");
                Sfx.Play2D("UI_Confirm", 0.8f);
            }
        }

        public void AttemptEscape()
        {
            if (!Progress.SignalSent)
            {
                ShowNotification("The extraction boat is waiting for the radio beacon.");
                Sfx.Play2D("UI_Error", 0.7f);
                return;
            }

            if (Progress.RecordEscaped())
            {
                Sfx.Play2D("Boat_Thruster", 0.8f);
                CompleteMission();
            }
        }

        public void ShowNotification(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                NotificationRaised?.Invoke(message);
            }
        }

        public void RequestDialogue(string speaker, string body)
        {
            DialogueRequested?.Invoke(speaker, body);
        }

        public void RequestKeypad(KeypadInteractable keypad)
        {
            if (keypad != null)
            {
                KeypadRequested?.Invoke(keypad);
            }
        }

        public void RestartMission()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(activeScene.name);
            }
        }

        public void ReturnToMainMenu()
        {
            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                ShowNotification("Main menu scene is not configured.");
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void ApplyState(GameState state)
        {
            State = state;
            bool isPlaying = state == GameState.Playing;
            bool freezesTime = state == GameState.Paused || state == GameState.Victory || state == GameState.Defeat;
            Time.timeScale = freezesTime ? 0f : 1f;
            Cursor.visible = !isPlaying;
            Cursor.lockState = isPlaying ? CursorLockMode.Locked : CursorLockMode.None;
            StateChanged?.Invoke(state);
        }

        private void HandleTimerChanged(float remaining)
        {
            TimerChanged?.Invoke(remaining);
        }

        private void HandleTimerExpired()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            ShowNotification("Facility lockdown complete. The island is sealed.");
            FailMission();
        }

        private void HandleProgressChanged()
        {
            ProgressChanged?.Invoke(Progress);
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            if (Timer != null)
            {
                Timer.Changed -= HandleTimerChanged;
                Timer.Expired -= HandleTimerExpired;
            }

            if (Progress != null)
            {
                Progress.Changed -= HandleProgressChanged;
            }

            Instance = null;
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
