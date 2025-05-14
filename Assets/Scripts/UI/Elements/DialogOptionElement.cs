using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Elements
{
    public class DialogOptionElement : MonoBehaviour, IUIPoolable
    {
        [SerializeField] private TextMeshProUGUI number;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Button button;

        public UnityEvent OnClick;

        public void OnGet()
        {
            text.text = string.Empty;
            button.onClick.AddListener(() => OnClick?.Invoke());

        }

        public void OnRelease()
        {
            text.text = string.Empty;
            number.text = string.Empty;
            button.onClick.RemoveAllListeners();
        }

        public void ShowText(int no, string content)
        {
            number.text = no.ToString();
            text.text = content;
        }

        public void Hide()
        {

        }
    }
}