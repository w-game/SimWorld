using System.Collections.Generic;
using UI.Elements;
using UI.Models;
using UnityEngine;

namespace UI.Popups
{
    public class PopBulletinBoardModel : ModelBase<PopBulletinBoard>
    {
        public override string Path => "PopBulletinBoard";
        public override ViewType ViewType => ViewType.Popup;
    }

    public class PopBulletinBoard : ViewBase<PopBulletinBoardModel>
    {
        [SerializeField] private Transform contents;
        private List<BulletinElement> _bulletinElements = new List<BulletinElement>();
        public override void OnShow()
        {
            var element = UIManager.I.GetElement<BulletinElement>("Prefabs/UI/Elements/BulletinElement", Vector3.zero, contents);
            _bulletinElements.Add(element);
            element.SetAncientText("招佃示告", "今有良田五亩，水利便捷，宜耕宜种，欲觅勤劳善耕之人租佃。有意者可往城东王掌柜处洽询，面议租事，切勿错失良机。");

            var element2 = UIManager.I.GetElement<BulletinElement>("Prefabs/UI/Elements/BulletinElement", Vector3.zero, contents);
            _bulletinElements.Add(element2);
            element2.SetAncientText("缉拿令", "张三，男，三十岁，身高一丈二尺，体重一百四十斤，黑发黑瞳。该犯盗窃财物，恶意伤人，罪行重大，已畏罪潜逃。今府衙发榜通缉，凡民有知其踪迹者，或能缉拿归案者，赏银五百两，官府重谢。若有藏匿包庇之辈，悉以同罪论处。");

            var element3 = UIManager.I.GetElement<BulletinElement>("Prefabs/UI/Elements/BulletinElement", Vector3.zero, contents);
            _bulletinElements.Add(element3);
            element3.SetAncientText("警示榜", "近有宵小为患，潜入民宅，行窃财物，乡民损失颇巨。凡四邻八舍，当加强防范，夜闭门户，切勿疏忽。若察觉可疑之人影行迹，速告官府，以保安宁。");
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