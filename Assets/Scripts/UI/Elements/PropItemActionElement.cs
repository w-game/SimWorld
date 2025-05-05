using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Elements
{
    public class PropItemActionElement : MonoBehaviour, IUIPoolable
    {
        [SerializeField] private Button itemButton;
        [SerializeField] private TextMeshProUGUI itemCountText;

        public void Init(string actionName, UnityAction onClick)
        {
            itemButton.onClick.AddListener(onClick);
            itemButton.interactable = true;
            itemCountText.text = actionName;
        }

        public void OnGet()
        {
            itemButton.onClick.RemoveAllListeners();
        }

        public void OnRelease()
        {
            
        }
    }
}