using UnityEngine;

namespace SignalLost.UI
{
    public sealed class MenuCameraDrift : MonoBehaviour
    {
        [SerializeField] private float horizontalSway = 0.9f;
        [SerializeField] private float verticalSway = 0.18f;
        [SerializeField] private float yawSway = 1.4f;
        [SerializeField] private float speed = 0.08f;

        private Vector3 basePosition;
        private Quaternion baseRotation;

        private void Awake()
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
        }

        private void Update()
        {
            float time = Time.unscaledTime * speed;
            transform.position = basePosition + new Vector3(
                Mathf.Sin(time * 1.1f) * horizontalSway,
                Mathf.Sin(time * 1.7f) * verticalSway,
                0f);
            transform.rotation = baseRotation * Quaternion.Euler(
                Mathf.Sin(time * 1.3f) * 0.35f,
                Mathf.Sin(time) * yawSway,
                Mathf.Sin(time * 0.9f) * 0.2f);
        }
    }
}
