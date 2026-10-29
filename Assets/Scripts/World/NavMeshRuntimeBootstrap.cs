using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace SignalLost.World
{
    [DefaultExecutionOrder(-10000)]
    public sealed class NavMeshRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private NavMeshSurface surface;

        private void Awake()
        {
            if (surface == null)
            {
                surface = GetComponent<NavMeshSurface>();
            }

            if (surface == null || surface.navMeshData == null)
            {
                Debug.LogError("[NavMeshBootstrap] NavMeshSurface data is missing.");
                return;
            }

            surface.AddData();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;

            if (surface == null)
            {
                yield break;
            }

            surface.AddData();

            if (NavMesh.CalculateTriangulation().indices.Length < 3)
            {
                Debug.LogError("[NavMeshBootstrap] Baked NavMesh failed to load at runtime.");
            }
        }
    }
}
