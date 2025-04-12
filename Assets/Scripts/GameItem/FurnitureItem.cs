using UnityEngine;

namespace GameItem
{
    public class FurnitureItem : GameItemBase
    {
        public FurnitureItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
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
}