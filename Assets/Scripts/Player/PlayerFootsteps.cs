using UnityEngine;
using SignalLost.Core;
using SignalLost.World;

namespace SignalLost.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] private float walkStride = 1.75f;
        [SerializeField] private float sprintStride = 2.35f;
        [SerializeField] private float walkThreshold = 5.8f;
        [SerializeField, Range(0f, 1f)] private float stepVolume = 0.4f;

        private readonly AudioClip[][] surfaceClips = new AudioClip[6][];
        private CharacterController controller;
        private AudioSource source;
        private GameManager manager;
        private float distanceAccumulator;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = stepVolume;

            for (int i = 0; i < surfaceClips.Length; i++)
            {
                surfaceClips[i] = new AudioClip[3];
            }
        }

        private void Start()
        {
            manager = GameManager.Instance;
        }

        private void Update()
        {
            if (manager != null && manager.State != GameState.Playing)
            {
                distanceAccumulator = 0f;
                return;
            }

            if (!controller.isGrounded)
            {
                distanceAccumulator = 0f;
                return;
            }

            Vector3 velocity = controller.velocity;
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            if (speed < 0.75f)
            {
                distanceAccumulator = Mathf.MoveTowards(distanceAccumulator, 0f, Time.deltaTime * 2f);
                return;
            }

            distanceAccumulator += speed * Time.deltaTime;
            float stride = speed > walkThreshold ? sprintStride : walkStride;
            if (distanceAccumulator >= stride)
            {
                distanceAccumulator = 0f;
                PlayStep();
            }
        }

        private void PlayStep()
        {
            SurfaceType surface = DetectSurface();
            int surfaceIndex = (int)surface;
            AudioClip[] clips = surfaceClips[surfaceIndex];

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                {
                    clips[i] = LoadClip(surface, i + 1);
                }
            }

            int startIndex = Random.Range(0, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip clip = clips[(startIndex + i) % clips.Length];
                if (clip != null)
                {
                    source.pitch = Random.Range(0.94f, 1.05f);
                    source.PlayOneShot(clip, stepVolume);
                    break;
                }
            }
        }

        private SurfaceType DetectSurface()
        {
            Vector3 origin = transform.position + Vector3.up * 0.25f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 1.6f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i].collider;
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                SurfaceIdentifier identifier = hit.GetComponentInParent<SurfaceIdentifier>();
                if (identifier != null)
                {
                    return identifier.Type;
                }

                if (hit is TerrainCollider)
                {
                    return SurfaceType.Grass;
                }
            }

            return SurfaceType.Concrete;
        }

        private static AudioClip LoadClip(SurfaceType surface, int index)
        {
            string surfaceName = surface == SurfaceType.Dirt || surface == SurfaceType.Sand
                ? "Grass"
                : surface.ToString();
            return Resources.Load<AudioClip>($"Audio/SFX/Footstep_{surfaceName}_{index}");
        }
    }
}
