using UI.Popups;
using UnityEngine.Events;

namespace UI.Models
{
    public interface ISelectItem
    {
        PropType PropType { get; }
        void OnSelected(string id, int amount = 1);
    }
    public class PopSelectSeedModel : ModelBase<PopSelectSeed>
    {
        public override string Path => "PopSelectSeed";
        public override ViewType ViewType => ViewType.Popup;

        public ISelectItem SelectItem => Data[0] as ISelectItem;


        public void OnSelected(string id)
        {
            SelectItem?.OnSelected(id);
        }
    }
}