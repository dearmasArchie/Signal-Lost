using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SignalLost.EditorTools
{
    public static class SignalLostWorldAssetFactory
    {
        public const string GeneratedRoot = "Assets/Generated";
        public const string MaterialRoot = "Assets/Generated/Materials";
        public const string TerrainLayerRoot = "Assets/Generated/TerrainLayers";
        public const string TerrainDataRoot = "Assets/Generated/Terrain";
        public const string PrefabRoot = "Assets/Prefabs";

        private const string TextureRoot = "Assets/ThirdParty/ambientCG/Textures/";

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, TerrainLayer> TerrainLayers = new Dictionary<string, TerrainLayer>();

        public static void EnsureFolders()
        {
            EnsureFolder("Assets", "Generated");
            EnsureFolder(GeneratedRoot, "Materials");
            EnsureFolder(GeneratedRoot, "TerrainLayers");
            EnsureFolder(GeneratedRoot, "Terrain");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets", "Scenes");
        }

        public static Material GetMaterial(string key)
        {
            if (Materials.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            EnsureFolders();
            Material material = CreateMaterial(key);
            Materials[key] = material;
            return material;
        }

        public static TerrainLayer GetTerrainLayer(string key)
        {
            if (TerrainLayers.TryGetValue(key, out TerrainLayer cached))
            {
                return cached;
            }

            EnsureFolders();
            TerrainLayer layer = CreateTerrainLayer(key);
            TerrainLayers[key] = layer;
            return layer;
        }

        public static Texture2D LoadTexture(string textureName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + textureName);
        }

        private static Material CreateMaterial(string key)
        {
            string path = $"{MaterialRoot}/M_{key}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (material == null)
            {
                material = new Material(shader) { name = $"M_{key}" };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            ConfigureMaterial(key, material);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void ConfigureMaterial(string key, Material material)
        {
            switch (key)
            {
                case "Concrete":
                    ConfigureLit(material, "Concrete034", new Color(0.66f, 0.66f, 0.63f), 3f, 0.22f, 0f);
                    break;
                case "MetalPlates":
                    ConfigureLit(material, "MetalPlates006", new Color(0.58f, 0.61f, 0.64f), 2.5f, 0.52f, 0.75f);
                    break;
                case "Metal":
                    ConfigureLit(material, "Metal032", new Color(0.5f, 0.54f, 0.57f), 2f, 0.62f, 0.8f);
                    break;
                case "Wood":
                    ConfigureLit(material, "Wood051", new Color(0.62f, 0.47f, 0.31f), 2f, 0.25f, 0f);
                    break;
                case "Rock":
                    ConfigureLit(material, "Rock030", new Color(0.43f, 0.43f, 0.4f), 3f, 0.18f, 0f);
                    break;
                case "Sand":
                    ConfigureLit(material, "Ground054", new Color(0.82f, 0.72f, 0.52f), 4f, 0.25f, 0f);
                    break;
                case "Grass":
                    ConfigureLit(material, "Grass004", new Color(0.44f, 0.58f, 0.3f), 4f, 0.25f, 0f);
                    break;
                case "Glass":
                    ConfigureSolid(material, new Color(0.08f, 0.19f, 0.23f, 0.72f), 0.92f, 0.25f);
                    break;
                case "Hologram":
                    ConfigureSolid(material, new Color(0.08f, 0.72f, 0.9f, 0.32f), 0.85f, 0f);
                    MakeTransparent(material);
                    ConfigureEmission(material, new Color(0.02f, 0.5f, 0.85f) * 2.5f);
                    break;
                case "Water":
                    ConfigureLit(material, "Ice003", new Color(0.3f, 0.67f, 0.75f, 0.78f), 2f, 0.94f, 0.05f);
                    MakeTransparent(material);
                    break;
                case "EmissiveRed":
                    ConfigureSolid(material, new Color(1f, 0.06f, 0.03f, 1f), 0.45f, 0f);
                    ConfigureEmission(material, new Color(1f, 0.03f, 0.01f) * 5f);
                    break;
                case "EmissiveGreen":
                    ConfigureSolid(material, new Color(0.1f, 1f, 0.35f, 1f), 0.45f, 0f);
                    ConfigureEmission(material, new Color(0.03f, 1f, 0.2f) * 4f);
                    break;
                case "EmissiveCyan":
                    ConfigureSolid(material, new Color(0.12f, 0.8f, 1f, 1f), 0.45f, 0f);
                    ConfigureEmission(material, new Color(0.04f, 0.65f, 1f) * 4f);
                    break;
                case "EmissiveWarm":
                    ConfigureSolid(material, new Color(1f, 0.72f, 0.35f, 1f), 0.4f, 0f);
                    ConfigureEmission(material, new Color(1f, 0.55f, 0.2f) * 3f);
                    break;
                default:
                    ConfigureSolid(material, Color.magenta, 0.4f, 0f);
                    break;
            }
        }

        private static void ConfigureLit(
            Material material,
            string textureGroup,
            Color tint,
            float tiling,
            float smoothness,
            float metallic)
        {
            Texture2D color = LoadTexture($"{textureGroup}_Color.jpg");
            Texture2D normal = LoadTexture($"{textureGroup}_NormalGL.jpg");

            if (color != null)
            {
                material.SetTexture("_BaseMap", color);
                material.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
                material.mainTexture = color;
                material.mainTextureScale = new Vector2(tiling, tiling);
            }

            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetTextureScale("_BumpMap", new Vector2(tiling, tiling));
                material.EnableKeyword("_NORMALMAP");
            }

            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.renderQueue = -1;
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        private static void ConfigureSolid(Material material, Color color, float smoothness, float metallic)
        {
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.renderQueue = -1;
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        private static void ConfigureEmission(Material material, Color emission)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        private static void MakeTransparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static TerrainLayer CreateTerrainLayer(string key)
        {
            string path = $"{TerrainLayerRoot}/TL_{key}.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);

            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }

            layer.name = $"TL_{key}";
            string textureGroup;
            float smoothness;
            float tileSize;

            switch (key)
            {
                case "Grass":
                    textureGroup = "Grass004";
                    smoothness = 0.3f;
                    tileSize = 12f;
                    break;
                case "Dirt":
                    textureGroup = "Ground037";
                    smoothness = 0.24f;
                    tileSize = 8f;
                    break;
                case "Sand":
                    textureGroup = "Ground054";
                    smoothness = 0.2f;
                    tileSize = 10f;
                    break;
                case "Rock":
                    textureGroup = "Rock030";
                    smoothness = 0.18f;
                    tileSize = 13f;
                    break;
                default:
                    textureGroup = "Grass004";
                    smoothness = 0.3f;
                    tileSize = 12f;
                    break;
            }

            layer.diffuseTexture = LoadTexture($"{textureGroup}_Color.jpg");
            layer.normalMapTexture = LoadTexture($"{textureGroup}_NormalGL.jpg");
            layer.tileSize = new Vector2(tileSize, tileSize);
            layer.smoothness = smoothness;
            layer.metallic = 0f;
            layer.normalScale = 1f;
            EditorUtility.SetDirty(layer);
            AssetDatabase.SaveAssetIfDirty(layer);
            return layer;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
