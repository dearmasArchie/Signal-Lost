using UnityEditor;
using UnityEngine;

namespace SignalLost.EditorTools
{
    public static class SignalLostWorldTerrain
    {
        public const float Size = 320f;
        public const float Height = 64f;
        public const float WaterLevel = 1f;
        public const string DataAssetPath = "Assets/Generated/Terrain/SignalLostTerrain.asset";

        private const int HeightmapResolution = 513;
        private const int AlphamapResolution = 512;

        private static readonly Vector2[][] Paths =
        {
            new[]
            {
                new Vector2(0f, -84f),
                new Vector2(0f, -76f),
                new Vector2(0f, -35f)
            },
            new[]
            {
                new Vector2(0f, -35f),
                new Vector2(-20f, -18f),
                new Vector2(-45f, 3f),
                new Vector2(-56f, 8f)
            },
            new[]
            {
                new Vector2(0f, -35f),
                new Vector2(19f, -12f),
                new Vector2(42f, 13f),
                new Vector2(56f, 24f)
            },
            new[]
            {
                new Vector2(0f, -35f),
                new Vector2(0f, -5f),
                new Vector2(0f, 34f),
                new Vector2(0f, 64f)
            },
            new[]
            {
                new Vector2(0f, -84f),
                new Vector2(0f, -112f)
            }
        };

        private static readonly Pad[] Pads =
        {
            new Pad(new Vector2(0f, -76f), 9f, 8f, 3f),
            new Pad(new Vector2(-64f, 8f), 13f, 10f, 8f),
            new Pad(new Vector2(64f, 24f), 13f, 10f, 10f),
            new Pad(new Vector2(0f, 76f), 18f, 12f, 24f)
        };

        public static Terrain Create(Transform parent)
        {
            if (AssetDatabase.LoadAssetAtPath<TerrainData>(DataAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(DataAssetPath);
            }

            TerrainData data = new TerrainData
            {
                heightmapResolution = HeightmapResolution,
                alphamapResolution = AlphamapResolution,
                baseMapResolution = 1024,
                size = new Vector3(Size, Height, Size)
            };

            data.terrainLayers = new[]
            {
                SignalLostWorldAssetFactory.GetTerrainLayer("Grass"),
                SignalLostWorldAssetFactory.GetTerrainLayer("Dirt"),
                SignalLostWorldAssetFactory.GetTerrainLayer("Sand"),
                SignalLostWorldAssetFactory.GetTerrainLayer("Rock")
            };

            int resolution = data.heightmapResolution;
            float[,] heights = new float[resolution, resolution];

            for (int z = 0; z < resolution; z++)
            {
                float wz = -Size * 0.5f + (z / (resolution - 1f)) * Size;

                for (int x = 0; x < resolution; x++)
                {
                    float wx = -Size * 0.5f + (x / (resolution - 1f)) * Size;
                    heights[z, x] = Mathf.Clamp01(ComputeHeight(wx, wz) / Height);
                }
            }

            ApplyPads(heights, resolution);
            data.SetHeights(0, 0, heights);
            data.SetAlphamaps(0, 0, BuildSplatMap(data, heights));
            AssetDatabase.CreateAsset(data, DataAssetPath);

            GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "Terrain";
            terrainObject.transform.SetParent(parent, false);
            terrainObject.transform.position = new Vector3(-Size * 0.5f, 0f, -Size * 0.5f);
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 5f;
            terrain.basemapDistance = 1400f;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            EditorUtility.SetDirty(terrainObject);
            AssetDatabase.SaveAssets();
            return terrain;
        }

        public static float GetHeight(Terrain terrain, float worldX, float worldZ)
        {
            return terrain != null ? terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) : 0f;
        }

        public static float DistanceToPaths(float worldX, float worldZ)
        {
            Vector2 point = new Vector2(worldX, worldZ);
            float closest = float.MaxValue;

            foreach (Vector2[] path in Paths)
            {
                for (int i = 0; i < path.Length - 1; i++)
                {
                    closest = Mathf.Min(closest, DistanceToSegment(point, path[i], path[i + 1]));
                }
            }

            return closest;
        }

        public static bool IsNearStructure(float worldX, float worldZ, float margin)
        {
            Vector2 point = new Vector2(worldX, worldZ);

            foreach (Pad pad in Pads)
            {
                if (Vector2.Distance(point, pad.Center) < pad.Radius + margin)
                {
                    return true;
                }
            }

            return false;
        }

        public static float SampleHeight(float worldX, float worldZ)
        {
            Terrain terrain = Terrain.activeTerrain;

            if (terrain == null)
            {
                return 0f;
            }

            return terrain.SampleHeight(new Vector3(worldX, 0f, worldZ));
        }

        private static float ComputeHeight(float x, float z)
        {
            float radius = Mathf.Sqrt(x * x + z * z);
            float radial = Mathf.Pow(1f - Smooth01(radius / 120f), 1.2f) * 8f;
            float north = Smooth01(Mathf.InverseLerp(-20f, 110f, z));
            float ridge = Mathf.Pow(north, 1.4f) * 24f;
            float noise = (Mathf.PerlinNoise(x * 0.033f + 11.2f, z * 0.033f + 4.7f) - 0.5f) * 3.2f;
            noise += (Mathf.PerlinNoise(x * 0.105f + 2.3f, z * 0.105f + 8.7f) - 0.5f) * 1.1f;
            float coast = Smooth01(Mathf.InverseLerp(100f, 140f, radius)) * 9f;
            return Mathf.Max(0f, 1.5f + radial + ridge + noise * (1f - coast / 12f) - coast);
        }

        private static void ApplyPads(float[,] heights, int resolution)
        {
            foreach (Pad pad in Pads)
            {
                float xMin = pad.Center.x - pad.Radius - pad.Blend;
                float xMax = pad.Center.x + pad.Radius + pad.Blend;
                float zMin = pad.Center.y - pad.Radius - pad.Blend;
                float zMax = pad.Center.y + pad.Radius + pad.Blend;
                int xStart = Mathf.Clamp(Mathf.FloorToInt((xMin + Size * 0.5f) / Size * (resolution - 1f)), 0, resolution - 1);
                int xEnd = Mathf.Clamp(Mathf.CeilToInt((xMax + Size * 0.5f) / Size * (resolution - 1f)), 0, resolution - 1);
                int zStart = Mathf.Clamp(Mathf.FloorToInt((zMin + Size * 0.5f) / Size * (resolution - 1f)), 0, resolution - 1);
                int zEnd = Mathf.Clamp(Mathf.CeilToInt((zMax + Size * 0.5f) / Size * (resolution - 1f)), 0, resolution - 1);

                for (int z = zStart; z <= zEnd; z++)
                {
                    float wz = -Size * 0.5f + (z / (resolution - 1f)) * Size;

                    for (int x = xStart; x <= xEnd; x++)
                    {
                        float wx = -Size * 0.5f + (x / (resolution - 1f)) * Size;
                        float distance = Vector2.Distance(new Vector2(wx, wz), pad.Center);

                        if (distance > pad.Radius + pad.Blend)
                        {
                            continue;
                        }

                        float blend = distance <= pad.Radius
                            ? 1f
                            : 1f - Smooth01(Mathf.InverseLerp(pad.Radius, pad.Radius + pad.Blend, distance));
                        float target = pad.Height / Height;
                        heights[z, x] = Mathf.Lerp(heights[z, x], target, blend);
                    }
                }
            }
        }

        private static float[,,] BuildSplatMap(TerrainData data, float[,] heights)
        {
            int resolution = data.alphamapResolution;
            float[,,] map = new float[resolution, resolution, 4];

            for (int z = 0; z < resolution; z++)
            {
                float v = z / (resolution - 1f);
                float wz = -Size * 0.5f + v * Size;

                for (int x = 0; x < resolution; x++)
                {
                    float u = x / (resolution - 1f);
                    float wx = -Size * 0.5f + u * Size;
                    int heightX = Mathf.RoundToInt(u * (heights.GetLength(1) - 1));
                    int heightZ = Mathf.RoundToInt(v * (heights.GetLength(0) - 1));
                    float height = heights[heightZ, heightX] * Height;
                    float steepness = data.GetSteepness(u, v);
                    float pathDistance = DistanceToPaths(wx, wz);
                    float variation = Mathf.PerlinNoise(wx * 0.08f + 5.1f, wz * 0.08f + 19.4f);

                    float grass = 1f;
                    float dirt = Mathf.Max(0f, 1f - Smooth01(Mathf.InverseLerp(1.4f, 4.2f, pathDistance))) * 1.8f;
                    float sand = Mathf.Max(0f, 1f - Smooth01(Mathf.InverseLerp(2.6f, 4.8f, height))) * 2.2f;
                    float rock = Smooth01(Mathf.InverseLerp(30f, 48f, steepness)) * 1.5f;
                    rock += Smooth01(Mathf.InverseLerp(22f, 31f, height)) * 0.35f;
                    rock += Mathf.Max(0f, variation - 0.58f) * 2.2f * Smooth01(Mathf.InverseLerp(12f, 24f, height));

                    if (height > 18f)
                    {
                        grass *= 0.75f;
                    }

                    float total = grass + dirt + sand + rock;
                    map[z, x, 0] = grass / total;
                    map[z, x, 1] = dirt / total;
                    map[z, x, 2] = sand / total;
                    map[z, x, 3] = rock / total;
                }
            }

            return map;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared < 0.0001f)
            {
                return Vector2.Distance(point, a);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
            return Vector2.Distance(point, a + segment * t);
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private readonly struct Pad
        {
            public Pad(Vector2 center, float radius, float blend, float height)
            {
                Center = center;
                Radius = radius;
                Blend = blend;
                Height = height;
            }

            public Vector2 Center { get; }

            public float Radius { get; }

            public float Blend { get; }

            public float Height { get; }
        }
    }
}
