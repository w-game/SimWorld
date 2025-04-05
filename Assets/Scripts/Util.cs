using UnityEngine;

public static class Util
{
    public static Vector2Int ToVector2Int(this Vector3 from)
    {
        return new Vector2Int((int)from.x, (int)from.y);
    }
}