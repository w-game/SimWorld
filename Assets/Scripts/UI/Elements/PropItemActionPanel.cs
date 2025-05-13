using System;
using System.Collections.Generic;
using AI;
using GameItem;
using UI.Models;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Elements
{
    public class PropItemActionPanel : MonoBehaviour, IUIPoolable
    {
        private List<PropItemActionElement> _actionElements = new List<PropItemActionElement>();
        public void Init(ItemSlotElement slotElement)
        {
            var propItem = slotElement.PropItem;
            switch (propItem.Config.type)
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
                var action = ActionPool.Get<TakeItemFromInventory>(propItem);
                GameManager.I.CurrentAgent.Brain.RegisterAction(action, true);
            });

            CreateElement("Drop", () =>
            {
                // Handle drop action
                var model = IModel.GetModel<PopCountSelectorModel>();
                var countSelectData = new CountSelectData("Drop", propItem.Quantity);
                countSelectData.ConfirmEvent += (count) =>
                {
                    PropGameItem createdPropItem = null;
                    var propConfig = propItem.Config as PropConfig;
                    if (propConfig.type == PropType.Food)
                    {
                        createdPropItem = GameItemManager.CreateGameItem<FoodItem>(propConfig, GameManager.I.CurrentAgent.Pos, GameItemType.Static, count);
                    }
                    else
                    {
                        createdPropItem = GameItemManager.CreateGameItem<PropGameItem>(propConfig, GameManager.I.CurrentAgent.Pos, GameItemType.Static, count);
                    }
                    createdPropItem.Owner = GameManager.I.CurrentAgent.Owner;
                    GameManager.I.CurrentAgent.Bag.RemoveItem(propItem.Config as PropConfig, count);
                };
                model.ShowUI(countSelectData);
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