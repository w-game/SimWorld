using TMPro;
using UnityEngine;

namespace UI.Elements
{
    public class BulletinElement : MonoBehaviour, IUIPoolable
    {
        [SerializeField] private Transform contents;
        private GameObject _titleObj;
        private void Awake()
        {
            // test
            SetAncientText("招佃示告", "今有良田五亩，水利便捷，宜耕宜种，欲觅勤劳善耕之人租佃。有意者可往城东王掌柜处洽询，面议租事，切勿错失良机。");
        }
        public void SetAncientText(string title, string content, int maxRowsPerColumn = 8)
        {
            // 分列
            int totalChars = content.Length;
            int colCount = Mathf.CeilToInt((float)totalChars / maxRowsPerColumn);

            for (int col = 0; col < colCount; col++)
            {
                var newCol = new GameObject("Column");
                newCol.transform.SetParent(contents);
                TextMeshProUGUI tmp = newCol.AddComponent<TextMeshProUGUI>();
                tmp.transform.localScale = Vector3.one;

                // 生成竖排文字
                string colText = "";
                for (int row = 0; row < maxRowsPerColumn; row++)
                {
                    int index = (colCount - 1 - col) * maxRowsPerColumn + row;
                    if (index >= totalChars) break;
                    colText += content[index] + "\n";
                }

                tmp.text = colText;
                tmp.fontSize = 24;
                tmp.color = Color.black;
            }

            // 创建标题
            var ts = "";
            foreach (var t in title)
            {
                ts += t.ToString() + "\n";
            }
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(transform, false);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = ts;
            titleTmp.fontSize = 32;
            titleTmp.lineSpacing = 20f;
            titleTmp.color = Color.black;
            titleTmp.alignment = TextAlignmentOptions.Center;
            _titleObj = titleObj;
        }

        public void OnGet()
        {
            // 清空旧的列
            foreach (Transform child in contents)
                Destroy(child.gameObject);

            if (_titleObj != null)
                Destroy(_titleObj);
        }

        public void OnRelease()
        {
            
        }
    }
}