using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Elements
{
    public class PropItemActionPanel : MonoBehaviour, IUIPoolable
    {
        private List<PropItemActionElement> _actionElements = new List<PropItemActionElement>();
        public void Init(ConfigBase config)
        {
            switch (config.type)
            {
                case "Food":
                    CreateElement("Eat", () =>
                    {
                        // Handle eat action
                    });
                    break;
            }

            CreateElement("Take in hand", () =>
            {
                // Handle use action
            });

            CreateElement("Drop", () =>
            {
                // Handle drop action
            });
        }
        
        private void CreateElement(string actionName, UnityAction onClick)
        {
            var actionElement = UIManager.I.GetElement<PropItemActionElement>("Prefabs/UI/Elements/PropItemActionElement", Vector3.zero, transform);
            actionElement.Init(actionName, onClick);
            _actionElements.Add(actionElement);
        }

        public void OnGet()
        {

        }

        public void OnRelease()
        {
           Clear();
        }

        internal void Clear()
        {
            foreach (var actionElement in _actionElements)
            {
                UIManager.I.ReleaseElement(actionElement, "Prefabs/UI/Elements/PropItemActionElement");
            }
            _actionElements.Clear();
        }
    }
}