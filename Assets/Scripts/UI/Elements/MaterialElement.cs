using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Elements
{
    public class MaterialElement : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI requireAmountText;
        [SerializeField] private TextMeshProUGUI currentAmountText;

        public void Init(CraftMaterialConfig config)
        {
            icon.sprite = Resources.Load<Sprite>(GameManager.I.ConfigReader.GetConfig<PropConfig>(config.id).icon);
            requireAmountText.text = config.amount.ToString();

            List<PropItem> propItems = GameManager.I.CurrentAgent.Bag.CheckItem(config.id);

            int totalAmount = 0;
            foreach (var item in propItems)
            {
                totalAmount += item.Quantity;
            }
            currentAmountText.text = totalAmount.ToString();

            if (totalAmount >= config.amount)
            {
                currentAmountText.color = Color.green;
            }
            else
            {
                currentAmountText.color = Color.red;
            }
        }
    }
}