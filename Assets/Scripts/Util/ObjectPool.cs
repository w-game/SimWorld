using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnGet();
    void OnRelease();
}

public class ObjectPool<T> where T : MonoBehaviour, IPoolable
{
    private readonly Dictionary<string, Queue<T>> _pool = new Dictionary<string, Queue<T>>();
    private readonly int _maxSize;

    public ObjectPool(int maxSize)
    {
        _maxSize = maxSize;
    }

    public T Get(string prefabPath, Vector3 pos)
    {
        if (_pool.ContainsKey(prefabPath) && _pool[prefabPath].Count > 0)
        {
            var i = _pool[prefabPath].Dequeue();
            i.transform.position = pos;
            i.gameObject.SetActive(true);
            i.OnGet();
            return i;
        }
        var prefab = Resources.Load<GameObject>(prefabPath);
        T instance = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity).GetComponent<T>();
        instance.OnGet();
        return instance;
    }

    public void Release(T instance, string prefabPath)
    {
        if (!_pool.ContainsKey(prefabPath))
        {
            _pool[prefabPath] = new Queue<T>();
        }
        if (_pool[prefabPath].Count < _maxSize)
        {
            instance.OnRelease();
            _pool[prefabPath].Enqueue(instance);
            instance.gameObject.SetActive(false);
        }
        else
        {
            instance.OnRelease();
            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}