using System.Collections.Generic;
using Map;
using UnityEngine;

public class Test : MonoBehaviour
{
    private Dictionary<Vector2Int, CellType> _cellMap;
    private Dictionary<Room, Color> _roomColors = new Dictionary<Room, Color>();
    private HashSet<Vector2Int> _wallPositions = new HashSet<Vector2Int>();

    void Start()
    {
        Vector2Int minPos = new Vector2Int(10, 10);
        Vector2Int size = new Vector2Int(12, 12);
        System.Random rand = new System.Random();

        _cellMap = Room.SplitBSPRooms(minPos, size, rand);
        _roomColors.Clear();
        _wallPositions.Clear();
        foreach (var kv in _cellMap)
        {
            Vector2Int pos = kv.Key;
            if (kv.Value == CellType.Wall) _wallPositions.Add(pos);
            // Additional logic to handle room colors can be added here if needed
        }
    }

    void OnDrawGizmos()
    {
        if (_cellMap == null) return;

        foreach (var kv in _cellMap)
        {
            Vector3 center = new Vector3(kv.Key.x + 0.5f, kv.Key.y + 0.5f, 0);
            Vector3 size = new Vector3(1f, 1f, 0.1f);

            switch (kv.Value)
            {
                case CellType.Room:
                    Gizmos.color = new Color(0.7f, 0.7f, 1f); break;
                case CellType.Wall:
                    Gizmos.color = new Color(0.75f, 0.75f, 0.75f); break;
                default:
                    continue;
            }

            Gizmos.DrawCube(center, size);
        }
    }
}