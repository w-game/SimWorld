using Citizens;
using Map;
using TMPro;
using UI.Elements;
using UI.Models;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class PopHouseDetails : ViewBase<PopHouseDetailsModel>
    {
        [SerializeField] private TextMeshProUGUI propertyName;
        [SerializeField] private TextMeshProUGUI propertyDetails;
        [SerializeField] private GameObject recruitmentItemPrefab;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button rentButton;

        public override void OnShow()
        {
            if (Model.Property != null)
            {
                if (Model.Property.House.HouseType == HouseType.House)
                {
                    propertyName.text = Model.Property.House.HouseType.ToString();
                    buyButton.onClick.AddListener(() =>
                    {
                        Model.BuyProperty();
                    });
                }
                else
                {
                    var property = PropertyManager.I.BusinessProperties[Model.Property];
                    propertyName.text = Model.Property.House.HouseType.ToString();
                    propertyDetails.text = $"Employee: {property.Employees.Count}";
                    foreach (var job in property.JobRecruitCount)
                    {
                        var recruitmentItem = Instantiate(recruitmentItemPrefab, propertyDetails.transform.parent);
                        recruitmentItem.GetComponent<JobRecruitElement>().Init(job.Key, (jobConfig) =>
                        {
                            property.AddApplicant(jobConfig, GameManager.I.CurrentAgent);
                        });
                    }
                    buyButton.onClick.AddListener(Model.BuyProperty);
                    rentButton.onClick.AddListener(Model.RentProperty);
                }
            }

        }
    }
}