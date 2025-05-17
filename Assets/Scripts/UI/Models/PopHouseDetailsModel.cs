using System;
using Citizens;
using UI.Views;

namespace UI.Models
{
    public class PopHouseDetailsModel : ModelBase<PopHouseDetails>
    {
        public override string Path => "PopHouseDetails";
        
        public override ViewType ViewType => ViewType.Popup;

        public Property Property => PropertyManager.I.Properties.Find(p => p.House == Data[0]);

        public PopHouseDetailsModel()
        {
        }

        public void BuyProperty()
        {
            if (Property != null)
            {
                Property.Transfer(GameManager.I.CurrentAgent.Owner);
            }
        }

        public void RentProperty()
        {
            if (Property != null)
            {
                Property.LeaseTo(GameManager.I.CurrentAgent.Citizen, DateTime.Now, DateTime.Now.AddDays(30));
            }
        }
    }
}