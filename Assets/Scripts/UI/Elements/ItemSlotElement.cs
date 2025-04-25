using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Elements
{
    public class ItemSlotElement : MonoBehaviour
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private Button itemButton;
        [SerializeField] private TextMeshProUGUI itemCountText;
        public void Init(ConfigBase config, UnityAction<ConfigBase> onClick)
        {
            UpdateItemSlot(config, -1);
            itemButton.onClick.AddListener(() => onClick(config));
        }
        
        public void UpdateItemSlot(ConfigBase newConfig, int count = -1)
        {
            if (newConfig == null)
            {
                itemIcon.gameObject.SetActive(false);
                itemCountText.gameObject.SetActive(false);
                return;
            }

            itemIcon.sprite = Resources.Load<Sprite>(newConfig.icon);
            itemIcon.gameObject.SetActive(true);
            if (count > 0)
            {
                itemCountText.text = count.ToString();
                itemCountText.gameObject.SetActive(true);
            }
            else
            {
                itemCountText.gameObject.SetActive(false);
            }
        }
    }
}