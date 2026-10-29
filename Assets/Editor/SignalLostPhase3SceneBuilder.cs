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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SignalLost.EditorTools
{
    public static class SignalLostPhase3SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/SignalLost.unity";
        public const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        public const string NavMeshAssetPath = "Assets/Generated/NavMesh/SignalLostNavMesh.asset";

        private const string ModelRoot = "Assets/ThirdParty/Kenney/Models/";
        private const string SkyboxPath = "Assets/Generated/Materials/M_Skybox.mat";
        private const string HdriPath = "Assets/ThirdParty/PolyHaven/HDRI/KloofendalPartlyCloudy4k.hdr";

        private static Transform worldRoot;
        private static Terrain terrain;
        private static Material concrete;
        private static Material metalPlates;
        private static Material metal;
        private static Material wood;
        private static Material rock;
        private static Material sand;
        private static Material glass;
        private static Material water;
        private static Material emissiveRed;
        private static Material emissiveGreen;
        private static Material emissiveCyan;
        private static Material emissiveWarm;
        private static Material hologram;

        [MenuItem("Signal Lost/Phase 3/Build World Scene")]
        public static void BuildWorld()
        {
            try
            {
                if (EditorApplication.isPlaying)
                {
                    throw new InvalidOperationException("Exit play mode before building the Phase 3 scene.");
                }

                SignalLostWorldAssetFactory.EnsureFolders();
                AssetDatabase.Refresh();
                InitializeMaterials();

                if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) != null)
                {
                    AssetDatabase.DeleteAsset(PlayerPrefabPath);
                }

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                ConfigureLighting();
                worldRoot = Group(null, "World");
                terrain = SignalLostWorldTerrain.Create(worldRoot);
                BuildWater(worldRoot);
                BuildSafeRoom(worldRoot);
                BuildDock(worldRoot);
                BuildBunker(worldRoot, "BunkerA", -64f, 8f, 8.2f, true);
                BuildBunker(worldRoot, "BunkerB", 64f, 24f, 10.2f, false);
                BuildRadioTower(worldRoot);
                BuildShoreBarrier(worldRoot);
                ScatterEnvironment(worldRoot);
                BuildNavigationMarkers(worldRoot);

                GameObject playerPrefab = BuildPlayerPrefab();
                InstantiatePlayer(playerPrefab);
                CreateSystems();
                BakeNavigation();
                SaveSceneAndBuildSettings();

                RenderSettings.fog = true;
                Debug.Log(
                    $"[SignalLostPhase3] PHASE3_WORLD_BUILT scene={ScenePath} "
                    + $"navMesh={NavMesh.CalculateTriangulation().indices.Length / 3} triangles");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SignalLostPhase3] World build failed.");
                Debug.LogException(exception);
                throw;
            }
        }

        private static void InitializeMaterials()
        {
            concrete = SignalLostWorldAssetFactory.GetMaterial("Concrete");
            metalPlates = SignalLostWorldAssetFactory.GetMaterial("MetalPlates");
            metal = SignalLostWorldAssetFactory.GetMaterial("Metal");
            wood = SignalLostWorldAssetFactory.GetMaterial("Wood");
            rock = SignalLostWorldAssetFactory.GetMaterial("Rock");
            sand = SignalLostWorldAssetFactory.GetMaterial("Sand");
            glass = SignalLostWorldAssetFactory.GetMaterial("Glass");
            water = SignalLostWorldAssetFactory.GetMaterial("Water");
            emissiveRed = SignalLostWorldAssetFactory.GetMaterial("EmissiveRed");
            emissiveGreen = SignalLostWorldAssetFactory.GetMaterial("EmissiveGreen");
            emissiveCyan = SignalLostWorldAssetFactory.GetMaterial("EmissiveCyan");
            emissiveWarm = SignalLostWorldAssetFactory.GetMaterial("EmissiveWarm");
            hologram = SignalLostWorldAssetFactory.GetMaterial("Hologram");
        }

        private static void ConfigureLighting()
        {
            RenderSettings.skybox = CreateSkyboxMaterial();
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.05f;
            RenderSettings.reflectionIntensity = 0.85f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0034f;
            RenderSettings.fogColor = new Color(0.61f, 0.7f, 0.73f);
            QualitySettings.shadowDistance = 180f;

            Transform lighting = Group(null, "Lighting");
            GameObject sunObject = new GameObject("Sun");
            sunObject.transform.SetParent(lighting, false);
            sunObject.transform.rotation = Quaternion.Euler(47f, -38f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.76f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.82f;
            sun.shadowBias = 0.04f;
            RenderSettings.sun = sun;

            GameObject fillObject = new GameObject("Cool Fill");
            fillObject.transform.SetParent(lighting, false);
            fillObject.transform.rotation = Quaternion.Euler(32f, 142f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.45f, 0.63f, 0.78f);
            fill.intensity = 0.22f;
            fill.shadows = LightShadows.None;
        }

        private static Material CreateSkyboxMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);

            if (material == null)
            {
                Shader shader = Shader.Find("Skybox/Panoramic");

                if (shader == null)
                {
                    return null;
                }

                material = new Material(shader) { name = "M_Skybox" };
                AssetDatabase.CreateAsset(material, SkyboxPath);
            }

            Texture hdri = AssetDatabase.LoadAssetAtPath<Texture>(HdriPath);
            material.SetTexture("_MainTex", hdri);
            material.SetFloat("_Mapping", 1f);
            material.SetFloat("_ImageType", 0f);
            material.SetFloat("_Exposure", 1.05f);
            material.SetFloat("_Rotation", 28f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void BuildWater(Transform parent)
        {
            GameObject waterObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterObject.name = "Water";
            waterObject.transform.SetParent(parent, false);
            waterObject.transform.position = new Vector3(0f, SignalLostWorldTerrain.WaterLevel, 0f);
            waterObject.transform.localScale = new Vector3(70f, 1f, 70f);
            Object.DestroyImmediate(waterObject.GetComponent<Collider>());
            Renderer renderer = waterObject.GetComponent<Renderer>();
            renderer.sharedMaterial = water;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            MarkStatic(waterObject);
        }

        private static void BuildSafeRoom(Transform parent)
        {
            Transform group = Group(parent, "SafeRoom");
            const float centerX = 0f;
            const float centerZ = -76f;
            const float floorTop = 3.2f;
            const float width = 12f;
            const float depth = 9.5f;
            const float wallHeight = 3.4f;
            const float wallThickness = 0.4f;
            float halfWidth = width * 0.5f;
            float halfDepth = depth * 0.5f;

            CreateBox(
                group,
                "Foundation",
                new Vector3(centerX, floorTop - 0.2f, centerZ),
                new Vector3(width + 0.6f, 0.4f, depth + 0.6f),
                concrete);
            CreateWallX(
                group, "NorthWall", centerZ + halfDepth, floorTop, centerX - halfWidth, centerX + halfWidth,
                wallHeight, wallThickness, concrete, centerX, 2.4f, 2.8f);
            CreateWallX(
                group, "SouthWall", centerZ - halfDepth, floorTop, centerX - halfWidth, centerX + halfWidth,
                wallHeight, wallThickness, concrete, centerX, 2.4f, 2.8f);
            CreateWallZ(
                group, "WestWall", centerX - halfWidth, floorTop, centerZ - halfDepth, centerZ + halfDepth,
                wallHeight, wallThickness, concrete, 0f, 0f, 0f);
            CreateWallZ(
                group, "EastWall", centerX + halfWidth, floorTop, centerZ - halfDepth, centerZ + halfDepth,
                wallHeight, wallThickness, concrete, 0f, 0f, 0f);
            CreateBox(
                group,
                "Roof",
                new Vector3(centerX, floorTop + wallHeight + 0.16f, centerZ),
                new Vector3(width + 0.7f, 0.32f, depth + 0.7f),
                metalPlates,
                true,
                true,
                true);
            CreateBox(
                group,
                "RoofBeamNorth",
                new Vector3(centerX, floorTop + wallHeight - 0.12f, centerZ + halfDepth - 0.05f),
                new Vector3(width + 1.2f, 0.28f, 0.3f),
                metal);
            CreateBox(
                group,
                "RoofBeamSouth",
                new Vector3(centerX, floorTop + wallHeight - 0.12f, centerZ - halfDepth + 0.05f),
                new Vector3(width + 1.2f, 0.28f, 0.3f),
                metal);
            CreateBox(
                group,
                "SkirtingWest",
                new Vector3(centerX - halfWidth + 0.04f, floorTop + 0.12f, centerZ),
                new Vector3(0.12f, 0.24f, depth - 0.2f),
                metal);
            CreateBox(
                group,
                "SkirtingEast",
                new Vector3(centerX + halfWidth - 0.04f, floorTop + 0.12f, centerZ),
                new Vector3(0.12f, 0.24f, depth - 0.2f),
                metal);

            CreateDoor(
                group,
                "SafehouseDoor",
                new Vector3(centerX, floorTop, centerZ + halfDepth),
                new Vector3(2.35f, 2.8f, 0.2f),
                Quaternion.identity,
                false);
            CreateDoorFrame(
                group,
                "NorthDoorFrame",
                new Vector3(centerX, floorTop, centerZ + halfDepth),
                2.7f,
                2.95f,
                0.56f,
                true);
            CreateDoorFrame(
                group,
                "SouthDoorFrame",
                new Vector3(centerX, floorTop, centerZ - halfDepth),
                2.7f,
                2.95f,
                0.56f,
                true);

            const float partitionX = 3.8f;
            CreateWallZ(
                group,
                "ArmoryPartition",
                partitionX,
                floorTop,
                centerZ - halfDepth + 0.05f,
                centerZ + halfDepth - 0.05f,
                3.15f,
                0.3f,
                metalPlates,
                centerZ - 0.7f,
                2.15f,
                2.55f);
            DoorInteractable armoryDoor = CreateDoor(
                group,
                "ArmoryDoor",
                new Vector3(partitionX, floorTop, centerZ - 0.7f),
                new Vector3(0.22f, 2.55f, 2.1f),
                Quaternion.identity,
                true);
            CreateDoorFrame(
                group,
                "ArmoryDoorFrame",
                new Vector3(partitionX, floorTop, centerZ - 0.7f),
                2.5f,
                2.65f,
                0.4f,
                false);

            GameObject keypadBody = CreateBox(
                group,
                "ArmoryKeypadBody",
                new Vector3(partitionX - 0.22f, floorTop + 1.35f, centerZ + 1.35f),
                new Vector3(0.2f, 0.72f, 0.5f),
                metal,
                false,
                true,
                false,
                false);
            GameObject keypadScreen = CreateBox(
                group,
                "KeypadScreen",
                new Vector3(partitionX - 0.34f, floorTop + 1.52f, centerZ + 1.35f),
                new Vector3(0.03f, 0.24f, 0.32f),
                emissiveRed,
                false,
                false,
                false,
                false);
            GameObject keypadRoot = new GameObject("ArmoryKeypad");
            keypadRoot.transform.SetParent(group, false);
            keypadRoot.transform.position = new Vector3(partitionX - 0.22f, floorTop + 1.35f, centerZ + 1.35f);
            BoxCollider keypadTrigger = keypadRoot.AddComponent<BoxCollider>();
            keypadTrigger.isTrigger = true;
            keypadTrigger.size = new Vector3(0.7f, 1.1f, 0.8f);
            KeypadInteractable keypad = keypadRoot.AddComponent<KeypadInteractable>();
            SetObjectField(keypad, "statusRenderer", keypadScreen.GetComponent<Renderer>());
            SetObjectArrayField(keypad, "controlledDoors", new Object[] { armoryDoor });

            CreateModel(
                group, "Workbench", "Survival/workbench",
                new Vector3(-4.65f, floorTop, centerZ + 1.15f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(2.2f, 1.0f, 0.9f), true);
            CreateModel(
                group, "WorkTerminal", "SpaceStation/computer",
                new Vector3(-5.1f, floorTop + 1.0f, centerZ + 0.55f),
                Quaternion.Euler(0f, 150f, 0f),
                new Vector3(0.55f, 0.45f, 0.5f), true);
            CreateBox(
                group,
                "WorkbenchTop",
                new Vector3(-4.65f, floorTop + 1.0f, centerZ + 1.6f),
                new Vector3(0.5f, 0.05f, 0.7f),
                wood);
            CreateAmmoPickup(group, "Ammo_Safehouse", new Vector3(-4.25f, floorTop + 1.08f, centerZ + 1.5f));
            CreateMedkitPickup(group, "Medkit_Safehouse", new Vector3(4.95f, floorTop + 0.06f, centerZ + 2.55f));

            CreateModel(
                group, "ArmoryTable", "SpaceStation/table-large",
                new Vector3(5.15f, floorTop, centerZ - 0.9f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(2.1f, 0.9f, 0.95f), true);
            CreateArcPistolPickup(group, "ArcPistolPickup", new Vector3(5.1f, floorTop + 0.92f, centerZ - 0.9f));
            CreateModel(
                group, "Locker", "SpaceStation/container-tall",
                new Vector3(5.2f, floorTop, centerZ + 2.9f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(0.9f, 2.0f, 0.65f), true);
            CreateModel(
                group, "SafeCrates", "Survival/box-large",
                new Vector3(-5.0f, floorTop, centerZ - 2.9f),
                Quaternion.Euler(0f, 24f, 0f),
                new Vector3(1.0f, 0.65f, 1.25f), true);

            CreateHologramNpc(group, new Vector3(-4.35f, floorTop, centerZ - 1.7f));
            CreateFixture(group, "SafeCeilingLightA", new Vector3(-2.1f, floorTop + 3.05f, centerZ), new Color(1f, 0.75f, 0.46f), 2.2f, 7f);
            CreateFixture(group, "SafeCeilingLightB", new Vector3(2.1f, floorTop + 3.05f, centerZ), new Color(1f, 0.75f, 0.46f), 2.2f, 7f);
            CreateBox(
                group,
                "CeilingStripA",
                new Vector3(-2.1f, floorTop + 3.33f, centerZ),
                new Vector3(1.8f, 0.03f, 0.2f),
                emissiveWarm,
                false,
                false,
                false);

            CreateSign(
                group,
                "SafehouseSign",
                "SAFEHOUSE",
                new Vector3(0f, floorTop + 3.1f, centerZ + halfDepth + 0.24f),
                0f,
                4.6f,
                0.95f,
                metalPlates);
            CreateSign(
                group,
                "ArmorySign",
                "ARMORY",
                new Vector3(partitionX, floorTop + 2.65f, centerZ - 0.7f),
                90f,
                2.3f,
                0.6f,
                metalPlates);

            CreatePathBeacon(group, new Vector3(2.6f, 0f, centerZ + halfDepth + 2.2f));
            CreateBox(
                group,
                "EntranceStep",
                new Vector3(centerX, floorTop - 0.08f, centerZ + halfDepth + 0.65f),
                new Vector3(3.0f, 0.16f, 1.2f),
                concrete);
            CreateBox(
                group,
                "DockStep",
                new Vector3(centerX, floorTop - 0.08f, centerZ - halfDepth - 0.65f),
                new Vector3(3.0f, 0.16f, 1.2f),
                concrete);
        }

        private static void BuildDock(Transform parent)
        {
            Transform group = Group(parent, "Dock");
            const float deckTop = 3.0f;
            const float deckStart = -81f;
            const float deckEnd = -116.5f;
            float deckCenter = (deckStart + deckEnd) * 0.5f;
            float deckLength = Mathf.Abs(deckEnd - deckStart);

            CreateBox(
                group,
                "Deck",
                new Vector3(0f, deckTop - 0.1f, deckCenter),
                new Vector3(4.2f, 0.2f, deckLength),
                wood);
            CreateBox(
                group,
                "DeckEdgeWest",
                new Vector3(-2.02f, deckTop - 0.02f, deckCenter),
                new Vector3(0.18f, 0.12f, deckLength),
                metal);
            CreateBox(
                group,
                "DeckEdgeEast",
                new Vector3(2.02f, deckTop - 0.02f, deckCenter),
                new Vector3(0.18f, 0.12f, deckLength),
                metal);

            for (float z = deckStart + 1.5f; z < deckEnd; z += 5.5f)
            {
                CreateCylinder(
                    group,
                    "PilingWest",
                    new Vector3(-1.85f, 0.65f, z),
                    0.16f,
                    5.3f,
                    wood);
                CreateCylinder(
                    group,
                    "PilingEast",
                    new Vector3(1.85f, 0.65f, z),
                    0.16f,
                    5.3f,
                    wood);
            }

            for (float z = deckStart + 2f; z < deckEnd - 1f; z += 4f)
            {
                CreateBox(
                    group,
                    "RailPostWest",
                    new Vector3(-2.04f, deckTop + 0.55f, z),
                    new Vector3(0.12f, 1.1f, 0.12f),
                    metal);
                CreateBox(
                    group,
                    "RailPostEast",
                    new Vector3(2.04f, deckTop + 0.55f, z),
                    new Vector3(0.12f, 1.1f, 0.12f),
                    metal);
            }

            CreateBox(
                group,
                "RailBeamWest",
                new Vector3(-2.04f, deckTop + 1.05f, deckCenter),
                new Vector3(0.1f, 0.1f, deckLength - 1.2f),
                metal);
            CreateBox(
                group,
                "RailBeamEast",
                new Vector3(2.04f, deckTop + 1.05f, deckCenter),
                new Vector3(0.1f, 0.1f, deckLength - 1.2f),
                metal);

            CreateBox(
                group,
                "Gangway",
                new Vector3(0f, deckTop - 0.03f, -117.4f),
                new Vector3(2.8f, 0.12f, 2.4f),
                metalPlates);
            CreateModel(
                group, "MooringBarrelA", "Survival/barrel",
                new Vector3(-1.45f, deckTop, deckStart + 2.2f),
                Quaternion.Euler(0f, 18f, 0f),
                new Vector3(0.65f, 0.95f, 0.65f), true);
            CreateModel(
                group, "MooringCrate", "Survival/box",
                new Vector3(1.42f, deckTop, deckEnd + 3.1f),
                Quaternion.Euler(0f, -22f, 0f),
                new Vector3(0.8f, 0.5f, 1.0f), true);
            CreatePathBeacon(group, new Vector3(-2.7f, 0f, deckEnd + 7f));
            CreateSign(
                group,
                "DockSign",
                "EXTRACTION",
                new Vector3(3.2f, deckTop + 2.1f, deckStart - 2.4f),
                0f,
                3.8f,
                0.8f,
                metalPlates);

            CreateEscapeBoat(group, new Vector3(0f, 0.55f, -121.2f));
            CreateModel(
                group, "BuoyA", "Watercraft/buoy",
                new Vector3(9f, SignalLostWorldTerrain.WaterLevel, -120f),
                Quaternion.Euler(0f, 20f, 0f),
                new Vector3(0f, 1.5f, 0f), false);
            CreateModel(
                group, "BuoyB", "Watercraft/buoy",
                new Vector3(-10f, SignalLostWorldTerrain.WaterLevel, -123f),
                Quaternion.Euler(0f, -15f, 0f),
                new Vector3(0f, 1.2f, 0f), false);
        }

        private static void BuildBunker(
            Transform parent,
            string name,
            float centerX,
            float centerZ,
            float floorTop,
            bool entranceEast)
        {
            Transform group = Group(parent, name);
            const float width = 18f;
            const float depth = 14f;
            const float wallHeight = 5f;
            const float thickness = 0.5f;
            float halfWidth = width * 0.5f;
            float halfDepth = depth * 0.5f;
            float doorX = centerX + (entranceEast ? halfWidth : -halfWidth);
            float direction = entranceEast ? 1f : -1f;

            CreateBox(
                group,
                "Foundation",
                new Vector3(centerX, floorTop - 0.2f, centerZ),
                new Vector3(width + 0.6f, 0.4f, depth + 0.6f),
                concrete);
            CreateWallX(
                group, "NorthWall", centerZ + halfDepth, floorTop, centerX - halfWidth, centerX + halfWidth,
                wallHeight, thickness, concrete, 0f, 0f, 0f);
            CreateWallX(
                group, "SouthWall", centerZ - halfDepth, floorTop, centerX - halfWidth, centerX + halfWidth,
                wallHeight, thickness, concrete, 0f, 0f, 0f);
            CreateWallZ(
                group, entranceEast ? "WestWall" : "EastWall", centerX - direction * halfWidth, floorTop,
                centerZ - halfDepth, centerZ + halfDepth, wallHeight, thickness, concrete, 0f, 0f, 0f);
            CreateWallZ(
                group, entranceEast ? "EastWall" : "WestWall", doorX, floorTop,
                centerZ - halfDepth, centerZ + halfDepth, wallHeight, thickness, metalPlates,
                centerZ, 3.2f, 3.5f);
            CreateBox(
                group,
                "Roof",
                new Vector3(centerX, floorTop + wallHeight + 0.18f, centerZ),
                new Vector3(width + 0.8f, 0.36f, depth + 0.8f),
                metalPlates,
                true,
                true,
                true);

            for (int i = -1; i <= 1; i += 2)
            {
                CreateBox(
                    group,
                    "RoofRib",
                    new Vector3(centerX + i * 5f, floorTop + wallHeight - 0.08f, centerZ),
                    new Vector3(0.3f, 0.25f, depth + 0.9f),
                    metal);
                CreateBox(
                    group,
                    "InteriorSupport",
                    new Vector3(centerX + i * 5.5f, floorTop + 2.45f, centerZ + i * 3.3f),
                    new Vector3(0.35f, 4.9f, 0.35f),
                    metal);
            }

            DoorInteractable door = CreateDoor(
                group,
                "BlastDoor",
                new Vector3(doorX, floorTop, centerZ),
                new Vector3(0.22f, 3.5f, 3.15f),
                Quaternion.identity,
                true);
            CreateDoorFrame(
                group,
                "BlastDoorFrame",
                new Vector3(doorX, floorTop, centerZ),
                3.6f,
                3.7f,
                0.72f,
                false);

            GameObject breaker = CreateBreaker(
                group,
                "Breaker",
                new Vector3(doorX + direction * 0.4f, floorTop + 1.4f, centerZ + 2.75f),
                entranceEast ? 1f : -1f,
                door);

            CreateModel(
                group, "Generator", "Space/machine_generatorLarge",
                new Vector3(centerX - direction * 4f, floorTop, centerZ - 3.9f),
                Quaternion.Euler(0f, entranceEast ? 90f : -90f, 0f),
                new Vector3(3f, 2.4f, 2.2f), true);
            CreateModel(
                group, "CrateCluster", "Survival/box-large",
                new Vector3(centerX + direction * 5.1f, floorTop, centerZ - 4.1f),
                Quaternion.Euler(0f, 20f, 0f),
                new Vector3(1.15f, 0.75f, 1.45f), true);
            CreateModel(
                group, "CrateStack", "Survival/box",
                new Vector3(centerX + direction * 5.1f, floorTop + 0.75f, centerZ - 4.1f),
                Quaternion.Euler(0f, -12f, 0f),
                new Vector3(1.0f, 0.55f, 1.2f), true);
            CreateModel(
                group, "FuelBarrel", "Survival/barrel",
                new Vector3(centerX - direction * 6.2f, floorTop, centerZ + 3.9f),
                Quaternion.Euler(0f, 0f, 0f),
                new Vector3(0.8f, 1.2f, 0.8f), true);

            float fuseX = centerX - direction * 6.7f;
            float fuseZ = centerZ + 2.8f;
            CreateBox(
                group,
                "FusePedestal",
                new Vector3(fuseX, floorTop + 0.45f, fuseZ),
                new Vector3(1.2f, 0.9f, 1.2f),
                metal);
            CreateBox(
                group,
                "FusePedestalScreen",
                new Vector3(fuseX, floorTop + 0.72f, fuseZ + 0.62f),
                new Vector3(0.5f, 0.25f, 0.03f),
                emissiveCyan,
                false,
                false);
            CreateFusePickup(
                group,
                name == "BunkerA" ? "Fuse_A" : "Fuse_B",
                new Vector3(fuseX, floorTop + 0.95f, fuseZ));
            CreateAmmoPickup(
                group,
                name == "BunkerA" ? "Ammo_BunkerA" : "Ammo_BunkerB",
                new Vector3(fuseX + direction * 1.1f, floorTop + 0.06f, fuseZ - 0.8f));

            CreateFixture(group, "BunkerLightA", new Vector3(centerX - 2.5f, floorTop + 4.2f, centerZ), new Color(0.95f, 0.72f, 0.42f), 2.4f, 8f);
            CreateFixture(group, "BunkerLightB", new Vector3(centerX + 2.5f, floorTop + 4.2f, centerZ), new Color(0.95f, 0.72f, 0.42f), 2.4f, 8f);
            CreateBox(
                group,
                "CeilingStrip",
                new Vector3(centerX, floorTop + 4.84f, centerZ),
                new Vector3(5.5f, 0.04f, 0.24f),
                emissiveWarm,
                false,
                false,
                false);

            CreateBox(
                group,
                "FloorMarkingA",
                new Vector3(centerX - direction * 2.5f, floorTop + 0.02f, centerZ),
                new Vector3(3.5f, 0.02f, 0.18f),
                emissiveWarm,
                false,
                false);
            CreateBox(
                group,
                "FloorMarkingB",
                new Vector3(centerX - direction * 2.5f, floorTop + 0.02f, centerZ),
                new Vector3(0.18f, 0.02f, 3.5f),
                emissiveWarm,
                false,
                false);

            GameObject beacon = CreateBox(
                group,
                "DoorBeacon",
                new Vector3(doorX + direction * 0.32f, floorTop + 4.2f, centerZ - 1.45f),
                new Vector3(0.16f, 0.36f, 0.16f),
                emissiveRed,
                false,
                false,
                false,
                false);
            CreatePointLight(
                group,
                "DoorBeaconLight",
                new Vector3(doorX + direction * 0.32f, floorTop + 4.2f, centerZ - 1.45f),
                new Color(1f, 0.1f, 0.04f),
                0.65f,
                3.2f);
            CreateSign(
                group,
                "BunkerSign",
                name == "BunkerA" ? "BUNKER A" : "BUNKER B",
                new Vector3(doorX + direction * 0.4f, floorTop + 4.25f, centerZ + 1.8f),
                entranceEast ? 90f : -90f,
                3.9f,
                0.8f,
                metalPlates);
            CreatePathBeacon(group, new Vector3(doorX + direction * 2.8f, 0f, centerZ + 4.2f));

            SetObjectField(breaker.GetComponent<PowerBreaker>(), "statusRenderer", breaker.transform.Find("StatusScreen").GetComponent<Renderer>());
        }

        private static void BuildRadioTower(Transform parent)
        {
            Transform group = Group(parent, "RadioTower");
            const float padTop = 24.2f;
            const float centerZ = 76f;

            CreateBox(
                group,
                "CompoundPad",
                new Vector3(0f, padTop - 0.15f, centerZ),
                new Vector3(26f, 0.3f, 26f),
                concrete);
            CreateBox(
                group,
                "PadStripeNorth",
                new Vector3(0f, padTop + 0.012f, centerZ + 10.4f),
                new Vector3(21f, 0.02f, 0.18f),
                emissiveWarm,
                false,
                false);
            CreateBox(
                group,
                "PadStripeSouth",
                new Vector3(0f, padTop + 0.012f, centerZ - 10.4f),
                new Vector3(21f, 0.02f, 0.18f),
                emissiveWarm,
                false,
                false);

            BuildTowerFence(group, padTop, centerZ);
            BuildTowerStructure(group, padTop, centerZ);
            BuildTowerShed(group, padTop, centerZ);

            CreateModel(
                group, "Generator", "Space/machine_generator",
                new Vector3(-9.4f, padTop, centerZ - 2f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(3.2f, 2.4f, 2.4f), true);
            CreateModel(
                group, "WirelessUnit", "Space/machine_wireless",
                new Vector3(8.3f, padTop, centerZ - 1.4f),
                Quaternion.Euler(0f, -90f, 0f),
                new Vector3(1.8f, 2.2f, 1.4f), true);
            CreateModel(
                group, "ControlComputer", "SpaceStation/computer-system",
                new Vector3(5.1f, padTop, centerZ + 2.3f),
                Quaternion.Euler(0f, 140f, 0f),
                new Vector3(1.3f, 1.25f, 0.9f), true);

            CreateSignalConsole(group, new Vector3(2.4f, padTop, centerZ + 3.9f));
            CreateFuseSockets(group, new Vector3(-2.6f, padTop, centerZ + 4.3f));
            CreateSign(
                group,
                "GateSign",
                "RADIO TOWER",
                new Vector3(0f, padTop + 3.45f, centerZ - 12.6f),
                0f,
                5.2f,
                0.95f,
                metalPlates);

            CreatePathBeacon(group, new Vector3(4.2f, 0f, centerZ - 10.5f));
            CreateBox(
                group,
                "TowerBaseStrip",
                new Vector3(0f, padTop + 0.012f, centerZ - 8.2f),
                new Vector3(6f, 0.02f, 0.2f),
                emissiveCyan,
                false,
                false);
        }

        private static void BuildTowerFence(Transform group, float padTop, float centerZ)
        {
            float south = centerZ - 12f;
            float north = centerZ + 12f;
            const float west = -12f;
            const float east = 12f;
            float[] southSegments = { -10f, -7.2f, -4.4f, -2.2f, 2.2f, 4.4f, 7.2f, 10f };

            foreach (float x in southSegments)
            {
                CreateModel(
                    group,
                    "FenceSouth",
                    "Nature/fence_simple",
                    new Vector3(x, padTop, south),
                    Quaternion.Euler(0f, 0f, 0f),
                    new Vector3(2.7f, 1.35f, 0.12f),
                    true);
            }

            for (float z = south + 1.35f; z <= north - 0.5f; z += 2.7f)
            {
                CreateModel(
                    group,
                    "FenceWest",
                    "Nature/fence_simple",
                    new Vector3(west, padTop, z),
                    Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(2.7f, 1.35f, 0.12f),
                    true);
                CreateModel(
                    group,
                    "FenceEast",
                    "Nature/fence_simple",
                    new Vector3(east, padTop, z),
                    Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(2.7f, 1.35f, 0.12f),
                    true);
            }

            for (float x = -10f; x <= 10f; x += 2.7f)
            {
                CreateModel(
                    group,
                    "FenceNorth",
                    "Nature/fence_simple",
                    new Vector3(x, padTop, north),
                    Quaternion.Euler(0f, 0f, 0f),
                    new Vector3(2.7f, 1.35f, 0.12f),
                    true);
            }

            CreateCylinder(group, "GatePostWest", new Vector3(-2.25f, padTop + 1.65f, south), 0.18f, 3.3f, metal);
            CreateCylinder(group, "GatePostEast", new Vector3(2.25f, padTop + 1.65f, south), 0.18f, 3.3f, metal);
            CreateBeamBetween(
                group, "GateHeader",
                new Vector3(-2.25f, padTop + 3.1f, south),
                new Vector3(2.25f, padTop + 3.1f, south),
                0.22f, metal);
        }

        private static void BuildTowerStructure(Transform group, float padTop, float centerZ)
        {
            Vector3[] baseCorners =
            {
                new Vector3(-2.4f, padTop, centerZ + 2.4f),
                new Vector3(2.4f, padTop, centerZ + 2.4f),
                new Vector3(-2.4f, padTop, centerZ - 2.4f),
                new Vector3(2.4f, padTop, centerZ - 2.4f)
            };
            Vector3[] topCorners =
            {
                new Vector3(-0.75f, padTop + 15.4f, centerZ + 0.75f),
                new Vector3(0.75f, padTop + 15.4f, centerZ + 0.75f),
                new Vector3(-0.75f, padTop + 15.4f, centerZ - 0.75f),
                new Vector3(0.75f, padTop + 15.4f, centerZ - 0.75f)
            };

            for (int i = 0; i < 4; i++)
            {
                CreateBeamBetween(group, "TowerLeg", baseCorners[i], topCorners[i], 0.28f, metal);
                CreateBox(
                    group,
                    "TowerFoot",
                    baseCorners[i] + Vector3.up * 0.2f,
                    new Vector3(0.8f, 0.4f, 0.8f),
                    concrete);
            }

            for (int level = 1; level <= 4; level++)
            {
                float t = level / 5f;
                Vector3 a = Vector3.Lerp(baseCorners[0], topCorners[0], t);
                Vector3 b = Vector3.Lerp(baseCorners[1], topCorners[1], t);
                Vector3 c = Vector3.Lerp(baseCorners[2], topCorners[2], t);
                Vector3 d = Vector3.Lerp(baseCorners[3], topCorners[3], t);
                CreateBeamBetween(group, "TowerBrace", a, b, 0.12f, metal);
                CreateBeamBetween(group, "TowerBrace", c, d, 0.12f, metal);
                CreateBeamBetween(group, "TowerBrace", a, c, 0.12f, metal);
                CreateBeamBetween(group, "TowerBrace", b, d, 0.12f, metal);
            }

            CreateBox(
                group,
                "TowerPlatform",
                new Vector3(0f, padTop + 15.45f, centerZ),
                new Vector3(3.3f, 0.22f, 3.3f),
                metalPlates);
            CreateBox(
                group,
                "TowerPlatformRailNorth",
                new Vector3(0f, padTop + 16.0f, centerZ + 1.55f),
                new Vector3(3.3f, 0.09f, 0.09f),
                metal);
            CreateBox(
                group,
                "TowerPlatformRailSouth",
                new Vector3(0f, padTop + 16.0f, centerZ - 1.55f),
                new Vector3(3.3f, 0.09f, 0.09f),
                metal);
            CreateBox(
                group,
                "TowerPlatformRailEast",
                new Vector3(1.55f, padTop + 16.0f, centerZ),
                new Vector3(0.09f, 0.09f, 3.3f),
                metal);
            CreateBox(
                group,
                "TowerPlatformRailWest",
                new Vector3(-1.55f, padTop + 16.0f, centerZ),
                new Vector3(0.09f, 0.09f, 3.3f),
                metal);

            CreateCylinder(group, "AntennaMast", new Vector3(0f, padTop + 19.8f, centerZ), 0.16f, 8.6f, metal);
            CreateBeamBetween(
                group, "AntennaBrace",
                new Vector3(-0.95f, padTop + 16.35f, centerZ),
                new Vector3(0f, padTop + 20.2f, centerZ),
                0.08f, metal);
            CreateBeamBetween(
                group, "AntennaBrace",
                new Vector3(0.95f, padTop + 16.35f, centerZ),
                new Vector3(0f, padTop + 20.2f, centerZ),
                0.08f, metal);
            CreateModel(
                group, "Dish", "Space/satelliteDish_large",
                new Vector3(0.85f, padTop + 15.6f, centerZ + 0.55f),
                Quaternion.Euler(0f, -30f, 0f),
                new Vector3(2.7f, 2.7f, 2.7f), true);
            CreateModel(
                group, "DishSmall", "Space/satelliteDish",
                new Vector3(-0.8f, padTop + 15.6f, centerZ - 0.4f),
                Quaternion.Euler(0f, 120f, 0f),
                new Vector3(1.6f, 1.6f, 1.6f), true);
            CreateSphere(
                group,
                "Beacon",
                new Vector3(0f, padTop + 24.1f, centerZ),
                0.18f,
                emissiveRed,
                false,
                false);
            CreatePointLight(
                group,
                "BeaconLight",
                new Vector3(0f, padTop + 24.0f, centerZ),
                new Color(1f, 0.08f, 0.03f),
                2.4f,
                9f);

            CreateModel(
                group, "Turret", "Space/turret_single",
                new Vector3(-4.3f, padTop, centerZ - 6.1f),
                Quaternion.Euler(0f, 25f, 0f),
                new Vector3(1.4f, 1.5f, 1.4f), true);

            CreateSign(
                group,
                "TransmitterSign",
                "EMERGENCY TRANSMITTER",
                new Vector3(0f, padTop + 4.2f, centerZ + 5.3f),
                180f,
                5.8f,
                0.9f,
                metalPlates);
        }

        private static void BuildTowerShed(Transform group, float padTop, float centerZ)
        {
            const float shedX = -8.5f;
            float shedZ = centerZ + 6f;
            const float width = 8f;
            const float depth = 6f;
            const float height = 3.15f;
            float halfWidth = width * 0.5f;
            float halfDepth = depth * 0.5f;

            CreateWallX(
                group, "ShedNorth", shedZ + halfDepth, padTop, shedX - halfWidth, shedX + halfWidth,
                height, 0.28f, metalPlates, 0f, 0f, 0f);
            CreateWallX(
                group, "ShedSouth", shedZ - halfDepth, padTop, shedX - halfWidth, shedX + halfWidth,
                height, 0.28f, metalPlates, 0f, 0f, 0f);
            CreateWallZ(
                group, "ShedWest", shedX - halfWidth, padTop, shedZ - halfDepth, shedZ + halfDepth,
                height, 0.28f, metalPlates, 0f, 0f, 0f);
            CreateWallZ(
                group, "ShedEast", shedX + halfWidth, padTop, shedZ - halfDepth, shedZ + halfDepth,
                height, 0.28f, metalPlates, shedZ, 2.15f, 2.6f);
            CreateBox(
                group,
                "ShedRoof",
                new Vector3(shedX, padTop + height + 0.15f, shedZ),
                new Vector3(width + 0.7f, 0.3f, depth + 0.7f),
                metalPlates,
                true,
                true,
                true);
            CreateFixture(
                group,
                "ShedLight",
                new Vector3(shedX, padTop + 2.7f, shedZ),
                new Color(0.92f, 0.72f, 0.4f),
                1.7f,
                5f);
            CreateModel(
                group, "CacheBench", "Survival/workbench",
                new Vector3(shedX - 2.2f, padTop, shedZ - 1.2f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(1.9f, 0.9f, 0.8f), true);
            CreateBox(
                group,
                "CacheCrate",
                new Vector3(shedX + 2.3f, padTop + 0.35f, shedZ + 1.2f),
                new Vector3(1.15f, 0.7f, 0.95f),
                metal);
            CreateModel(
                group, "CacheComputer", "SpaceStation/computer-screen",
                new Vector3(shedX + 1.7f, padTop, shedZ - 1.6f),
                Quaternion.Euler(0f, -120f, 0f),
                new Vector3(1.0f, 1.2f, 0.6f), true);
            CreateFusePickup(
                group,
                "Fuse_Tower",
                new Vector3(shedX - 1.5f, padTop + 0.98f, shedZ - 0.8f));
            CreateSign(
                group,
                "CacheSign",
                "RELAY CACHE",
                new Vector3(shedX + halfWidth + 0.25f, padTop + 2.9f, shedZ),
                90f,
                2.8f,
                0.62f,
                metalPlates);
        }

        private static void BuildShoreBarrier(Transform parent)
        {
            Transform group = Group(parent, "InvisibleShoreBarrier");
            const int segments = 64;
            const float radius = 118f;

            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                GameObject barrier = new GameObject($"ShoreBarrier_{i:00}");
                barrier.transform.SetParent(group, false);
                barrier.transform.position = new Vector3(x, 18f, z);
                barrier.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
                BoxCollider collider = barrier.AddComponent<BoxCollider>();
                collider.size = new Vector3(4f, 52f, 11.8f);
                MarkStatic(barrier);
            }
        }

        private static void ScatterEnvironment(Transform parent)
        {
            Random.InitState(4471);
            Transform trees = Group(parent, "Vegetation");
            Transform props = Group(parent, "NaturalProps");
            string[] palms =
            {
                "Nature/tree_palm",
                "Nature/tree_palmTall",
                "Nature/tree_palmBend",
                "Nature/tree_palmDetailedTall"
            };
            string[] pines =
            {
                "Nature/tree_pineRoundA",
                "Nature/tree_pineTallA",
                "Nature/tree_detailed",
                "Nature/tree_default"
            };
            string[] bushes =
            {
                "Nature/plant_bush",
                "Nature/plant_bushDetailed",
                "Nature/plant_bushLarge",
                "Nature/plant_bushSmall"
            };
            string[] grass =
            {
                "Nature/grass",
                "Nature/grass_large"
            };

            int treeCount = 0;
            int attempts = 0;

            while (treeCount < 84 && attempts < 2600)
            {
                attempts++;
                float x = Random.Range(-104f, 104f);
                float z = Random.Range(-108f, 106f);
                float height = SignalLostWorldTerrain.SampleHeight(x, z);

                if (height < 3.1f || height > 27f
                    || SignalLostWorldTerrain.DistanceToPaths(x, z) < 5.5f
                    || SignalLostWorldTerrain.IsNearStructure(x, z, 5.5f)
                    || IsSteep(x, z, 1.15f))
                {
                    continue;
                }

                bool lowland = height < 11f && z < 40f;
                string[] pool = lowland ? palms : pines;
                string path = pool[Random.Range(0, pool.Length)];
                float targetHeight = Random.Range(5.2f, 9.2f);
                GameObject treeObject = CreateUniformModel(
                    trees,
                    $"Tree_{treeCount:000}",
                    path,
                    new Vector3(x, height, z),
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    targetHeight,
                    false);
                CapsuleCollider trunk = treeObject.AddComponent<CapsuleCollider>();
                trunk.center = new Vector3(0f, targetHeight * 0.32f, 0f);
                trunk.height = Mathf.Max(2f, targetHeight * 0.62f);
                trunk.radius = Random.Range(0.24f, 0.42f);
                treeCount++;
            }

            for (int i = 0; i < 76; i++)
            {
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    float x = Random.Range(-105f, 105f);
                    float z = Random.Range(-108f, 108f);
                    float height = SignalLostWorldTerrain.SampleHeight(x, z);

                    if (height < 2.7f || height > 27f
                        || SignalLostWorldTerrain.DistanceToPaths(x, z) < 2.4f
                        || SignalLostWorldTerrain.IsNearStructure(x, z, 3f))
                    {
                        continue;
                    }

                    CreateUniformModel(
                        trees,
                        $"Bush_{i:000}",
                        bushes[Random.Range(0, bushes.Length)],
                        new Vector3(x, height, z),
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                        Random.Range(0.55f, 1.5f),
                        false);
                    break;
                }
            }

            for (int i = 0; i < 130; i++)
            {
                float x = Random.Range(-108f, 108f);
                float z = Random.Range(-112f, 108f);
                float height = SignalLostWorldTerrain.SampleHeight(x, z);

                if (height < 2.4f
                    || SignalLostWorldTerrain.DistanceToPaths(x, z) < 1.8f
                    || SignalLostWorldTerrain.IsNearStructure(x, z, 2.5f))
                {
                    continue;
                }

                CreateUniformModel(
                    trees,
                    $"Grass_{i:000}",
                    grass[Random.Range(0, grass.Length)],
                    new Vector3(x, height, z),
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    Random.Range(0.25f, 0.65f),
                    false);
            }

            for (int i = 0; i < 54; i++)
            {
                for (int attempt = 0; attempt < 60; attempt++)
                {
                    float x = Random.Range(-108f, 108f);
                    float z = Random.Range(-112f, 108f);
                    float height = SignalLostWorldTerrain.SampleHeight(x, z);

                    if (height < 1.4f
                        || SignalLostWorldTerrain.DistanceToPaths(x, z) < 2.2f
                        || SignalLostWorldTerrain.IsNearStructure(x, z, 3f))
                    {
                        continue;
                    }

                    CreateModel(
                        props,
                        $"Rock_{i:000}",
                        i % 3 == 0 ? "Nature/rock_largeA" : "Nature/rock_smallA",
                        new Vector3(x, height, z),
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                        new Vector3(Random.Range(0.8f, 2.8f), Random.Range(0.5f, 1.8f), Random.Range(0.8f, 2.8f)),
                        true);
                    break;
                }
            }

            for (int i = 0; i < 20; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(86f, 107f);
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                float height = SignalLostWorldTerrain.SampleHeight(x, z);

                if (height < 1.2f || SignalLostWorldTerrain.DistanceToPaths(x, z) < 8f)
                {
                    continue;
                }

                CreateModel(
                    props,
                    $"Cliff_{i:00}",
                    i % 2 == 0 ? "Nature/cliff_large_rock" : "Nature/cliff_rock",
                    new Vector3(x, height, z),
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    new Vector3(Random.Range(3.2f, 7.5f), Random.Range(2.2f, 6f), Random.Range(3.2f, 7.5f)),
                    true);
            }

            for (int i = 0; i < 42; i++)
            {
                float x = Random.Range(-90f, 90f);
                float z = Random.Range(-90f, 84f);
                float height = SignalLostWorldTerrain.SampleHeight(x, z);

                if (height < 3f || SignalLostWorldTerrain.DistanceToPaths(x, z) < 2.5f
                    || SignalLostWorldTerrain.IsNearStructure(x, z, 2f))
                {
                    continue;
                }

                CreateUniformModel(
                    trees,
                    $"Flower_{i:000}",
                    i % 3 == 0 ? "Nature/flower_redA" : i % 3 == 1 ? "Nature/flower_purpleA" : "Nature/flower_yellowA",
                    new Vector3(x, height, z),
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    Random.Range(0.16f, 0.32f),
                    false);
            }

            CreateSign(
                trees,
                "ForkSign",
                "SITE 12\nDOCK   A   B   TOWER",
                new Vector3(2.7f, SignalLostWorldTerrain.SampleHeight(2.7f, -37f) + 1.65f, -37f),
                180f,
                5.4f,
                1.75f,
                metalPlates);

            Debug.Log($"[SignalLostPhase3] Scattered {treeCount} trees and environmental props.");
        }

        private static void BuildNavigationMarkers(Transform parent)
        {
            Transform markers = Group(parent, "MissionMarkers");
            CreateMarker(markers, "SentrySpawn_01", -20f, 6f);
            CreateMarker(markers, "SentrySpawn_02", 40f, 16f);
            CreateMarker(markers, "SentrySpawn_03", -6f, 38f);
            CreateMarker(markers, "SentrySpawn_04", 9f, 61f);
            CreateMarker(markers, "SentrySpawn_Heavy", 0f, 69f);
            CreateMarker(markers, "ExtractionPoint", 0f, -118f);
        }

        private static void CreateMarker(Transform parent, string name, float x, float z)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(x, SignalLostWorldTerrain.SampleHeight(x, z) + 0.15f, z);
        }

        private static GameObject BuildPlayerPrefab()
        {
            GameObject root = new GameObject("Player");
            root.tag = "Player";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            CharacterController controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.32f;
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.35f;
            controller.skinWidth = 0.04f;

            PlayerInputReader input = root.AddComponent<PlayerInputReader>();
            FirstPersonController firstPerson = root.AddComponent<FirstPersonController>();
            root.AddComponent<Health>();
            root.AddComponent<PlayerHealth>();
            PlayerInventory inventory = root.AddComponent<PlayerInventory>();
            PlayerInteraction interaction = root.AddComponent<PlayerInteraction>();

            GameObject cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform, false);
            cameraPivot.transform.localPosition = new Vector3(0f, 1.63f, 0f);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameraPivot.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;
            camera.fieldOfView = 72f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            cameraObject.AddComponent<AudioListener>();

            GameObject weaponHolder = new GameObject("WeaponHolder");
            weaponHolder.transform.SetParent(cameraObject.transform, false);
            weaponHolder.transform.localPosition = new Vector3(0.32f, -0.24f, 0.56f);
            weaponHolder.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            GameObject weaponVisual = CreateModel(
                weaponHolder.transform,
                "ArcPistolVisual",
                "Blaster/blaster-b",
                weaponHolder.transform.position,
                weaponHolder.transform.rotation,
                new Vector3(0.2f, 0.18f, 0.64f),
                false,
                false);
            weaponVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(weaponHolder.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.05f, 0.34f);

            GameObject muzzleLightObject = new GameObject("MuzzleFlash");
            muzzleLightObject.transform.SetParent(muzzle.transform, false);
            Light muzzleLight = muzzleLightObject.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.color = new Color(0.25f, 0.9f, 1f);
            muzzleLight.range = 3.2f;
            muzzleLight.intensity = 3.6f;
            muzzleLight.shadows = LightShadows.None;
            muzzleLight.enabled = false;

            ArcPistol pistol = weaponHolder.AddComponent<ArcPistol>();
            SetObjectField(pistol, "weaponVisual", weaponVisual);
            SetObjectField(pistol, "muzzle", muzzle.transform);
            SetObjectField(pistol, "aimCamera", camera);
            SetObjectField(pistol, "muzzleFlash", muzzleLight);
            SetObjectField(inventory, "arcPistol", pistol);
            SetIntField(inventory, "startingMedkits", 1);
            SetObjectField(firstPerson, "cameraPivot", cameraPivot.transform);
            SetObjectField(interaction, "interactionCamera", camera);
            SetObjectField(input, "inputActions", AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(SignalLostInputActionsBuilder.AssetPath));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath, out bool success);
            Object.DestroyImmediate(root);

            if (!success || prefab == null)
            {
                throw new InvalidOperationException($"Failed to save player prefab to {PlayerPrefabPath}.");
            }

            return prefab;
        }

          private static void InstantiatePlayer(GameObject prefab)
          {
              GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
              player.name = "Player";
              Vector3 spawnPos = new Vector3(0f, 1000f, -78f);
              if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 2000f, Physics.DefaultRaycastLayers))
              {
                  float heightOffset = 0.9f;
                  CharacterController controller = player.GetComponent<CharacterController>();
                  if (controller != null)
                  {
                      heightOffset = controller.height * 0.5f + controller.skinWidth;
                  }
                  spawnPos = new Vector3(hit.point.x, hit.point.y + heightOffset, hit.point.z);
              }
              else if (Terrain.activeTerrain != null)
              {
                  float terrainHeight = Terrain.activeTerrain.SampleHeight(new Vector3(0f, 0f, -78f)) + Terrain.activeTerrain.transform.position.y;
                  float heightOffset = 0.9f;
                  CharacterController controller = player.GetComponent<CharacterController>();
                  if (controller != null)
                  {
                      heightOffset = controller.height * 0.5f + controller.skinWidth;
                  }
                  spawnPos = new Vector3(0f, terrainHeight + heightOffset, -78f);
              }
              else
              {
                  spawnPos = new Vector3(0f, 3.5f, -78f);
              }
              player.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
          }

        private static void CreateSystems()
        {
            GameObject systems = new GameObject("Systems");
            GameObject managerObject = new GameObject("GameManager");
            managerObject.transform.SetParent(systems.transform, false);
            managerObject.AddComponent<GameManager>();
        }

        private static void BakeNavigation()
        {
            NavMeshSurface surface = worldRoot.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;
            surface.defaultArea = 0;
            surface.BuildNavMesh();

            if (surface.navMeshData == null)
            {
                throw new InvalidOperationException("NavMeshSurface did not produce NavMeshData.");
            }

            const string navMeshFolder = "Assets/Generated/NavMesh";
            if (!AssetDatabase.IsValidFolder(navMeshFolder))
            {
                AssetDatabase.CreateFolder("Assets/Generated", "NavMesh");
            }

            if (AssetDatabase.LoadMainAssetAtPath(NavMeshAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(NavMeshAssetPath);
            }

            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshAssetPath);
            worldRoot.gameObject.AddComponent<NavMeshRuntimeBootstrap>();
            EditorUtility.SetDirty(surface);
            AssetDatabase.SaveAssets();

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Debug.Log($"[SignalLostPhase3] NavMesh baked with {triangulation.indices.Length / 3} triangles.");
        }

        private static void SaveSceneAndBuildSettings()
        {
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save scene to {ScenePath}.");
            }

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(entry => entry.path != ScenePath)
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static DoorInteractable CreateDoor(
            Transform parent,
            string name,
            Vector3 basePosition,
            Vector3 panelSize,
            Quaternion rotation,
            bool startsLocked)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;
            root.transform.rotation = rotation;
            GameObject panel = CreateBox(
                root.transform,
                "Panel",
                basePosition + Vector3.up * (panelSize.y * 0.5f),
                panelSize,
                metalPlates,
                true,
                true,
                true,
                false);
            CreateBox(
                root.transform,
                "Handle",
                basePosition + rotation * new Vector3(panelSize.x * 0.28f, panelSize.y * 0.48f, panelSize.z * 0.53f),
                new Vector3(0.12f, 0.45f, 0.12f),
                metal,
                false,
                true,
                false,
                false);
            DoorInteractable door = root.AddComponent<DoorInteractable>();
            SetObjectField(door, "doorPanel", panel.transform);
            SetVector3Field(door, "openOffset", new Vector3(0f, panelSize.y + 0.2f, 0f));
            SetBoolField(door, "startsLocked", startsLocked);
            return door;
        }

        private static void CreateDoorFrame(
            Transform parent,
            string name,
            Vector3 basePosition,
            float width,
            float height,
            float depth,
            bool alongX)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;
            float side = width * 0.5f + 0.08f;

            if (alongX)
            {
                CreateBox(root.transform, "FrameLeft", basePosition + new Vector3(-side, height * 0.5f, 0f), new Vector3(0.16f, height, depth), metal);
                CreateBox(root.transform, "FrameRight", basePosition + new Vector3(side, height * 0.5f, 0f), new Vector3(0.16f, height, depth), metal);
                CreateBox(root.transform, "FrameTop", basePosition + new Vector3(0f, height, 0f), new Vector3(width + 0.32f, 0.16f, depth), metal);
            }
            else
            {
                CreateBox(root.transform, "FrameLeft", basePosition + new Vector3(0f, height * 0.5f, -side), new Vector3(depth, height, 0.16f), metal);
                CreateBox(root.transform, "FrameRight", basePosition + new Vector3(0f, height * 0.5f, side), new Vector3(depth, height, 0.16f), metal);
                CreateBox(root.transform, "FrameTop", basePosition + new Vector3(0f, height, 0f), new Vector3(depth, 0.16f, width + 0.32f), metal);
            }
        }

        private static GameObject CreateBreaker(
            Transform parent,
            string name,
            Vector3 position,
            float facing,
            DoorInteractable door)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            CreateBox(
                root.transform,
                "Body",
                position,
                new Vector3(0.24f, 0.95f, 0.65f),
                metal,
                false,
                true,
                false,
                false);
            GameObject screen = CreateBox(
                root.transform,
                "StatusScreen",
                position + new Vector3(facing * 0.14f, 0.2f, 0f),
                new Vector3(0.03f, 0.24f, 0.3f),
                emissiveRed,
                false,
                false,
                false,
                false);
            CreateBox(
                root.transform,
                "Lever",
                position + new Vector3(facing * 0.17f, -0.2f, 0f),
                new Vector3(0.08f, 0.3f, 0.1f),
                emissiveWarm,
                false,
                false,
                false,
                false);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.7f, 1.3f, 0.9f);
            PowerBreaker breaker = root.AddComponent<PowerBreaker>();
            SetObjectField(breaker, "statusRenderer", screen.GetComponent<Renderer>());
            SetObjectArrayField(breaker, "poweredDoors", new Object[] { door });
            return root;
        }

        private static void CreateHologramNpc(Transform parent, Vector3 basePosition)
        {
            GameObject root = new GameObject("NPC_DrReyes");
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;

            GameObject body = CreateCapsule(
                root.transform,
                "HologramBody",
                basePosition + Vector3.up * 0.78f,
                new Vector3(0.55f, 0.62f, 0.55f),
                hologram,
                false);
            CreateSphere(
                root.transform,
                "HologramHead",
                basePosition + Vector3.up * 1.6f,
                0.3f,
                hologram,
                false,
                false);
            CreateCylinder(
                root.transform,
                "HologramEmitter",
                basePosition + Vector3.up * 0.05f,
                0.48f,
                0.1f,
                metal,
                false);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.95f, 0f);
            trigger.size = new Vector3(1.0f, 1.95f, 1.0f);
            root.AddComponent<NpcDialogueInteractable>();
            CreatePointLight(root.transform, "HologramLight", basePosition + Vector3.up * 1.1f, new Color(0.12f, 0.75f, 1f), 0.9f, 2.7f);
            CreateSign(
                parent,
                "NpcNameplate",
                "DR. REYES",
                basePosition + new Vector3(0f, 2.2f, 0f),
                0f,
                2.1f,
                0.5f,
                metalPlates);
            body.name = "HologramBody";
        }

        private static void CreateFixture(
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            float intensity,
            float range)
        {
            GameObject housing = CreateBox(
                parent,
                name,
                position,
                new Vector3(1.15f, 0.08f, 0.22f),
                metal,
                false,
                false);
            CreateBox(
                parent,
                name + "Lens",
                position + Vector3.down * 0.06f,
                new Vector3(0.88f, 0.03f, 0.13f),
                emissiveWarm,
                false,
                false,
                false);
            CreatePointLight(parent, name + "Light", position + Vector3.down * 0.35f, color, intensity, range);
        }

        private static GameObject CreateFusePickup(Transform parent, string name, Vector3 basePosition)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;
            CreateCylinder(
                root.transform,
                "FuseBody",
                basePosition + Vector3.up * 0.24f,
                0.13f,
                0.42f,
                metal,
                false);
            CreateCylinder(
                root.transform,
                "FuseCore",
                basePosition + Vector3.up * 0.24f,
                0.16f,
                0.2f,
                emissiveCyan,
                false);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.3f, 0f);
            trigger.size = new Vector3(0.6f, 0.8f, 0.6f);
            root.AddComponent<FusePickup>();
            CreatePointLight(root.transform, "FuseGlow", basePosition + Vector3.up * 0.32f, new Color(0.1f, 0.85f, 1f), 0.8f, 2.4f);
            return root;
        }

        private static GameObject CreateArcPistolPickup(Transform parent, string name, Vector3 basePosition)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;
            CreateModel(
                root.transform,
                "ArcPistolDisplay",
                "Blaster/blaster-b",
                basePosition,
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(0.24f, 0.2f, 0.74f),
                false,
                true,
                false);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.2f, 0f);
            trigger.size = new Vector3(0.9f, 0.5f, 0.9f);
            root.AddComponent<ArcPistolPickup>();
            CreatePointLight(root.transform, "PickupGlow", basePosition + Vector3.up * 0.3f, new Color(0.16f, 0.85f, 1f), 0.7f, 2.2f);
            return root;
        }

        private static GameObject CreateAmmoPickup(Transform parent, string name, Vector3 basePosition)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;
            CreateBox(
                root.transform,
                "AmmoCase",
                basePosition + Vector3.up * 0.14f,
                new Vector3(0.48f, 0.28f, 0.34f),
                metal,
                false,
                true,
                false,
                false);
            CreateBox(
                root.transform,
                "AmmoCell",
                basePosition + Vector3.up * 0.22f,
                new Vector3(0.18f, 0.1f, 0.22f),
                emissiveCyan,
                false,
                false,
                false,
                false);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.22f, 0f);
            trigger.size = new Vector3(0.8f, 0.55f, 0.65f);
            root.AddComponent<AmmoPickup>();
            return root;
        }

        private static GameObject CreateMedkitPickup(Transform parent, string name, Vector3 basePosition)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;
            CreateBox(
                root.transform,
                "Case",
                basePosition + Vector3.up * 0.14f,
                new Vector3(0.52f, 0.28f, 0.4f),
                SignalLostWorldAssetFactory.GetMaterial("Metal"),
                false,
                true,
                false,
                false);
            CreateBox(
                root.transform,
                "Handle",
                basePosition + Vector3.up * 0.32f,
                new Vector3(0.24f, 0.06f, 0.07f),
                metal,
                false,
                false,
                false,
                false);
            CreateBox(
                root.transform,
                "CrossVertical",
                basePosition + Vector3.up * 0.285f,
                new Vector3(0.08f, 0.02f, 0.28f),
                emissiveRed,
                false,
                false,
                false,
                false);
            CreateBox(
                root.transform,
                "CrossHorizontal",
                basePosition + Vector3.up * 0.285f,
                new Vector3(0.22f, 0.02f, 0.08f),
                emissiveRed,
                false,
                false,
                false,
                false);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.22f, 0f);
            trigger.size = new Vector3(0.75f, 0.55f, 0.65f);
            root.AddComponent<MedkitPickup>();
            return root;
        }

        private static void CreateSignalConsole(Transform parent, Vector3 basePosition)
        {
            GameObject root = new GameObject("SignalConsole");
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;
            CreateBox(
                root.transform,
                "ConsoleBase",
                basePosition + Vector3.up * 0.55f,
                new Vector3(1.7f, 1.1f, 0.85f),
                metalPlates,
                true,
                true,
                false,
                false);
            GameObject screen = CreateBox(
                root.transform,
                "ConsoleScreen",
                basePosition + new Vector3(0f, 0.72f, 0.46f),
                new Vector3(1.25f, 0.48f, 0.04f),
                emissiveWarm,
                false,
                false,
                false,
                false);
            CreateBox(
                root.transform,
                "ConsoleKeyboard",
                basePosition + new Vector3(0f, 1.12f, 0.34f),
                new Vector3(1.15f, 0.06f, 0.42f),
                metal,
                false,
                true,
                false,
                false);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.65f, 0.1f);
            trigger.size = new Vector3(2.1f, 1.5f, 1.4f);
            SignalConsole console = root.AddComponent<SignalConsole>();
            SetObjectField(console, "statusRenderer", screen.GetComponent<Renderer>());

            GameObject beaconObject = new GameObject("TransmitterBeacon");
            beaconObject.transform.SetParent(root.transform, false);
            beaconObject.transform.position = basePosition + new Vector3(0f, 2.15f, 0f);
            Light beaconLight = beaconObject.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = new Color(0.2f, 1f, 0.55f);
            beaconLight.range = 8f;
            beaconLight.intensity = 3.5f;
            beaconLight.shadows = LightShadows.None;
            beaconLight.enabled = false;
            SetObjectField(console, "beaconLight", beaconLight);
            CreateSphere(
                root.transform,
                "BeaconLamp",
                basePosition + new Vector3(0f, 2.15f, 0f),
                0.14f,
                emissiveCyan,
                false,
                false);
            CreateSign(
                root.transform,
                "ConsoleSign",
                "TRANSMITTER",
                basePosition + new Vector3(0f, 1.75f, 0.5f),
                0f,
                2.6f,
                0.48f,
                metal);
        }

        private static void CreateFuseSockets(Transform parent, Vector3 basePosition)
        {
            Transform group = Group(parent, "OverrideFuseBank");
            group.position = basePosition;
            CreateBox(
                group,
                "Rack",
                basePosition + Vector3.up * 0.9f,
                new Vector3(2.7f, 1.7f, 0.55f),
                metalPlates,
                true,
                true,
                false,
                false);

            for (int i = 0; i < 3; i++)
            {
                float offset = (i - 1) * 0.82f;
                GameObject socketRoot = new GameObject($"FuseSocket_{i + 1}");
                socketRoot.transform.SetParent(group, false);
                socketRoot.transform.position = basePosition + new Vector3(offset, 1.25f, 0.31f);
                CreateBox(
                    socketRoot.transform,
                    "SocketBody",
                    socketRoot.transform.position,
                    new Vector3(0.58f, 0.55f, 0.18f),
                    metal,
                    false,
                    true,
                    false,
                    false);
                GameObject status = CreateBox(
                    socketRoot.transform,
                    "StatusLamp",
                    socketRoot.transform.position + new Vector3(0f, 0.18f, 0.12f),
                    new Vector3(0.16f, 0.16f, 0.04f),
                    emissiveRed,
                    false,
                    false,
                    false,
                    false);
                GameObject fuseVisual = CreateCylinder(
                    socketRoot.transform,
                    "InstalledFuse",
                    socketRoot.transform.position + new Vector3(0f, -0.02f, 0.16f),
                    0.1f,
                    0.34f,
                    emissiveCyan,
                    false,
                    false);
                fuseVisual.SetActive(false);
                BoxCollider trigger = socketRoot.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(0.75f, 0.85f, 0.5f);
                FuseSocket socket = socketRoot.AddComponent<FuseSocket>();
                SetIntField(socket, "socketNumber", i + 1);
                SetObjectField(socket, "fuseVisual", fuseVisual.transform);
                SetObjectField(socket, "statusRenderer", status.GetComponent<Renderer>());
            }

            CreateSign(
                group,
                "FuseBankSign",
                "OVERRIDE FUSES",
                basePosition + new Vector3(0f, 1.98f, 0.0f),
                0f,
                2.9f,
                0.45f,
                metal);
        }

        private static void CreateEscapeBoat(Transform parent, Vector3 basePosition)
        {
            GameObject root = new GameObject("EscapeBoat");
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;
            CreateModel(
                root.transform,
                "ExtractionBoat",
                "Watercraft/boat-speed-a",
                basePosition,
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(4.2f, 2.4f, 8.2f),
                false,
                true,
                false);
            BoxCollider hull = root.AddComponent<BoxCollider>();
            hull.center = new Vector3(0f, 1.1f, 0f);
            hull.size = new Vector3(3.2f, 1.8f, 7.6f);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.2f, 0f);
            trigger.size = new Vector3(4.6f, 2.8f, 8.8f);
            root.AddComponent<EscapeBoat>();
            CreatePointLight(
                root.transform,
                "BoatBeacon",
                basePosition + new Vector3(0f, 2.6f, -2.8f),
                new Color(0.2f, 0.9f, 1f),
                1.4f,
                6f);
            CreateSign(
                root.transform,
                "BoatSign",
                "EXTRACTION",
                basePosition + new Vector3(0f, 2.7f, -3.2f),
                180f,
                2.8f,
                0.6f,
                metalPlates);
        }

        private static void CreatePathBeacon(Transform parent, Vector3 position)
        {
            float ground = SignalLostWorldTerrain.SampleHeight(position.x, position.z);

            if (position.z < 2f)
            {
                ground = Mathf.Max(ground, SignalLostWorldTerrain.SampleHeight(position.x, position.z));
            }

            CreateCylinder(
                parent,
                "PathBeaconPost",
                new Vector3(position.x, ground + 1.7f, position.z),
                0.09f,
                3.4f,
                metal);
            CreateBox(
                parent,
                "PathBeaconLamp",
                new Vector3(position.x, ground + 3.45f, position.z),
                new Vector3(0.28f, 0.36f, 0.28f),
                emissiveWarm,
                false,
                false);
            CreatePointLight(
                parent,
                "PathBeaconLight",
                new Vector3(position.x, ground + 3.25f, position.z),
                new Color(1f, 0.58f, 0.26f),
                0.75f,
                5f);
        }

        private static void CreateSign(
            Transform parent,
            string name,
            string text,
            Vector3 position,
            float yaw,
            float width,
            float height,
            Material boardMaterial)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(root.transform, false);
            board.transform.localPosition = Vector3.zero;
            board.transform.localScale = new Vector3(width, height, 0.1f);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            Renderer boardRenderer = board.GetComponent<Renderer>();
            boardRenderer.sharedMaterial = boardMaterial;
            MarkStatic(board);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (font == null)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                GameObject textObject = new GameObject("Text");
                textObject.transform.SetParent(root.transform, false);
                textObject.transform.localPosition = new Vector3(0f, 0f, i == 0 ? 0.06f : -0.06f);
                textObject.transform.localRotation = Quaternion.Euler(0f, i == 0 ? 0f : 180f, 0f);
                TextMesh textMesh = textObject.AddComponent<TextMesh>();
                textMesh.text = text;
                textMesh.font = font;
                textMesh.fontSize = 72;
                textMesh.characterSize = 0.26f;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.color = new Color(0.94f, 0.95f, 0.9f);
                MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
                textRenderer.sharedMaterial = font.material;
                textRenderer.shadowCastingMode = ShadowCastingMode.Off;
                textRenderer.receiveShadows = false;
                MarkStatic(textObject);
            }
        }

        private static void CreateWallX(
            Transform parent,
            string name,
            float z,
            float baseY,
            float xMin,
            float xMax,
            float height,
            float thickness,
            Material material,
            float openingCenter,
            float openingWidth,
            float openingHeight)
        {
            if (openingWidth <= 0.05f)
            {
                CreateBox(
                    parent, name, new Vector3((xMin + xMax) * 0.5f, baseY + height * 0.5f, z),
                    new Vector3(xMax - xMin, height, thickness), material);
                return;
            }

            float openingMin = openingCenter - openingWidth * 0.5f;
            float openingMax = openingCenter + openingWidth * 0.5f;

            if (openingMin - xMin > 0.05f)
            {
                CreateBox(
                    parent, name + "Left", new Vector3((xMin + openingMin) * 0.5f, baseY + height * 0.5f, z),
                    new Vector3(openingMin - xMin, height, thickness), material);
            }

            if (xMax - openingMax > 0.05f)
            {
                CreateBox(
                    parent, name + "Right", new Vector3((openingMax + xMax) * 0.5f, baseY + height * 0.5f, z),
                    new Vector3(xMax - openingMax, height, thickness), material);
            }

            if (height - openingHeight > 0.05f)
            {
                CreateBox(
                    parent, name + "Lintel",
                    new Vector3(openingCenter, baseY + openingHeight + (height - openingHeight) * 0.5f, z),
                    new Vector3(openingWidth, height - openingHeight, thickness), material);
            }
        }

        private static void CreateWallZ(
            Transform parent,
            string name,
            float x,
            float baseY,
            float zMin,
            float zMax,
            float height,
            float thickness,
            Material material,
            float openingCenter,
            float openingWidth,
            float openingHeight)
        {
            if (openingWidth <= 0.05f)
            {
                CreateBox(
                    parent, name, new Vector3(x, baseY + height * 0.5f, (zMin + zMax) * 0.5f),
                    new Vector3(thickness, height, zMax - zMin), material);
                return;
            }

            float openingMin = openingCenter - openingWidth * 0.5f;
            float openingMax = openingCenter + openingWidth * 0.5f;

            if (openingMin - zMin > 0.05f)
            {
                CreateBox(
                    parent, name + "Bottom", new Vector3(x, baseY + height * 0.5f, (zMin + openingMin) * 0.5f),
                    new Vector3(thickness, height, openingMin - zMin), material);
            }

            if (zMax - openingMax > 0.05f)
            {
                CreateBox(
                    parent, name + "Top", new Vector3(x, baseY + height * 0.5f, (openingMax + zMax) * 0.5f),
                    new Vector3(thickness, height, zMax - openingMax), material);
            }

            if (height - openingHeight > 0.05f)
            {
                CreateBox(
                    parent, name + "Lintel",
                    new Vector3(x, baseY + openingHeight + (height - openingHeight) * 0.5f, openingCenter),
                    new Vector3(thickness, height - openingHeight, openingWidth), material);
            }
        }

        private static GameObject CreateBox(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            Material material,
            bool withCollider = true,
            bool castShadows = true,
            bool ignoreNavigation = false,
            bool markStatic = true)
        {
            return CreateBox(
                parent, name, position, Quaternion.identity, size, material,
                withCollider, castShadows, ignoreNavigation, markStatic);
        }

        private static GameObject CreateBox(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation,
            Vector3 size,
            Material material,
            bool withCollider = true,
            bool castShadows = true,
            bool ignoreNavigation = false,
            bool markStatic = true)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.SetPositionAndRotation(position, rotation);
            gameObject.transform.localScale = size;
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = castShadows;

            if (!withCollider)
            {
                Object.DestroyImmediate(gameObject.GetComponent<Collider>());
            }

            if (ignoreNavigation)
            {
                IgnoreNavigation(gameObject);
            }

            if (markStatic)
            {
                MarkStatic(gameObject);
            }

            return gameObject;
        }

        private static GameObject CreateCylinder(
            Transform parent,
            string name,
            Vector3 position,
            float radius,
            float height,
            Material material,
            bool withCollider = true,
            bool castShadows = true)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = castShadows;

            if (!withCollider)
            {
                Object.DestroyImmediate(gameObject.GetComponent<Collider>());
            }

            MarkStatic(gameObject);
            return gameObject;
        }

        private static GameObject CreateSphere(
            Transform parent,
            string name,
            Vector3 position,
            float radius,
            Material material,
            bool withCollider,
            bool castShadows)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = Vector3.one * (radius * 2f);
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = castShadows;

            if (!withCollider)
            {
                Object.DestroyImmediate(gameObject.GetComponent<Collider>());
            }

            MarkStatic(gameObject);
            return gameObject;
        }

        private static GameObject CreateCapsule(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool withCollider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (!withCollider)
            {
                Object.DestroyImmediate(gameObject.GetComponent<Collider>());
            }

            return gameObject;
        }

        private static void CreateBeamBetween(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            float thickness,
            Material material)
        {
            Vector3 direction = end - start;
            float length = direction.magnitude;

            if (length < 0.01f)
            {
                return;
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            CreateBox(
                parent,
                name,
                (start + end) * 0.5f,
                rotation,
                new Vector3(thickness, length, thickness),
                material);
        }

        private static GameObject CreateModel(
            Transform parent,
            string name,
            string modelPath,
            Vector3 position,
            Quaternion rotation,
            Vector3 targetSize,
            bool withCollider,
            bool castShadows = true,
            bool markStatic = true)
        {
            GameObject wrapper = InstantiateModel(parent, name, modelPath, position, rotation, out GameObject model, out Bounds initialBounds);

            if (initialBounds.size.y <= 0.0001f)
            {
                return wrapper;
            }

            Vector3 scaleFactor = Vector3.one;

            if (targetSize.x > 0.001f)
            {
                scaleFactor.x = targetSize.x / initialBounds.size.x;
            }

            if (targetSize.y > 0.001f)
            {
                scaleFactor.y = targetSize.y / initialBounds.size.y;
            }

            if (targetSize.z > 0.001f)
            {
                scaleFactor.z = targetSize.z / initialBounds.size.z;
            }

            model.transform.localScale = Vector3.Scale(model.transform.localScale, scaleFactor);
            FinishModel(wrapper, model, withCollider, castShadows, markStatic);
            return wrapper;
        }

        private static GameObject CreateUniformModel(
            Transform parent,
            string name,
            string modelPath,
            Vector3 position,
            Quaternion rotation,
            float targetHeight,
            bool withCollider)
        {
            GameObject wrapper = InstantiateModel(parent, name, modelPath, position, rotation, out GameObject model, out Bounds initialBounds);

            if (initialBounds.size.y <= 0.0001f)
            {
                return wrapper;
            }

            float scale = targetHeight / initialBounds.size.y;
            model.transform.localScale *= scale;
            FinishModel(wrapper, model, withCollider, true, true);
            return wrapper;
        }

        private static GameObject InstantiateModel(
            Transform parent,
            string name,
            string modelPath,
            Vector3 position,
            Quaternion rotation,
            out GameObject model,
            out Bounds initialBounds)
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

            initialBounds = GetRendererBounds(model);
            return wrapper;
        }

        private static void FinishModel(
            GameObject wrapper,
            GameObject model,
            bool withCollider,
            bool castShadows,
            bool markStatic)
        {
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
                MarkStatic(wrapper);
            }
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

        private static void IgnoreNavigation(GameObject gameObject)
        {
            NavMeshModifier modifier = gameObject.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
        }

        private static void MarkStatic(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(
                    child.gameObject,
                    StaticEditorFlags.BatchingStatic
                    | StaticEditorFlags.OccludeeStatic
                    | StaticEditorFlags.ContributeGI);
            }
        }

        private static bool IsSteep(float x, float z, float maximumDelta)
        {
            float center = SignalLostWorldTerrain.SampleHeight(x, z);
            float offset = 1.5f;
            float deltaX = Mathf.Abs(SignalLostWorldTerrain.SampleHeight(x + offset, z) - center);
            float deltaZ = Mathf.Abs(SignalLostWorldTerrain.SampleHeight(x, z + offset) - center);
            return Mathf.Max(deltaX, deltaZ) > maximumDelta;
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
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static Transform Group(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
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

        private static void SetVector3Field(Object target, string fieldName, Vector3 value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                throw new InvalidOperationException($"Field not found: {fieldName} on {target.GetType().Name}");
            }

            property.vector3Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

