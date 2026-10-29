using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SignalLost.Core;
using SignalLost.Player;

namespace SignalLost.UI
{
    public sealed class SignalLostHUD : MonoBehaviour
    {
        [Header("Persistent HUD")]
        [SerializeField] private CanvasGroup hudGroup;
        [SerializeField] private Image healthFill;
        [SerializeField] private Text healthText;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image overheatFill;
        [SerializeField] private Image secondaryChargeFill;
        [SerializeField] private Text timerText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text fuseText;
        [SerializeField] private Text medkitText;
        [SerializeField] private Text ammoText;
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private Text promptText;
        [SerializeField] private Image crosshair;
        [SerializeField] private Color crosshairIdleColor = new Color(0.55f, 0.8f, 0.9f, 0.7f);
        [SerializeField] private Color crosshairActiveColor = new Color(0.25f, 1f, 0.8f, 1f);
        [SerializeField] private Image damageOverlay;
        [SerializeField] private GameObject wavePanel;
        [SerializeField] private Image waveFill;
        [SerializeField] private Text waveText;
        [SerializeField] private GameObject bossPanel;
        [SerializeField] private Image bossFill;
        [SerializeField] private Text bossText;

        [Header("Notifications")]
        [SerializeField] private CanvasGroup notificationGroup;
        [SerializeField] private Text notificationText;

        [Header("Modal Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Text victoryDetailText;
        [SerializeField] private Text defeatDetailText;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Text dialogueSpeakerText;
        [SerializeField] private Text dialogueBodyText;
        [SerializeField] private KeypadPanelUI keypadPanel;

        private GameManager manager;
        private PlayerHealth health;
        private PlayerInventory inventory;
        private ArcPistol pistol;
        private PlayerInteraction interaction;
        private PlayerInputReader input;
        private Coroutine notificationRoutine;
        private Coroutine damageRoutine;
        private float lastHealth;
        private string lastNotification = string.Empty;
        private bool modalActive;

        private void Start()
        {
            manager = GameManager.Instance;
            PlayerInteraction[] interactions = FindObjectsByType<PlayerInteraction>(FindObjectsInactive.Exclude);
            if (interactions.Length > 0)
            {
                interaction = interactions[0];
                inventory = interaction.Inventory;
                health = interaction.Health;
                pistol = interaction.Weapon;
            }

            if (health == null)
            {
                health = FindAnyObjectByType<PlayerHealth>();
            }

            if (inventory == null)
            {
                inventory = FindAnyObjectByType<PlayerInventory>();
            }

            if (pistol == null)
            {
                pistol = FindAnyObjectByType<ArcPistol>(FindObjectsInactive.Include);
            }

            input = FindAnyObjectByType<PlayerInputReader>();

            Subscribe();
            RefreshAll();

            if (hudGroup != null)
            {
                hudGroup.alpha = 0f;
                hudGroup.blocksRaycasts = false;
            }

            if (notificationGroup != null)
            {
                notificationGroup.alpha = 0f;
                notificationGroup.blocksRaycasts = false;
            }

            if (damageOverlay != null)
            {
                damageOverlay.color = new Color(0.8f, 0.05f, 0.02f, 0f);
            }

            HideDirectPanels();

            if (manager != null)
            {
                ApplyState(manager.State);
            }
        }

        private void Update()
        {
            if (manager == null || input == null)
            {
                return;
            }

            if (manager.State == GameState.Playing &&
                !input.GameplayBlocked &&
                input.HealPressedThisFrame)
            {
                TryUseMedkit();
            }

            if (timerText != null && manager.Timer != null)
            {
                timerText.text = manager.Timer.Clock;
                timerText.color = manager.Timer.Remaining <= 60f
                    ? new Color(1f, 0.35f, 0.25f)
                    : new Color(0.78f, 0.95f, 1f);
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void ResumeGame()
        {
            manager?.ResumeMission();
        }

        public void RestartGame()
        {
            manager?.RestartMission();
        }

        public void ReturnToMenu()
        {
            manager?.ReturnToMainMenu();
        }

        public void CloseDialogue()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            EndModal();
        }

        private void Subscribe()
        {
            if (manager != null)
            {
                manager.StateChanged += HandleStateChanged;
                manager.ProgressChanged += HandleProgressChanged;
                manager.NotificationRaised += HandleNotification;
                manager.DialogueRequested += HandleDialogue;
                manager.KeypadRequested += HandleKeypad;
            }

            if (health != null)
            {
                health.HealthChanged += HandleHealthChanged;
            }

            if (inventory != null)
            {
                inventory.Changed += HandleInventoryChanged;
            }

            if (pistol != null)
            {
                pistol.AmmoChanged += HandleAmmoChanged;
            }

            if (interaction != null)
            {
                interaction.PromptChanged += HandlePromptChanged;
            }

            if (keypadPanel != null)
            {
                keypadPanel.Closed += EndModal;
            }
        }

        private void Unsubscribe()
        {
            if (manager != null)
            {
                manager.StateChanged -= HandleStateChanged;
                manager.ProgressChanged -= HandleProgressChanged;
                manager.NotificationRaised -= HandleNotification;
                manager.DialogueRequested -= HandleDialogue;
                manager.KeypadRequested -= HandleKeypad;
            }

            if (health != null)
            {
                health.HealthChanged -= HandleHealthChanged;
            }

            if (inventory != null)
            {
                inventory.Changed -= HandleInventoryChanged;
            }

            if (pistol != null)
            {
                pistol.AmmoChanged -= HandleAmmoChanged;
            }

            if (interaction != null)
            {
                interaction.PromptChanged -= HandlePromptChanged;
            }

            if (keypadPanel != null)
            {
                keypadPanel.Closed -= EndModal;
            }
        }

        private void RefreshAll()
        {
            if (health != null)
            {
                lastHealth = health.CurrentHealth;
                RefreshHealth(health.CurrentHealth, health.MaxHealth);
            }

            if (pistol != null)
            {
                RefreshAmmo(pistol.MagazineAmmo, pistol.ReserveAmmo);
            }

            if (inventory != null)
            {
                RefreshInventory();
            }

            if (manager != null && manager.Progress != null)
            {
                RefreshProgress();
                if (manager.Timer != null)
                {
                    timerText.text = manager.Timer.Clock;
                }
            }

            HandlePromptChanged(string.Empty);
        }

        private void HandleStateChanged(GameState state)
        {
            ApplyState(state);
        }

        private void ApplyState(GameState state)
        {
            bool showHud = state == GameState.Playing ||
                           state == GameState.Paused ||
                           state == GameState.Victory ||
                           state == GameState.Defeat;

            if (hudGroup != null)
            {
                hudGroup.alpha = showHud ? 1f : 0f;
                hudGroup.blocksRaycasts = showHud;
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(state == GameState.Paused);
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(state == GameState.Victory);
            }

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(state == GameState.Defeat);
            }

            if (state == GameState.Victory && victoryDetailText != null && manager != null && manager.Timer != null)
            {
                float elapsed = manager.Timer.Duration - manager.Timer.Remaining;
                victoryDetailText.text = $"Extraction confirmed in {MissionTimer.Format(elapsed)}.\nThe mainland received your signal.";
            }

            if (state == GameState.Defeat && defeatDetailText != null)
            {
                defeatDetailText.text = string.IsNullOrEmpty(lastNotification)
                    ? "The outpost fell silent. No signal escaped the island."
                    : lastNotification;
            }

            if (state != GameState.Playing)
            {
                CloseModalSilently();
                if (promptPanel != null)
                {
                    promptPanel.SetActive(false);
                }
            }
            else if (input != null && !modalActive)
            {
                input.SetGameplayBlocked(false);
            }
        }

        private void HandleProgressChanged(MissionProgress progress)
        {
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            if (manager == null || manager.Progress == null)
            {
                return;
            }

            MissionProgress progress = manager.Progress;
            if (objectiveText != null)
            {
                objectiveText.text = progress.ObjectiveText;
            }

            if (fuseText != null)
            {
                fuseText.text = $"FUSES  recovered {progress.FusesCollected}/{progress.RequiredFuses}   installed {progress.FusesInstalled}/{progress.RequiredFuses}";
            }
        }

        private void HandleHealthChanged(float current, float maximum)
        {
            if (current < lastHealth - 0.01f)
            {
                if (damageRoutine != null)
                {
                    StopCoroutine(damageRoutine);
                }

                damageRoutine = StartCoroutine(DamageFlash());
            }

            lastHealth = current;
            RefreshHealth(current, maximum);
        }

        private void RefreshHealth(float current, float maximum)
        {
            if (healthFill != null)
            {
                healthFill.fillAmount = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
                healthFill.color = healthFill.fillAmount <= 0.3f
                    ? new Color(1f, 0.3f, 0.22f)
                    : new Color(0.2f, 0.95f, 0.7f);
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
            }
        }

        private void HandleAmmoChanged(int magazine, int reserve)
        {
            RefreshAmmo(magazine, reserve);
        }

        private void RefreshAmmo(int magazine, int reserve)
        {
            if (ammoText != null)
            {
                ammoText.text = $"{magazine:00}  /  {reserve:000}";
            }
        }

        private void HandleInventoryChanged()
        {
            RefreshInventory();
        }

        private void RefreshInventory()
        {
            if (medkitText != null && inventory != null)
            {
                medkitText.text = $"MEDKITS  {inventory.Medkits}    [H] Use";
            }
        }

        private void HandlePromptChanged(string prompt)
        {
            bool hasPrompt = !string.IsNullOrEmpty(prompt);
            if (promptPanel != null)
            {
                promptPanel.SetActive(hasPrompt);
            }

            if (promptText != null)
            {
                promptText.text = prompt;
            }

            if (crosshair != null)
            {
                crosshair.color = hasPrompt ? crosshairActiveColor : crosshairIdleColor;
            }
        }

        private void HandleNotification(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            lastNotification = message;
            if (notificationText != null)
            {
                notificationText.text = message;
            }

            if (notificationRoutine != null)
            {
                StopCoroutine(notificationRoutine);
            }

            notificationRoutine = StartCoroutine(ShowNotificationRoutine());
        }

        private IEnumerator ShowNotificationRoutine()
        {
            if (notificationGroup == null)
            {
                yield break;
            }

            notificationGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(3f);

            float elapsed = 0f;
            while (elapsed < 0.45f)
            {
                elapsed += Time.unscaledDeltaTime;
                notificationGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.45f);
                yield return null;
            }

            notificationGroup.alpha = 0f;
            notificationRoutine = null;
        }

        private IEnumerator DamageFlash()
        {
            if (damageOverlay == null)
            {
                yield break;
            }

            float elapsed = 0f;
            Color color = damageOverlay.color;
            while (elapsed < 0.45f)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0.42f, 0f, elapsed / 0.45f);
                damageOverlay.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            damageOverlay.color = new Color(color.r, color.g, color.b, 0f);
            damageRoutine = null;
        }

        private void HandleDialogue(string speaker, string body)
        {
            if (dialoguePanel == null || keypadPanel == null)
            {
                return;
            }

            keypadPanel.ForceClose();
            dialoguePanel.SetActive(true);
            if (dialogueSpeakerText != null)
            {
                dialogueSpeakerText.text = speaker;
            }

            if (dialogueBodyText != null)
            {
                dialogueBodyText.text = body;
            }

            BeginModal();
        }

        private void HandleKeypad(Interactables.KeypadInteractable keypad)
        {
            if (keypadPanel == null)
            {
                return;
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            BeginModal();
            keypadPanel.Open(keypad);
        }

        private void BeginModal()
        {
            modalActive = true;
            input?.SetGameplayBlocked(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void EndModal()
        {
            modalActive = false;
            if (input != null)
            {
                input.SetGameplayBlocked(false);
            }

            if (manager != null && manager.State == GameState.Playing)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void CloseModalSilently()
        {
            if (keypadPanel != null)
            {
                keypadPanel.ForceClose();
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (modalActive)
            {
                modalActive = false;
                input?.SetGameplayBlocked(false);
            }
        }

        private void HideDirectPanels()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(false);
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (promptPanel != null)
            {
                promptPanel.SetActive(false);
            }
        }

        private void TryUseMedkit()
        {
            if (inventory == null)
            {
                return;
            }

            if (inventory.Medkits <= 0)
            {
                HandleNotification("No medkits remaining.");
                return;
            }

            if (health != null && !health.CanHeal)
            {
                HandleNotification("Vitals already stable.");
                return;
            }

            if (inventory.UseMedkit(25f))
            {
                HandleNotification("Medkit applied. +25 vitality.");
            }
        }
    }
}
