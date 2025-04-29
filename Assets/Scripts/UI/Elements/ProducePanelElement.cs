using AI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Elements
{
    public class ProducePanelElement : ElementBase<PropConfig>, IPoolable
    {
        [SerializeField] private Image icon;
        [SerializeField] private Button btn;

        public override void Init(PropConfig data, UnityAction<PropConfig> action, params object[] args)
        {
            icon.sprite = Resources.Load<Sprite>(data.icon);
            btn.onClick.AddListener(() => action?.Invoke(data));
        }

        public void OnGet()
        {

        }

        public void OnRelease()
        {
            btn.onClick.RemoveAllListeners();
        }
        
        public void ReleaseSelf()
        {
            GameManager.I.GameItemManager.ItemUIPool.Release(this, "Prefabs/UI/Elements/ProducePanelElement");
        }
    }
}