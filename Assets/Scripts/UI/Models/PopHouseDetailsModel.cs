using Map;
using UI.Views;

namespace UI.Models
{
    public class PopHouseDetailsModel : ModelBase<PopHouseDetails>
    {
        public override string Path => "PopHouseDetails";
        
        public override ViewType ViewType => ViewType.Popup;

        public PopHouseDetailsModel() 
        {
        }
    }
}