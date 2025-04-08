using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameItem
{
    public class TableItem : GameItemBase<BuildingConfig>
    {
        public override string ItemName => "木桌";

        public List<ChairItem> Chairs { get; } = new List<ChairItem>();

        private void Start()
        {
            foreach (Transform chair in transform)
            {
                var chairItem = chair.GetComponent<ChairItem>();
                if (chairItem != null)
                {
                    Chairs.Add(chairItem);
                }
            }
        }

        public ChairItem GetChair()
        {
            return Chairs[0];
        }
    }
}