using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SignalLost.Interactables;

namespace SignalLost.UI
{
    public sealed class KeypadPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text displayText;
        [SerializeField] private Text statusText;

        private KeypadInteractable keypad;
        private string code = string.Empty;
        private Coroutine closeRoutine;

        public event Action Closed;

        public void Open(KeypadInteractable target)
        {
            keypad = target;
            code = string.Empty;
            if (root != null)
            {
                root.SetActive(true);
            }

            if (displayText != null)
            {
                displayText.text = "----";
                displayText.color = new Color(0.75f, 0.95f, 1f);
            }

            if (statusText != null)
            {
                statusText.text = "ENTER ACCESS CODE";
                statusText.color = new Color(0.65f, 0.85f, 0.9f);
            }
        }

        public void AppendDigit(string digit)
        {
            if (root == null || !root.activeSelf || code.Length >= 4 || string.IsNullOrEmpty(digit))
            {
                return;
            }

            code += digit.Substring(0, 1);
            RefreshDisplay();
        }

        public void ClearCode()
        {
            code = string.Empty;
            RefreshDisplay();
        }

        public void Submit()
        {
            if (keypad == null)
            {
                Close();
                return;
            }

            if (code.Length < 4)
            {
                SetStatus("ENTER ALL FOUR DIGITS", new Color(1f, 0.6f, 0.25f));
                return;
            }

            bool accepted = keypad.TrySubmitCode(code);
            if (accepted)
            {
                SetStatus("ACCESS GRANTED", new Color(0.3f, 1f, 0.55f));
                if (closeRoutine != null)
                {
                    StopCoroutine(closeRoutine);
                }

                closeRoutine = StartCoroutine(CloseAfterDelay(0.75f));
            }
            else
            {
                SetStatus("ACCESS DENIED", new Color(1f, 0.3f, 0.25f));
                code = string.Empty;
                RefreshDisplay();
            }
        }

        public void Close()
        {
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            keypad = null;
            code = string.Empty;
            if (root != null)
            {
                root.SetActive(false);
            }

            Closed?.Invoke();
        }

        public void ForceClose()
        {
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            keypad = null;
            code = string.Empty;
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private IEnumerator CloseAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            closeRoutine = null;
            Close();
        }

        private void RefreshDisplay()
        {
            if (displayText == null)
            {
                return;
            }

            string shown = code.PadRight(4, '-');
            displayText.text = $"{shown.Substring(0, 2)} {shown.Substring(2, 2)}";
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
            statusText.color = color;
        }
    }
}
