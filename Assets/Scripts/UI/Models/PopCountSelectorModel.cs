using System;
using UI.Popups;

namespace UI.Models
{
    public class CountSelectData
    {
        public string Title { get; }
        public int MaxCount { get; }
        public event Action<int> ConfirmEvent;
        public event Action CancelEvent;

        public CountSelectData(string title, int maxCount)
        {
            Title = title;
            MaxCount = maxCount;
        }

        public void Confirm(int count)
        {
            ConfirmEvent?.Invoke(count);
        }

        public void Cancel()
        {
            CancelEvent?.Invoke();
        }
    }
    public class PopCountSelectorModel : ModelBase<PopCountSelector>
    {
        public override string Path => "PopCountSelector";
        public override ViewType ViewType => ViewType.Popup;

        public CountSelectData CountSelect => Data[0] as CountSelectData;
        public int Count { get; set; }

        protected override void OnShow()
        {
            Count = CountSelect.MaxCount;            
        }


        public void Confirm()
        {
            if (Count < 1) return;
            CountSelect.Confirm(Count);
        }

        public void Cancel()
        {
            CountSelect.Cancel();
        }
    }
}