using GameItem;
using TMPro;
using UI.Models;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups
{
    public class PopSeedIncubator : PopInventoryType<PopSeedIncubatorModel>
    {
        [SerializeField] private Image cultivatingIcon;
        [SerializeField] private Image cultivatingProgress;

        [SerializeField] private GameObject changeTargetPanel;
        [SerializeField] private Image changeTargetIcon;

        [SerializeField] private Button eventButton;
        [SerializeField] private TextMeshProUGUI eventBtnText;

        protected override int SlotAmount => 16;
        protected override Inventory Inventory => GameManager.I.CurrentAgent.Bag;

        protected override PropType PropType => PropType.Crop;

        private PopSeedIncubatorModel _model;

        public override void OnShow()
        {
            base.OnShow();
            _model = Model as PopSeedIncubatorModel;
            _model.SeedIncubatorItem.OnFinish += OnFinish;
            _model.SeedIncubatorItem.OnChange += UpdateView;
            _model.SeedIncubatorItem.OnProgress += UpdateProgress;

            UpdateView();
        }

        public override void OnHide()
        {
            base.OnHide();
            _model.SeedIncubatorItem.OnFinish -= OnFinish;
            _model.SeedIncubatorItem.OnChange -= UpdateView;
            _model.SeedIncubatorItem.OnProgress -= UpdateProgress;
        }

        private void UpdateView()
        {
            changeTargetPanel.SetActive(false);
            eventButton.gameObject.SetActive(false);
            cultivatingProgress.fillAmount = 0;

            if (_model.SeedIncubatorItem.Done)
            {
                cultivatingIcon.sprite = Resources.Load<Sprite>(_model.SeedIncubatorItem.Seed.icon);
                eventButton.gameObject.SetActive(true);
                eventButton.onClick.RemoveAllListeners();
                eventButton.onClick.AddListener(TakeSeed);
                eventBtnText.text = "Take";
            }
            else
            {
                if (_model.SeedIncubatorItem.CultivatingItem != null)
                {
                    cultivatingIcon.sprite = Resources.Load<Sprite>(_model.SeedIncubatorItem.CultivatingItem.Config.icon);
                }
                else
                {
                    cultivatingIcon.sprite = null;
                }
            }
        }

        private void UpdateProgress(float progress)
        {
            cultivatingProgress.fillAmount = progress / 100f;
        }

        protected override void OnItemClicked(PropItem propItem)
        {
            if (_model.SeedIncubatorItem.Done)
            {
                return;
            }
            changeTargetPanel.SetActive(true);
            eventButton.gameObject.SetActive(true);
            changeTargetIcon.sprite = Resources.Load<Sprite>(propItem.Config.icon);
            eventButton.onClick.RemoveAllListeners();
            eventBtnText.text = "Change";
            eventButton.onClick.AddListener(() =>
            {
                changeTargetPanel.SetActive(false);
                eventButton.gameObject.SetActive(false);

                _model.SetNewItem(propItem, Inventory);
            });
        }

        private void OnFinish(PropConfig propConfig)
        {
            changeTargetPanel.SetActive(false);
            eventButton.onClick.RemoveAllListeners();
            eventButton.onClick.AddListener(TakeSeed);
            eventBtnText.text = "Take";

            eventButton.gameObject.SetActive(true);
            cultivatingIcon.sprite = Resources.Load<Sprite>(propConfig.icon);
        }

        private void TakeSeed()
        {
            _model.TakeSeed(Inventory);
        }
    }
}