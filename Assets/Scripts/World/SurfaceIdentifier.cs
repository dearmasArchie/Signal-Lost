using UnityEngine;

namespace SignalLost.World
{
    public enum SurfaceType
    {
        Grass,
        Dirt,
        Concrete,
        Metal,
        Wood,
        Sand
    }

    public sealed class SurfaceIdentifier : MonoBehaviour
    {
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Concrete;

        public SurfaceType Type => surfaceType;

        public void Configure(SurfaceType type)
        {
            surfaceType = type;
        }
    }
}
