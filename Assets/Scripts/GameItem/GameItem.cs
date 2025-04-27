using System;
using System.Collections.Generic;
using AI;
using Citizens;
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
        Vector3 Pos { get; set; }
        GameItemType ItemType { get; set; }
        Vector2Int Size { get; }
        ConfigBase ConfigBase { get; }
        GameItemUI UI { get; }
        bool Walkable { get; }
        void CalcSize();

        void ShowUI();
        void HideUI();
        void Destroy();
        void DoUpdate();
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
        public abstract bool Walkable { get; }
        public virtual List<Vector2Int> OccupiedPositions { get; } = new List<Vector2Int>();

        public GameItemBase(T config, Vector3 pos)
        {
            ConfigBase = config;
            _pos = pos;
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
                Debug.Log($"GameItemBase ShowUI {Config.prefab}");
                UI = GameManager.I.GameItemManager.ItemUIPool.Get(Config.prefab, Pos);
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