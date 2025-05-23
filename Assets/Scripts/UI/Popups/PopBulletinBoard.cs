using System.Collections.Generic;
using Citizens;
using GameItem;
using UI.Elements;
using UI.Models;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popups
{
    public class PopBulletinBoardModel : ModelBase<PopBulletinBoard>
    {
        public override string Path => "PopBulletinBoard";
        public override ViewType ViewType => ViewType.Popup;

        public BulletinBoardItem BulletinBoardItem => Data[0] as BulletinBoardItem;
    }

    public class PopBulletinBoard : ViewBase<PopBulletinBoardModel>
    {
        [SerializeField] private Transform contents;
        private List<BulletinElement> _bulletinElements = new List<BulletinElement>();

        private bool IsOverlapping(Vector3 newPos)
        {
            foreach (var element in _bulletinElements)
            {
                var rectTransform = element.transform as RectTransform;
                if (rectTransform == null) continue;

                float width = rectTransform.rect.width;
                float height = rectTransform.rect.height;

                float minDistanceX = width * 0.8f;
                float minDistanceY = height * 0.8f;

                if (Mathf.Abs(element.transform.localPosition.x - newPos.x) < minDistanceX &&
                    Mathf.Abs(element.transform.localPosition.y - newPos.y) < minDistanceY)
                {
                    return true;
                }
            }
            return false;
        }

        private Vector3 GetRandomPosition()
        {
            RectTransform parentRect = contents as RectTransform;
            if (parentRect == null) return Vector3.zero;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                float x = Random.Range(-400f, 400f);
                float y = Random.Range(-200f, 200f);
                Vector3 candidate = new Vector3(x, y, 0f);

                // 判断是否在父容器内（留出固定边距）
                float padding = 100f;
                Vector2 halfSize = new Vector2(padding, padding);
                if (candidate.x >= parentRect.rect.min.x + halfSize.x &&
                    candidate.x <= parentRect.rect.max.x - halfSize.x &&
                    candidate.y >= parentRect.rect.min.y + halfSize.y &&
                    candidate.y <= parentRect.rect.max.y - halfSize.y && !IsOverlapping(candidate))
                {
                    return candidate;
                }
            }

            // 尝试失败返回最后一个候选
            return new Vector3(Random.Range(-300f, 300f), Random.Range(-150f, 150f), 0f);
        }

        private void CreateBulletin(string title, string content)
        {
            var element = UIManager.I.GetElement<BulletinElement>("Prefabs/UI/Elements/BulletinElement", Vector3.zero, contents);
            element.transform.localPosition = GetRandomPosition();
            _bulletinElements.Add(element);
            element.SetAncientText(title, content);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)element.transform);
        }

        public override void OnShow()
        {
            // CreateBulletin("招佃示告", "今有良田五亩，水利便捷，宜耕宜种，欲觅勤劳善耕之人租佃。有意者可往城东王掌柜处洽询，面议租事，切勿错失良机。");
            // CreateBulletin("缉拿令", "张三，男，三十岁，身高一丈二尺，体重一百四十斤，黑发黑瞳。该犯盗窃财物，恶意伤人，罪行重大，已畏罪潜逃。今府衙发榜通缉，凡民有知其踪迹者，或能缉拿归案者，赏银五百两，官府重谢。若有藏匿包庇之辈，悉以同罪论处。");
            // CreateBulletin("警示榜", "近有宵小为患，潜入民宅，行窃财物，乡民损失颇巨。凡四邻八舍，当加强防范，夜闭门户，切勿疏忽。若察觉可疑之人影行迹，速告官府，以保安宁。");

            foreach (var bulletin in Model.BulletinBoardItem.Bulletins)
            {
                CreateBulletin(bulletin.title, bulletin.content);
            }
        }

        public override void OnHide()
        {
            foreach (var element in _bulletinElements)
            {
                UIManager.I.ReleaseElement(element, "Prefabs/UI/Elements/BulletinElement");
            }
        }
    }
}