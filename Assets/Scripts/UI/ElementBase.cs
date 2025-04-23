using UnityEngine;
using UnityEngine.Events;

namespace UI
{
    public abstract class ElementBase<T> : MonoBehaviour
    {
        public virtual void Init(T data, UnityAction<T> action, params object[] args)
        {
            // Initialize the element with provided data and action
        }
    }
}