using System;
using System.Collections;
using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Enemies;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.World
{
    public sealed class LaserTripwire : MonoBehaviour, IDamageable
    {
        [SerializeField] private GameObject beam;
        [SerializeField] private float beamDamagePerSecond = 10f;

        private bool disabled;

        public bool IsAlive => !disabled;
        public Transform DamageTransform => transform;

        public bool TakeDamage(DamageInfo damage)
        {
            if (disabled || damage.Amount <= 0f)
            {
                return false;
            }

            Disable();
            return true;
        }

        private void Disable()
        {
            if (disabled)
            {
                return;
            }

            disabled = true;
            if (beam != null)
            {
                beam.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        private void OnTriggerStay(Collider other)
        {
            if (disabled)
            {
                return;
            }

            if (other.TryGetComponent(out PlayerHealth health) && health.IsAlive && health.Health != null)
            {
                health.Health.TakeDamage(new DamageInfo(beamDamagePerSecond * Time.deltaTime, DamageType.Environmental, transform.position, Vector3.up, gameObject));
            }
        }
    }

    public sealed class BunkerHazards : MonoBehaviour
    {
        [SerializeField] private GameObject[] laserBeams;
        [SerializeField] private Renderer[] switchRenderers;
        [SerializeField] private Material switchOff;
        [SerializeField] private Material switchOn;
        [SerializeField] private GameObject fusePodA;
        [SerializeField] private GameObject electrifiedWater;
        [SerializeField] private GameObject[] cratePlatforms;
        [SerializeField] private GameObject quarantineDoors;
        [SerializeField] private float quarantineDuration = 45f;
        [SerializeField] private Transform[] quarantineSpawns;
        [SerializeField] private GameObject enforcerPrefab;
        [SerializeField] private GameObject wardenPrefab;
        [SerializeField] private GameObject fusePodB;

        private readonly int[] correctOrder = new[] { 2, 1, 3 };
        private int sequenceIndex;
        private bool sequenceFailed;
        private bool fuseAUnlocked;
        private bool waterDrained;
        private bool quarantineActive;
        private bool fuseBUnlocked;

        public void ActivateSwitch(int index)
        {
            if (fuseAUnlocked || sequenceFailed)
            {
                return;
            }

            if (index == correctOrder[sequenceIndex])
            {
                sequenceIndex++;
                if (switchRenderers != null && index >= 1 && index <= switchRenderers.Length)
                {
                    if (switchOn != null)
                    {
                        switchRenderers[index - 1].material = switchOn;
                    }
                }
                if (sequenceIndex >= correctOrder.Length)
                {
                    UnlockFuseA();
                }
                return;
            }

            sequenceFailed = true;
            GameManager.Instance?.ShowNotification("Wrong sequence! Alarm triggered.");
            SpawnDroneNearBreaker();
        }

        public void DrainWater()
        {
            if (waterDrained)
            {
                return;
            }

            waterDrained = true;
            if (electrifiedWater != null)
            {
                electrifiedWater.SetActive(false);
            }
            GameManager.Instance?.ShowNotification("Floodway drained.");
        }

        public void GrabFuseB()
        {
            if (fuseBUnlocked || quarantineActive)
            {
                StartQuarantine();
                return;
            }

            if (fuseBUnlocked)
            {
                return;
            }

            GameManager.Instance?.ShowNotification("Fuse collected.");
            GameManager.Instance?.NotifyFuseCollected();
            if (fusePodB != null)
            {
                fusePodB.SetActive(false);
            }
            fuseBUnlocked = true;
        }

        private void StartQuarantine()
        {
            if (quarantineActive)
            {
                return;
            }

            quarantineActive = true;
            if (quarantineDoors != null)
            {
                quarantineDoors.SetActive(true);
            }
            GameManager.Instance?.ShowNotification("Quarantine lockdown engaged. Survive 45 seconds.");
            StartCoroutine(QuarantineRoutine());
        }

        private IEnumerator QuarantineRoutine()
        {
            float end = Time.time + quarantineDuration;
            int spawned = 0;
            while (Time.time < end)
            {
                if (spawned < 3 && Time.time > end - quarantineDuration + 5f * spawned)
                {
                    SpawnQuarantineEnemy(spawned == 2);
                    spawned++;
                }
                yield return null;
            }

            if (quarantineDoors != null)
            {
                quarantineDoors.SetActive(false);
            }
            GameManager.Instance?.ShowNotification("Quarantine doors unsealed.");
            if (!fuseBUnlocked)
            {
                GameManager.Instance?.ShowNotification("Fuse collected.");
                GameManager.Instance?.NotifyFuseCollected();
                if (fusePodB != null)
                {
                    fusePodB.SetActive(false);
                }
                fuseBUnlocked = true;
            }
        }

        private void UnlockFuseA()
        {
            if (fuseAUnlocked)
            {
                return;
            }

            fuseAUnlocked = true;
            if (fusePodA != null)
            {
                fusePodA.SetActive(false);
            }
            if (laserBeams != null)
            {
                for (int i = 0; i < laserBeams.Length; i++)
                {
                    if (laserBeams[i] != null)
                    {
                        laserBeams[i].SetActive(false);
                    }
                }
            }
            GameManager.Instance?.ShowNotification("Fuse collected.");
            GameManager.Instance?.NotifyFuseCollected();
        }

        private void SpawnDroneNearBreaker()
        {
            if (enforcerPrefab == null)
            {
                return;
            }

            Vector3 pos = transform.position + new Vector3(0f, 0f, 30f);
            Instantiate(enforcerPrefab, pos, Quaternion.identity);
        }

        private void SpawnQuarantineEnemy(bool heavy)
        {
            GameObject prefab = heavy ? wardenPrefab : enforcerPrefab;
            if (prefab == null || quarantineSpawns == null || quarantineSpawns.Length == 0)
            {
                return;
            }

            Transform t = quarantineSpawns[UnityEngine.Random.Range(0, quarantineSpawns.Length)];
            Instantiate(prefab, t.position, t.rotation);
        }

        private void Update()
        {
            if (!waterDrained && electrifiedWater != null && electrifiedWater.activeSelf)
            {
                Collider[] cols = Physics.OverlapBox(electrifiedWater.transform.position, electrifiedWater.transform.localScale * 0.5f);
                for (int i = 0; i < cols.Length; i++)
                {
                    if (cols[i].TryGetComponent(out PlayerHealth health) && health.IsAlive && health.Health != null)
                    {
                        health.Health.TakeDamage(new DamageInfo(10f * Time.deltaTime, DamageType.Environmental, electrifiedWater.transform.position, Vector3.up, gameObject));
                    }
                }
            }
        }
    }
}
