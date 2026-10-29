using System;
using System.Collections.Generic;
using System.Linq;
using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Enemies;
using SignalLost.Interactables;
using SignalLost.Player;
using SignalLost.UI;
using SignalLost.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SignalLost.EditorTools
{
    public static class SignalLostPhase4Validator
    {
        [MenuItem("Signal Lost/Phase 4/Validate Complete Game")]
        public static void Validate()
        {
            List<string> errors = new List<string>();
            ValidateBuildSettings(errors);
            ValidateAssets(errors);
            ValidateGameplayScene(errors);
            ValidateMainMenuScene(errors);

            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"[SignalLostPhase4] {error}");
                }

                throw new InvalidOperationException($"Phase 4 validation failed with {errors.Count} error(s).");
            }

            Debug.Log("[SignalLostPhase4] PHASE4_COMPLETE_GAME_OK");
        }

        private static void ValidateBuildSettings(List<string> errors)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (scenes.Length < 2)
            {
                errors.Add("Build settings must contain MainMenu and SignalLost.");
                return;
            }

            if (scenes[0].path != SignalLostPhase4Builder.MainMenuScenePath || !scenes[0].enabled)
            {
                errors.Add("MainMenu must be the first enabled build scene.");
            }

                if (scenes[1].path != "Assets/Scenes/IslandExterior.unity" || !scenes[1].enabled)
                {
                    errors.Add("IslandExterior must be the second enabled build scene.");
                }
        }

        private static void ValidateAssets(List<string> errors)
        {
            foreach (string path in new[]
            {
                SignalLostPhase4Builder.MainMenuScenePath,
                SignalLostPhase3SceneBuilder.ScenePath,
                SignalLostPhase3SceneBuilder.PlayerPrefabPath,
                SignalLostPhase3SceneBuilder.NavMeshAssetPath,
                SignalLostPhase4Builder.StandardSentryPrefabPath,
                SignalLostPhase4Builder.WardenPrefabPath,
                SignalLostPhase4Builder.ArcProjectilePrefabPath,
                SignalLostPhase4Builder.LaserProjectilePrefabPath,
                SignalLostPhase4Builder.AmmoPickupPrefabPath,
                SignalLostPhase4Builder.MedkitPickupPrefabPath,
                SignalLostPhase4Builder.CratePrefabPath,
                "Assets/Generated/Materials/M_Skybox.mat",
                "Assets/Generated/Materials/M_Water.mat",
                "Assets/Generated/Materials/M_TrailCyan.mat",
                "Assets/Generated/Materials/M_TrailRed.mat"
            })
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                {
                    errors.Add($"Missing game asset: {path}");
                }
            }

            foreach (string clip in new[]
            {
                "Ambient",
                "Combat",
                "Menu",
                "Victory",
                "Defeat",
                "GameOver"
            })
            {
                if (Resources.Load<AudioClip>("Audio/Music/" + clip) == null)
                {
                    errors.Add($"Missing music clip: {clip}");
                }
            }

            foreach (string font in new[] { "KenneyFuture", "KenneyFutureNarrow", "KenneyMiniSquare" })
            {
                if (Resources.Load<Font>("Fonts/" + font) == null)
                {
                    errors.Add($"Missing font: {font}");
                }
            }

            foreach (string sprite in new[]
            {
                "PanelGlass",
                "PanelRect",
                "PanelRectScrews",
                "ButtonRect",
                "BarFrame",
                "CrosshairWhite"
            })
            {
                if (Resources.Load<Sprite>("UI/" + sprite) == null)
                {
                    errors.Add($"Missing UI sprite: {sprite}");
                }
            }
        }

        private static void ValidateGameplayScene(List<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SignalLostPhase3SceneBuilder.ScenePath) == null)
            {
                errors.Add("Gameplay scene is missing.");
                return;
            }

            EditorSceneManager.OpenScene(SignalLostPhase3SceneBuilder.ScenePath, OpenSceneMode.Single);
            ValidateGameplayPlayer(errors);
            ValidateEnemies(errors);
            ValidateCrates(errors);
            ValidateGameplayUi(errors);
            ValidateAudio(errors);

            if (SceneComponents<NavMeshRuntimeBootstrap>().Length != 1)
            {
                errors.Add("Gameplay scene is missing NavMeshRuntimeBootstrap.");
            }

            if (SceneComponents<SurfaceIdentifier>().Length < 4)
            {
                errors.Add("Expected surface identifiers on the dock, safe room and tower pad.");
            }

            ValidateMissingScripts(SceneManager.GetActiveScene(), "Gameplay", errors);
        }

        private static void ValidateGameplayPlayer(List<string> errors)
        {
            PlayerHealth[] players = SceneComponents<PlayerHealth>();

            if (players.Length != 1)
            {
                errors.Add($"Expected exactly one player, found {players.Length}.");
                return;
            }

            GameObject player = players[0].gameObject;
            ArcPistol pistol = player.GetComponentInChildren<ArcPistol>(true);
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            Health health = player.GetComponent<Health>();
            PlayerFootsteps footsteps = player.GetComponent<PlayerFootsteps>();

            if (pistol == null)
            {
                errors.Add("Scene player has no Arc Pistol.");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(pistol);
                RequireReference(serialized, "projectilePrefab", "Scene Arc Pistol projectile", errors);
                RequireNumber(serialized, "damage", 25f, "Arc Pistol damage", errors);
                RequireNumber(serialized, "stunDuration", 0.5f, "Arc Pistol stun duration", errors);
                RequireInt(serialized, "magazineSize", 15, "Arc Pistol magazine size", errors);
                RequireNumber(serialized, "aimFieldOfView", 56f, "Arc Pistol aim FOV", errors);
            }

            if (inventory == null)
            {
                errors.Add("Scene player has no inventory.");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(inventory);
                RequireInt(serialized, "startingMedkits", 2, "Starting medkits", errors);
            }

            if (health == null)
            {
                errors.Add("Scene player has no Health component.");
            }
            else if (Mathf.Abs(health.MaxHealth - 100f) > 0.01f)
            {
                errors.Add($"Player max health is {health.MaxHealth}, expected 100.");
            }

            if (footsteps == null)
            {
                errors.Add("Scene player is missing PlayerFootsteps.");
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SignalLostPhase3SceneBuilder.PlayerPrefabPath);

            if (prefab == null)
            {
                errors.Add("Player prefab is missing.");
                return;
            }

            if (prefab.GetComponent<PlayerFootsteps>() == null)
            {
                errors.Add("Player prefab is missing PlayerFootsteps.");
            }

            ArcPistol prefabPistol = prefab.GetComponentInChildren<ArcPistol>(true);
            if (prefabPistol != null
                && new SerializedObject(prefabPistol).FindProperty("projectilePrefab").objectReferenceValue == null)
            {
                errors.Add("Player prefab Arc Pistol has no projectile prefab.");
            }
        }

        private static void ValidateEnemies(List<string> errors)
        {
            SentryBotAI[] bots = SceneComponents<SentryBotAI>();
            if (bots.Length != 5)
            {
                errors.Add($"Expected five sentries, found {bots.Length}.");
            }

            int wardens = bots.Count(bot => bot.IsHeavyWarden);
            if (wardens != 1)
            {
                errors.Add($"Expected one Heavy Warden, found {wardens}.");
            }

            foreach (SentryBotAI bot in bots)
            {
                SerializedObject serialized = new SerializedObject(bot);
                SerializedProperty patrol = serialized.FindProperty("patrolPoints");

                if (patrol == null || patrol.arraySize < 2)
                {
                    errors.Add($"{bot.name} has fewer than two patrol points.");
                }
                else
                {
                    for (int i = 0; i < patrol.arraySize; i++)
                    {
                        if (patrol.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        {
                            errors.Add($"{bot.name} patrol point {i + 1} is missing.");
                        }
                    }
                }

                RequireReference(serialized, "laserProjectilePrefab", $"{bot.name} projectile", errors);
                RequireReference(serialized, "muzzle", $"{bot.name} muzzle", errors);
                RequireReference(serialized, "sensorRenderer", $"{bot.name} sensor", errors);
                RequireNumber(serialized, "standardHealth", 75f, "Standard sentry health", errors);
                RequireNumber(serialized, "wardenHealth", 150f, "Warden health", errors);
            }

            foreach (string path in new[]
            {
                SignalLostPhase4Builder.StandardSentryPrefabPath,
                SignalLostPhase4Builder.WardenPrefabPath
            })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                SentryBotAI ai = prefab?.GetComponent<SentryBotAI>();

                if (ai == null)
                {
                    errors.Add($"{path} has no SentryBotAI.");
                    continue;
                }

                if (prefab.GetComponent<NavMeshAgent>() == null
                    || prefab.GetComponent<Health>() == null
                    || prefab.GetComponent<AudioSource>() == null)
                {
                    errors.Add($"{path} is missing NavMeshAgent, Health or AudioSource.");
                }

                SerializedObject serialized = new SerializedObject(ai);
                RequireReference(serialized, "laserProjectilePrefab", $"{prefab.name} projectile", errors);
                RequireReference(serialized, "muzzle", $"{prefab.name} muzzle", errors);
                RequireReference(serialized, "sensorRenderer", $"{prefab.name} sensor", errors);
            }
        }

        private static void ValidateCrates(List<string> errors)
        {
            DestructibleCrate[] crates = SceneComponents<DestructibleCrate>();
            if (crates.Length != 5)
            {
                errors.Add($"Expected five destructible crates, found {crates.Length}.");
            }

            foreach (DestructibleCrate crate in crates)
            {
                SerializedObject serialized = new SerializedObject(crate);
                SerializedProperty drops = serialized.FindProperty("dropPrefabs");

                if (drops == null || drops.arraySize != 2)
                {
                    errors.Add($"{crate.name} does not drop two pickups.");
                }
                else
                {
                    for (int i = 0; i < drops.arraySize; i++)
                    {
                        if (drops.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        {
                            errors.Add($"{crate.name} drop {i + 1} is missing.");
                        }
                    }
                }

                if (crate.GetComponent<Health>() == null)
                {
                    errors.Add($"{crate.name} has no Health component.");
                }
            }

            foreach (string path in new[]
            {
                SignalLostPhase4Builder.AmmoPickupPrefabPath,
                SignalLostPhase4Builder.MedkitPickupPrefabPath
            })
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    errors.Add($"Pickup prefab is missing: {path}");
                }
            }
        }

        private static void ValidateGameplayUi(List<string> errors)
        {
            SignalLostHUD[] hud = SceneComponents<SignalLostHUD>();
            if (hud.Length != 1)
            {
                errors.Add($"Expected one gameplay HUD, found {hud.Length}.");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(hud[0]);
                foreach (string field in new[]
                {
                    "hudGroup",
                    "healthFill",
                    "healthText",
                    "timerText",
                    "objectiveText",
                    "fuseText",
                    "medkitText",
                    "ammoText",
                    "promptPanel",
                    "promptText",
                    "crosshair",
                    "damageOverlay",
                    "notificationGroup",
                    "notificationText",
                    "pausePanel",
                    "victoryPanel",
                    "defeatPanel",
                    "victoryDetailText",
                    "defeatDetailText",
                    "dialoguePanel",
                    "dialogueSpeakerText",
                    "dialogueBodyText",
                    "keypadPanel"
                })
                {
                    RequireReference(serialized, field, $"HUD {field}", errors);
                }
            }

            KeypadPanelUI keypad = SceneComponents<KeypadPanelUI>().FirstOrDefault();
            if (keypad == null)
            {
                errors.Add("Keypad panel UI is missing.");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(keypad);
                RequireReference(serialized, "root", "Keypad root", errors);
                RequireReference(serialized, "displayText", "Keypad display", errors);
                RequireReference(serialized, "statusText", "Keypad status", errors);
            }

            EventSystem eventSystem = SceneComponents<EventSystem>().FirstOrDefault();
            if (eventSystem == null)
            {
                errors.Add("Gameplay EventSystem is missing.");
            }
            else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                errors.Add("Gameplay EventSystem has no InputSystemUIInputModule.");
            }
            else
            {
                InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (module.actionsAsset == null
                    || module.point == null
                    || module.leftClick == null
                    || module.move == null)
                {
                    errors.Add("Gameplay UI input actions are not fully assigned.");
                }
            }
        }

        private static void ValidateAudio(List<string> errors)
        {
            MusicDirector[] directors = SceneComponents<MusicDirector>();
            if (directors.Length != 1)
            {
                errors.Add($"Expected one gameplay MusicDirector, found {directors.Length}.");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(directors[0]);
                RequireString(serialized, "initialTrack", "Ambient", "Gameplay music track", errors);
                RequireBool(serialized, "followGameplay", true, "Gameplay music following", errors);
            }

            AudioSource[] ambient = SceneComponents<AudioSource>()
                .Where(source => source.loop && source.playOnAwake && source.spatialBlend > 0.9f)
                .ToArray();

            if (ambient.Length < 3)
            {
                errors.Add($"Expected at least three ambient looping audio sources, found {ambient.Length}.");
            }
        }

        private static void ValidateMainMenuScene(List<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SignalLostPhase4Builder.MainMenuScenePath) == null)
            {
                errors.Add("Main menu scene is missing.");
                return;
            }

            EditorSceneManager.OpenScene(SignalLostPhase4Builder.MainMenuScenePath, OpenSceneMode.Single);

            MainMenuUI[] menus = SceneComponents<MainMenuUI>();
            if (menus.Length != 1)
            {
                errors.Add($"Expected one MainMenuUI, found {menus.Length}.");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(menus[0]);
                RequireReference(serialized, "controlsPanel", "Main menu controls panel", errors);
                RequireString(serialized, "gameplaySceneName", "SignalLost", "Main menu scene target", errors);
            }

            if (SceneComponents<MenuCameraDrift>().Length != 1)
            {
                errors.Add("Main menu camera drift is missing.");
            }

            if (SceneComponents<Camera>().Length != 1)
            {
                errors.Add("Main menu must contain exactly one camera.");
            }

            if (SceneComponents<AudioListener>().Length != 1)
            {
                errors.Add("Main menu must contain exactly one AudioListener.");
            }

            MusicDirector[] directors = SceneComponents<MusicDirector>();
            if (directors.Length != 1)
            {
                errors.Add($"Expected one menu MusicDirector, found {directors.Length}.");
            }
            else
            {
                SerializedObject serialized = new SerializedObject(directors[0]);
                RequireString(serialized, "initialTrack", "Menu", "Menu music track", errors);
                RequireBool(serialized, "followGameplay", false, "Menu music following", errors);
            }

            if (SceneComponents<Canvas>().Length < 1)
            {
                errors.Add("Main menu canvas is missing.");
            }

            if (SceneComponents<Text>().Length < 4)
            {
                errors.Add("Main menu text is incomplete.");
            }

            if (SceneComponents<Button>().Length < 4)
            {
                errors.Add("Main menu buttons are incomplete.");
            }

            EventSystem eventSystem = SceneComponents<EventSystem>().FirstOrDefault();
            if (eventSystem == null || eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                errors.Add("Main menu EventSystem is missing its input module.");
            }

            ValidateMissingScripts(SceneManager.GetActiveScene(), "Main menu", errors);
        }

        private static void RequireReference(
            SerializedObject serialized,
            string fieldName,
            string label,
            List<string> errors)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null || property.objectReferenceValue == null)
            {
                errors.Add($"{label} is not assigned.");
            }
        }

        private static void RequireNumber(
            SerializedObject serialized,
            string fieldName,
            float expected,
            string label,
            List<string> errors)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                if (expected != 0f)
                {
                    errors.Add($"{label} field is missing.");
                }

                return;
            }

            if (Mathf.Abs(property.floatValue - expected) > 0.01f)
            {
                errors.Add($"{label} is {property.floatValue}, expected {expected}.");
            }
        }

        private static void RequireInt(
            SerializedObject serialized,
            string fieldName,
            int expected,
            string label,
            List<string> errors)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null || property.intValue != expected)
            {
                errors.Add($"{label} is not {expected}.");
            }
        }

        private static void RequireString(
            SerializedObject serialized,
            string fieldName,
            string expected,
            string label,
            List<string> errors)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null || property.stringValue != expected)
            {
                errors.Add($"{label} is not set to {expected}.");
            }
        }

        private static void RequireBool(
            SerializedObject serialized,
            string fieldName,
            bool expected,
            string label,
            List<string> errors)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null || property.boolValue != expected)
            {
                errors.Add($"{label} is not {expected}.");
            }
        }

        private static void ValidateMissingScripts(Scene scene, string label, List<string> errors)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);

                    if (missing > 0)
                    {
                        errors.Add($"{label}: {transform.name} has {missing} missing script(s).");
                    }
                }
            }
        }

        private static T[] SceneComponents<T>() where T : Component
        {
            Scene scene = SceneManager.GetActiveScene();
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include)
                .Where(component => component != null && component.gameObject.scene == scene)
                .ToArray();
        }
    }
}

