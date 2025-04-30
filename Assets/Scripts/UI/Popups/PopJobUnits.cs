using System;
using System.Collections.Generic;
using Citizens;
using UI.Elements;
using UI.Models;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class PopJobUnits : ViewBase<PopJobUnitsModel>
    {
        [SerializeField] private JobUnitElement jobUnitPrefab;
        [SerializeField] private Transform jobUnitContainer;
        [SerializeField] private Image autoAssignSlider;
        [SerializeField] private Button autoAssignButton;

        public override void OnShow()
        {
            foreach (var jobUnits in Model.JobUnits)
            {
                foreach (var unit in jobUnits.Value)
                {
                    var jobUnitElement = Instantiate(jobUnitPrefab, jobUnitContainer);
                    jobUnitElement.Init((jobUnits.Key, unit), OnDoBtnClicked);
                    unit.OnJobUnitDone += (jobUnit) =>
                    {
                        Destroy(jobUnitElement.gameObject);
                    };
                }
            }

            autoAssignSlider.gameObject.SetActive(Model.Job.AutoAssign);
            autoAssignButton.onClick.AddListener(OnAutoAssignButtonClicked);
        }

        private void OnDoBtnClicked((Type, JobUnit) jobUnitData, JobUnitElement jobUnitElement)
        {
            Model.DoJobUnit(jobUnitData.Item1, jobUnitData.Item2);
            jobUnitElement.SetBtnInteractable(false);
        }

        private void OnAutoAssignButtonClicked()
        {
            Model.Job.AutoAssign = !Model.Job.AutoAssign;
            autoAssignSlider.gameObject.SetActive(Model.Job.AutoAssign);
        }

        public override void OnHide()
        {
            base.OnHide();
            autoAssignButton.onClick.RemoveListener(OnAutoAssignButtonClicked);
        }
    }
}