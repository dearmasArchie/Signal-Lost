using System;
using System.Collections.Generic;
using SignalLost.World;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SignalLost.EditorTools
{
    public static class SignalLostPhase5Builder
    {
        public const string IslandExteriorScenePath = "Assets/Scenes/IslandExterior.unity";
        public const string UndergroundScenePath = "Assets/Scenes/UndergroundFacility.unity";

        [MenuItem("Signal Lost/Phase 5/Build All")]
        public static void BuildAll()
        {
            BuildUndergroundScene();
            BuildPortals();
            Debug.Log("[SignalLostPhase5] PHASE5_BUILT");
        }

        private static void BuildUndergroundScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            GameObject root = new GameObject("UndergroundFacility");
            Transform world = new GameObject("World").transform;
            world.SetParent(root.transform);
            Transform props = new GameObject("Props").transform;
            props.SetParent(root.transform);
            Transform lights = new GameObject("Lights").transform;
            lights.SetParent(root.transform);
            Transform nav = new GameObject("NavMesh").transform;
            nav.SetParent(root.transform);

            Material metal = CreateMaterial("M_UndergroundMetal", new Color(0.16f, 0.2f, 0.24f));
            Material concrete = CreateMaterial("M_UndergroundConcrete", new Color(0.22f, 0.24f, 0.26f));
            Material hazard = CreateMaterial("M_UndergroundHazard", new Color(1f, 0.16f, 0.08f));
            Material waterMat = CreateMaterial("M_UndergroundWater", new Color(0.12f, 0.28f, 0.32f), true);
            Material glowCyan = CreateMaterial("M_UndergroundGlowCyan", new Color(0.12f, 0.9f, 1f), true);
            Material glowRed = CreateMaterial("M_UndergroundGlowRed", new Color(1f, 0.12f, 0.08f), true);

            GameObject hub = CreateBox(props, "ElevatorHub", new Vector3(0f, 0f, 0f), new Vector3(16f, 4f, 16f), concrete);
            CreateBox(props, "HubFloor", new Vector3(0f, 0.05f, 0f), new Vector3(16f, 0.1f, 16f), concrete).transform.SetParent(hub.transform);
            CreateBox(props, "HubCeiling", new Vector3(0f, 4.05f, 0f), new Vector3(16f, 0.1f, 16f), metal).transform.SetParent(hub.transform);
            CreateBox(props, "HubWallNorth", new Vector3(0f, 2f, 8f), new Vector3(16f, 4f, 0.4f), concrete).transform.SetParent(hub.transform);
            CreateBox(props, "HubWallSouth", new Vector3(0f, 2f, -8f), new Vector3(16f, 4f, 0.4f), concrete).transform.SetParent(hub.transform);
            CreateBox(props, "HubWallEast", new Vector3(8f, 2f, 0f), new Vector3(0.4f, 4f, 16f), concrete).transform.SetParent(hub.transform);
            CreateBox(props, "HubWallWest", new Vector3(-8f, 2f, 0f), new Vector3(0.4f, 4f, 16f), concrete).transform.SetParent(hub.transform);

            GameObject corridorA = CreateBox(props, "CorridorWingA", new Vector3(0f, 0f, 20f), new Vector3(6f, 4f, 16f), concrete);
            CreateBox(corridorA.transform, "A_Floor", new Vector3(0f, 0.05f, 0f), new Vector3(6f, 0.1f, 16f), concrete);
            CreateBox(corridorA.transform, "A_Ceiling", new Vector3(0f, 4.05f, 0f), new Vector3(6f, 0.1f, 16f), metal);
            CreateBox(corridorA.transform, "A_WallLeft", new Vector3(-3.2f, 2f, 0f), new Vector3(0.4f, 4f, 16f), concrete);
            CreateBox(corridorA.transform, "A_WallRight", new Vector3(3.2f, 2f, 0f), new Vector3(0.4f, 4f, 16f), concrete);
            for (int i = 0; i < 5; i++)
            {
                CreateBox(corridorA.transform, $"TripwireEmitter_{i}", new Vector3(0f, 2f, -6f + i * 3f), new Vector3(1.2f, 1.2f, 0.12f), glowRed);
            }

            GameObject breakerRoom = CreateBox(props, "BreakerRoom", new Vector3(0f, 0f, 36f), new Vector3(10f, 4f, 10f), concrete);
            CreateBox(breakerRoom.transform, "BR_Floor", new Vector3(0f, 0.05f, 0f), new Vector3(10f, 0.1f, 10f), concrete);
            CreateBox(breakerRoom.transform, "BR_Ceiling", new Vector3(0f, 4.05f, 0f), new Vector3(10f, 0.1f, 10f), metal);
            CreateBox(breakerRoom.transform, "BR_WallN", new Vector3(0f, 2f, 5.2f), new Vector3(10f, 4f, 0.4f), concrete);
            CreateBox(breakerRoom.transform, "BR_WallS", new Vector3(0f, 2f, -5.2f), new Vector3(10f, 4f, 0.4f), concrete);
            CreateBox(breakerRoom.transform, "BR_WallE", new Vector3(5.2f, 2f, 0f), new Vector3(0.4f, 4f, 10f), concrete);
            CreateBox(breakerRoom.transform, "BR_WallW", new Vector3(-5.2f, 2f, 0f), new Vector3(0.4f, 4f, 10f), concrete);
            for (int i = 0; i < 3; i++)
            {
                CreateBox(breakerRoom.transform, $"Switch_{i + 1}", new Vector3(-2f + i * 2f, 1.6f, 4.8f), new Vector3(0.8f, 1.2f, 0.12f), metal);
            }
            CreateBox(breakerRoom.transform, "FusePod", new Vector3(0f, 1.2f, -4.8f), new Vector3(2f, 2.4f, 0.12f), glowCyan);

            GameObject corridorB = CreateBox(props, "CorridorWingB", new Vector3(-16f, 0f, 0f), new Vector3(16f, 4f, 6f), concrete);
            CreateBox(corridorB.transform, "B_Floor", new Vector3(0f, 0.05f, 0f), new Vector3(16f, 0.1f, 6f), concrete);
            CreateBox(corridorB.transform, "B_Ceiling", new Vector3(0f, 4.05f, 0f), new Vector3(16f, 0.1f, 6f), metal);
            CreateBox(corridorB.transform, "B_WallTop", new Vector3(0f, 2f, 3.2f), new Vector3(16f, 4f, 0.4f), concrete);
            CreateBox(corridorB.transform, "B_WallBottom", new Vector3(0f, 2f, -3.2f), new Vector3(16f, 4f, 0.4f), concrete);

            GameObject floodway = CreateBox(props, "Floodway", new Vector3(-32f, -0.4f, 0f), new Vector3(16f, 3.6f, 10f), concrete);
            CreateBox(floodway.transform, "FW_WallN", new Vector3(0f, 2f, 5.2f), new Vector3(16f, 4f, 0.4f), concrete);
            CreateBox(floodway.transform, "FW_WallS", new Vector3(0f, 2f, -5.2f), new Vector3(16f, 4f, 0.4f), concrete);
            CreateBox(floodway.transform, "FW_WallE", new Vector3(8.2f, 2f, 0f), new Vector3(0.4f, 4f, 10f), concrete);
            CreateBox(floodway.transform, "FW_WallW", new Vector3(-8.2f, 2f, 0f), new Vector3(0.4f, 4f, 10f), concrete);
            GameObject water = CreateBox(floodway.transform, "ElectrifiedWater", new Vector3(0f, -0.1f, 0f), new Vector3(15.6f, 0.2f, 9.6f), waterMat);
            water.GetComponent<Collider>().isTrigger = true;
            for (int i = 0; i < 6; i++)
            {
                CreateBox(floodway.transform, $"CratePlatform_{i}", new Vector3(-6f + i * 2.4f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 1.6f), metal);
            }
            CreateBox(floodway.transform, "DrainValve", new Vector3(7.6f, 1.6f, 0f), new Vector3(0.4f, 1.2f, 0.8f), hazard);

            GameObject quarantine = CreateBox(props, "QuarantineRoom", new Vector3(-48f, 0f, 0f), new Vector3(14f, 4f, 14f), concrete);
            CreateBox(quarantine.transform, "QR_Floor", new Vector3(0f, 0.05f, 0f), new Vector3(14f, 0.1f, 14f), concrete);
            CreateBox(quarantine.transform, "QR_Ceiling", new Vector3(0f, 4.05f, 0f), new Vector3(14f, 0.1f, 14f), metal);
            CreateBox(quarantine.transform, "QR_WallN", new Vector3(0f, 2f, 7.2f), new Vector3(14f, 4f, 0.4f), concrete);
            CreateBox(quarantine.transform, "QR_WallS", new Vector3(0f, 2f, -7.2f), new Vector3(14f, 4f, 0.4f), concrete);
            CreateBox(quarantine.transform, "QR_WallE", new Vector3(7.2f, 2f, 0f), new Vector3(0.4f, 4f, 14f), concrete);
            CreateBox(quarantine.transform, "QR_WallW", new Vector3(-7.2f, 2f, 0f), new Vector3(0.4f, 4f, 14f), concrete);
            CreateBox(quarantine.transform, "FusePodB", new Vector3(0f, 1.2f, 6.8f), new Vector3(2f, 2.4f, 0.12f), glowCyan);
            for (int i = 0; i < 4; i++)
            {
                CreateBox(quarantine.transform, $"SpawnPoint_{i}", new Vector3(-4f + i * 2.6f, 0.5f, -2f), new Vector3(0.6f, 0.6f, 0.6f), glowRed);
            }

            GameObject light = new GameObject("DirectionalLight");
            light.transform.SetParent(lights);
            Light l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.2f;
            l.color = new Color(0.7f, 0.8f, 0.9f);
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject navObj = new GameObject("NavMeshSurface");
            navObj.transform.SetParent(nav);
            NavMeshSurface surface = navObj.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;

            GameObject undergroundPortal = new GameObject("UndergroundExitPortal");
            undergroundPortal.transform.SetParent(props);
            undergroundPortal.transform.position = new Vector3(-6f, 1f, 0f);
            BoxCollider portalCol = undergroundPortal.AddComponent<BoxCollider>();
            portalCol.isTrigger = true;
            portalCol.size = new Vector3(2f, 2f, 2f);
            undergroundPortal.AddComponent<ScenePortal>().GetType();

            string path = UndergroundScenePath;
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SignalLostPhase5] Created {path}");
        }

        private static void BuildPortals()
        {
            Scene scene = EditorSceneManager.OpenScene(IslandExteriorScenePath, OpenSceneMode.Single);
            GameObject portalsRoot = new GameObject("Portals");
            GameObject elevator = GameObject.Find("World/Elevator") ?? GameObject.Find("Elevator");
            if (elevator == null)
            {
                elevator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                elevator.name = "ElevatorHouse";
                elevator.transform.position = new Vector3(0f, 2f, 40f);
                elevator.transform.localScale = new Vector3(6f, 4f, 6f);
            }
            GameObject portal = new GameObject("UndergroundPortal");
            portal.transform.SetParent(portalsRoot.transform);
            portal.transform.position = elevator.transform.position + Vector3.up * 0.5f;
            BoxCollider col = portal.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(4f, 3f, 4f);
            ScenePortal sp = portal.AddComponent<ScenePortal>();
            SerializedObject so = new SerializedObject(sp);
            so.FindProperty("targetScene").stringValue = "UndergroundFacility";
            so.ApplyModifiedProperties();
            EditorSceneManager.SaveScene(scene, IslandExteriorScenePath);
            Debug.Log("[SignalLostPhase5] Portals updated");
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            if (mat != null)
            {
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            return go;
        }

        private static Material CreateMaterial(string name, Color color, bool emissive = false)
        {
            string dir = "Assets/Generated/Materials";
            if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            {
                AssetDatabase.CreateFolder("Assets", "Generated");
            }
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets/Generated", "Materials");
            }
            string path = $"{dir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2f);
            }
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
