using System;
using System.Collections.Generic;
using AI;
using Citizens;
using UnityEngine;
using UnityEngine.Events;

namespace GameItem
{
    public class FurnitureItem : StaticGameItem
    {
        public override bool Walkable => true;
        public Agent Using { get; internal set; }

        public FurnitureItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }

        public override List<IAction> ItemActions()
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

        protected override List<IAction> ActionsOnClick()
        {
            return new List<IAction>()
            {
                new SleepAction(GameManager.I.CurrentAgent.State.Sleep, this)
            };
        }
    }

    public class TableItem : FurnitureItem
    {
        public override bool Walkable => false;
        public List<ChairItem> Chairs { get; } = new List<ChairItem>();

        public TableItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }

        public ChairItem GetChair()
        {
            return Chairs[0];
        }

        public override List<IAction> ItemActions()
        {
            return new List<IAction>()
            {
            };
        }
    }

    public class ChairItem : FurnitureItem
    {
        public event UnityAction<Agent> OnSit;
        public ChairItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }

        public override List<IAction> ItemActions()
        {
            return new List<IAction>()
            {

            };
        }

        internal void SitDown(Agent agent)
        {
            Using = agent;
            OnSit?.Invoke(agent);
        }
    }

    public class WellItem : FurnitureItem
    {
        public override bool Walkable => false;
        public WellItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }

        public override List<IAction> ItemActions()
        {
            return new List<IAction>()
            {
                new DrawWaterAction(this)
            };
        }
    }

    public class StoveItem : FurnitureItem
    {
        public override bool Walkable => false;
        public StoveItem(ConfigBase config, Vector3 pos = default) : base(config, pos)
        {
        }


        public void SetUsing(Agent agent)
        {
            Using = agent;
        }
    }
}