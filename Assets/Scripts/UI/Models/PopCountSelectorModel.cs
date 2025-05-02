using UI.Popups;

namespace UI.Models
{
    public interface ICountSelect
    {
        string Title { get; }
        int MaxCount { get; }
        void Confirm(int count);
        void Cancel();
    }
    public class PopCountSelectorModel : ModelBase<PopCountSelector>
    {
        public override string Path => "PopCountSelector";
        public override ViewType ViewType => ViewType.Popup;

        public ICountSelect CountSelect => Data[0] as ICountSelect;
        public int Count { get; set; }

        protected override void OnShow()
        {
            Count = CountSelect.MaxCount;            
        }


        public void Confirm()
        {
            CountSelect.Confirm(Count);
        }

        public void Cancel()
        {
            CountSelect.Cancel();
        }
    }
}