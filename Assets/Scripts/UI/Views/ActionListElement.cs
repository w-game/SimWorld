using System.Collections.Generic;
using System.Linq;
using AI;
using TMPro;
using UnityEngine;

namespace UI.Views
{
    public class ActionListElement : MonoSingleton<ActionListElement>
    {
        [SerializeField] private List<TextMeshProUGUI> actionItems;

        private IAction CurAction => GameManager.I.CurrentAgent.Brain.CurAction;
        
        public void Init()
        {
            GameManager.I.CurrentAgent.Brain.OnActionRegister += OnActionRegister;
        }

        private void OnDisable()
        {
            GameManager.I.CurrentAgent.Brain.OnActionRegister -= OnActionRegister;
        }

        private void OnActionRegister(IAction action)
        {
            action.OnCompleted += OnActionCompleted;
            UpdateUI();
        }

        // 当 Action 完成后调用
        private void OnActionCompleted(IAction action)
        {
            action.OnCompleted -= OnActionCompleted;
            UpdateUI();
        }

        private void UpdateUI()
        {
            foreach (var item in actionItems)
            {
                item.text = string.Empty;
                item.enabled = false;
            }

            if (CurAction != null)
            {
                actionItems[0].text = CurAction.ActionName;
                actionItems[0].enabled = true;
            }
            else
            {
                return;
            }

            var actionQueue = GameManager.I.CurrentAgent.Brain.ActiveActions.ToList();

            for (int i = 1; i < actionQueue.Count && i < actionItems.Count; i++)
            {
                actionItems[i].text = actionQueue[i].ActionName;
                actionItems[i].enabled = true;
            }
        }
    }
}