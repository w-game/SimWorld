using UI.Popups;
using UnityEngine.Events;

namespace UI.Models
{
    public class PopSelectSeedModel : ModelBase<PopSelectSeed>
    {
        public override string Path => "PopSelectSeed";
        public override ViewType ViewType => ViewType.Popup;

        private UnityAction<string> _callback;

        public PopSelectSeedModel(UnityAction<string> callback)
        {
            _callback = callback;
        }

        public void ExecuteCallback(string seedId)
        {
            _callback?.Invoke(seedId);
        }

        protected override void OnHideUI()
        {
            _callback?.Invoke(string.Empty);
        }
    }
}