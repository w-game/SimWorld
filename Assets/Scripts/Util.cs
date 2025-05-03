using System.Collections.Generic;
using UnityEngine;

public static class Util
{
    public static Vector2Int ToVector2Int(this Vector3 from)
    {
        return new Vector2Int((int)from.x, (int)from.y);
    }

    public static Vector3 ToVector3(this Vector2Int from)
    {
        return new Vector3(from.x, from.y, 0);
    }

    public static void Shuffle<T>(this List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}