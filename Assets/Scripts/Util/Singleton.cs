public class Singleton<T> where T : new()
{
    private static T _instance;

    public static T I
    {
        get
        {
            if (_instance == null)
            {
                _instance = new T();
            }
            return _instance;
        }
    }
}