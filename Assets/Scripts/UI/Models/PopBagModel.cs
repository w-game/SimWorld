using UI.Popups;

namespace UI.Models
{
    public class PopBagModel : ModelBase<PopBag>
    {
        public override string Path => "PopBag";

        public override ViewType ViewType => ViewType.Popup;
    }
}