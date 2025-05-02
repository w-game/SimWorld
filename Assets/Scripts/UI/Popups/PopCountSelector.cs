using TMPro;
using UI.Models;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups
{
    public class PopCountSelector : ViewBase<PopCountSelectorModel>
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private Slider selection;
        [SerializeField] private Button confirmButton;
        public override void OnShow()
        {
            selection.maxValue = Model.CountSelect.MaxCount;
            selection.value = Model.CountSelect.MaxCount;
            title.text = $"{Model.CountSelect.Title}\n<size=32>{Model.CountSelect.MaxCount}</size>";


            selection.onValueChanged.AddListener(OnValueChanged);
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            closeBtn.onClick.AddListener(OnCancelButtonClicked);
        }

        private void OnCancelButtonClicked()
        {
            Model.Cancel();
            Close();
        }

        public void OnValueChanged(float value)
        {
            Model.Count = (int)value;
            title.text = $"{Model.CountSelect.Title}\n<size=32>{Model.Count}</size>";
        }

        public void OnConfirmButtonClicked()
        {
            Model.Confirm();
            Close();
        }
    }
}