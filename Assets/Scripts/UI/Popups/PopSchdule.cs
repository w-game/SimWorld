using UnityEngine;

namespace UI.Popups
{
    public class PopSchedule : ViewBase
    {
        [SerializeField] private GameObject scheduleItemPrefab;
        public override void OnShow()
        {
            base.OnShow();

            UpdateScheduleDisplay();
        }

        private void UpdateScheduleDisplay()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            foreach (var schedule in GameManager.I.CurrentAgent.Schedules)
            {
                
            }
        }
    }
}