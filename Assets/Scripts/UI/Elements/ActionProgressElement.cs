using AI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Elements
{
    public class ActionProgressElement : ElementBase<IAction>, IPoolable
    {
        [SerializeField] private Image fill;

        private IAction _action;
        public override void Init(IAction data, UnityAction<IAction> action, params object[] args)
        {
            _action = data;
            data.OnActionProgress += SetProgress;
            data.OnCompleted += ReleaseSelf;
        }

        public void OnGet()
        {
            fill.fillAmount = 0f;
        }

        public void OnRelease()
        {
            _action.OnActionProgress -= SetProgress;
            _action.OnCompleted -= ReleaseSelf;
            _action = null;
        }

        private void ReleaseSelf(IAction action)
        {
            GameManager.I.GameItemManager.ItemUIPool.Release(this, "Prefab/ActionProgress");
        }

        public void SetProgress(float progress)
        {
            if (_action is SingleActionBase singleAction)
            {
                fill.fillAmount = progress / 100f;
            }
            else if (_action is MultiTimesActionBase multiTimesAction)
            {
                fill.fillAmount = (multiTimesAction.CurTime * 100f + progress) / (multiTimesAction.TotalTimes * 100f);
            }
            else
            {
                fill.fillAmount = progress / 100f;
            }
        }
    }
}