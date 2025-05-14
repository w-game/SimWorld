using System;
using Citizens;
using Map;
using UI.Views;

namespace UI.Models
{
    public class PopHouseDetailsModel : ModelBase<PopHouseDetails>
    {
        public override string Path => "PopHouseDetails";
        
        public override ViewType ViewType => ViewType.Popup;
        
        public IHouse House => Data[0] as IHouse;

        public PopHouseDetailsModel()
        {
        }

        public void BuyProperty()
        {
            if (House != null)
            {
                var property = Property.Properties[House];
                if (property != null)
                {
                    property.BeBought(GameManager.I.CurrentAgent);
                }
            }
        }

        public void RentProperty()
        {
            if (House != null)
            {
                var property = Property.Properties[House];
                if (property != null)
                {
                    property.LeaseTo(GameManager.I.CurrentAgent.Citizen.Family);
                }
            }
        }
    }
}