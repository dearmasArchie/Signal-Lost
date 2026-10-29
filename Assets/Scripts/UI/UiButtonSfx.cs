using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SignalLost.Audio;

namespace SignalLost.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UiButtonSfx : MonoBehaviour, IPointerEnterHandler
    {
        private Button button;
        private bool hasClicked;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(PlayClick);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClick);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && button.interactable)
            {
                Sfx.Play2D("UI_Tick", 0.16f);
            }
        }

        private void PlayClick()
        {
            if (hasClicked)
            {
                return;
            }

            hasClicked = true;
            Sfx.Play2D("UI_Confirm", 0.45f);
            CancelInvoke(nameof(ResetClick));
            Invoke(nameof(ResetClick), 0.05f);
        }

        private void ResetClick()
        {
            hasClicked = false;
        }
    }
}
