using System;
using System.Collections.Generic;
using System.Linq;
using AI;
using Citizens;
using Map;
using UnityEngine;

namespace GameItem
{
    public enum GameItemType
    {
        Static,
        Dynamic
    }

    public interface IGameItem
    {
        public bool Active { get; set; }
        Vector3 Pos { get; set; }
        GameItemType ItemType { get; set; }
        Vector2Int Size { get; }
        ConfigBase ConfigBase { get; }
        GameItemUI UI { get; }
        Family Owner { get; set; }
        bool Walkable { get; }

        void Init(ConfigBase config, Vector3 pos, params object[] args);
        void CalcSize();
        void ShowUI();
        void HideUI();
        void Destroy();
        void DoUpdate();
        List<Vector3> ArroundPosList();
        List<IAction> ItemActions(IGameItem agent);
        List<IAction> ActionsOnClick(Agent agent);
        List<Vector2Int> OccupiedPositions { get; }
    }

    public abstract class GameItemBase<T> : IGameItem where T : ConfigBase
    {
        protected Vector3 _pos;
        public Vector3 Pos
        {
            get => _pos;
            set
            {
                _pos = value;
                if (UI != null)
                {
                    UI.transform.position = value;
                }
            }
        }

        public GameItemType ItemType { get; set; }
        public Vector2Int Size { get; protected set; } = new Vector2Int(1, 1);

        public ConfigBase ConfigBase { get; protected set; }
        public T Config => (T)ConfigBase;
        public GameItemUI UI { get; protected set; }
        public Family Owner { get; set; }
        public bool Walkable { get; protected set; }
        public virtual List<Vector2Int> OccupiedPositions { get; } = new List<Vector2Int>();
        public bool Active { get; set; } = true;

        void IGameItem.Init(ConfigBase config, Vector3 pos, params object[] args) => Init((T)config, pos, args);

        public virtual void Init(T config, Vector3 pos, params object[] args)
        {
            ConfigBase = config;
            _pos = pos;

            Walkable = config == null ? false : config.walkable;
        }

        public List<Vector3> ArroundPosList()
        {
            HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();

            foreach (var pos in OccupiedPositions)
            {
                var targetPos = new Vector3(pos.x, pos.y) + Pos;
                TrySetPos(targetPos + new Vector3(0, -1), occupiedPositions);
                TrySetPos(targetPos + new Vector3(-1, 0), occupiedPositions);
                TrySetPos(targetPos + new Vector3(1, 0), occupiedPositions);
                TrySetPos(targetPos + new Vector3(0, 1), occupiedPositions);
            }
            return occupiedPositions.ToList();
        }

        private void TrySetPos(Vector3 pos, HashSet<Vector3> occupiedPositions)
        {
            var items = GameManager.I.GameItemManager.GetItemsAtPos(pos);
            var walkable = true;

            foreach (var item in items)
            {
                if (item.Walkable == false)
                {
                    walkable = false;
                    break;
                }
            }

            if (walkable)
            {
                occupiedPositions.Add(pos);
            }
        }

        public void CalcSize()
        {
            int sizeX = Size.x, sizeY = Size.y;

            int startX = sizeX == 1 ? 0 : -sizeX % 2;
            int startY = sizeY == 1 ? 0 : -sizeY % 2;

            for (int i = startX; i < startX + sizeX; i++)
            {
                for (int j = startY; j < startY + sizeY; j++)
                {
                    OccupiedPositions.Add(new Vector2Int(i, j));
                }
            }
        }

        public virtual void ShowUI()
        {
            if (UI == null)
            {
                UI = GameManager.I.GameItemManager.ItemUIPool.Get<GameItemUI>(Config.prefab, Pos + new Vector3(0.5f, 0.5f, 0));
                UI.Init(this);
            }
        }

        public virtual void HideUI()
        {
            if (UI != null)
            {
                Debug.Log($"GameItemBase HideUI {UI.name}");
                GameManager.I.GameItemManager.ItemUIPool.Release(UI, Config.prefab);
                UI = null;
            }
        }

        public void DoUpdate()
        {
            Update();
        }

        public virtual void Update()
        {

        }

        public virtual void Destroy()
        {
            HideUI();
        }

        public abstract List<IAction> ItemActions(IGameItem agent);

        public virtual List<IAction> ActionsOnClick(Agent agent)
        {
            return ItemActions(agent);
        }
    }
}