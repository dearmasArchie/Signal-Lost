using System;
using System.Collections;
using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;
using UnityEngine.AI;

namespace SignalLost.Enemies
{
    public sealed class BossController : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 500f;
        [SerializeField] private GameObject frontShield;
        [SerializeField] private Transform[] plasmaOrbs;
        [SerializeField] private float plasmaInterval = 4f;
        [SerializeField] private Transform[] pylons;
        [SerializeField] private Transform core;
        [SerializeField] private GameObject explosionPrefab;

        private Health health;
        private bool phaseTwo;
        private float nextPlasma;
        private int destroyedPylons;
        private bool dead;

        public bool IsAlive => health != null && health.IsAlive;
        public Transform DamageTransform => core != null ? core : transform;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (health == null)
            {
                health = gameObject.AddComponent<Health>();
            }
            health.Configure(maxHealth);
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
            if (frontShield != null)
            {
                frontShield.SetActive(true);
            }
        }

        private void Update()
        {
            if (dead || health == null || !health.IsAlive)
            {
                return;
            }

            if (health.CurrentHealth <= maxHealth * 0.5f && !phaseTwo)
            {
                EnterPhaseTwo();
            }

            if (!phaseTwo && Time.time >= nextPlasma)
            {
                nextPlasma = Time.time + plasmaInterval;
                FirePlasma();
            }
        }

        public bool TakeDamage(DamageInfo damage)
        {
            if (dead || health == null)
            {
                return false;
            }

            if (!phaseTwo && frontShield != null && frontShield.activeSelf && damage.Type == DamageType.Arc && damage.Amount < 50f)
            {
                return false;
            }

            if (!phaseTwo && frontShield != null && frontShield.activeSelf && damage.Amount >= 50f)
            {
                frontShield.SetActive(false);
            }

            bool result = health.TakeDamage(damage);
            return result;
        }

        public void DestroyPylon()
        {
            destroyedPylons++;
            if (destroyedPylons >= 2 && phaseTwo && frontShield != null)
            {
                frontShield.SetActive(false);
            }
        }

        private void EnterPhaseTwo()
        {
            phaseTwo = true;
            if (frontShield != null)
            {
                frontShield.SetActive(true);
            }
            GameManager.Instance?.ShowNotification("UNIT 00-TITAN - CORE EXPOSED VIA PYLONS");
        }

        private void FirePlasma()
        {
            if (plasmaOrbs == null)
            {
                return;
            }

            for (int i = 0; i < plasmaOrbs.Length; i++)
            {
                if (plasmaOrbs[i] == null)
                {
                    continue;
                }

                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "PlasmaOrb";
                orb.transform.position = plasmaOrbs[i].position;
                orb.transform.localScale = Vector3.one * 0.4f;
                orb.AddComponent<Rigidbody>().useGravity = false;
                orb.GetComponent<Collider>().isTrigger = true;
                orb.AddComponent<PlasmaOrb>();
            }
        }

        private void HandleDamaged(DamageInfo info, float amount)
        {
        }

        private void HandleDied()
        {
            if (dead)
            {
                return;
            }

            dead = true;
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }

            Sfx.PlayAt("Explosion_Large", transform.position, 1f);
            GameManager.Instance?.ShowNotification("Unit 00-Titan destroyed.");
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
                health.Died -= HandleDied;
            }
        }
    }

    public sealed class PlasmaOrb : MonoBehaviour
    {
        private void Start()
        {
            Destroy(gameObject, 5f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerHealth health) && health.IsAlive && health.Health != null)
            {
                health.Health.TakeDamage(new DamageInfo(15f, DamageType.Laser, transform.position, Vector3.up, gameObject));
            }
        }
    }

    public sealed class BossPylon : MonoBehaviour, IDamageable
    {
        private BossController boss;
        private bool destroyed;

        public bool IsAlive => !destroyed;
        public Transform DamageTransform => transform;

        private void Awake()
        {
            boss = GetComponentInParent<BossController>();
        }

        public bool TakeDamage(DamageInfo damage)
        {
            if (destroyed || damage.Amount <= 0f)
            {
                return false;
            }

            destroyed = true;
            boss?.DestroyPylon();
            gameObject.SetActive(false);
            return true;
        }
    }

    public sealed class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private float duration = 90f;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject[] enemyPrefabs;
        [SerializeField] private int waveCount = 3;

        private bool active;
        private float endTime;
        private int currentWave;

        public bool IsActive => active;

        public void StartDefense()
        {
            if (active)
            {
                return;
            }

            active = true;
            endTime = Time.time + duration;
            currentWave = 0;
            GameManager.Instance?.ShowNotification("Radio Tower defense - Hold the courtyard!");
            StartCoroutine(SpawnWaves());
        }

        private IEnumerator SpawnWaves()
        {
            for (int wave = 0; wave < waveCount; wave++)
            {
                int count = 2 + wave;
                for (int i = 0; i < count; i++)
                {
                    SpawnEnemy(wave);
                    yield return new WaitForSeconds(1f);
                }
                yield return new WaitForSeconds(8f);
            }

            yield return new WaitForSeconds(Mathf.Max(0f, endTime - Time.time));
            GameManager.Instance?.ShowNotification("Signal alignment complete.");
        }

        private void SpawnEnemy(int wave)
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
            {
                return;
            }

            GameObject prefab = enemyPrefabs[Mathf.Min(wave, enemyPrefabs.Length - 1)];
            Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            Instantiate(prefab, point.position, point.rotation);
        }
    }
}
