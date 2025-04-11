using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameItem
{
    public class TableItem : GameItemBase
    {
        public List<ChairItem> Chairs { get; } = new List<ChairItem>();

        public TableItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }

        public ChairItem GetChair()
        {
            return Chairs[0];
        }
    }
}