using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Elements
{
    public class EffectElement : MonoBehaviour, IUIPoolable
    {
        [SerializeField] private Image effectIcon;
        [SerializeField] private TextMeshProUGUI effectValueText;
        public void OnGet()
        {

        }

        public void OnRelease()
        {

        }

        internal void UpdateInfo(EffectConfig config)
        {
            if (config == null)
            {
                effectIcon.gameObject.SetActive(false);
                effectValueText.gameObject.SetActive(false);
                return;
            }

            effectIcon.sprite = Resources.Load<Sprite>($"Textures/{config.id}");
            effectValueText.text = config.value > 0 ? $"+{config.value} {config.name}" : $"{config.value} {config.name}";
        }
    }
}