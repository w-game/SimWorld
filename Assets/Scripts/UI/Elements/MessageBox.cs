using DG.Tweening;
using UnityEngine;

namespace UI.Elements
{
    public class MessageBox : MonoBehaviour
    {
        [SerializeField] private Transform messageContainer;

        public static MessageBox I { get; private set; }

        void Awake()
        {
            I = this;
        }

        public void Init()
        {
            GameManager.I.CurrentAgent.Bag.OnInventoryChanged += OnInventoryChanged;
        }

        public void OnInventoryChanged(PropItem item, int quantity)
        {
            if (quantity > 0)
            {
                ShowMessage($"Added {quantity} {item.Config.name}", item.Config.icon);
            }
            else
            {
                ShowMessage($"Removed {Mathf.Abs(quantity)} {item.Config.name}", item.Config.icon);
            }
        }

        public void ShowMessage(string message, string iconPath)
        {
            var messageElement = GameManager.I.GameItemManager.ItemUIPool.Get<MessageElement>("Prefabs/UI/Elements/MessageElement", messageContainer);
            messageElement.Init(new Message { msg = message, iconPath = iconPath });

            DOTween.Sequence()
                .AppendCallback(() =>
                {
                    messageElement.GetComponent<CanvasGroup>().alpha = 1;
                })
                .AppendInterval(2f)
                .Append(messageElement.GetComponent<CanvasGroup>().DOFade(0, 3f).OnComplete(() =>
                {
                    GameManager.I.GameItemManager.ItemUIPool.Release(messageElement, "Prefabs/UI/Elements/MessageElement");
                }));
        }
        
        public void ClearMessages()
        {
            foreach (Transform child in messageContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}