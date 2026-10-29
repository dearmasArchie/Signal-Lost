using System.Collections;
using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Effects;
using SignalLost.Player;
using UnityEngine;
using UnityEngine.AI;

namespace SignalLost.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class SentryBotAI : MonoBehaviour, IStunnable
    {
        private enum BotState
        {
            Patrol,
            Chase,
            Attack,
            Stunned,
            Dead
        }

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField, Min(0.1f)] private float patrolSpeed = 1.6f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.4f;

        [Header("Senses")]
        [SerializeField, Min(1f)] private float sightDistance = 15f;
        [SerializeField, Range(1f, 360f)] private float fieldOfView = 90f;
        [SerializeField, Min(1f)] private float hearingRadius = 12f;
        [SerializeField, Min(0.1f)] private float eyeHeight = 1.4f;
        [SerializeField, Min(0.1f)] private float playerAimHeight = 1.2f;
        [SerializeField] private LayerMask sightMask = ~0;

        [Header("Attack")]
        [SerializeField, Min(1f)] private float attackRange = 8f;
        [SerializeField, Min(0.2f)] private float attackInterval = 1.5f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(1f)] private float projectileSpeed = 28f;
        [SerializeField] private GameObject laserProjectilePrefab;
        [SerializeField] private Transform muzzle;

        [Header("Identity")]
        [SerializeField] private bool heavyWarden;
        [SerializeField, Min(1f)] private float standardHealth = 75f;
        [SerializeField, Min(1f)] private float wardenHealth = 150f;
        [SerializeField, Min(0.5f)] private float lostSightTimeout = 5f;

        [Header("Presentation")]
        [SerializeField] private Renderer sensorRenderer;
        [SerializeField] private Color patrolColor = new Color(0.15f, 0.45f, 1f);
        [SerializeField] private Color chaseColor = new Color(1f, 0.82f, 0.1f);
        [SerializeField] private Color attackColor = new Color(1f, 0.12f, 0.08f);
        [SerializeField] private Color stunnedColor = new Color(0.6f, 0.9f, 1f);
        [SerializeField] private Color hitColor = new Color(1f, 0.08f, 0.03f);
        [SerializeField] private Color laserColor = new Color(1f, 0.15f, 0.05f);
        [SerializeField, Min(0.5f)] private float deathDelay = 3.5f;
        [SerializeField] private GameObject explosionPrefab;

        private NavMeshAgent agent;
        private Health health;
        private PlayerHealth player;
        private Renderer[] bodyRenderers;
        private Color[] bodyBaseColors;
        private MaterialPropertyBlock propertyBlock;
        private BotState state = BotState.Patrol;
        private Vector3 lastKnownPlayerPosition;
        private Vector3 startPosition;
        private int patrolIndex;
        private float lostSightTimer;
        private float nextAttackTime;
        private float stunEndTime;
        private float flashEndTime;
        private float alertSoundTime;
        private bool hasVisual;

        public bool IsDead => state == BotState.Dead;

        public bool IsAlerted => state == BotState.Chase || state == BotState.Attack;

        public bool IsHeavyWarden => heavyWarden;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();

            if (health == null)
            {
                health = gameObject.AddComponent<Health>();
            }

            health.Configure(heavyWarden ? wardenHealth : standardHealth);
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;

            if (heavyWarden)
            {
                sightDistance *= 1.2f;
                attackRange *= 1.1f;
                attackInterval *= 0.75f;
            }

            propertyBlock = new MaterialPropertyBlock();
            bodyRenderers = GetComponentsInChildren<Renderer>(true);
            bodyBaseColors = new Color[bodyRenderers.Length];

            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                bodyBaseColors[i] = ReadRendererColor(bodyRenderers[i]);
            }

            startPosition = transform.position;
            agent.speed = patrolSpeed;
            ApplySensorStateColor();
        }

        private void OnEnable()
        {
            ArcPistol.WeaponFired += HandleWeaponFired;
        }

        private void OnDisable()
        {
            ArcPistol.WeaponFired -= HandleWeaponFired;
        }

        private void Start()
        {
            player = Object.FindAnyObjectByType<PlayerHealth>();

            if (!agent.enabled || !agent.isOnNavMesh)
            {
                agent.enabled = false;
                StartCoroutine(EnableAgentWhenNavMeshReady());
            }
        }

        private IEnumerator EnableAgentWhenNavMeshReady()
        {
            Vector3 spawn = transform.position;
            float deadline = Time.time + 5f;

            while (agent != null && !agent.enabled)
            {
                if (NavMesh.SamplePosition(spawn, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.enabled = true;
                    agent.Warp(hit.position);
                    agent.speed = patrolSpeed;
                    yield break;
                }

                if (Time.time >= deadline)
                {
                    Debug.LogError($"[SentryBot] {name} could not initialize its NavMeshAgent.");
                    yield break;
                }

                yield return null;
            }
        }

        private void Update()
        {
            if (state == BotState.Dead)
            {
                return;
            }

            UpdateHitFlash();

            if (!agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            if (state == BotState.Stunned)
            {
                if (Time.time >= stunEndTime)
                {
                    ApplyBodyBaseColors();
                    SetState(CanSeePlayer(out _) ? BotState.Chase : BotState.Patrol);
                }

                return;
            }

            GameManager manager = GameManager.Instance;

            if (manager != null && manager.State != GameState.Playing)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }

                return;
            }

            if (player == null || !player.IsAlive)
            {
                player = Object.FindAnyObjectByType<PlayerHealth>();
            }

            if (agent.isOnNavMesh)
            {
                agent.isStopped = state == BotState.Attack;
            }

            float playerDistance = 0f;
            hasVisual = player != null && player.IsAlive && CanSeePlayer(out playerDistance);

            if (hasVisual)
            {
                lastKnownPlayerPosition = player.transform.position;
                lostSightTimer = 0f;
            }

            if (!hasVisual)
            {
                lostSightTimer += Time.deltaTime;
            }

            switch (state)
            {
                case BotState.Patrol:
                    UpdatePatrol();
                    break;
                case BotState.Chase:
                    UpdateChase(playerDistance);
                    break;
                case BotState.Attack:
                    UpdateAttack(playerDistance);
                    break;
            }
        }

        public void ApplyStun(float duration)
        {
            if (state == BotState.Dead || duration <= 0f)
            {
                return;
            }

            stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);
            SetState(BotState.Stunned);
        }

        private void UpdatePatrol()
        {
            if (hasVisual)
            {
                SetState(BotState.Chase);
                return;
            }

            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Transform waypoint = patrolPoints[patrolIndex];

                if (waypoint != null)
                {
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.35f)
                    {
                        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                        waypoint = patrolPoints[patrolIndex];
                    }

                    if (waypoint != null && agent.isOnNavMesh)
                    {
                        agent.SetDestination(waypoint.position);
                    }

                    return;
                }
            }

            if (!agent.pathPending && !agent.hasPath && agent.isOnNavMesh)
            {
                agent.SetDestination(startPosition);
            }
        }

        private void UpdateChase(float playerDistance)
        {
            if (hasVisual && playerDistance <= attackRange)
            {
                SetState(BotState.Attack);
                return;
            }

            if (!hasVisual && lostSightTimer >= lostSightTimeout)
            {
                SetState(BotState.Patrol);
                return;
            }

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(lastKnownPlayerPosition);
            }
        }

        private void UpdateAttack(float playerDistance)
        {
            if (!hasVisual)
            {
                SetState(BotState.Chase);
                return;
            }

            if (playerDistance > attackRange * 1.15f)
            {
                SetState(BotState.Chase);
                return;
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackInterval;
            FireLaser();
        }

        private bool CanSeePlayer(out float distance)
        {
            distance = 0f;

            if (player == null || !player.IsAlive)
            {
                return false;
            }

            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 target = player.transform.position + Vector3.up * playerAimHeight;
            Vector3 toTarget = target - eye;
            distance = toTarget.magnitude;

            if (distance > sightDistance || distance <= 0.001f)
            {
                return false;
            }

            if (Vector3.Angle(transform.forward, toTarget) > fieldOfView * 0.5f)
            {
                return false;
            }

            RaycastHit[] hits = Physics.RaycastAll(
                eye,
                toTarget.normalized,
                distance,
                sightMask,
                QueryTriggerInteraction.Ignore);

            RaycastHit? closest = null;
            float closestDistance = float.MaxValue;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closest = hit;
                }
            }

            return closest.HasValue
                && closest.Value.collider.GetComponentInParent<PlayerHealth>() != null;
        }

        private void FireLaser()
        {
            if (player == null)
            {
                return;
            }

            Vector3 origin = muzzle != null
                ? muzzle.position
                : transform.position + Vector3.up * eyeHeight + transform.forward * 0.5f;
            Vector3 target = player.transform.position + Vector3.up * playerAimHeight;
            Vector3 direction = (target - origin).normalized;

            EnergyProjectile.Spawn(
                laserProjectilePrefab,
                gameObject,
                origin,
                direction,
                attackDamage,
                0f,
                DamageType.Laser,
                projectileSpeed,
                laserColor,
                0.1f);

            Sfx.PlayAt("Weapon_HeavyAlt", origin, 0.65f, Random.Range(0.92f, 1.05f));
        }

        private void HandleWeaponFired(Vector3 position, float radius)
        {
            if (state == BotState.Dead || state == BotState.Stunned)
            {
                return;
            }

            float effectiveRadius = radius > 0f ? Mathf.Min(hearingRadius, radius) : hearingRadius;

            if (Vector3.Distance(transform.position, position) > effectiveRadius)
            {
                return;
            }

            lastKnownPlayerPosition = position;
            lostSightTimer = 0f;
            SetState(BotState.Chase);
        }

        private void HandleDamaged(DamageInfo damage, float amount)
        {
            if (state == BotState.Dead)
            {
                return;
            }

            flashEndTime = Time.time + 0.12f;
            ApplyBodyColor(hitColor);
        }

        private void HandleDied()
        {
            if (state == BotState.Dead)
            {
                return;
            }

            state = BotState.Dead;
            hasVisual = false;
            stunEndTime = 0f;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            agent.enabled = false;

            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            if (sensorRenderer != null)
            {
                sensorRenderer.enabled = false;
            }

            Sfx.PlayAt("Explosion_Bot", transform.position, 0.9f);

            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, transform.position + Vector3.up, Quaternion.identity);
                Destroy(explosion, 3f);
            }
            else
            {
                CombatVfx.SpawnExplosion(
                    transform.position + Vector3.up,
                    heavyWarden ? new Color(1f, 0.35f, 0.05f) : new Color(0.25f, 0.9f, 1f),
                    heavyWarden ? 1.5f : 1f);
            }

            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            yield return new WaitForSecondsRealtime(deathDelay);

            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        private void SetState(BotState newState)
        {
            if (state == newState || state == BotState.Dead)
            {
                return;
            }

            state = newState;
            bool haltAgent = newState == BotState.Attack || newState == BotState.Stunned;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = haltAgent;
            }

            agent.updateRotation = newState != BotState.Attack && newState != BotState.Stunned;
            agent.speed = newState == BotState.Patrol ? patrolSpeed : chaseSpeed;
            lostSightTimer = newState == BotState.Patrol ? 0f : lostSightTimer;

            if (newState == BotState.Patrol)
            {
                patrolIndex = FindNearestPatrolIndex();

                if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[patrolIndex] != null && agent.isOnNavMesh)
                {
                    agent.SetDestination(patrolPoints[patrolIndex].position);
                }
            }
            else if (newState == BotState.Chase && agent.isOnNavMesh)
            {
                agent.SetDestination(lastKnownPlayerPosition);
            }
            else if (newState == BotState.Attack)
            {
                nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.25f);
            }

            ApplySensorStateColor();

            if ((newState == BotState.Chase || newState == BotState.Attack) &&
                Time.time >= alertSoundTime)
            {
                alertSoundTime = Time.time + 1.5f;
                Sfx.PlayAt("UI_Error", transform.position + Vector3.up * 1.4f, 0.3f, 1.35f);
            }
        }

        private int FindNearestPatrolIndex()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return 0;
            }

            int nearest = 0;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null)
                {
                    continue;
                }

                float distance = (patrolPoints[i].position - transform.position).sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }
            }

            return nearest;
        }

        private void UpdateHitFlash()
        {
            if (flashEndTime <= 0f || Time.time < flashEndTime)
            {
                return;
            }

            flashEndTime = 0f;
            ApplyBodyBaseColors();
            ApplySensorStateColor();
        }

        private void ApplySensorStateColor()
        {
            if (sensorRenderer == null)
            {
                return;
            }

            Color color;

            switch (state)
            {
                case BotState.Chase:
                    color = chaseColor;
                    break;
                case BotState.Attack:
                    color = attackColor;
                    break;
                case BotState.Stunned:
                    color = stunnedColor;
                    break;
                case BotState.Dead:
                    color = Color.black;
                    break;
                default:
                    color = patrolColor;
                    break;
            }

            SetRendererColor(sensorRenderer, color);
        }

        private void ApplyBodyColor(Color color)
        {
            if (bodyRenderers == null)
            {
                return;
            }

            foreach (Renderer renderer in bodyRenderers)
            {
                SetRendererColor(renderer, color);
            }
        }

        private void ApplyBodyBaseColors()
        {
            if (bodyRenderers == null || bodyBaseColors == null)
            {
                return;
            }

            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                SetRendererColor(bodyRenderers[i], bodyBaseColors[i]);
            }
        }

        private void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private static Color ReadRendererColor(Renderer renderer)
        {
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return Color.white;
            }

            Material material = renderer.sharedMaterial;

            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return Color.white;
        }

        private void OnDestroy()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }
    }
}
