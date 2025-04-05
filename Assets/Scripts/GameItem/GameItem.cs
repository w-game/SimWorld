using UnityEngine;

namespace GameItem
{
    public interface IGameItem
    {
        string ItemName { get; }
    }

    public abstract class GameItemBase : MonoBehaviour, IGameItem
    {
        public abstract string ItemName { get; }
    }
}