using UnityEngine;
using UnityEngine.SceneManagement;

namespace SignalLost.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private string gameplaySceneName = "IslandExterior";

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            HideControls();
        }

        public void PlayGame()
        {
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void ShowControls()
        {
            if (controlsPanel != null)
            {
                controlsPanel.SetActive(true);
            }
        }

        public void HideControls()
        {
            if (controlsPanel != null)
            {
                controlsPanel.SetActive(false);
            }
        }

        public void QuitGame()
        {
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
