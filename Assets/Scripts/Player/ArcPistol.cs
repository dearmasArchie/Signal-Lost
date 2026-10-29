using System;
using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Effects;
using SignalLost.World;
using UnityEngine;

namespace SignalLost.Player
{
    public sealed class ArcPistol : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Camera aimCamera;
        [SerializeField, Min(0f)] private float damage = 25f;
        [SerializeField, Min(0f)] private float stunDuration = 0.5f;
        [SerializeField, Min(1f)] private float projectileSpeed = 55f;
        [SerializeField, Min(0.01f)] private float projectileRadius = 0.12f;
        [SerializeField] private Color projectileColor = new Color(0.2f, 0.85f, 1f);

        [Header("Ammo")]
        [SerializeField, Min(1)] private int magazineSize = 12;
        [SerializeField, Min(0)] private int startingReserve = 48;
        [SerializeField, Min(1)] private int maximumReserve = 60;
        [SerializeField, Min(0.05f)] private float fireInterval = 0.12f;
        [SerializeField, Min(0.1f)] private float reloadTime = 1.5f;

        [Header("Secondary")]
        [SerializeField, Min(0f)] private float secondaryDamage = 60f;
        [SerializeField, Min(1f)] private float secondaryChargeTime = 1.5f;
        [SerializeField, Min(1)] private int secondaryAmmoCost = 4;
        [SerializeField, Min(0.01f)] private float secondaryRange = 60f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Overheat")]
        [SerializeField, Min(1)] private int shotsPerOverheat = 6;
        [SerializeField, Min(0.5f)] private float overheatCooldown = 2f;

        [Header("Presentation")]
        [SerializeField] private GameObject weaponVisual;
        [SerializeField] private Light muzzleFlash;
        [SerializeField, Min(20f)] private float aimFieldOfView = 56f;

        private PlayerInputReader input;
        private PlayerInventory inventory;
        private float nextFireTime;
        private float reloadEndTime;
        private float flashEndTime;
        private float defaultFieldOfView = 72f;
        private bool reloading;
        private bool equipped;
        private int rapidShots;
        private float lastShotTime;
        private bool overheated;
        private float overheatEndTime;
        private float secondaryChargeStart;
        private bool chargingSecondary;

        public int MagazineAmmo { get; private set; }

        public int ReserveAmmo { get; private set; }

        public bool IsReloading => reloading;

        public bool IsEquipped => equipped;

        public bool IsOverheated => overheated;

        public float OverheatNormalized => overheated ? 1f : Mathf.Clamp01(rapidShots / (float)shotsPerOverheat);

        public float SecondaryChargeNormalized => chargingSecondary ? Mathf.Clamp01((Time.time - secondaryChargeStart) / secondaryChargeTime) : 0f;

        public event Action<int, int> AmmoChanged;
        public event Action<float> OverheatChanged;
        public event Action<float> SecondaryChargeChanged;

        public static event Action<Vector3, float> WeaponFired;

        private void Awake()
        {
            input = GetComponentInParent<PlayerInputReader>();
            inventory = GetComponentInParent<PlayerInventory>();

            if (aimCamera == null)
            {
                aimCamera = GetComponentInParent<Camera>();
            }

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            if (aimCamera != null)
            {
                defaultFieldOfView = aimCamera.fieldOfView;
            }
        }

        private void Start()
        {
            MagazineAmmo = Mathf.Clamp(magazineSize, 0, maximumReserve);
            ReserveAmmo = Mathf.Clamp(startingReserve, 0, maximumReserve - MagazineAmmo);
            SetEquipped(inventory == null || inventory.HasArcPistol);
            AmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);
        }

        private void Update()
        {
            GameManager manager = GameManager.Instance;
            bool canAim = manager != null &&
                          manager.State == GameState.Playing &&
                          equipped &&
                          input != null &&
                          input.AimHeld;

            if (aimCamera != null)
            {
                float targetFov = canAim ? aimFieldOfView : defaultFieldOfView;
                aimCamera.fieldOfView = Mathf.Lerp(
                    aimCamera.fieldOfView,
                    targetFov,
                    Time.unscaledDeltaTime * 10f);
            }

            if (manager == null || manager.State != GameState.Playing)
            {
                return;
            }

            if (muzzleFlash != null && muzzleFlash.enabled && Time.time >= flashEndTime)
            {
                muzzleFlash.enabled = false;
            }

            if (overheated && Time.time >= overheatEndTime)
            {
                overheated = false;
                rapidShots = 0;
                OverheatChanged?.Invoke(0f);
            }

            if (reloading)
            {
                if (Time.time >= reloadEndTime)
                {
                    FinishReload();
                }

                return;
            }

            if (!equipped || input == null)
            {
                return;
            }

            if (input.ReloadPressedThisFrame)
            {
                BeginReload();
            }

            if (input.AimHeld && !overheated && MagazineAmmo >= secondaryAmmoCost)
            {
                if (!chargingSecondary)
                {
                    chargingSecondary = true;
                    secondaryChargeStart = Time.time;
                }
                SecondaryChargeChanged?.Invoke(SecondaryChargeNormalized);
                if (SecondaryChargeNormalized >= 1f)
                {
                    FireSecondary();
                }
                return;
            }

            if (chargingSecondary)
            {
                chargingSecondary = false;
                SecondaryChargeChanged?.Invoke(0f);
            }

            if (overheated)
            {
                return;
            }

            if (input.FireHeld && Time.time >= nextFireTime)
            {
                Fire();
            }
        }

        public void SetEquipped(bool value)
        {
            equipped = value;

            if (weaponVisual != null)
            {
                weaponVisual.SetActive(value);
            }
        }

        public bool AddAmmo(int amount)
        {
            if (amount <= 0 || ReserveAmmo >= maximumReserve)
            {
                return false;
            }

            ReserveAmmo = Mathf.Clamp(ReserveAmmo + amount, 0, maximumReserve);
            AmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);
            return true;
        }

        private void Fire()
        {
            if (MagazineAmmo <= 0)
            {
                BeginReload();
                Sfx.Play2D("UI_Error", 0.35f);
                return;
            }

            if (overheated)
            {
                return;
            }

            nextFireTime = Time.time + fireInterval;
            MagazineAmmo--;
            AmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);

            if (Time.time - lastShotTime > 0.4f)
            {
                rapidShots = 0;
            }
            lastShotTime = Time.time;
            rapidShots++;
            OverheatChanged?.Invoke(OverheatNormalized);
            if (rapidShots >= shotsPerOverheat)
            {
                overheated = true;
                overheatEndTime = Time.time + overheatCooldown;
                OverheatChanged?.Invoke(1f);
                Sfx.PlayAt("Weapon_Overheat", muzzle != null ? muzzle.position : transform.position, 0.7f);
            }

            Vector3 origin = muzzle != null ? muzzle.position : transform.position + transform.forward * 0.35f;
            Vector3 direction = aimCamera != null ? aimCamera.transform.forward : transform.forward;
            EnergyProjectile.Spawn(
                projectilePrefab,
                transform.root.gameObject,
                origin,
                direction,
                damage,
                stunDuration,
                DamageType.Arc,
                projectileSpeed,
                projectileColor,
                projectileRadius);

            if (muzzleFlash != null)
            {
                muzzleFlash.enabled = true;
                flashEndTime = Time.time + 0.05f;
            }

            Sfx.PlayAt("Weapon_ArcPistol", origin, 0.8f, UnityEngine.Random.Range(0.97f, 1.03f));
            WeaponFired?.Invoke(origin, 12f);

            if (MagazineAmmo <= 0)
            {
                BeginReload();
            }
        }

        private void FireSecondary()
        {
            if (MagazineAmmo < secondaryAmmoCost || overheated)
            {
                chargingSecondary = false;
                SecondaryChargeChanged?.Invoke(0f);
                return;
            }

            MagazineAmmo -= secondaryAmmoCost;
            AmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);
            chargingSecondary = false;
            SecondaryChargeChanged?.Invoke(0f);
            nextFireTime = Time.time + 0.6f;

            Vector3 origin = muzzle != null ? muzzle.position : transform.position + transform.forward * 0.35f;
            Vector3 direction = aimCamera != null ? aimCamera.transform.forward : transform.forward;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, secondaryRange, hitMask))
            {
                if (hit.collider.TryGetComponent(out IDamageable damageable) && damageable.IsAlive)
                {
                    damageable.TakeDamage(new DamageInfo(secondaryDamage, DamageType.Arc, hit.point, hit.normal, transform.root.gameObject, 0f));
                }
                if (hit.collider.TryGetComponent(out Health health2) && health2 != null)
                {
                    health2.TakeDamage(new DamageInfo(secondaryDamage, DamageType.Arc, hit.point, hit.normal, transform.root.gameObject, 0f));
                }
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.enabled = true;
                flashEndTime = Time.time + 0.12f;
            }

            Sfx.PlayAt("Weapon_ArcPistol", origin, 0.9f, 0.7f);
            WeaponFired?.Invoke(origin, 18f);
        }

        private void BeginReload()
        {
            if (reloading || MagazineAmmo >= magazineSize || ReserveAmmo <= 0)
            {
                return;
            }

            reloading = true;
            reloadEndTime = Time.time + reloadTime;
            Sfx.PlayAt("Weapon_Overheat", muzzle != null ? muzzle.position : transform.position, 0.55f);
        }

        private void FinishReload()
        {
            int needed = magazineSize - MagazineAmmo;
            int transferred = Mathf.Min(needed, ReserveAmmo);
            MagazineAmmo += transferred;
            ReserveAmmo -= transferred;
            reloading = false;
            AmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);
        }
    }
}
