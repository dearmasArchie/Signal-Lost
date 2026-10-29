using UnityEngine;
using UnityEngine.SceneManagement;

namespace SignalLost.World
{
    public sealed class ScenePortal : MonoBehaviour
    {
        [SerializeField] private string targetScene = "UndergroundFacility";
        [SerializeField] private bool requirePlayer = true;

        private bool triggered;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered)
            {
                return;
            }

            if (requirePlayer && !other.CompareTag("Player"))
            {
                return;
            }

            triggered = true;
            if (string.IsNullOrEmpty(targetScene))
            {
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(targetScene);
        }
    }
}
