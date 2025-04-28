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

    public T Get<T>(string prefabPath, Vector3 pos) where T : MonoBehaviour, IPoolable
    {
        if (_pool.ContainsKey(prefabPath) && _pool[prefabPath].Count > 0)
        {
            var i = _pool[prefabPath].Dequeue() as T;
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
        }
        else
        {
            instance.OnRelease();
            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}