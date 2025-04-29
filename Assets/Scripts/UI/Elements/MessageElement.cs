using UnityEngine.Events;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UI.Elements
{
    public struct Message
    {
        public string msg;
        public string iconPath;
    }
    public class MessageElement : ElementBase<Message>, IPoolable
    {
        [SerializeField] private TextMeshProUGUI msg;
        [SerializeField] private Image icon;
        public override void Init(Message data, UnityAction<Message> action = null, params object[] args)
        {
            msg.text = data.msg;
            icon.sprite = Resources.Load<Sprite>(data.iconPath);
            action?.Invoke(data);
        }

        public void OnGet()
        {
            
        }

        public void OnRelease()
        {
            
        }
    }
}