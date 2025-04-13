using System.Collections.Generic;
using AI;
using UnityEngine;

namespace GameItem
{
    public class FurnitureItem : GameItemBase
    {
        public FurnitureItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }

        public override List<IAction> OnSelected()
        {
            return new List<IAction>()
            {

            };
        }

        public override void ShowUI()
        {
            base.ShowUI();

            var resourceConfig = ConvtertConfig<BuildingConfig>();
            UI.SetRenderer(resourceConfig.icon);
        }

        public override void Update()
        {
            // Implement any specific update logic for furniture items here
        }
    }

    public class ToiletItem : FurnitureItem
    {
        public ToiletItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }
    }

    public class BedItem : FurnitureItem
    {
        public BedItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }
    }

    public class TableItem : FurnitureItem
    {
        public List<ChairItem> Chairs { get; } = new List<ChairItem>();

        public TableItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }

        public ChairItem GetChair()
        {
            return Chairs[0];
        }

        public override List<IAction> OnSelected()
        {
            return new List<IAction>()
            {
            };
        }
    }

    public class ChairItem : FurnitureItem
    {
        public ChairItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }

        public override List<IAction> OnSelected()
        {
            return new List<IAction>()
            {

            };
        }
    }

    public class WellItem : FurnitureItem
    {
        public WellItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }

        public override List<IAction> OnSelected()
        {
            return new List<IAction>()
            {
                new DrawWaterAction(this)
            };
        }
    }

    public class StoveItem : FurnitureItem
    {
        public StoveItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }
    }
}