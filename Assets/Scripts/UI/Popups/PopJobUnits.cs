using System;
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
                    CreateElement(jobUnits.Key, unit);
                }
            }

            autoAssignSlider.gameObject.SetActive(Model.SelfJob.AutoAssign);
            autoAssignButton.onClick.AddListener(OnAutoAssignButtonClicked);

            Model.SelfJob.Property.OnJobUnitAdded += CreateElement;
        }

        private void CreateElement(Type type, JobUnit jobUnit)
        {
            var jobUnitElement = Instantiate(jobUnitPrefab, jobUnitContainer);
            jobUnitElement.Init((type, jobUnit), OnDoBtnClicked);
            jobUnit.OnJobUnitDone += (jobUnit) =>
            {
                Destroy(jobUnitElement.gameObject);
            };
            jobUnitElement.SetBtnInteractable(!Model.SelfJob.AutoAssign);
        }

        private void OnDoBtnClicked((Type, JobUnit) jobUnitData, JobUnitElement jobUnitElement)
        {
            if (Model.DoJobUnit(jobUnitData.Item1, jobUnitData.Item2))
            {
                jobUnitElement.SetBtnInteractable(false);
            }
        }

        private void OnAutoAssignButtonClicked()
        {
            Model.SelfJob.AutoAssign = !Model.SelfJob.AutoAssign;
            autoAssignSlider.gameObject.SetActive(Model.SelfJob.AutoAssign);
            foreach (Transform child in jobUnitContainer)
            {
                var jobUnitElement = child.GetComponent<JobUnitElement>();
                if (jobUnitElement != null)
                {
                    jobUnitElement.SetBtnInteractable(!Model.SelfJob.AutoAssign);
                }
            }
        }

        public override void OnHide()
        {
            base.OnHide();
            autoAssignButton.onClick.RemoveListener(OnAutoAssignButtonClicked);
            Model.SelfJob.Property.OnJobUnitAdded -= CreateElement;
        }
    }
}