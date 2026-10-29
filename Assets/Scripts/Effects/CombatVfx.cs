using UnityEngine;

namespace SignalLost.Effects
{
    public static class CombatVfx
    {
        private static Material particleMaterial;

        public static void SpawnImpact(Vector3 point, Vector3 normal, Color color, float scale = 1f)
        {
            GameObject root = new GameObject("VFX_Impact");
            root.transform.position = point;
            root.transform.rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up);

            ParticleSystem sparks = CreateBurst(root.transform, "Sparks", color, 16, 0.32f, 0.075f, 1.2f, 2.6f);
            ParticleSystem.ShapeModule shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 36f;
            shape.radius = 0.02f;

            AddLight(root.transform, color, 2.4f, 0.16f, 2.2f * scale, 2.2f);
            Object.Destroy(root, 1.1f);
        }

        public static void SpawnExplosion(Vector3 point, Color color, float scale = 1f)
        {
            GameObject root = new GameObject("VFX_Explosion");
            root.transform.position = point;

            ParticleSystem sparks = CreateBurst(root.transform, "Sparks", color, 36, 0.65f, 0.14f * scale, 3.8f * scale, 1.8f);
            ParticleSystem.ShapeModule sparkShape = sparks.shape;
            sparkShape.shapeType = ParticleSystemShapeType.Sphere;
            sparkShape.radius = 0.18f * scale;

            ParticleSystem smoke = CreateBurst(root.transform, "Smoke", new Color(0.12f, 0.12f, 0.14f, 0.55f), 24, 1.5f, 0.55f * scale, 1.4f * scale, 0.35f);
            ParticleSystem.ShapeModule smokeShape = smoke.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Sphere;
            smokeShape.radius = 0.3f * scale;

            AddLight(root.transform, color, 6f, 0.42f, 7f * scale, 4f);
            Object.Destroy(root, 2.8f);
        }

        private static ParticleSystem CreateBurst(
            Transform parent,
            string name,
            Color color,
            int count,
            float lifetime,
            float size,
            float speed,
            float gravity)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            ParticleSystem particles = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.duration = 0.15f;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sharedMaterial = GetParticleMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return particles;
        }

        private static void AddLight(Transform parent, Color color, float range, float intensity, float duration, float yOffset)
        {
            GameObject lightObject = new GameObject("Flash");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = new Vector3(0f, yOffset, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;

            VfxLightFade fade = lightObject.AddComponent<VfxLightFade>();
            fade.Configure(light, duration, intensity);
        }

        private static Material GetParticleMaterial()
        {
            if (particleMaterial != null)
            {
                return particleMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }

            particleMaterial = new Material(shader)
            {
                name = "M_VfxParticle",
                color = Color.white
            };
            return particleMaterial;
        }
    }

    public sealed class VfxLightFade : MonoBehaviour
    {
        private Light targetLight;
        private float duration;
        private float startIntensity;

        public void Configure(Light lightSource, float fadeDuration, float intensity)
        {
            targetLight = lightSource;
            duration = Mathf.Max(0.01f, fadeDuration);
            startIntensity = intensity;
        }

        private void Update()
        {
            if (targetLight == null)
            {
                return;
            }

            targetLight.intensity = Mathf.Lerp(startIntensity, 0f, Time.deltaTime / duration);
            startIntensity = targetLight.intensity;
            if (targetLight.intensity <= 0.01f)
            {
                targetLight.enabled = false;
            }
        }
    }
}
