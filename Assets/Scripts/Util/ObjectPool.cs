using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnGet();
    void OnRelease();
}

public class ObjectPool
{
    private readonly Dictionary<string, Queue<IPoolable>> _pool = new Dictionary<string, Queue<IPoolable>>();
    private readonly int _maxSize;

    public ObjectPool(int maxSize)
    {
        _maxSize = maxSize;
    }

    public T Get<T>(string prefabPath, Vector3 pos, Transform parent) where T : MonoBehaviour, IPoolable
    {
        if (parent == null)
        {
            parent = GameManager.I.transform;
        }

        if (_pool.ContainsKey(prefabPath) && _pool[prefabPath].Count > 0)
        {
            var i = _pool[prefabPath].Dequeue() as T;
            i.transform.position = pos;
            i.gameObject.SetActive(true);
            i.OnGet();
            i.transform.SetParent(parent);
            return i;
        }
        var prefab = Resources.Load<GameObject>(prefabPath);
        var instance = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity, parent);
        T t = instance.GetComponent<T>() ?? instance.AddComponent<T>();
        t.OnGet();
        return t;
    }

    public T Get<T>(string prefabPath, Vector3 pos = default) where T : MonoBehaviour, IPoolable
    {
        return Get<T>(prefabPath, pos, null);
    }

    public T Get<T>(string prefabPath, Transform parent) where T : MonoBehaviour, IPoolable
    {
        return Get<T>(prefabPath, default, parent);
    }

    public void Release<T>(T instance, string prefabPath) where T : MonoBehaviour, IPoolable
    {
        if (!_pool.ContainsKey(prefabPath))
        {
            _pool[prefabPath] = new Queue<IPoolable>();
        }

        if (_pool[prefabPath].Count < _maxSize)
        {
            instance.OnRelease();
            _pool[prefabPath].Enqueue(instance);
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(GameManager.I.transform);
        }
        else
        {
            instance.OnRelease();
            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}