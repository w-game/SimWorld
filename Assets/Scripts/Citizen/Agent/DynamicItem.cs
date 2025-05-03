using System.Collections.Generic;
using UnityEngine;

namespace GameItem
{
    public abstract class DynamicItem : GameItemBase<ConfigBase>
    {
        public List<IGameItem> OtherAround { get; private set; } = new List<IGameItem>();

        public new Vector3 Pos
        {
            get => _pos;
            set
            {
                OtherAround = GameItemManager.UpdateDynamicItems(this, value);
                _pos = value;
                if (UI != null)
                {
                    UI.transform.position = value;
                }
            }
        }
        
        public DynamicItem(ConfigBase config, Vector3 pos) : base(config, pos)
        {
        }
    }
}