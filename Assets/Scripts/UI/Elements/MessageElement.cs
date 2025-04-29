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
        public MessageType type;
    }
    public class MessageElement : ElementBase<Message>, IPoolable
    {
        [SerializeField] private TextMeshProUGUI msg;
        [SerializeField] private Image icon;
        public override void Init(Message data, UnityAction<Message> action = null, params object[] args)
        {
            msg.text = data.msg;
            icon.sprite = Resources.Load<Sprite>(data.iconPath);

            switch (data.type)
            {
                case MessageType.Info:
                    msg.color = Color.white;
                    break;
                case MessageType.Warning:

                    msg.color = Color.yellow;
                    break;
                case MessageType.Error:

                    msg.color = Color.red;
                    break;
            }
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