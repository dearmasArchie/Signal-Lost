using System;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Player
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingMedkits;
        [SerializeField] private ArcPistol arcPistol;

        private PlayerHealth playerHealth;

        public int Medkits { get; private set; }

        public bool HasArcPistol { get; private set; }

        public event Action Changed;

        private void Awake()
        {
            Medkits = Mathf.Max(0, startingMedkits);
            playerHealth = GetComponent<PlayerHealth>();

            if (arcPistol == null)
            {
                arcPistol = GetComponentInChildren<ArcPistol>(true);
            }
        }

        public void GiveArcPistol()
        {
            if (HasArcPistol)
            {
                return;
            }

            HasArcPistol = true;
            arcPistol?.SetEquipped(true);
            GameManager.Instance?.NotifyArcPistolCollected();
            Changed?.Invoke();
        }

        public void CollectFuse()
        {
            GameManager.Instance?.NotifyFuseCollected();
        }

        public void AddMedkit(int amount = 1)
        {
            Medkits = Mathf.Max(0, Medkits + Mathf.Max(0, amount));
            Changed?.Invoke();
        }

        public bool UseMedkit(float healAmount = 25f)
        {
            if (Medkits <= 0 || playerHealth == null || !playerHealth.CanHeal)
            {
                return false;
            }

            if (playerHealth.Heal(healAmount) <= 0f)
            {
                return false;
            }

            Medkits--;
            Changed?.Invoke();
            return true;
        }

        public int GetAvailableFuses()
        {
            return GameManager.Instance != null ? GameManager.Instance.Progress.AvailableFuses : 0;
        }
    }
}
