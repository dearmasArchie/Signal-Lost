using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Enemies;
using SignalLost.Effects;
using SignalLost.Interactables;
using SignalLost.Player;
using SignalLost.UI;
using SignalLost.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SignalLost.EditorTools
{
    public static class SignalLostPhase4Builder
    {
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string StandardSentryPrefabPath = "Assets/Prefabs/SentryBot.prefab";
        public const string WardenPrefabPath = "Assets/Prefabs/SentryWarden.prefab";
        public const string ArcProjectilePrefabPath = "Assets/Prefabs/ArcProjectile.prefab";
        public const string LaserProjectilePrefabPath = "Assets/Prefabs/LaserProjectile.prefab";
        public const string AmmoPickupPrefabPath = "Assets/Prefabs/AmmoPickup.prefab";
        public const string MedkitPickupPrefabPath = "Assets/Prefabs/MedkitPickup.prefab";
        public const string CratePrefabPath = "Assets/Prefabs/DestructibleCrate.prefab";

        private const string ModelRoot = "Assets/ThirdParty/Kenney/Models/";
        private const string MaterialRoot = "Assets/Generated/Materials/";
        private const string SkyboxPath = "Assets/Generated/Materials/M_Skybox.mat";

        private static Material metal;
        private static Material metalPlates;
        private static Material wood;
        private static Material rock;
        private static Material sand;
        private static Material grass;
        private static Material water;
        private static Material concrete;
        private static Material emissiveCyan;
        private static Material emissiveRed;
        private static Material emissiveWarm;
        private static Material emissiveGreen;

        [MenuItem("Signal Lost/Phase 4/Build Complete Game")]
        public static void BuildAll()
        {
            try
            {
                if (EditorApplication.isPlaying)
                {
                    throw new InvalidOperationException("Exit play mode before building Phase 4.");
                }

                SignalLostInputActionsBuilder.Build();
                SignalLostPhase3SceneBuilder.BuildWorld();

                InitializeMaterials();
                BuildProjectilePrefabs(out GameObject arcProjectile, out GameObject laserProjectile);
                BuildSentryPrefabs(laserProjectile, out GameObject standardSentry, out GameObject warden);
                BuildPickupPrefabs(out GameObject ammoPickup, out GameObject medkitPickup);
                BuildCratePrefab(ammoPickup, medkitPickup, out GameObject cratePrefab);

                AddPlayerSystems(arcProjectile);
                AddSurfaceIdentifiers();
                AddAmbientAudio();
                AddCrates(cratePrefab);
                AddEnemies(standardSentry, warden);
                AddMusicDirector();
                BuildHud();

                SaveGameplayScene();
                BuildMainMenu();
                ConfigureBuildSettings();

                Debug.Log(
                    $"[SignalLostPhase4] PHASE4_GAME_BUILT "
                    + $"gameplay={SignalLostPhase3SceneBuilder.ScenePath} menu={MainMenuScenePath} "
                    + $"enemies=5 crates=5 prefabs=7");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SignalLostPhase4] Complete game build failed.");
                Debug.LogException(exception);
                throw;
            }
        }

        private static void InitializeMaterials()
        {
            metal = SignalLostWorldAssetFactory.GetMaterial("Metal");
            metalPlates = SignalLostWorldAssetFactory.GetMaterial("MetalPlates");
            wood = SignalLostWorldAssetFactory.GetMaterial("Wood");
            rock = SignalLostWorldAssetFactory.GetMaterial("Rock");
            sand = SignalLostWorldAssetFactory.GetMaterial("Sand");
            grass = SignalLostWorldAssetFactory.GetMaterial("Grass");
            water = SignalLostWorldAssetFactory.GetMaterial("Water");
            concrete = SignalLostWorldAssetFactory.GetMaterial("Concrete");
            emissiveCyan = SignalLostWorldAssetFactory.GetMaterial("EmissiveCyan");
            emissiveRed = SignalLostWorldAssetFactory.GetMaterial("EmissiveRed");
            emissiveWarm = SignalLostWorldAssetFactory.GetMaterial("EmissiveWarm");
            emissiveGreen = SignalLostWorldAssetFactory.GetMaterial("EmissiveGreen");
        }

        private static void AddPlayerSystems(GameObject projectilePrefab)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(SignalLostPhase3SceneBuilder.PlayerPrefabPath);
            try
            {
                if (contents.GetComponent<PlayerFootsteps>() == null)
                {
                    contents.AddComponent<PlayerFootsteps>();
                }

                ArcPistol pistol = contents.GetComponentInChildren<ArcPistol>(true);
                if (pistol != null)
                {
                    SetObjectField(pistol, "projectilePrefab", projectilePrefab);
                    SetFloatField(pistol, "aimFieldOfView", 56f);
                }

                PlayerInventory inventory = contents.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    SetIntField(inventory, "startingMedkits", 2);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, SignalLostPhase3SceneBuilder.PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                if (player.GetComponent<PlayerFootsteps>() == null)
                {
                    player.AddComponent<PlayerFootsteps>();
                }

                ArcPistol pistol = player.GetComponentInChildren<ArcPistol>(true);
                if (pistol != null)
                {
                    SetObjectField(pistol, "projectilePrefab", projectilePrefab);
                }
            }
        }

        private static void BuildProjectilePrefabs(out GameObject arcProjectile, out GameObject laserProjectile)
        {
            Material trailCyan = GetOrCreateTrailMaterial("M_TrailCyan", new Color(0.2f, 0.9f, 1f, 1f));
            Material trailRed = GetOrCreateTrailMaterial("M_TrailRed", new Color(1f, 0.28f, 0.08f, 1f));

            arcProjectile = CreateProjectilePrefab(
                "ArcProjectile",
                ArcProjectilePrefabPath,
                new Color(0.2f, 0.9f, 1f),
                emissiveCyan,
                trailCyan);
            laserProjectile = CreateProjectilePrefab(
                "LaserProjectile",
                LaserProjectilePrefabPath,
                new Color(1f, 0.22f, 0.06f),
                emissiveRed,
                trailRed);
        }

        private static GameObject CreateProjectilePrefab(
            string name,
            string prefabPath,
            Color color,
            Material coreMaterial,
            Material trailMaterial)
        {
            GameObject root = new GameObject(name);

            GameObject visual = CreatePrimitive(
                root.transform,
                "Core",
                PrimitiveType.Sphere,
                Vector3.zero,
                new Vector3(0.16f, 0.16f, 0.16f),
                coreMaterial);
            RemoveCollider(visual);

            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.14f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0f;
            trail.numCapVertices = 2;
            trail.sharedMaterial = trailMaterial;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);

            Light glow = root.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = color;
            glow.range = 2.6f;
            glow.intensity = 1.8f;
            glow.shadows = LightShadows.None;

            root.AddComponent<EnergyProjectile>();
            return SavePrefab(root, prefabPath);
        }

        private static void BuildSentryPrefabs(
            GameObject laserProjectile,
            out GameObject standardSentry,
            out GameObject warden)
        {
            standardSentry = CreateSentryPrefab(
                "SentryBot",
                StandardSentryPrefabPath,
                false,
                laserProjectile);
            warden = CreateSentryPrefab(
                "SentryWarden",
                WardenPrefabPath,
                true,
                laserProjectile);
        }

        private static GameObject CreateSentryPrefab(
            string name,
            string prefabPath,
            bool heavy,
            GameObject laserProjectile)
        {
            GameObject root = new GameObject(name);
            float bodyScale = heavy ? 1.22f : 1f;

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.radius = heavy ? 0.62f : 0.46f;
            agent.height = heavy ? 2.15f : 1.8f;
            agent.baseOffset = 0f;
            agent.speed = heavy ? 1.75f : 1.6f;
            agent.angularSpeed = 420f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 1.1f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQualityObstacleAvoidance;
            agent.avoidancePriority = heavy ? 40 : 55;
            agent.enabled = false;

            BoxCollider bodyCollider = root.AddComponent<BoxCollider>();
            bodyCollider.center = new Vector3(0f, heavy ? 1.1f : 0.98f, 0f);
            bodyCollider.size = heavy
                ? new Vector3(1.35f, 2.1f, 1.35f)
                : new Vector3(1.05f, 1.85f, 1.05f);

            root.AddComponent<Health>();
            SentryBotAI ai = root.AddComponent<SentryBotAI>();

            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);

            GameObject body = CreatePrimitive(
                visual,
                "Hull",
                PrimitiveType.Sphere,
                new Vector3(0f, 1.12f * bodyScale, 0f),
                new Vector3(0.92f, 0.82f, 0.7f) * bodyScale,
                heavy ? metalPlates : metal);
            RemoveCollider(body);

            GameObject lower = CreatePrimitive(
                visual,
                "HoverCore",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.62f * bodyScale, 0f),
                new Vector3(0.72f, 0.38f, 0.72f) * bodyScale,
                metal);
            RemoveCollider(lower);

            GameObject ring = CreatePrimitive(
                visual,
                "HoverRing",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.7f * bodyScale, 0f),
                new Vector3(1.15f, 0.045f, 1.15f) * bodyScale,
                emissiveCyan);
            RemoveCollider(ring);

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject pod = CreatePrimitive(
                    visual,
                    side < 0 ? "PodL" : "PodR",
                    PrimitiveType.Cube,
                    new Vector3(side * 0.58f * bodyScale, 1.12f * bodyScale, 0.02f),
                    new Vector3(0.28f, 0.5f, 0.52f) * bodyScale,
                    metalPlates);
                RemoveCollider(pod);
            }

            if (heavy)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject shoulder = CreatePrimitive(
                        visual,
                        side < 0 ? "ArmorL" : "ArmorR",
                        PrimitiveType.Cube,
                        new Vector3(side * 0.78f * bodyScale, 1.4f * bodyScale, 0f),
                        new Vector3(0.32f, 0.3f, 0.72f) * bodyScale,
                        emissiveWarm);
                    RemoveCollider(shoulder);
                }
            }

            GameObject eye = CreatePrimitive(
                visual,
                "SensorEye",
                PrimitiveType.Sphere,
                new Vector3(0f, 1.32f * bodyScale, 0.4f * bodyScale),
                new Vector3(0.3f, 0.22f, 0.2f) * bodyScale,
                emissiveRed);
            RemoveCollider(eye);

            GameObject antenna = CreatePrimitive(
                visual,
                "Antenna",
                PrimitiveType.Cube,
                new Vector3(0f, 1.65f * bodyScale, 0f),
                new Vector3(0.04f, 0.42f, 0.04f) * bodyScale,
                metal);
            RemoveCollider(antenna);

            GameObject barrel = CreatePrimitive(
                visual,
                "Barrel",
                PrimitiveType.Cylinder,
                new Vector3(0f, 1.04f * bodyScale, 0.54f * bodyScale),
                new Vector3(0.1f, 0.34f, 0.1f) * bodyScale,
                metal);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            RemoveCollider(barrel);

            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(visual, false);
            muzzle.localPosition = new Vector3(0f, 1.04f * bodyScale, 0.78f * bodyScale);

            AudioSource servo = root.AddComponent<AudioSource>();
            servo.clip = Resources.Load<AudioClip>("Audio/SFX/Generator_Hum");
            servo.loop = true;
            servo.playOnAwake = true;
            servo.spatialBlend = 1f;
            servo.volume = heavy ? 0.13f : 0.08f;
            servo.minDistance = 2.5f;
            servo.maxDistance = 16f;
            servo.rolloffMode = AudioRolloffMode.Logarithmic;
            servo.dopplerLevel = 0f;

            SetBoolField(ai, "heavyWarden", heavy);
            SetObjectField(ai, "sensorRenderer", eye.GetComponent<Renderer>());
            SetObjectField(ai, "muzzle", muzzle);
            SetObjectField(ai, "laserProjectilePrefab", laserProjectile);

            return SavePrefab(root, prefabPath);
        }

        private static void BuildPickupPrefabs(out GameObject ammoPickup, out GameObject medkitPickup)
        {
            ammoPickup = CreateAmmoPrefab();
            medkitPickup = CreateMedkitPrefab();
        }

        private static GameObject CreateAmmoPrefab()
        {
            GameObject root = new GameObject("AmmoPickup");
            GameObject body = CreatePrimitive(
                root.transform,
                "BatteryPack",
                PrimitiveType.Cube,
                new Vector3(0f, 0.18f, 0f),
                new Vector3(0.5f, 0.28f, 0.34f),
                metalPlates);
            RemoveCollider(body);

            for (int i = -1; i <= 1; i += 2)
            {
                GameObject cell = CreatePrimitive(
                    root.transform,
                    "Cell",
                    PrimitiveType.Cylinder,
                    new Vector3(i * 0.13f, 0.32f, 0f),
                    new Vector3(0.11f, 0.14f, 0.11f),
                    emissiveCyan);
                RemoveCollider(cell);
            }

            CreatePointLight(root.transform, "PickupGlow", new Vector3(0f, 0.45f, 0f), new Color(0.2f, 0.9f, 1f), 0.8f, 3.5f);

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.22f, 0f);
            trigger.size = new Vector3(0.72f, 0.62f, 0.62f);

            AmmoPickup pickup = root.AddComponent<AmmoPickup>();
            SetIntField(pickup, "batteryCells", 30);
            return SavePrefab(root, AmmoPickupPrefabPath);
        }

        private static GameObject CreateMedkitPrefab()
        {
            GameObject root = new GameObject("MedkitPickup");
            GameObject body = CreatePrimitive(
                root.transform,
                "Case",
                PrimitiveType.Cube,
                new Vector3(0f, 0.17f, 0f),
                new Vector3(0.52f, 0.3f, 0.4f),
                metalPlates);
            RemoveCollider(body);

            GameObject crossA = CreatePrimitive(
                root.transform,
                "CrossA",
                PrimitiveType.Cube,
                new Vector3(0f, 0.33f, 0f),
                new Vector3(0.09f, 0.025f, 0.27f),
                emissiveGreen);
            RemoveCollider(crossA);

            GameObject crossB = CreatePrimitive(
                root.transform,
                "CrossB",
                PrimitiveType.Cube,
                new Vector3(0f, 0.33f, 0f),
                new Vector3(0.24f, 0.025f, 0.09f),
                emissiveGreen);
            RemoveCollider(crossB);

            GameObject handle = CreatePrimitive(
                root.transform,
                "Handle",
                PrimitiveType.Cube,
                new Vector3(0f, 0.36f, 0f),
                new Vector3(0.22f, 0.05f, 0.07f),
                metal);
            RemoveCollider(handle);

            CreatePointLight(root.transform, "PickupGlow", new Vector3(0f, 0.45f, 0f), new Color(0.2f, 1f, 0.45f), 0.7f, 3f);

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.22f, 0f);
            trigger.size = new Vector3(0.72f, 0.62f, 0.62f);

            MedkitPickup pickup = root.AddComponent<MedkitPickup>();
            SetIntField(pickup, "medkits", 1);
            return SavePrefab(root, MedkitPickupPrefabPath);
        }

        private static void BuildCratePrefab(GameObject ammoPickup, GameObject medkitPickup, out GameObject cratePrefab)
        {
            GameObject root = new GameObject("DestructibleCrate");
            CreateModel(
                root.transform,
                "CrateVisual",
                "Survival/box-large",
                Vector3.zero,
                Quaternion.identity,
                new Vector3(1.15f, 1.0f, 1.15f),
                true,
                true,
                false);

            root.AddComponent<Health>();
            DestructibleCrate crate = root.AddComponent<DestructibleCrate>();
            SetFloatField(crate, "crateHealth", 30f);
            SetObjectArrayField(crate, "dropPrefabs", new Object[] { ammoPickup, medkitPickup });
            cratePrefab = SavePrefab(root, CratePrefabPath);
        }

        private static void AddSurfaceIdentifiers()
        {
            GameObject dock = GameObject.Find("World/Dock");
            if (dock != null)
            {
                AddSurface(dock.transform.Find("Deck"), SurfaceType.Wood);
                AddSurface(dock.transform.Find("Gangway"), SurfaceType.Metal);
            }

            AddSurface(GameObject.Find("World/SafeRoom")?.transform.Find("Foundation"), SurfaceType.Concrete);
            AddSurface(GameObject.Find("World/RadioTower")?.transform.Find("CompoundPad"), SurfaceType.Concrete);
        }

        private static void AddSurface(Transform target, SurfaceType type)
        {
            if (target == null)
            {
                return;
            }

            SurfaceIdentifier identifier = target.GetComponent<SurfaceIdentifier>();
            if (identifier == null)
            {
                identifier = target.gameObject.AddComponent<SurfaceIdentifier>();
            }

            identifier.Configure(type);
        }

        private static void AddAmbientAudio()
        {
            Transform world = GameObject.Find("World")?.transform;
            if (world == null)
            {
                return;
            }

            Transform audioRoot = new GameObject("AmbientAudio").transform;
            audioRoot.SetParent(world, false);

            CreateLoopingSource(
                audioRoot,
                "SafeRoomGeneratorHum",
                new Vector3(4.3f, 4.25f, -73f),
                "Generator_Hum",
                0.1f,
                2f,
                13f);
            CreateLoopingSource(
                audioRoot,
                "DockTransformerHum",
                new Vector3(-5.5f, 3.7f, -86f),
                "Generator_Hum",
                0.07f,
                2f,
                11f);
            CreateLoopingSource(
                audioRoot,
                "TowerShedHum",
                new Vector3(2.5f, 25.1f, 73f),
                "Generator_Hum",
                0.06f,
                2f,
                10f);
        }

        private static void AddCrates(GameObject cratePrefab)
        {
            Transform group = new GameObject("DestructibleCrates").transform;
            group.SetParent(GameObject.Find("World")?.transform, false);

            CreateCrateInstance(group, cratePrefab, new Vector3(5.2f, 3.2f, -72.5f), 18f);
            CreateCrateInstance(group, cratePrefab, new Vector3(-4.4f, 3.0f, -89f), -24f);
            CreateCrateInstance(group, cratePrefab, new Vector3(-57.5f, 8.2f, 11f), 32f);
            CreateCrateInstance(group, cratePrefab, new Vector3(57.5f, 10.2f, 19f), -12f);
            CreateCrateInstance(group, cratePrefab, new Vector3(4.4f, 24.2f, 73.5f), 8f);
        }

        private static void CreateCrateInstance(Transform parent, GameObject prefab, Vector3 position, float yaw)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = "SupplyCrate";
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        }

        private static void AddEnemies(GameObject standardPrefab, GameObject wardenPrefab)
        {
            Transform world = GameObject.Find("World")?.transform;
            if (world == null)
            {
                throw new InvalidOperationException("World root missing.");
            }

            Transform group = new GameObject("Enemies").transform;
            group.SetParent(world, false);

            CreateEnemy(
                group,
                standardPrefab,
                "Sentry_01",
                "SentrySpawn_01",
                new[]
                {
                    new Vector3(-20f, 0f, 6f),
                    new Vector3(-13f, 0f, 14f),
                    new Vector3(-23f, 0f, 20f),
                    new Vector3(-30f, 0f, 11f)
                });
            CreateEnemy(
                group,
                standardPrefab,
                "Sentry_02",
                "SentrySpawn_02",
                new[]
                {
                    new Vector3(40f, 0f, 16f),
                    new Vector3(32f, 0f, 24f),
                    new Vector3(39f, 0f, 34f),
                    new Vector3(48f, 0f, 26f)
                });
            CreateEnemy(
                group,
                standardPrefab,
                "Sentry_03",
                "SentrySpawn_03",
                new[]
                {
                    new Vector3(-6f, 0f, 38f),
                    new Vector3(-16f, 0f, 44f),
                    new Vector3(-8f, 0f, 52f),
                    new Vector3(2f, 0f, 44f)
                });
            CreateEnemy(
                group,
                standardPrefab,
                "Sentry_04",
                "SentrySpawn_04",
                new[]
                {
                    new Vector3(9f, 0f, 61f),
                    new Vector3(18f, 0f, 64f),
                    new Vector3(8f, 0f, 70f),
                    new Vector3(-2f, 0f, 64f)
                });
            CreateEnemy(
                group,
                wardenPrefab,
                "Sentry_Warden",
                "SentrySpawn_Heavy",
                new[]
                {
                    new Vector3(0f, 0f, 69f),
                    new Vector3(9f, 0f, 66f),
                    new Vector3(4f, 0f, 74f),
                    new Vector3(-6f, 0f, 70f)
                });
        }

        private static void CreateEnemy(
            Transform parent,
            GameObject prefab,
            string name,
            string markerName,
            Vector3[] routePoints)
        {
            Transform marker = GameObject.Find($"World/MissionMarkers/{markerName}")?.transform;
            if (marker == null)
            {
                throw new InvalidOperationException($"Missing enemy spawn marker {markerName}.");
            }

            Vector3 spawn = SampleNavMesh(marker.position);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;

            Transform route = new GameObject($"{name}_Route").transform;
            route.SetParent(parent, false);
            Transform[] patrol = new Transform[routePoints.Length];

            for (int i = 0; i < routePoints.Length; i++)
            {
                Vector3 point = SampleNavMesh(routePoints[i]);
                Transform waypoint = new GameObject($"{name}_Point_{i + 1}").transform;
                waypoint.SetParent(route, false);
                waypoint.position = point;
                patrol[i] = waypoint;
            }

            Vector3 facing = patrol[0].position - spawn;
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(facing.normalized, Vector3.up)
                : Quaternion.identity;

            instance.transform.SetPositionAndRotation(spawn, rotation);
            SetObjectArrayField(instance.GetComponent<SentryBotAI>(), "patrolPoints", patrol);
        }

        private static Vector3 SampleNavMesh(Vector3 raw)
        {
            Vector3 probe = new Vector3(raw.x, raw.y + 2f, raw.z);
            if (NavMesh.SamplePosition(probe, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                return hit.position + Vector3.up * 0.05f;
            }

            return raw;
        }

        private static void AddMusicDirector()
        {
            GameObject systems = GameObject.Find("Systems");
            if (systems == null)
            {
                systems = new GameObject("Systems");
            }

            GameObject music = new GameObject("MusicDirector");
            music.transform.SetParent(systems.transform, false);
            MusicDirector director = music.AddComponent<MusicDirector>();
            SetStringField(director, "initialTrack", "Ambient");
            SetBoolField(director, "followGameplay", true);
        }

        private static void BuildHud()
        {
            GameObject canvasObject = SignalLostPhase4Ui.CreateCanvas("HUD_Canvas", 20).gameObject;
            Transform canvas = canvasObject.transform;
            SignalLostPhase4Ui.CreateEventSystem(canvas);

            RectTransform hudRootRect = SignalLostPhase4Ui.CreateRect(
                canvas,
                "HUD_Root",
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            hudRootRect.offsetMin = Vector2.zero;
            hudRootRect.offsetMax = Vector2.zero;
            CanvasGroup hudGroup = hudRootRect.gameObject.AddComponent<CanvasGroup>();
            Transform hud = hudRootRect;

            Color cyan = new Color(0.32f, 0.94f, 1f);
            Color pale = new Color(0.9f, 0.97f, 1f);
            Color muted = new Color(0.62f, 0.8f, 0.84f);
            Color panelColor = new Color(0.045f, 0.11f, 0.14f, 0.88f);

            Image healthPanel = SignalLostPhase4Ui.CreatePanel(
                hud, "HealthPanel",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(36f, -36f), new Vector2(340f, 112f),
                "PanelGlass", panelColor);
            SignalLostPhase4Ui.CreateText(
                healthPanel.transform, "Label", "VITAL SIGNS", SignalLostPhase4Ui.HeaderFont, 18, cyan,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -14f), new Vector2(280f, 24f));

            Image healthFrame = SignalLostPhase4Ui.CreatePanel(
                healthPanel.transform, "HealthFrame",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -48f), new Vector2(296f, 30f),
                "PanelRect", new Color(0.01f, 0.03f, 0.04f, 0.95f));

            RectTransform fillRect = SignalLostPhase4Ui.CreateRect(
                healthFrame.transform, "HealthFill",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            Image healthFill = fillRect.gameObject.AddComponent<Image>();
            healthFill.color = new Color(0.2f, 0.95f, 0.7f);
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = 0;
            healthFill.fillAmount = 1f;
            healthFill.raycastTarget = false;

            Text healthText = SignalLostPhase4Ui.CreateText(
                healthPanel.transform, "HealthText", "100 / 100", SignalLostPhase4Ui.NumericFont, 20, pale,
                TextAnchor.MiddleRight, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -80f), new Vector2(296f, 24f));

            Image timerPanel = SignalLostPhase4Ui.CreatePanel(
                hud, "TimerPanel",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(286f, 126f),
                "PanelGlassNotches", new Color(0.035f, 0.1f, 0.13f, 0.9f));
            SignalLostPhase4Ui.CreateText(
                timerPanel.transform, "TimerLabel", "LOCKDOWN", SignalLostPhase4Ui.HeaderFont, 18, muted,
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -14f), new Vector2(0f, 24f));
            Text timerText = SignalLostPhase4Ui.CreateText(
                timerPanel.transform, "TimerText", "10:00", SignalLostPhase4Ui.NumericFont, 52, new Color(0.78f, 0.95f, 1f),
                TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f), new Vector2(0f, 0f));
            SignalLostPhase4Ui.AddTextShadow(timerText, new Color(0f, 0f, 0f, 0.7f), new Vector2(2f, -2f));

            Image objectivePanel = SignalLostPhase4Ui.CreatePanel(
                hud, "ObjectivePanel",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-36f, -36f), new Vector2(460f, 164f),
                "PanelRectScrews", panelColor);
            SignalLostPhase4Ui.CreateText(
                objectivePanel.transform, "ObjectiveLabel", "CURRENT OBJECTIVE", SignalLostPhase4Ui.HeaderFont, 18, cyan,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -16f), new Vector2(400f, 24f));
            Text objectiveText = SignalLostPhase4Ui.CreateText(
                objectivePanel.transform, "ObjectiveText", "Talk to Dr. Reyes", SignalLostPhase4Ui.BodyFont, 24, pale,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -50f), new Vector2(-48f, 62f));
            Text fuseText = SignalLostPhase4Ui.CreateText(
                objectivePanel.transform, "FuseText", "FUSES  recovered 0/3   installed 0/3", SignalLostPhase4Ui.NumericFont, 17, muted,
                TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 18f), new Vector2(-48f, 24f));

            Image ammoPanel = SignalLostPhase4Ui.CreatePanel(
                hud, "AmmoPanel",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-36f, 36f), new Vector2(360f, 112f),
                "PanelGlass", panelColor);
            SignalLostPhase4Ui.CreateText(
                ammoPanel.transform, "AmmoLabel", "ARC PISTOL  //  AMMO", SignalLostPhase4Ui.HeaderFont, 17, muted,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -14f), new Vector2(-40f, 24f));
            Text ammoText = SignalLostPhase4Ui.CreateText(
                ammoPanel.transform, "AmmoText", "15  /  060", SignalLostPhase4Ui.NumericFont, 38, cyan,
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -44f), new Vector2(-40f, 44f));

            Image medkitPanel = SignalLostPhase4Ui.CreatePanel(
                hud, "MedkitPanel",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-36f, 160f), new Vector2(360f, 54f),
                "PanelRect", new Color(0.035f, 0.1f, 0.13f, 0.8f));
            Text medkitText = SignalLostPhase4Ui.CreateText(
                medkitPanel.transform, "MedkitText", "MEDKITS  2    [H] Use", SignalLostPhase4Ui.NumericFont, 19, pale,
                TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                new Vector2(18f, 0f), new Vector2(-36f, 0f));

            Image promptPanelImage = SignalLostPhase4Ui.CreatePanel(
                hud, "PromptPanel",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 94f), new Vector2(880f, 56f),
                "PanelGlass", new Color(0.03f, 0.09f, 0.11f, 0.88f));
            Text promptText = SignalLostPhase4Ui.CreateText(
                promptPanelImage.transform, "PromptText", "[E] Interact", SignalLostPhase4Ui.BodyFont, 23, pale,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            
            Image crosshair = SignalLostPhase4Ui.CreatePanel(
                hud, "Crosshair",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(34f, 34f),
                "CrosshairWhite", new Color(0.55f, 0.8f, 0.9f, 0.7f));
            crosshair.raycastTarget = false;

            Image notificationPanel = SignalLostPhase4Ui.CreatePanel(
                hud, "NotificationPanel",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -178f), new Vector2(960f, 64f),
                "PanelGlass", new Color(0.02f, 0.08f, 0.1f, 0.82f));
            CanvasGroup notificationGroup = notificationPanel.gameObject.AddComponent<CanvasGroup>();
            notificationGroup.alpha = 0f;
            notificationGroup.blocksRaycasts = false;
            Text notificationText = SignalLostPhase4Ui.CreateText(
                notificationPanel.transform, "NotificationText", "", SignalLostPhase4Ui.BodyFont, 22, pale,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(-28f, -8f));

            Image damageOverlay = SignalLostPhase4Ui.CreateFullScreenImage(
                hud, "DamageOverlay", new Color(0.8f, 0.05f, 0.02f, 0f), false);

            GameObject keypad = BuildKeypadPanel(hud, out KeypadPanelUI keypadComponent);
            GameObject dialogue = BuildDialoguePanel(hud, out Text dialogueSpeaker, out Text dialogueBody);
            GameObject pause = BuildPausePanel(hud);
            GameObject victory = BuildVictoryPanel(hud, out Text victoryDetail);
            GameObject defeat = BuildDefeatPanel(hud, out Text defeatDetail);

            GameObject controllerObject = new GameObject("HUD_Controller");
            controllerObject.transform.SetParent(canvas, false);
            SignalLostHUD hudComponent = controllerObject.AddComponent<SignalLostHUD>();

            SetObjectField(hudComponent, "hudGroup", hudGroup);
            SetObjectField(hudComponent, "healthFill", healthFill);
            SetObjectField(hudComponent, "healthText", healthText);
            SetObjectField(hudComponent, "timerText", timerText);
            SetObjectField(hudComponent, "objectiveText", objectiveText);
            SetObjectField(hudComponent, "fuseText", fuseText);
            SetObjectField(hudComponent, "medkitText", medkitText);
            SetObjectField(hudComponent, "ammoText", ammoText);
            SetObjectField(hudComponent, "promptPanel", promptPanelImage.gameObject);
            SetObjectField(hudComponent, "promptText", promptText);
            SetObjectField(hudComponent, "crosshair", crosshair);
            SetObjectField(hudComponent, "damageOverlay", damageOverlay);
            SetObjectField(hudComponent, "notificationGroup", notificationGroup);
            SetObjectField(hudComponent, "notificationText", notificationText);
            SetObjectField(hudComponent, "pausePanel", pause);
            SetObjectField(hudComponent, "victoryPanel", victory);
            SetObjectField(hudComponent, "defeatPanel", defeat);
            SetObjectField(hudComponent, "victoryDetailText", victoryDetail);
            SetObjectField(hudComponent, "defeatDetailText", defeatDetail);
            SetObjectField(hudComponent, "dialoguePanel", dialogue);
            SetObjectField(hudComponent, "dialogueSpeakerText", dialogueSpeaker);
            SetObjectField(hudComponent, "dialogueBodyText", dialogueBody);
            SetObjectField(hudComponent, "keypadPanel", keypadComponent);

            WireHudButtons(hudComponent, pause, victory, defeat, dialogue, keypad);
        }

        private static GameObject BuildKeypadPanel(Transform hud, out KeypadPanelUI keypadComponent)
        {
            Image overlay = SignalLostPhase4Ui.CreateFullScreenImage(hud, "KeypadOverlay", new Color(0f, 0f, 0f, 0.68f), true);
            keypadComponent = overlay.gameObject.AddComponent<KeypadPanelUI>();

            Image panel = SignalLostPhase4Ui.CreatePanel(
                overlay.transform, "KeypadPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(470f, 670f),
                "PanelRectScrews", new Color(0.03f, 0.08f, 0.1f, 0.99f));

            SignalLostPhase4Ui.CreateText(
                panel.transform, "Title", "ARMORY KEYPAD", SignalLostPhase4Ui.HeaderFont, 25, new Color(0.32f, 0.94f, 1f),
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -26f), new Vector2(-40f, 32f));
            Text display = SignalLostPhase4Ui.CreateText(
                panel.transform, "Display", "-- --", SignalLostPhase4Ui.NumericFont, 54, new Color(0.75f, 0.95f, 1f),
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -78f), new Vector2(-72f, 74f));
            Image displayFrame = SignalLostPhase4Ui.CreatePanel(
                panel.transform, "DisplayFrame",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -92f), new Vector2(344f, 92f),
                "PanelRect", new Color(0.01f, 0.04f, 0.05f, 0.95f));
            displayFrame.transform.SetAsFirstSibling();
            Text status = SignalLostPhase4Ui.CreateText(
                panel.transform, "Status", "ENTER ACCESS CODE", SignalLostPhase4Ui.NumericFont, 18, new Color(0.65f, 0.85f, 0.9f),
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -196f), new Vector2(-60f, 26f));

            string[] labels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLR", "0", "ENTER" };
            for (int i = 0; i < labels.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                float x = -130f + column * 130f;
                float y = -252f - row * 84f;
                Button button = SignalLostPhase4Ui.CreateButton(
                    panel.transform,
                    $"Key_{labels[i]}",
                    labels[i],
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(x, y), new Vector2(112f, 68f), 25);

                if (labels[i] == "CLR")
                {
                    SignalLostPhase4Ui.SetOnClick(button, keypadComponent.ClearCode);
                }
                else if (labels[i] == "ENTER")
                {
                    SignalLostPhase4Ui.SetOnClick(button, keypadComponent.Submit);
                }
                else
                {
                    UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
                        button.onClick,
                        keypadComponent.AppendDigit,
                        labels[i]);
                }
            }

            Button close = SignalLostPhase4Ui.CreateButton(
                panel.transform,
                "Close",
                "X",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-18f, -14f), new Vector2(48f, 42f), 20);
            SignalLostPhase4Ui.SetOnClick(close, keypadComponent.Close);

            SetObjectField(keypadComponent, "root", overlay.gameObject);
            SetObjectField(keypadComponent, "displayText", display);
            SetObjectField(keypadComponent, "statusText", status);
            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        private static GameObject BuildDialoguePanel(Transform hud, out Text speakerText, out Text bodyText)
        {
            Image overlay = SignalLostPhase4Ui.CreateFullScreenImage(hud, "DialogueOverlay", new Color(0f, 0f, 0f, 0.58f), true);
            Image panel = SignalLostPhase4Ui.CreatePanel(
                overlay.transform, "DialoguePanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(940f, 450f),
                "PanelRectScrews", new Color(0.025f, 0.075f, 0.1f, 0.98f));

            speakerText = SignalLostPhase4Ui.CreateText(
                panel.transform, "Speaker", "DR. REYES", SignalLostPhase4Ui.HeaderFont, 30, new Color(0.32f, 0.94f, 1f),
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -38f), new Vector2(-96f, 42f));
            bodyText = SignalLostPhase4Ui.CreateText(
                panel.transform, "Body", "", SignalLostPhase4Ui.BodyFont, 25, new Color(0.88f, 0.96f, 1f),
                TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -105f), new Vector2(-96f, 220f));

            Button close = SignalLostPhase4Ui.CreateButton(
                panel.transform, "Continue", "CONTINUE",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-48f, 34f), new Vector2(210f, 58f), 22);
            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        private static GameObject BuildPausePanel(Transform hud)
        {
            Image overlay = SignalLostPhase4Ui.CreateFullScreenImage(hud, "PauseOverlay", new Color(0f, 0f, 0f, 0.72f), true);
            Image panel = SignalLostPhase4Ui.CreatePanel(
                overlay.transform, "PausePanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(560f, 500f),
                "PanelRectScrews", new Color(0.025f, 0.075f, 0.1f, 0.99f));

            SignalLostPhase4Ui.CreateText(
                panel.transform, "Title", "PAUSED", SignalLostPhase4Ui.HeaderFont, 54, new Color(0.82f, 0.97f, 1f),
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -46f), new Vector2(-40f, 70f));
            SignalLostPhase4Ui.CreateText(
                panel.transform, "Subtitle", "Signal timer suspended", SignalLostPhase4Ui.BodyFont, 20, new Color(0.62f, 0.8f, 0.84f),
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -122f), new Vector2(-40f, 30f));

            Button resume = SignalLostPhase4Ui.CreateButton(
                panel.transform, "Resume", "RESUME",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -190f), new Vector2(360f, 64f), 25);
            Button restart = SignalLostPhase4Ui.CreateButton(
                panel.transform, "Restart", "RESTART MISSION",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -270f), new Vector2(360f, 64f), 25);
            Button menu = SignalLostPhase4Ui.CreateButton(
                panel.transform, "MainMenu", "RETURN TO MENU",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -350f), new Vector2(360f, 64f), 25);

            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        private static GameObject BuildVictoryPanel(Transform hud, out Text detailText)
        {
            Image overlay = SignalLostPhase4Ui.CreateFullScreenImage(hud, "VictoryOverlay", new Color(0.01f, 0.06f, 0.07f, 0.8f), true);
            Image panel = SignalLostPhase4Ui.CreatePanel(
                overlay.transform, "VictoryPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(680f, 540f),
                "PanelRectScrews", new Color(0.025f, 0.085f, 0.1f, 0.99f));

            SignalLostPhase4Ui.CreateText(
                panel.transform, "Title", "SIGNAL RESTORED", SignalLostPhase4Ui.HeaderFont, 45, new Color(0.3f, 1f, 0.6f),
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -52f), new Vector2(-40f, 64f));
            detailText = SignalLostPhase4Ui.CreateText(
                panel.transform, "Detail", "", SignalLostPhase4Ui.BodyFont, 22, new Color(0.86f, 0.96f, 1f),
                TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -140f), new Vector2(-100f, 90f));

            Button replay = SignalLostPhase4Ui.CreateButton(
                panel.transform, "Replay", "PLAY AGAIN",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -282f), new Vector2(360f, 64f), 24);
            Button menu = SignalLostPhase4Ui.CreateButton(
                panel.transform, "MainMenu", "RETURN TO MENU",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -362f), new Vector2(360f, 64f), 24);

            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        private static GameObject BuildDefeatPanel(Transform hud, out Text detailText)
        {
            Image overlay = SignalLostPhase4Ui.CreateFullScreenImage(hud, "DefeatOverlay", new Color(0.09f, 0.015f, 0.01f, 0.82f), true);
            Image panel = SignalLostPhase4Ui.CreatePanel(
                overlay.transform, "DefeatPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(680f, 540f),
                "PanelRectScrews", new Color(0.09f, 0.04f, 0.035f, 0.99f));

            SignalLostPhase4Ui.CreateText(
                panel.transform, "Title", "SIGNAL LOST", SignalLostPhase4Ui.HeaderFont, 50, new Color(1f, 0.36f, 0.26f),
                TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -52f), new Vector2(-40f, 64f));
            detailText = SignalLostPhase4Ui.CreateText(
                panel.transform, "Detail", "", SignalLostPhase4Ui.BodyFont, 22, new Color(0.96f, 0.88f, 0.84f),
                TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -140f), new Vector2(-100f, 90f));

            Button retry = SignalLostPhase4Ui.CreateButton(
                panel.transform, "Retry", "RETRY MISSION",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -282f), new Vector2(360f, 64f), 24);
            Button menu = SignalLostPhase4Ui.CreateButton(
                panel.transform, "MainMenu", "RETURN TO MENU",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -362f), new Vector2(360f, 64f), 24);

            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        private static void WireHudButtons(
            SignalLostHUD hud,
            GameObject pause,
            GameObject victory,
            GameObject defeat,
            GameObject dialogue,
            GameObject keypad)
        {
            Button pauseResume = pause.transform.Find("PausePanel/Resume")?.GetComponent<Button>();
            Button pauseRestart = pause.transform.Find("PausePanel/Restart")?.GetComponent<Button>();
            Button pauseMenu = pause.transform.Find("PausePanel/MainMenu")?.GetComponent<Button>();
            Button victoryReplay = victory.transform.Find("VictoryPanel/Replay")?.GetComponent<Button>();
            Button victoryMenu = victory.transform.Find("VictoryPanel/MainMenu")?.GetComponent<Button>();
            Button defeatRetry = defeat.transform.Find("DefeatPanel/Retry")?.GetComponent<Button>();
            Button defeatMenu = defeat.transform.Find("DefeatPanel/MainMenu")?.GetComponent<Button>();
            Button dialogueContinue = dialogue.transform.Find("DialoguePanel/Continue")?.GetComponent<Button>();

            ReplaceClick(pauseResume, hud.ResumeGame);
            ReplaceClick(pauseRestart, hud.RestartGame);
            ReplaceClick(pauseMenu, hud.ReturnToMenu);
            ReplaceClick(victoryReplay, hud.RestartGame);
            ReplaceClick(victoryMenu, hud.ReturnToMenu);
            ReplaceClick(defeatRetry, hud.RestartGame);
            ReplaceClick(defeatMenu, hud.ReturnToMenu);
            ReplaceClick(dialogueContinue, hud.CloseDialogue);
        }

        private static void ReplaceClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
            }

            SignalLostPhase4Ui.SetOnClick(button, action);
        }

        private static SignalLostHUD FindHud()
        {
            return Object.FindAnyObjectByType<SignalLostHUD>(FindObjectsInactive.Include);
        }

        private static void SaveGameplayScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SignalLostPhase3SceneBuilder.ScenePath))
            {
                throw new InvalidOperationException("Failed to save Phase 4 gameplay scene.");
            }
        }

        private static void BuildMainMenu()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureMenuLighting();

            Transform menuWorld = new GameObject("MenuWorld").transform;
            BuildMenuSeascape(menuWorld);

            GameObject systems = new GameObject("MenuSystems");
            SignalLostPhase4Ui.CreateEventSystem(systems.transform);
            MainMenuUI menuUi = systems.AddComponent<MainMenuUI>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 3.8f, -20f);
            cameraObject.transform.LookAt(new Vector3(-8f, 5.5f, 30f));
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 400f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<MenuCameraDrift>();

            GameObject musicObject = new GameObject("MenuMusic");
            musicObject.transform.SetParent(systems.transform, false);
            MusicDirector music = musicObject.AddComponent<MusicDirector>();
            SetStringField(music, "initialTrack", "Menu");
            SetBoolField(music, "followGameplay", false);
            SetFloatField(music, "volume", 0.34f);

            BuildMenuUi(systems.transform, menuUi, out GameObject controlsPanel);
            SetObjectField(menuUi, "controlsPanel", controlsPanel);
            SetStringField(menuUi, "gameplaySceneName", "IslandExterior");

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, MainMenuScenePath))
            {
                throw new InvalidOperationException("Failed to save main menu scene.");
            }
        }

        private static void ConfigureMenuLighting()
        {
            Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
            }

            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.8f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.006f;
            RenderSettings.fogColor = new Color(0.45f, 0.6f, 0.65f);
            QualitySettings.shadowDistance = 150f;

            GameObject sunObject = new GameObject("MenuSun");
            sunObject.transform.rotation = Quaternion.Euler(38f, -52f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.82f, 0.64f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;
            RenderSettings.sun = sun;

            GameObject fillObject = new GameObject("MenuFill");
            fillObject.transform.rotation = Quaternion.Euler(30f, 140f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.42f, 0.62f, 0.82f);
            fill.intensity = 0.28f;
            fill.shadows = LightShadows.None;
        }

        private static void BuildMenuSeascape(Transform parent)
        {
            GameObject waterPlane = CreatePrimitive(
                parent,
                "Ocean",
                PrimitiveType.Plane,
                Vector3.zero,
                new Vector3(90f, 1f, 90f),
                water);
            RemoveCollider(waterPlane);
            Renderer waterRenderer = waterPlane.GetComponent<Renderer>();
            waterRenderer.shadowCastingMode = ShadowCastingMode.Off;
            waterRenderer.receiveShadows = false;

            RemoveCollider(CreatePrimitive(
                parent,
                "IslandBase",
                PrimitiveType.Sphere,
                new Vector3(0f, -5f, 30f),
                new Vector3(76f, 14f, 52f),
                rock));
            RemoveCollider(CreatePrimitive(
                parent,
                "IslandGrass",
                PrimitiveType.Sphere,
                new Vector3(0f, 1f, 30f),
                new Vector3(58f, 7.5f, 38f),
                grass));
            RemoveCollider(CreatePrimitive(
                parent,
                "CliffLeft",
                PrimitiveType.Sphere,
                new Vector3(-29f, -1f, 44f),
                new Vector3(26f, 11f, 22f),
                rock));
            RemoveCollider(CreatePrimitive(
                parent,
                "CliffRight",
                PrimitiveType.Sphere,
                new Vector3(30f, -2f, 41f),
                new Vector3(22f, 8f, 19f),
                rock));
            RemoveCollider(CreatePrimitive(
                parent,
                "ShoreRock",
                PrimitiveType.Sphere,
                new Vector3(-11f, -0.5f, 15f),
                new Vector3(9f, 3f, 6f),
                rock));

            CreateMenuModel(parent, "PalmA", "Nature/tree_palmTall", new Vector3(-15f, 3.4f, 25f), Quaternion.Euler(0f, 24f, 0f), 8.4f);
            CreateMenuModel(parent, "PalmB", "Nature/tree_palmBend", new Vector3(12f, 3.1f, 24f), Quaternion.Euler(0f, -38f, 0f), 7.4f);
            CreateMenuModel(parent, "PalmC", "Nature/tree_palmDetailedTall", new Vector3(-22f, 2.7f, 32f), Quaternion.Euler(0f, 72f, 0f), 8.8f);
            CreateMenuModel(parent, "PalmD", "Nature/tree_palm", new Vector3(18f, 3f, 34f), Quaternion.Euler(0f, 9f, 0f), 7.8f);
            CreateMenuModel(parent, "PalmE", "Nature/tree_palmTall", new Vector3(-7f, 4.1f, 37f), Quaternion.Euler(0f, 112f, 0f), 7.2f);
            CreateMenuModel(parent, "PalmF", "Nature/tree_palm", new Vector3(5f, 4.2f, 19f), Quaternion.Euler(0f, 53f, 0f), 7.6f);
            CreateMenuModel(parent, "PineA", "Nature/tree_pineRoundA", new Vector3(-18f, 3.2f, 38f), Quaternion.Euler(0f, 10f, 0f), 8.2f);

            CreateMenuModel(parent, "MenuBoat", "Watercraft/boat-speed-f", new Vector3(16.5f, 0.3f, 8f), Quaternion.Euler(0f, 208f, 0f), 5.8f, new Vector3(5f, 3f, 9f));
            CreateMenuModel(parent, "ShoreDrum", "Space/barrel", new Vector3(7.4f, 4.1f, 20f), Quaternion.Euler(0f, 20f, 0f), 1.1f);

            Transform tower = new GameObject("RadioTowerSilhouette").transform;
            tower.SetParent(parent, false);
            CreateMenuBox(tower, "Mast", new Vector3(8f, 12f, 31f), new Vector3(0.35f, 17f, 0.35f), metal);
            CreateMenuBox(tower, "CrossA", new Vector3(8f, 11f, 31f), new Vector3(4.2f, 0.18f, 0.18f), metal);
            CreateMenuBox(tower, "CrossB", new Vector3(8f, 15f, 31f), new Vector3(3.1f, 0.16f, 0.16f), metal);
            CreateMenuBox(tower, "Shed", new Vector3(8f, 4.6f, 31f), new Vector3(4.2f, 2.8f, 3.4f), metalPlates);
            CreateMenuBox(tower, "ShedDoor", new Vector3(8f, 4.2f, 32.72f), new Vector3(1.2f, 2.2f, 0.08f), emissiveWarm);
            RemoveCollider(CreatePrimitive(
                tower,
                "Beacon",
                PrimitiveType.Sphere,
                new Vector3(8f, 20.8f, 31f),
                new Vector3(0.52f, 0.52f, 0.52f),
                emissiveRed));
            CreatePointLight(tower, "BeaconLight", new Vector3(8f, 20.8f, 31f), new Color(1f, 0.16f, 0.08f), 4.5f, 16f);

            CreateMenuModel(parent, "Dish", "Space/satelliteDish_detailed", new Vector3(8f, 18.3f, 31f), Quaternion.Euler(0f, 30f, 0f), 2.4f, new Vector3(3.2f, 2.2f, 3.2f));

            for (int i = 0; i < 4; i++)
            {
                float x = -23f + i * 11f;
                float z = 19f + (i % 2) * 7f;
                CreateMenuModel(
                    parent,
                    $"ShoreRock{i}",
                    i % 2 == 0 ? "Nature/rock_largeA" : "Nature/rock_smallA",
                    new Vector3(x, 1.2f, z),
                    Quaternion.Euler(0f, i * 33f, 0f),
                    i % 2 == 0 ? 1.8f : 1.1f);
            }
        }

        private static void BuildMenuUi(Transform systems, MainMenuUI menuUi, out GameObject controlsPanel)
        {
            Canvas canvas = SignalLostPhase4Ui.CreateCanvas("Menu_Canvas", 10);
            Transform root = canvas.transform;

            SignalLostPhase4Ui.CreateFullScreenImage(root, "MenuShade", new Color(0.01f, 0.025f, 0.04f, 0.42f), false);

            Image panel = SignalLostPhase4Ui.CreatePanel(
                root, "MenuPanel",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(96f, 0f), new Vector2(566f, 830f),
                "PanelGlass", new Color(0.015f, 0.055f, 0.07f, 0.63f));

            SignalLostPhase4Ui.CreatePanel(
                panel.transform, "Accent",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(22f, 0f), new Vector2(4f, 760f),
                null, new Color(0.25f, 0.9f, 0.95f, 0.75f));

            Text title = SignalLostPhase4Ui.CreateText(
                panel.transform, "Title", "PROJECT\nOUTPOST", SignalLostPhase4Ui.HeaderFont, 62,
                new Color(0.9f, 0.98f, 1f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -42f), new Vector2(-80f, 176f));
            title.lineSpacing = 0.82f;
            SignalLostPhase4Ui.AddTextShadow(title, new Color(0f, 0f, 0f, 0.7f), new Vector2(3f, -3f));

            SignalLostPhase4Ui.CreateText(
                panel.transform, "Subtitle", "S I G N A L   L O S T", SignalLostPhase4Ui.BodyFont, 23,
                new Color(0.32f, 0.94f, 1f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -232f), new Vector2(-80f, 36f));

            SignalLostPhase4Ui.CreatePanel(
                panel.transform, "Divider",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -286f), new Vector2(380f, 2f),
                null, new Color(0.25f, 0.9f, 0.95f, 0.45f));

            SignalLostPhase4Ui.CreateText(
                panel.transform, "Tagline", "Restore the beacon.\nSurvive the lockdown.", SignalLostPhase4Ui.BodyFont, 20,
                new Color(0.66f, 0.82f, 0.86f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -314f), new Vector2(-80f, 76f));

            Button play = SignalLostPhase4Ui.CreateButton(
                panel.transform, "Play", "PLAY",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -420f), new Vector2(370f, 66f), 27);
            Button controls = SignalLostPhase4Ui.CreateButton(
                panel.transform, "Controls", "CONTROLS",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -500f), new Vector2(370f, 66f), 27);
            Button quit = SignalLostPhase4Ui.CreateButton(
                panel.transform, "Quit", "QUIT",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -580f), new Vector2(370f, 66f), 27);

            SignalLostPhase4Ui.CreateText(
                panel.transform, "Footer", "IT 402W  //  FINAL PROJECT", SignalLostPhase4Ui.NumericFont, 16,
                new Color(0.45f, 0.64f, 0.68f), TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(48f, 40f), new Vector2(-80f, 28f));

            Image controlsOverlay = SignalLostPhase4Ui.CreateFullScreenImage(
                root, "ControlsOverlay", new Color(0.005f, 0.02f, 0.03f, 0.82f), true);
            controlsPanel = controlsOverlay.gameObject;

            Image controlsBox = SignalLostPhase4Ui.CreatePanel(
                controlsOverlay.transform, "ControlsPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(980f, 720f),
                "PanelRectScrews", new Color(0.02f, 0.07f, 0.09f, 0.99f));

            SignalLostPhase4Ui.CreateText(
                controlsBox.transform, "Title", "CONTROLS", SignalLostPhase4Ui.HeaderFont, 40,
                new Color(0.32f, 0.94f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(-60f, 54f));

            string controlsText =
                "MOVE                 W A S D\n" +
                "LOOK                 MOUSE\n" +
                "SPRINT               LEFT SHIFT\n" +
                "JUMP                 SPACE\n\n" +
                "INTERACT             E\n" +
                "FIRE                 LEFT MOUSE\n" +
                "AIM                  RIGHT MOUSE\n" +
                "RELOAD               R\n" +
                "USE MEDKIT           H\n" +
                "PAUSE                ESC\n\n" +
                "Recover the override fuses, restore the beacon,\n" +
                "and reach the extraction boat before lockdown.";
            SignalLostPhase4Ui.CreateText(
                controlsBox.transform, "Body", controlsText, SignalLostPhase4Ui.BodyFont, 22,
                new Color(0.86f, 0.95f, 0.98f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(90f, -112f), new Vector2(-180f, 500f));

            Button close = SignalLostPhase4Ui.CreateButton(
                controlsBox.transform, "Close", "BACK",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 42f), new Vector2(260f, 60f), 23);

            SignalLostPhase4Ui.SetOnClick(play, menuUi.PlayGame);
            SignalLostPhase4Ui.SetOnClick(controls, menuUi.ShowControls);
            SignalLostPhase4Ui.SetOnClick(quit, menuUi.QuitGame);
            SignalLostPhase4Ui.SetOnClick(close, menuUi.HideControls);

            controlsOverlay.gameObject.SetActive(false);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(SignalLostPhase3SceneBuilder.ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return gameObject;
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            Object.DestroyImmediate(root);

            if (!success || prefab == null)
            {
                throw new InvalidOperationException($"Failed to save prefab {path}.");
            }

            return prefab;
        }

        private static Material GetOrCreateTrailMaterial(string name, Color color)
        {
            string path = MaterialRoot + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                }

                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void CreateMenuBox(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            RemoveCollider(CreatePrimitive(parent, name, PrimitiveType.Cube, position, size, material));
        }

        private static void CreateMenuModel(
            Transform parent,
            string name,
            string modelPath,
            Vector3 position,
            Quaternion rotation,
            float height)
        {
            CreateMenuModel(parent, name, modelPath, position, rotation, height, Vector3.zero);
        }

        private static void CreateMenuModel(
            Transform parent,
            string name,
            string modelPath,
            Vector3 position,
            Quaternion rotation,
            float height,
            Vector3 targetSize)
        {
            GameObject wrapper = InstantiateModel(parent, name, modelPath, position, rotation, out GameObject model);
            Bounds bounds = GetRendererBounds(model);

            if (bounds.size.y <= 0.0001f)
            {
                return;
            }

            if (targetSize.sqrMagnitude > 0.001f)
            {
                Vector3 factor = new Vector3(
                    targetSize.x > 0.001f ? targetSize.x / bounds.size.x : 1f,
                    targetSize.y > 0.001f ? targetSize.y / bounds.size.y : 1f,
                    targetSize.z > 0.001f ? targetSize.z / bounds.size.z : 1f);
                model.transform.localScale = Vector3.Scale(model.transform.localScale, factor);
            }
            else
            {
                model.transform.localScale *= height / bounds.size.y;
            }

            bounds = GetRendererBounds(model);
            Vector3 offset = new Vector3(
                bounds.center.x - wrapper.transform.position.x,
                bounds.min.y - wrapper.transform.position.y,
                bounds.center.z - wrapper.transform.position.z);
            model.transform.position -= offset;
        }

        private static void CreateModel(
            Transform parent,
            string name,
            string modelPath,
            Vector3 position,
            Quaternion rotation,
            Vector3 targetSize,
            bool withCollider,
            bool castShadows,
            bool markStatic)
        {
            GameObject wrapper = InstantiateModel(parent, name, modelPath, position, rotation, out GameObject model);
            Bounds initialBounds = GetRendererBounds(model);

            if (initialBounds.size.y > 0.0001f)
            {
                Vector3 factor = new Vector3(
                    targetSize.x > 0.001f ? targetSize.x / initialBounds.size.x : 1f,
                    targetSize.y > 0.001f ? targetSize.y / initialBounds.size.y : 1f,
                    targetSize.z > 0.001f ? targetSize.z / initialBounds.size.z : 1f);
                model.transform.localScale = Vector3.Scale(model.transform.localScale, factor);
            }

            Bounds bounds = GetRendererBounds(model);
            Vector3 offset = new Vector3(
                bounds.center.x - wrapper.transform.position.x,
                bounds.min.y - wrapper.transform.position.y,
                bounds.center.z - wrapper.transform.position.z);
            model.transform.position -= offset;
            bounds = GetRendererBounds(model);

            if (withCollider)
            {
                Bounds localBounds = WorldBoundsToLocal(wrapper.transform, bounds);
                BoxCollider collider = wrapper.AddComponent<BoxCollider>();
                collider.center = localBounds.center;
                collider.size = new Vector3(
                    Mathf.Abs(localBounds.size.x),
                    Mathf.Abs(localBounds.size.y),
                    Mathf.Abs(localBounds.size.z));
            }

            foreach (Renderer renderer in wrapper.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = castShadows;
            }

            if (markStatic)
            {
                foreach (Transform child in wrapper.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(
                        child.gameObject,
                        StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                }
            }
        }

        private static GameObject InstantiateModel(
            Transform parent,
            string name,
            string modelPath,
            Vector3 position,
            Quaternion rotation,
            out GameObject model)
        {
            string path = ModelRoot + modelPath + ".fbx";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing model: {path}");
            }

            GameObject wrapper = new GameObject(name);
            wrapper.transform.SetParent(parent, false);
            wrapper.transform.SetPositionAndRotation(position, rotation);
            model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wrapper.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = name + "_Visual";
            model.transform.localPosition = Vector3.zero;

            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }

            return wrapper;
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Bounds WorldBoundsToLocal(Transform transform, Bounds worldBounds)
        {
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;
            Vector3 localMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 localMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 local = transform.InverseTransformPoint(corner);
                        localMin = Vector3.Min(localMin, local);
                        localMax = Vector3.Max(localMax, local);
                    }
                }
            }

            return new Bounds((localMin + localMax) * 0.5f, localMax - localMin);
        }

        private static void CreateLoopingSource(
            Transform parent,
            string name,
            Vector3 position,
            string clipName,
            float volume,
            float minDistance,
            float maxDistance)
        {
            GameObject sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(parent, false);
            sourceObject.transform.position = position;

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.clip = Resources.Load<AudioClip>("Audio/SFX/" + clipName);
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 1f;
            source.volume = volume;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.dopplerLevel = 0f;
        }

        private static void CreatePointLight(
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            float intensity,
            float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void SetObjectField(Object target, string fieldName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException($"Field not found: {fieldName} on {target.GetType().Name}");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArrayField(Object target, string fieldName, Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException($"Array field not found: {fieldName} on {target.GetType().Name}");
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolField(Object target, string fieldName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException($"Field not found: {fieldName} on {target.GetType().Name}");
            }

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetIntField(Object target, string fieldName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException($"Field not found: {fieldName} on {target.GetType().Name}");
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloatField(Object target, string fieldName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException($"Field not found: {fieldName} on {target.GetType().Name}");
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStringField(Object target, string fieldName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException($"Field not found: {fieldName} on {target.GetType().Name}");
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}



