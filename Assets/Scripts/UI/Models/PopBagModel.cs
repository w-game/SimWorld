using GameItem;
using UI.Popups;

namespace UI.Models
{
    public class PopBagModel : ModelBase<PopBag>
    {
        public override string Path => "PopBag";

        public override ViewType ViewType => ViewType.Popup;

        public ContainerItem containerItem{

            get
            {
                if (Data == null || Data.Length == 0)
                {
                    return null;
                }
                return Data[0] as ContainerItem;
            }
        }
    }
}