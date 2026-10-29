using SignalLost.Audio;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Effects
{
    public class EnergyProjectile : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float speed = 45f;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.12f;
        [SerializeField, Min(0.1f)] private float lifetime = 5f;
        [SerializeField] private GameObject impactPrefab;

        private GameObject ownerRoot;
        private Vector3 direction;
        private float damage;
        private float stunDuration;
        private DamageType damageType;
        private float age;

        public void Initialize(
            GameObject owner,
            Vector3 travelDirection,
            float damageAmount,
            float stunSeconds,
            DamageType type,
            float speedOverride = 0f)
        {
            ownerRoot = owner;
            direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector3.forward;
            damage = Mathf.Max(0f, damageAmount);
            stunDuration = Mathf.Max(0f, stunSeconds);
            damageType = type;

            if (speedOverride > 0f)
            {
                speed = speedOverride;
            }

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        public void SetCollisionRadius(float radius)
        {
            collisionRadius = Mathf.Max(0.01f, radius);
        }

        public static EnergyProjectile Spawn(
            GameObject prefab,
            GameObject owner,
            Vector3 origin,
            Vector3 travelDirection,
            float damage,
            float stunSeconds,
            DamageType type,
            float speed,
            Color color,
            float radius = 0.12f)
        {
            Vector3 normalizedDirection = travelDirection.sqrMagnitude > 0.0001f
                ? travelDirection.normalized
                : Vector3.forward;

            GameObject instance = prefab != null
                ? Instantiate(prefab, origin, Quaternion.LookRotation(normalizedDirection, Vector3.up))
                : CreateFallback(origin, normalizedDirection, color);

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }

            EnergyProjectile projectile = instance.GetComponent<EnergyProjectile>();

            if (projectile == null)
            {
                projectile = instance.AddComponent<EnergyProjectile>();
            }

            projectile.SetCollisionRadius(radius);
            projectile.Initialize(owner, normalizedDirection, damage, stunSeconds, type, speed);
            return projectile;
        }

        private void Update()
        {
            age += Time.deltaTime;

            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float step = speed * Time.deltaTime;

            if (step <= 0f || TryHit(step))
            {
                return;
            }

            transform.position += direction * step;
        }

        private bool TryHit(float distance)
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                transform.position,
                collisionRadius,
                direction,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            bool hasHit = false;
            RaycastHit closestHit = default;
            float closestDistance = float.MaxValue;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                if (ownerRoot != null && hit.collider.transform.IsChildOf(ownerRoot.transform))
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<EnergyProjectile>() != null)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestHit = hit;
                    hasHit = true;
                }
            }

            if (!hasHit)
            {
                return false;
            }

            transform.position = closestHit.point;
            IDamageable damageable = closestHit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(new DamageInfo(
                    damage,
                    damageType,
                    closestHit.point,
                    closestHit.normal,
                    ownerRoot,
                    stunDuration));

                if (stunDuration > 0f)
                {
                    IStunnable stunnable = closestHit.collider.GetComponentInParent<IStunnable>();

                    if (stunnable != null)
                    {
                        stunnable.ApplyStun(stunDuration);
                    }
                }
            }

            SpawnImpact(closestHit.point, closestHit.normal);
            Destroy(gameObject);
            return true;
        }

        private void SpawnImpact(Vector3 point, Vector3 normal)
        {
            Sfx.PlayAt(damageType == DamageType.Arc ? "Impact_Punch" : "Impact_Metal", point, 0.55f, Random.Range(0.9f, 1.1f));

            if (impactPrefab == null)
            {
                Color color = damageType == DamageType.Arc
                    ? new Color(0.25f, 0.9f, 1f)
                    : new Color(1f, 0.25f, 0.08f);
                CombatVfx.SpawnImpact(point, normal, color, damageType == DamageType.Laser ? 0.8f : 1f);
                return;
            }

            GameObject impact = Instantiate(impactPrefab, point, Quaternion.LookRotation(normal, Vector3.up));
            Destroy(impact, 2f);
        }

        private static GameObject CreateFallback(Vector3 origin, Vector3 direction, Color color)
        {
            GameObject projectile = new GameObject("EnergyProjectile");
            projectile.transform.position = origin;
            projectile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(projectile.transform, false);
            visual.transform.localScale = Vector3.one * 0.16f;
            Destroy(visual.GetComponent<Collider>());

            Renderer renderer = visual.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader != null)
            {
                Material material = new Material(shader);

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }

                renderer.material = material;
            }

            TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.12f;
            trail.startWidth = 0.08f;
            trail.endWidth = 0f;
            trail.material = renderer.sharedMaterial;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);

            Light glow = projectile.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = color;
            glow.range = 2.5f;
            glow.intensity = 1.75f;

            return projectile;
        }
    }
}
