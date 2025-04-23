using Citizens;
using Map;
using TMPro;
using UI.Elements;
using UnityEngine;

namespace UI.Views
{
    public class PopHouseDetails : ViewBase
    {
        [SerializeField] private TextMeshProUGUI propertyName;
        [SerializeField] private TextMeshProUGUI propertyDetails;
        [SerializeField] private GameObject recruitmentItemPrefab;
        public override void OnShow()
        {
            base.OnShow();
            var house = Model.Data[0] as House;
            if (house != null)
            {
                var property = Property.Properties[house];
                propertyName.text = house.HouseType.ToString();
                propertyDetails.text = $"Employee: {property.Employees.Count}";
                foreach (var job in property.JobRecruitCount)
                {
                    var recruitmentItem = Instantiate(recruitmentItemPrefab, propertyDetails.transform.parent);
                    recruitmentItem.GetComponent<JobRecruitElement>().Init(job.Key, (jobConfig) =>
                    {
                        property.AddApplicant(jobConfig, GameManager.I.CurrentAgent);
                    });
                }
            }
        }
    }
}