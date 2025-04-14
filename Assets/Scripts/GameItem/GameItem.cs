using System;
using System.Collections.Generic;
using AI;
using Citizens;
using UnityEngine;

namespace GameItem
{
    public interface IGameItem
    {
        Vector3 Pos { get; set; }
        ConfigBase Config { get; }
        GameItemUI UI { get; }
        void ShowUI();
        void HideUI();
        void Destroy();
        void DoUpdate();
        List<IAction> ItemActions();
    }

    public abstract class GameItemBase : IGameItem
    {
        private Vector3 _pos;
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

        public ConfigBase Config { get; protected set; }
        public GameItemUI UI { get; protected set; }
        public Family Owner { get; set; }

        public GameItemBase(ConfigBase config, Vector3 pos = default)
        {
            Config = config;
            Pos = pos;
            GameManager.I.GameItemManager.RegisterGameItem(this);
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

        public T ConvtertConfig<T>() where T : ConfigBase
        {
            return Config as T;
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
            if (UI != null)
            {
                HideUI();
            }
            GameManager.I.GameItemManager.UnregisterGameItem(this);
        }

        public abstract List<IAction> ItemActions();
    }

    public abstract class StaticGameItem : GameItemBase
    {
        public StaticGameItem(ConfigBase config, Vector3 pos) : base(config, pos)
        {
        }
    }

    public abstract class DynamicGameItem : GameItemBase
    {
        public DynamicGameItem(ConfigBase config, Vector3 pos) : base(config, pos)
        {
        }
    }
}