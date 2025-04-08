using UnityEngine;

namespace GameItem
{
    public interface IGameItem
    {
    }

    public abstract class GameItemBase : MonoBehaviour, IGameItem
    {
        protected SpriteRenderer _sr;
        public ConfigBase Config { get; protected set; }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public virtual void Init(ConfigBase config)
        {
            Config = config;
            _sr.sprite = Resources.Load<Sprite>(config.icon);
        }

        public T ConvtertConfig<T>() where T : ConfigBase
        {
            return Config as T;
        }
    }
}