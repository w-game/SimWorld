using System;
using Citizens;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Elements
{
    public class JobUnitElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI jobName;
        [SerializeField] private Button doBtn;
        public void Init((Type, JobUnit) data, UnityAction<(Type, JobUnit), JobUnitElement> action)
        {
            jobName.text = data.Item2.Action.ActionName;
            doBtn.onClick.AddListener(() => action?.Invoke((data.Item1, data.Item2), this));
        }

        internal void SetBtnInteractable(bool v)
        {
            doBtn.interactable = v;
        }
    }
}