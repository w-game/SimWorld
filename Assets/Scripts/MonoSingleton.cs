using UnityEngine;

/// <summary>
/// A generic MonoBehaviour singleton that persists across scene loads.
/// </summary>
/// 
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// Access singleton instance. Creates the instance if one doesn't already exist.
    /// </summary>
    public static T I
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning("[MonoSingleton] Instance '" + typeof(T) +
                    "' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // Look for an existing instance in the scene.
                    _instance = (T)FindObjectOfType(typeof(T));

                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        Debug.LogError("[MonoSingleton] More than one instance of " + typeof(T) +
                            " found in the scene. There should only be one singleton instance.");
                        return _instance;
                    }

                    // If no instance exists, create a new GameObject.
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = "(singleton) " + typeof(T).ToString();

                        // Make instance persistent between scene loads.
                        DontDestroyOnLoad(singletonObject);
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// When the application quits, mark the singleton as destroyed to avoid creating a new instance.
    /// </summary>
    protected virtual void OnDestroy()
    {
        _applicationIsQuitting = true;
    }
}