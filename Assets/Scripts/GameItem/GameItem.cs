using System;
using UnityEngine;

namespace GameItem
{
    public interface IGameItem
    {
        Vector3 Pos { get; set; }
        ConfigBase Config { get; }
        GameItemUI UI { get; }
        void DoUpdate();
    }

    public abstract class GameItemBase : IGameItem
    {
        public Vector3 Pos { get; set; }

        public ConfigBase Config { get; protected set; }
        public GameItemUI UI { get; protected set; }

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
                UI = GameManager.I.InstantiateObject(Config.prefab, Pos).GetComponent<GameItemUI>();
                UI.Init(this);
            }
        }

        public void HideUI()
        {
            if (UI != null)
            {
                UI.OnHide();
                UnityEngine.Object.Destroy(UI.gameObject);
            }
        }

        public T ConvtertConfig<T>() where T : ConfigBase
        {
            return Config as T;
        }

        public virtual void DoUpdate()
        {
            if (UI != null)
            {
                UI.transform.position = Pos;
            }

            Update();
        }

        public virtual void Update()
        {

        }

        internal void Destroy()
        {
            HideUI();
            GameManager.I.GameItemManager.UnregisterGameItem(this);
        }
    }
}