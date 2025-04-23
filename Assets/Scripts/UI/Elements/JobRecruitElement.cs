using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Elements
{
    public class JobRecruitElement : ElementBase<JobConfig>
    {
        [SerializeField] private TextMeshProUGUI jobName;
        [SerializeField] private Button applyButton;

        private JobConfig _jobConfig;
        public override void Init(JobConfig data, UnityAction<JobConfig> action, params object[] args)
        {
            _jobConfig = data;
            if (_jobConfig != null)
            {
                jobName.text = _jobConfig.name;
            }

            applyButton.onClick.AddListener(() =>
            {
                action?.Invoke(_jobConfig);
            });
        }
    }
}