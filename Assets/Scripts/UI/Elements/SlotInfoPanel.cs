using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.Elements
{
    public class SlotInfoPanel : MonoBehaviour, IUIPoolable
    {
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private Transform _effectParent;

        private List<EffectElement> _effectElements = new List<EffectElement>();

        public void OnGet()
        {

        }

        public void OnRelease()
        {
            foreach (var effectElement in _effectElements)
            {
                UIManager.I.ReleaseElement(effectElement, "Prefabs/UI/Elements/EffectElement");
            }
            _effectElements.Clear();
        }

        internal void UpdateInfo(PropConfig config)
        {
            if (config == null)
            {
                itemNameText.text = "";
                typeText.text = "";
                itemDescriptionText.text = "";
                return;
            }

            itemNameText.text = config.name;
            typeText.text = config.type.ToUpper();
            itemDescriptionText.text = config.description;

            foreach (var effect in config.Effects)
            {
                var effectElement = UIManager.I.GetElement<EffectElement>("Prefabs/UI/Elements/EffectElement", Vector3.zero, _effectParent);
                effectElement.UpdateInfo(effect);
                _effectElements.Add(effectElement);
            }
        }
    }
}