using Citizens;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Elements
{
    public class JobRecruitElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI jobName;
        [SerializeField] private Button applyButton;

        public void Init(WorkType workType, UnityAction<WorkType> action)
        {
            switch (workType)
            {
                case WorkType.Farmer:
                    jobName.text = "Farmer";
                    break;
                case WorkType.FarmHelper:
                    jobName.text = "Farm Helper";
                    break;
                case WorkType.Waiter:
                    jobName.text = "Waiter";
                    break;
                case WorkType.Cooker:
                    jobName.text = "Cooker";
                    break;
                case WorkType.Salesman:
                    jobName.text = "Salesman";
                    break;
                case WorkType.CEO:
                    jobName.text = "CEO";
                    break;
            }

            applyButton.onClick.AddListener(() =>
            {
                action?.Invoke(workType);
            });
        }
    }
}