using System;
using System.Collections.Generic;
using System.Linq;
using SignalLost.Core;
using SignalLost.Interactables;
using SignalLost.Player;
using SignalLost.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SignalLost.EditorTools
{
    public static class SignalLostPhase3Validator
    {
        private static readonly string[] RequiredWorldGroups =
        {
            "SafeRoom",
            "Dock",
            "BunkerA",
            "BunkerB",
            "RadioTower",
            "InvisibleShoreBarrier",
            "Vegetation",
            "NaturalProps",
            "MissionMarkers"
        };

        private static readonly string[] RequiredMarkers =
        {
            "SentrySpawn_01",
            "SentrySpawn_02",
            "SentrySpawn_03",
            "SentrySpawn_04",
            "SentrySpawn_Heavy",
            "ExtractionPoint"
        };

        [MenuItem("Signal Lost/Phase 3/Validate World Scene")]
        public static void Validate()
        {
            List<string> errors = new List<string>();
            ValidateBuildSettings(errors);
            ValidateAssets(errors);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SignalLostPhase3SceneBuilder.ScenePath) != null)
            {
                EditorSceneManager.OpenScene(SignalLostPhase3SceneBuilder.ScenePath, OpenSceneMode.Single);
                ValidateScene(errors);
                ValidatePlayer(errors);
                ValidateNavigation(errors);
            }

            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"[SignalLostPhase3] {error}");
                }

                throw new InvalidOperationException($"Phase 3 validation failed with {errors.Count} error(s).");
            }

            Debug.Log("[SignalLostPhase3] PHASE3_WORLD_SCENE_OK");
        }

        private static void ValidateBuildSettings(List<string> errors)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (scenes.Length == 0)
            {
                errors.Add("Build settings contain no scenes.");
                return;
            }

            if (!scenes.Any(scene => scene.enabled && scene.path == SignalLostPhase3SceneBuilder.ScenePath))
            {
                errors.Add($"Build settings do not include enabled scene {SignalLostPhase3SceneBuilder.ScenePath}.");
            }
        }

        private static void ValidateAssets(List<string> errors)
        {
            foreach (string path in new[]
            {
                SignalLostPhase3SceneBuilder.ScenePath,
                SignalLostPhase3SceneBuilder.PlayerPrefabPath,
                SignalLostPhase3SceneBuilder.NavMeshAssetPath,
                SignalLostWorldTerrain.DataAssetPath,
                "Assets/Generated/Materials/M_Skybox.mat",
                "Assets/Generated/Materials/M_Water.mat",
                "Assets/Generated/Materials/M_Hologram.mat"
            })
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                {
                    errors.Add($"Missing generated asset: {path}");
                }
            }

            foreach (string layerName in new[] { "Grass", "Dirt", "Sand", "Rock" })
            {
                string path = $"Assets/Generated/TerrainLayers/TL_{layerName}.terrainlayer";

                if (AssetDatabase.LoadAssetAtPath<TerrainLayer>(path) == null)
                {
                    errors.Add($"Missing terrain layer: {path}");
                }
            }
        }

        private static void ValidateScene(List<string> errors)
        {
            Scene scene = SceneManager.GetActiveScene();

            if (scene.path != SignalLostPhase3SceneBuilder.ScenePath)
            {
                errors.Add($"Unexpected active scene: {scene.path}");
                return;
            }

            GameObject world = FindRoot(scene, "World");
            GameObject lighting = FindRoot(scene, "Lighting");
            GameObject systems = FindRoot(scene, "Systems");

            if (world == null)
            {
                errors.Add("World root object is missing.");
            }

            if (lighting == null)
            {
                errors.Add("Lighting root object is missing.");
            }

            if (systems == null)
            {
                errors.Add("Systems root object is missing.");
            }

            Terrain terrain = SceneComponents<Terrain>().FirstOrDefault(t => t.name == "Terrain");

            if (terrain == null)
            {
                errors.Add("Terrain object is missing.");
            }
            else
            {
                TerrainData data = terrain.terrainData;

                if (data == null)
                {
                    errors.Add("TerrainData is missing.");
                }
                else
                {
                    if (Mathf.Abs(data.size.x - SignalLostWorldTerrain.Size) > 0.1f
                        || Mathf.Abs(data.size.z - SignalLostWorldTerrain.Size) > 0.1f)
                    {
                        errors.Add($"Terrain size is {data.size.x}x{data.size.z}, expected 320x320.");
                    }

                    if (Mathf.Abs(data.size.y - SignalLostWorldTerrain.Height) > 0.1f)
                    {
                        errors.Add($"Terrain height is {data.size.y}, expected {SignalLostWorldTerrain.Height}.");
                    }

                    string[] layerNames = data.terrainLayers.Select(layer => layer != null ? layer.name : "null").ToArray();

                    foreach (string layerName in new[] { "Grass", "Dirt", "Sand", "Rock" })
                    {
                        if (!layerNames.Any(name => name.Contains(layerName)))
                        {
                            errors.Add($"Terrain layer missing: {layerName}.");
                        }
                    }

                    if (data.alphamapLayers != 4)
                    {
                        errors.Add($"Terrain alphamap has {data.alphamapLayers} layers, expected 4.");
                    }
                }

                if (Mathf.Abs(terrain.transform.position.x + SignalLostWorldTerrain.Size * 0.5f) > 0.1f
                    || Mathf.Abs(terrain.transform.position.z + SignalLostWorldTerrain.Size * 0.5f) > 0.1f)
                {
                    errors.Add("Terrain is not centered on the world origin.");
                }
            }

            if (world != null)
            {
                foreach (string groupName in RequiredWorldGroups)
                {
                    if (world.transform.Find(groupName) == null)
                    {
                        errors.Add($"World group missing: {groupName}.");
                    }
                }

                if (world.transform.Find("Water") == null)
                {
                    errors.Add("Water plane is missing.");
                }

                NavMeshSurface surface = world.GetComponent<NavMeshSurface>();

                if (surface == null)
                {
                    errors.Add("World is missing a NavMeshSurface.");
                }
                else if (world.GetComponent<NavMeshRuntimeBootstrap>() == null)
                {
                    errors.Add("World is missing NavMeshRuntimeBootstrap.");
                }

                Transform vegetation = world.transform.Find("Vegetation");
                Transform props = world.transform.Find("NaturalProps");

                if (vegetation != null && vegetation.Cast<Transform>().Count() < 30)
                {
                    errors.Add("Vegetation population is too sparse.");
                }

                if (props != null && props.Cast<Transform>().Count() < 25)
                {
                    errors.Add("Natural prop population is too sparse.");
                }

                Transform barrier = world.transform.Find("InvisibleShoreBarrier");

                if (barrier != null && barrier.Cast<Transform>().Count() != 64)
                {
                    errors.Add("Shore barrier should contain 64 segments.");
                }

                Transform markers = world.transform.Find("MissionMarkers");

                if (markers != null)
                {
                    foreach (string markerName in RequiredMarkers)
                    {
                        if (markers.Find(markerName) == null)
                        {
                            errors.Add($"Mission marker missing: {markerName}.");
                        }
                    }
                }
            }

            if (lighting != null)
            {
                Light sun = lighting.GetComponentsInChildren<Light>(true)
                    .FirstOrDefault(light => light.type == LightType.Directional && light.name == "Sun");

                if (sun == null)
                {
                    errors.Add("Directional sun light is missing.");
                }

                if (RenderSettings.sun == null)
                {
                    errors.Add("RenderSettings has no sun assigned.");
                }
            }

            if (RenderSettings.skybox == null || RenderSettings.skybox.GetTexture("_MainTex") == null)
            {
                errors.Add("Skybox material or HDRI texture is missing.");
            }

            if (!RenderSettings.fog)
            {
                errors.Add("Atmospheric fog is not enabled.");
            }

            if (systems != null && systems.GetComponentInChildren<GameManager>(true) == null)
            {
                errors.Add("GameManager is missing from Systems.");
            }

            int audioListeners = SceneComponents<AudioListener>().Length;

            if (audioListeners != 1)
            {
                errors.Add($"Expected exactly one AudioListener, found {audioListeners}.");
            }

            ValidateInteractables(errors);
            ValidateMissingScripts(scene, errors);
        }

        private static void ValidateInteractables(List<string> errors)
        {
            KeypadInteractable[] keypads = SceneComponents<KeypadInteractable>();

            if (keypads.Length != 1)
            {
                errors.Add($"Expected one armory keypad, found {keypads.Length}.");
            }
            else
            {
                KeypadInteractable keypad = keypads[0];
                SerializedObject serialized = new SerializedObject(keypad);

                if (serialized.FindProperty("accessCode").stringValue != "4471")
                {
                    errors.Add("Armory keypad access code is not 4471.");
                }

                SerializedProperty doors = serialized.FindProperty("controlledDoors");
                DoorInteractable armoryDoor = SceneComponents<DoorInteractable>()
                    .FirstOrDefault(door => door.name == "ArmoryDoor");

                if (armoryDoor == null)
                {
                    errors.Add("ArmoryDoor is missing.");
                }
                else if (doors == null || doors.arraySize != 1 || doors.GetArrayElementAtIndex(0).objectReferenceValue != armoryDoor)
                {
                    errors.Add("Armory keypad is not connected to ArmoryDoor.");
                }

                if (serialized.FindProperty("statusRenderer").objectReferenceValue == null)
                {
                    errors.Add("Armory keypad status renderer is missing.");
                }
            }

            int doorsCount = SceneComponents<DoorInteractable>().Length;

            if (doorsCount != 4)
            {
                errors.Add($"Expected four doors, found {doorsCount}.");
            }

            PowerBreaker[] breakers = SceneComponents<PowerBreaker>();

            if (breakers.Length != 2)
            {
                errors.Add($"Expected two power breakers, found {breakers.Length}.");
            }

            foreach (PowerBreaker breaker in breakers)
            {
                SerializedObject serialized = new SerializedObject(breaker);

                if (serialized.FindProperty("statusRenderer").objectReferenceValue == null)
                {
                    errors.Add($"Breaker {breaker.name} has no status renderer.");
                }

                SerializedProperty poweredDoors = serialized.FindProperty("poweredDoors");

                if (poweredDoors == null || poweredDoors.arraySize == 0)
                {
                    errors.Add($"Breaker {breaker.name} has no powered door.");
                }
            }

            string[] fuseNames = SceneComponents<FusePickup>().Select(pickup => pickup.name).OrderBy(name => name).ToArray();

            if (!fuseNames.SequenceEqual(new[] { "Fuse_A", "Fuse_B", "Fuse_Tower" }))
            {
                errors.Add($"Expected Fuse_A, Fuse_B and Fuse_Tower, found: {string.Join(", ", fuseNames)}.");
            }

            FuseSocket[] sockets = SceneComponents<FuseSocket>();

            if (sockets.Length != 3)
            {
                errors.Add($"Expected three fuse sockets, found {sockets.Length}.");
            }
            else
            {
                int[] socketNumbers = sockets
                    .Select(socket => new SerializedObject(socket).FindProperty("socketNumber").intValue)
                    .OrderBy(value => value)
                    .ToArray();

                if (!socketNumbers.SequenceEqual(new[] { 1, 2, 3 }))
                {
                    errors.Add("Fuse sockets are not numbered 1, 2 and 3.");
                }

                foreach (FuseSocket socket in sockets)
                {
                    SerializedObject serialized = new SerializedObject(socket);

                    if (serialized.FindProperty("fuseVisual").objectReferenceValue == null)
                    {
                        errors.Add($"Fuse socket {socket.name} has no installed fuse visual.");
                    }

                    if (serialized.FindProperty("statusRenderer").objectReferenceValue == null)
                    {
                        errors.Add($"Fuse socket {socket.name} has no status renderer.");
                    }
                }
            }

            if (SceneComponents<SignalConsole>().Length != 1)
            {
                errors.Add("Exactly one signal console is required.");
            }

            if (SceneComponents<NpcDialogueInteractable>().Length != 1)
            {
                errors.Add("Exactly one NPC briefing interactable is required.");
            }

            if (SceneComponents<EscapeBoat>().Length != 1)
            {
                errors.Add("Exactly one extraction boat is required.");
            }

            if (SceneComponents<ArcPistolPickup>().Length != 1)
            {
                errors.Add("Exactly one Arc Pistol pickup is required.");
            }

            if (SceneComponents<AmmoPickup>().Length < 3)
            {
                errors.Add("At least three battery pickups are required.");
            }

            if (SceneComponents<MedkitPickup>().Length < 1)
            {
                errors.Add("At least one medkit pickup is required.");
            }
        }

        private static void ValidatePlayer(List<string> errors)
        {
            GameObject player = SceneComponents<PlayerHealth>()
                .Select(health => health.gameObject)
                .FirstOrDefault();

            if (player == null)
            {
                errors.Add("Player instance is missing.");
            }
            else
            {
                ValidatePlayerObject(player, "Scene player", errors);

                if (Vector3.Distance(player.transform.position, new Vector3(0f, 3.23f, -78f)) > 0.2f)
                {
                    errors.Add("Player is not at the safe room spawn point.");
                }
            }

            if (SceneComponents<PlayerHealth>().Length != 1)
            {
                errors.Add("Expected exactly one player in the scene.");
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SignalLostPhase3SceneBuilder.PlayerPrefabPath);

            if (prefab == null)
            {
                errors.Add("Player prefab is missing.");
                return;
            }

            ValidatePlayerObject(prefab, "Player prefab", errors);
        }

        private static void ValidatePlayerObject(GameObject player, string label, List<string> errors)
        {
            if (!player.CompareTag("Player"))
            {
                errors.Add($"{label} does not use the Player tag.");
            }

            foreach (Type type in new[]
            {
                typeof(CharacterController),
                typeof(PlayerInputReader),
                typeof(FirstPersonController),
                typeof(Health),
                typeof(PlayerHealth),
                typeof(PlayerInventory),
                typeof(PlayerInteraction),
                typeof(ArcPistol)
            })
            {
                if (player.GetComponent(type) == null && player.GetComponentInChildren(type, true) == null)
                {
                    errors.Add($"{label} is missing {type.Name}.");
                }
            }

            Camera camera = player.GetComponentInChildren<Camera>(true);

            if (camera == null)
            {
                errors.Add($"{label} has no camera.");
            }
            else if (!camera.CompareTag("MainCamera"))
            {
                errors.Add($"{label} camera is not tagged MainCamera.");
            }

            if (player.GetComponentInChildren<AudioListener>(true) == null)
            {
                errors.Add($"{label} has no AudioListener.");
            }

            PlayerInputReader input = player.GetComponent<PlayerInputReader>();

            if (input != null)
            {
                SerializedProperty actions = new SerializedObject(input).FindProperty("inputActions");

                if (actions == null || actions.objectReferenceValue == null)
                {
                    errors.Add($"{label} has no input actions asset assigned.");
                }
            }

            ArcPistol pistol = player.GetComponentInChildren<ArcPistol>(true);

            if (pistol != null)
            {
                SerializedObject serialized = new SerializedObject(pistol);

                if (serialized.FindProperty("weaponVisual").objectReferenceValue == null)
                {
                    errors.Add($"{label} Arc Pistol has no weapon visual.");
                }

                if (serialized.FindProperty("muzzle").objectReferenceValue == null)
                {
                    errors.Add($"{label} Arc Pistol has no muzzle.");
                }

                if (serialized.FindProperty("aimCamera").objectReferenceValue == null)
                {
                    errors.Add($"{label} Arc Pistol has no aim camera.");
                }
            }
        }

        private static void ValidateNavigation(List<string> errors)
        {
            NavMeshSurface surface = SceneComponents<NavMeshSurface>().FirstOrDefault();

            if (surface == null)
            {
                return;
            }

            if (surface.navMeshData == null)
            {
                errors.Add("NavMeshSurface has no baked NavMeshData.");
                return;
            }

            if (AssetDatabase.GetAssetPath(surface.navMeshData) != SignalLostPhase3SceneBuilder.NavMeshAssetPath)
            {
                errors.Add("NavMeshSurface does not reference the saved NavMeshData asset.");
            }

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

            if (triangulation.indices.Length < 900)
            {
                errors.Add($"NavMesh has too few triangles: {triangulation.indices.Length / 3}.");
            }

            Vector3[] checkpoints =
            {
                new Vector3(0f, 3.23f, -78f),
                new Vector3(0f, 3.2f, -76f),
                new Vector3(0f, 3f, -112f),
                new Vector3(-60f, 8.2f, 8f),
                new Vector3(60f, 10.2f, 24f),
                new Vector3(0f, 24.2f, 76f),
                new Vector3(0f, SignalLostWorldTerrain.SampleHeight(0f, -35f) + 0.2f, -35f)
            };

            string[] names = { "spawn", "safe room", "dock", "Bunker A", "Bunker B", "radio tower", "path fork" };
            Vector3[] sampled = new Vector3[checkpoints.Length];

            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (!NavMesh.SamplePosition(checkpoints[i], out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    errors.Add($"NavMesh is missing near {names[i]}.");
                    continue;
                }

                sampled[i] = hit.position;
            }

            for (int i = 1; i < sampled.Length; i++)
            {
                if (sampled[i] == Vector3.zero)
                {
                    continue;
                }

                NavMeshPath path = new NavMeshPath();

                if (!NavMesh.CalculatePath(sampled[0], sampled[i], NavMesh.AllAreas, path)
                    || path.status != NavMeshPathStatus.PathComplete)
                {
                    errors.Add($"NavMesh path from spawn to {names[i]} is incomplete.");
                }
            }
        }

        private static void ValidateMissingScripts(Scene scene, List<string> errors)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);

                    if (missing > 0)
                    {
                        errors.Add($"{transform.name} has {missing} missing script(s).");
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

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }
    }
}

