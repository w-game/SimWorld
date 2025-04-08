using UnityEngine;

namespace GameItem
{
    public interface IGameItem
    {
        string ItemName { get; }
    }

    public abstract class MonoGameItem : MonoBehaviour, IGameItem
    {
        public abstract string ItemName { get; }
    }

    public abstract class GameItemBase<T> : MonoGameItem where T : ConfigBase
    {
        protected SpriteRenderer _sr;
        public T Config { get; protected set; }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public virtual void Init(T config)
        {
            Config = config;
            _sr.sprite = Resources.Load<Sprite>(config.icon);
        }
    }
}