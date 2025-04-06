using UnityEngine;

namespace GameItem
{
    public interface IGameItem
    {
        string ItemId { get; }
        string ItemName { get; }
    }

    public abstract class GameItemBase : MonoBehaviour, IGameItem
    {
        public string ItemId { get; protected set; }
        public abstract string ItemName { get; }

    }
}