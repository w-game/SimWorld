using System.Collections.Generic;
using AI;
using TMPro;
using UnityEngine;

namespace UI.Views
{
    public class ActionListElement : MonoSingleton<ActionListElement>
    {
        [Header("Action List UI")]
        [SerializeField] private GameObject actionListPanel;
        [SerializeField] private GameObject actionItemPrefab;
        [SerializeField] private Transform actionItemParent;
        [SerializeField] private List<TextMeshProUGUI> actionItems;
        
        // 新增：存储当前 Action 队列
        private List<IAction> actionQueue = new List<IAction>();

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
            // 按顺序将 Action 添加到队列中
            actionQueue.Add(action);
            // 订阅 Action 完成事件，假定 IAction 包含 OnCompleted 事件
            action.OnCompleted += OnActionCompleted;
            UpdateUI();
        }

        // 当 Action 完成后调用
        private void OnActionCompleted(IAction action)
        {
            action.OnCompleted -= OnActionCompleted;
            if (actionQueue.Contains(action))
            {
                actionQueue.Remove(action);
                UpdateUI();
            }
        }

        // 更新 UI 显示，根据队列顺序对应 actionItems
        private void UpdateUI()
        {
            // 先隐藏所有 UI 元素
            foreach (var image in actionItems)
            {
                image.enabled = false;
            }

            // 将队列中的 Action 按顺序显示到 UI 上
            for (int i = 0; i < actionQueue.Count && i < actionItems.Count; i++)
            {
                // 这里假设每个 Action 对应一个图标，若 IAction 定义中包含 Icon 或 Sprite 属性，可以赋值给 image.sprite
                actionItems[i].text = actionQueue[i].ActionName;
                actionItems[i].enabled = true;
            }
        }
    }
}