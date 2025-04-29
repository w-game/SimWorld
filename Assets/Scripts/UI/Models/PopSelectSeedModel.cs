using UI.Popups;
using UnityEngine.Events;

namespace UI.Models
{
    public interface ISelectItem
    {
        void OnSelected(string id, int amount = 1);
    }
    public class PopSelectSeedModel : ModelBase<PopSelectSeed>
    {
        public override string Path => "PopSelectSeed";
        public override ViewType ViewType => ViewType.Popup;

        public PropType PropType => (PropType)Data[1];
        private ISelectItem _selectItem => Data[0] as ISelectItem;


        public void OnSelected(string id)
        {
            _selectItem?.OnSelected(id);
        }
    }
}