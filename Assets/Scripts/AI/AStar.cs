using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public class AStar
    {
        public class Node
        {
            public Vector2Int Position;
            public Node Parent;
            public float G;
            public float H;
            public float F => G + H;

            public Node(Vector2Int position, Node parent, float g, float h)
            {
                Position = position;
                Parent = parent;
                G = g;
                H = h;
            }
        }

        private static readonly Vector2Int[] Directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        static Dictionary<Vector2Int, float> _costs = new Dictionary<Vector2Int, float>()
        {
            { Vector2Int.up, 1 },
            { Vector2Int.down, 1 },
            { Vector2Int.left, 1 },
            { Vector2Int.right, 1 },
            { new Vector2Int(1, 1), Mathf.Sqrt(2) },
            { new Vector2Int(1, -1), Mathf.Sqrt(2) },
            { new Vector2Int(-1, 1), Mathf.Sqrt(2) },
            { new Vector2Int(-1, -1), Mathf.Sqrt(2) }
        };

        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, System.Func<Vector2Int, bool> isWalkable, int maxSteps = 20000, int maxRange = 30)
        {
            if (Mathf.Abs(goal.x - start.x) > maxRange || Mathf.Abs(goal.y - start.y) > maxRange)
            {
                Debug.LogWarning("[AStar] Goal too far from start.");
                return null;
            }

            var openSet = new List<Node>();
            var closedSet = new HashSet<Vector2Int>();
            openSet.Add(new Node(start, null, 0, Heuristic(start, goal)));

            int steps = 0;

            while (openSet.Count > 0 && steps < maxSteps)
            {
                steps++;

                openSet.Sort((a, b) => a.F.CompareTo(b.F));
                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.Position == goal)
                    return ReconstructPath(current);

                closedSet.Add(current.Position);

                foreach (var dir in Directions)
                {
                    Vector2Int neighborPos = current.Position + dir;

                    if (dir == new Vector2Int(1, 1) && (!isWalkable(current.Position + Vector2Int.right) || !isWalkable(current.Position + Vector2Int.up)))
                        continue;
                    
                    if (dir == new Vector2Int(-1, -1) && (!isWalkable(current.Position + Vector2Int.left) || !isWalkable(current.Position + Vector2Int.down)))
                        continue;

                    if (dir == new Vector2Int(1, -1) && (!isWalkable(current.Position + Vector2Int.right) || !isWalkable(current.Position + Vector2Int.down)))
                        continue;

                    if (dir == new Vector2Int(-1, 1) && (!isWalkable(current.Position + Vector2Int.left) || !isWalkable(current.Position + Vector2Int.up)))
                        continue;

                    if (closedSet.Contains(neighborPos) || !isWalkable(neighborPos))
                        continue;

                    float tentativeG = current.G + _costs[dir];
                    var existing = openSet.Find(n => n.Position == neighborPos);

                    if (existing == null)
                    {
                        openSet.Add(new Node(neighborPos, current, tentativeG, Heuristic(neighborPos, goal)));
                    }
                    else if (tentativeG < existing.G)
                    {
                        existing.G = tentativeG;
                        existing.Parent = current;
                    }
                }
            }

            Debug.LogWarning("[AStar] Reached max steps without finding path.");
            return null;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
        }

        private static List<Vector2Int> ReconstructPath(Node node)
        {
            var path = new List<Vector2Int>();
            while (node != null)
            {
                path.Add(node.Position);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }
    }
}
