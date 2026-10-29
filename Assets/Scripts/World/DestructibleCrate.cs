using SignalLost.Audio;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.World
{
    [RequireComponent(typeof(Health))]
    public sealed class DestructibleCrate : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float crateHealth = 30f;
        [SerializeField] private GameObject[] dropPrefabs;
        [SerializeField, Min(0f)] private float dropScatterRadius = 0.35f;

        private Health health;
        private bool broken;

        private void Awake()
        {
            health = GetComponent<Health>();
            health.Configure(crateHealth);
            health.Died += HandleDestroyed;
        }

        private void HandleDestroyed()
        {
            if (broken)
            {
                return;
            }

            broken = true;
            Sfx.PlayAt("Impact_Wood", transform.position, 0.8f);

            if (dropPrefabs != null)
            {
                foreach (GameObject prefab in dropPrefabs)
                {
                    if (prefab == null)
                    {
                        continue;
                    }

                    Vector3 scatter = Random.insideUnitSphere * dropScatterRadius;
                    scatter.y = Mathf.Abs(scatter.y) + 0.25f;
                    Instantiate(prefab, transform.position + Vector3.up * 0.4f + scatter, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                }
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= HandleDestroyed;
            }
        }
    }
}
