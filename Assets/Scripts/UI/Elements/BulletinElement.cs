using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Elements
{
    public class BulletinElement : MonoBehaviour, IUIPoolable, IDragHandler, IPointerDownHandler, IPointerClickHandler
    {
        [SerializeField] private Transform contents;
        private GameObject _titleObj;
        private Vector2 _dragOffset;
        
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

        public void OnDrag(PointerEventData eventData)
        {
            RectTransform parentRect = transform.parent as RectTransform;
            RectTransform selfRect = transform as RectTransform;
    
            if (parentRect == null || selfRect == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out localPoint);

            float padding = 100f;
            Vector2 min = parentRect.rect.min + new Vector2(padding, padding) + selfRect.rect.size * 0.5f;
            Vector2 max = parentRect.rect.max - new Vector2(padding, padding) - selfRect.rect.size * 0.5f;

            localPoint -= _dragOffset;

            localPoint.x = Mathf.Clamp(localPoint.x, min.x, max.x);
            localPoint.y = Mathf.Clamp(localPoint.y, min.y, max.y);

            selfRect.localPosition = localPoint;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
            RectTransform selfRect = transform as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                selfRect,
                eventData.position,
                eventData.pressEventCamera,
                out _dragOffset);
        }

        public void OnPointerClick(PointerEventData eventData)
        {

        }
    }
}