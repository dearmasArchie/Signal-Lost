using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SignalLost.EditorTools
{
    internal sealed class SignalLostAssetPostprocessor : AssetPostprocessor
    {
        private const string KenneyModelRoot = "Assets/ThirdParty/Kenney/Models/";
        private const string AmbientCgTextureRoot = "Assets/ThirdParty/ambientCG/Textures/";
        private const string UiRoot = "Assets/Resources/UI/";
        private const string AudioRoot = "Assets/Resources/Audio/";
        private const string HdriPath = "Assets/ThirdParty/PolyHaven/HDRI/KloofendalPartlyCloudy4k.hdr";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(KenneyModelRoot, StringComparison.Ordinal))
            {
                return;
            }

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.useFileScale = true;
            importer.globalScale = 1f;
            importer.bakeAxisConversion = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
        }

        private void OnPreprocessTexture()
        {
            TextureImporter importer = (TextureImporter)assetImporter;

            if (assetPath.StartsWith(AmbientCgTextureRoot, StringComparison.Ordinal))
            {
                ConfigureMaterialTexture(importer);
                return;
            }

            if (assetPath.StartsWith(UiRoot, StringComparison.Ordinal))
            {
                ConfigureUiSprite(importer);
                return;
            }

            if (assetPath.Equals(HdriPath, StringComparison.Ordinal))
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
            }
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioRoot, StringComparison.Ordinal))
            {
                return;
            }

            AudioImporter importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool isMusic = assetPath.Contains("/Music/", StringComparison.Ordinal);
            settings.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = isMusic ? 0.7f : 0.75f;
            settings.preloadAudioData = !isMusic;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = !isMusic;
            importer.loadInBackground = isMusic;
        }

        private static void ConfigureMaterialTexture(TextureImporter importer)
        {
            bool isNormal = importer.assetPath.Contains("_NormalGL", StringComparison.Ordinal)
                || importer.assetPath.Contains("_Normal", StringComparison.Ordinal);
            bool isColor = importer.assetPath.Contains("_Color", StringComparison.Ordinal);

            importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = isColor;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }

        private static void ConfigureUiSprite(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = GetUiBorder(importer.assetPath);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }

        private static Vector4 GetUiBorder(string path)
        {
            if (path.EndsWith("/PanelRect.png", StringComparison.Ordinal)
                || path.EndsWith("/PanelRectScrews.png", StringComparison.Ordinal)
                || path.EndsWith("/ButtonRect.png", StringComparison.Ordinal)
                || path.EndsWith("/ButtonRectDepth.png", StringComparison.Ordinal))
            {
                return new Vector4(16f, 16f, 16f, 16f);
            }

            if (path.EndsWith("/BarFrame.png", StringComparison.Ordinal))
            {
                return new Vector4(12f, 4f, 12f, 4f);
            }

            return Vector4.zero;
        }
    }

    public static class SignalLostAssetImportPipeline
    {
        private static readonly string[] RequiredModels =
        {
            "Assets/ThirdParty/Kenney/Models/Nature/tree_palm.fbx",
            "Assets/ThirdParty/Kenney/Models/Survival/barrel.fbx",
            "Assets/ThirdParty/Kenney/Models/Space/machine_generator.fbx",
            "Assets/ThirdParty/Kenney/Models/SpaceStation/door-single.fbx",
            "Assets/ThirdParty/Kenney/Models/Watercraft/boat-speed-a.fbx",
            "Assets/ThirdParty/Kenney/Models/Blaster/blaster-a.fbx"
        };

        private static readonly string[] RequiredAudio =
        {
            "Assets/Resources/Audio/Music/Menu.ogg",
            "Assets/Resources/Audio/Music/Ambient.ogg",
            "Assets/Resources/Audio/Music/Combat.ogg",
            "Assets/Resources/Audio/Music/Victory.ogg",
            "Assets/Resources/Audio/Music/Defeat.ogg",
            "Assets/Resources/Audio/Music/GameOver.ogg",
            "Assets/Resources/Audio/SFX/Weapon_ArcPistol.ogg",
            "Assets/Resources/Audio/SFX/Footstep_Grass_1.ogg",
            "Assets/Resources/Audio/SFX/Footstep_Concrete_1.ogg",
            "Assets/Resources/Audio/SFX/Footstep_Wood_1.ogg",
            "Assets/Resources/Audio/SFX/Footstep_Metal_1.ogg"
        };

        private static readonly string[] RequiredSprites =
        {
            "Assets/Resources/UI/CrosshairWhite.png",
            "Assets/Resources/UI/PanelRect.png",
            "Assets/Resources/UI/ButtonRect.png",
            "Assets/Resources/UI/BarFrame.png"
        };

        public static void ImportAndValidate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset("Assets/ThirdParty", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset("Assets/Resources", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();

            List<string> errors = new List<string>();

            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/ThirdParty/Kenney/Models" });
            string[] sfxGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio/SFX" });
            string[] musicGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio/Music" });
            string[] uiGuids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Resources/UI" });
            string[] fontGuids = AssetDatabase.FindAssets("t:Font", new[] { "Assets/Resources/Fonts" });
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ThirdParty/ambientCG/Textures" });

            Debug.Log($"[SignalLostImport] Models={modelGuids.Length}, SFX={sfxGuids.Length}, Music={musicGuids.Length}, UI={uiGuids.Length}, Fonts={fontGuids.Length}, Textures={textureGuids.Length}");

            if (modelGuids.Length != 161)
            {
                errors.Add($"Expected 161 models, found {modelGuids.Length}.");
            }

            if (sfxGuids.Length != 37)
            {
                errors.Add($"Expected 37 SFX clips, found {sfxGuids.Length}.");
            }

            if (musicGuids.Length != 6)
            {
                errors.Add($"Expected 6 music clips, found {musicGuids.Length}.");
            }

            if (uiGuids.Length != 13)
            {
                errors.Add($"Expected 13 UI sprites, found {uiGuids.Length}.");
            }

            if (fontGuids.Length != 3)
            {
                errors.Add($"Expected 3 fonts, found {fontGuids.Length}.");
            }

            if (textureGuids.Length != 33)
            {
                errors.Add($"Expected 33 material texture maps, found {textureGuids.Length}.");
            }

            foreach (string path in RequiredModels.Concat(RequiredAudio).Concat(RequiredSprites))
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                {
                    errors.Add($"Missing required asset: {path}");
                }
            }

            if (AssetDatabase.LoadMainAssetAtPath("Assets/Resources/Fonts/KenneyFuture.ttf") == null)
            {
                errors.Add("Missing required font: Assets/Resources/Fonts/KenneyFuture.ttf");
            }

            if (AssetDatabase.LoadMainAssetAtPath("Assets/ThirdParty/PolyHaven/HDRI/KloofendalPartlyCloudy4k.hdr") == null)
            {
                errors.Add("Missing required HDRI.");
            }

            foreach (string colormapPath in new[]
            {
                "Assets/ThirdParty/Kenney/Models/Survival/Textures/colormap.png",
                "Assets/ThirdParty/Kenney/Models/SpaceStation/Textures/colormap.png",
                "Assets/ThirdParty/Kenney/Models/Watercraft/Textures/colormap.png",
                "Assets/ThirdParty/Kenney/Models/Blaster/Textures/colormap.png"
            })
            {
                if (AssetDatabase.LoadMainAssetAtPath(colormapPath) == null)
                {
                    errors.Add($"Missing Kenney colormap: {colormapPath}");
                }
            }

            Texture2D hdri = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/PolyHaven/HDRI/KloofendalPartlyCloudy4k.hdr");
            if (hdri != null)
            {
                Debug.Log($"[SignalLostImport] HDRI: {hdri.width}x{hdri.height}, format={hdri.format}");
            }

            foreach (string modelPath in RequiredModels)
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
                Material[] materials = assets.OfType<Material>().ToArray();
                string materialSummary = string.Join(", ", materials.Select(m => $"{m.name}({m.shader.name}, color={GetMaterialColor(m)}, texture={GetMaterialTexture(m)})"));
                Debug.Log($"[SignalLostImport] {modelPath}: renderers={materials.Length}, materials=[{materialSummary}]");

                if (modelPath.Contains("/Survival/", StringComparison.Ordinal)
                    || modelPath.Contains("/SpaceStation/", StringComparison.Ordinal)
                    || modelPath.Contains("/Watercraft/", StringComparison.Ordinal)
                    || modelPath.Contains("/Blaster/", StringComparison.Ordinal))
                {
                    Material colormap = materials.FirstOrDefault(m => m.name == "colormap");
                    if (colormap != null && GetMaterialTexture(colormap) == "none")
                    {
                        errors.Add($"Colormap texture not assigned: {modelPath}");
                    }
                }
            }

            string[] allModelPaths = AssetDatabase.FindAssets("t:Model", new[] { "Assets/ThirdParty/Kenney/Models" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            foreach (string modelPath in allModelPaths)
            {
                Material[] materials = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>().ToArray();
                foreach (Material material in materials)
                {
                    if (!material.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
                    {
                        errors.Add($"Non-URP material shader: {modelPath} -> {material.name} ({material.shader.name})");
                    }

                    if (material.name == "colormap" && GetMaterialTexture(material) == "none")
                    {
                        errors.Add($"Colormap texture missing on model: {modelPath}");
                    }
                }
            }

            foreach (string path in TexturePathsUnder("Assets/ThirdParty/ambientCG/Textures"))
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    errors.Add($"Texture importer missing: {path}");
                    continue;
                }

                if (path.Contains("_NormalGL", StringComparison.Ordinal) && importer.textureType != TextureImporterType.NormalMap)
                {
                    errors.Add($"Normal map not configured: {path}");
                }
            }

            foreach (string path in TexturePathsUnder("Assets/Resources/UI"))
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite)
                {
                    errors.Add($"UI sprite not configured: {path}");
                }
            }

            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"[SignalLostImport] {error}");
                }

                throw new InvalidOperationException($"Asset import validation failed with {errors.Count} error(s).");
            }

            Debug.Log("[SignalLostImport] PHASE1_ASSET_IMPORT_OK");
        }

        private static object GetMaterialColor(Material material)
        {
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return "n/a";
        }

        private static string GetMaterialTexture(Material material)
        {
            Texture texture = null;
            if (material.HasProperty("_BaseMap"))
            {
                texture = material.GetTexture("_BaseMap");
            }
            else if (material.HasProperty("_MainTex"))
            {
                texture = material.GetTexture("_MainTex");
            }

            return texture != null ? texture.name : "none";
        }

        private static IEnumerable<string> TexturePathsUnder(string root)
        {
            return AssetDatabase.FindAssets("t:Texture2D", new[] { root })
                .Select(AssetDatabase.GUIDToAssetPath);
        }
    }
}
