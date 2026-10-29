using System;
using System.Collections.Generic;
using SignalLost.Core;
using SignalLost.Enemies;
using SignalLost.Effects;
using SignalLost.Interactables;
using SignalLost.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SignalLost.EditorTools
{
    public static class SignalLostPhase2Validator
    {
        private static readonly string[] RequiredTypes =
        {
            "SignalLost.Core.GameManager",
            "SignalLost.Core.MissionTimer",
            "SignalLost.Core.MissionProgress",
            "SignalLost.Core.Health",
            "SignalLost.Core.HealthState",
            "SignalLost.Core.DamageInfo",
            "SignalLost.Player.PlayerInputReader",
            "SignalLost.Player.FirstPersonController",
            "SignalLost.Player.PlayerInteraction",
            "SignalLost.Player.PlayerInventory",
            "SignalLost.Player.PlayerHealth",
            "SignalLost.Player.ArcPistol",
            "SignalLost.Enemies.SentryBotAI",
            "SignalLost.Effects.EnergyProjectile",
            "SignalLost.Interactables.IInteractable",
            "SignalLost.Interactables.InteractableBase",
            "SignalLost.Interactables.DoorInteractable",
            "SignalLost.Interactables.KeypadLock",
            "SignalLost.Interactables.KeypadInteractable",
            "SignalLost.Interactables.FuseSocket",
            "SignalLost.Interactables.PowerBreaker",
            "SignalLost.Interactables.SignalConsole",
            "SignalLost.Interactables.EscapeBoat",
            "SignalLost.Interactables.NpcDialogueInteractable",
            "SignalLost.Interactables.PickupBase",
            "SignalLost.Interactables.FusePickup",
            "SignalLost.Interactables.AmmoPickup",
            "SignalLost.Interactables.MedkitPickup",
            "SignalLost.Interactables.ArcPistolPickup",
            "SignalLost.World.DestructibleCrate",
            "SignalLost.Audio.Sfx"
        };

        public static void Validate()
        {
            List<string> errors = new List<string>();
            SignalLostInputActionsBuilder.Build();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateTypes(errors);
            ValidateInputActions(errors);
            ValidateCoreLogic(errors);
            ValidateAssemblies(errors);

            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"[SignalLostPhase2] {error}");
                }

                throw new InvalidOperationException($"Phase 2 validation failed with {errors.Count} error(s).");
            }

            Debug.Log("[SignalLostPhase2] PHASE2_CORE_SYSTEMS_OK");
        }

        private static void ValidateTypes(List<string> errors)
        {
            Type gameManagerType = typeof(GameManager);
            System.Reflection.Assembly runtimeAssembly = gameManagerType.Assembly;

            foreach (string typeName in RequiredTypes)
            {
                if (runtimeAssembly.GetType(typeName) == null)
                {
                    errors.Add($"Missing runtime type: {typeName}");
                }
            }
        }

        private static void ValidateInputActions(List<string> errors)
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(SignalLostInputActionsBuilder.AssetPath);

            if (asset == null)
            {
                errors.Add($"Input actions asset missing: {SignalLostInputActionsBuilder.AssetPath}");
                return;
            }

            InputActionMap gameplay = asset.FindActionMap("Gameplay", false);

            if (gameplay == null)
            {
                errors.Add("Gameplay input action map is missing.");
                return;
            }

            foreach (string actionName in new[]
            {
                "Move",
                "Look",
                "Jump",
                "Sprint",
                "Interact",
                "Fire",
                "Aim",
                "Reload",
                "Pause"
            })
            {
                InputAction action = gameplay.FindAction(actionName, false);

                if (action == null)
                {
                    errors.Add($"Missing gameplay input action: {actionName}");
                }
                else if (action.bindings.Count == 0)
                {
                    errors.Add($"Gameplay input action has no bindings: {actionName}");
                }
            }

            if (asset.FindActionMap("UI", false) == null)
            {
                errors.Add("UI input action map is missing.");
            }

            InputActionAsset resourcesAsset = Resources.Load<InputActionAsset>(PlayerInputReader.ControlsResourcePath);

            if (resourcesAsset == null)
            {
                errors.Add($"Input actions cannot be loaded from Resources/{PlayerInputReader.ControlsResourcePath}");
            }
        }

        private static void ValidateCoreLogic(List<string> errors)
        {
            MissionTimer timer = new MissionTimer(10f);
            int expiryCount = 0;
            timer.Expired += () => expiryCount++;
            timer.Start();
            timer.Tick(4f);

            if (Mathf.Abs(timer.Remaining - 6f) > 0.001f)
            {
                errors.Add("MissionTimer did not reduce remaining time correctly.");
            }

            timer.Tick(10f);

            if (!timer.HasExpired || expiryCount != 1)
            {
                errors.Add("MissionTimer did not expire exactly once.");
            }

            if (MissionTimer.Format(600f) != "10:00" || MissionTimer.Format(0f) != "00:00")
            {
                errors.Add("MissionTimer clock formatting is incorrect.");
            }

            HealthState health = new HealthState(100f);
            int deathCount = 0;
            health.Died += () => deathCount++;
            health.ApplyDamage(75f);
            health.Heal(10f);

            if (Mathf.Abs(health.CurrentHealth - 35f) > 0.001f)
            {
                errors.Add("HealthState damage/heal calculation is incorrect.");
            }

            health.ApplyDamage(100f);

            if (health.IsAlive || deathCount != 1)
            {
                errors.Add("HealthState death handling is incorrect.");
            }

            KeypadLock keypad = new KeypadLock("4471");

            if (keypad.TryUnlock("0000") || !keypad.TryUnlock("4471") || !keypad.IsUnlocked)
            {
                errors.Add("KeypadLock code validation is incorrect.");
            }

            MissionProgress progress = new MissionProgress(3);

            if (progress.Stage != MissionStage.Briefing)
            {
                errors.Add("MissionProgress does not start at the briefing stage.");
            }

            progress.RecordBriefing();
            progress.RecordArcPistolCollected();

            for (int i = 0; i < 3; i++)
            {
                progress.CollectFuse();
            }

            if (progress.Stage != MissionStage.InstallFuses)
            {
                errors.Add("MissionProgress did not advance to fuse installation.");
            }

            for (int i = 0; i < 3; i++)
            {
                progress.InstallFuse();
            }

            if (progress.Stage != MissionStage.ActivateTransmitter || !progress.TrySendSignal())
            {
                errors.Add("MissionProgress did not advance to transmitter activation.");
            }

            if (progress.Stage != MissionStage.Escape || !progress.RecordEscaped() || progress.Stage != MissionStage.Complete)
            {
                errors.Add("MissionProgress did not complete the escape sequence.");
            }
        }

        private static void ValidateAssemblies(List<string> errors)
        {
            foreach (string path in new[]
            {
                "Assets/Scripts/SignalLost.Runtime.asmdef",
                "Assets/Editor/SignalLost.Editor.asmdef",
                "Assets/Scripts/Tests/EditMode/SignalLost.EditModeTests.asmdef"
            })
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                {
                    errors.Add($"Missing assembly definition: {path}");
                }
            }
        }
    }
}
