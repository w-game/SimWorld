// using System.Collections.Generic;
// using UnityEngine;

// public class AStar
// {
//     public class Node
//     {
//         public Vector2Int Position;
//         public Node Parent;
//         public int GCost; // Cost from start to current node
//         public int HCost; // Heuristic cost to the target
//         public int FCost => GCost + HCost; // Total cost

//         public Node(Vector2Int position, Node parent, int gCost, int hCost)
//         {
//             Position = position;
//             Parent = parent;
//             GCost = gCost;
//             HCost = hCost;
//         }
//     }

//     public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int target, int[,] grid)
//     {
//         List<Node> openList = new List<Node>();
//         HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

//         Node startNode = new Node(start, null, 0, GetHeuristic(start, target));
//         openList.Add(startNode);

//         while (openList.Count > 0)
//         {
//             Node currentNode = GetLowestFCostNode(openList);

//             if (currentNode.Position == target)
//                 return RetracePath(currentNode);

//             openList.Remove(currentNode);
//             closedSet.Add(currentNode.Position);

//             foreach (Vector2Int neighbor in GetNeighbors(currentNode.Position, grid))
//             {
//                 if (closedSet.Contains(neighbor))
//                     continue;

//                 int tentativeGCost = currentNode.GCost + 1;

//                 Node neighborNode = openList.Find(n => n.Position == neighbor);
//                 if (neighborNode == null)
//                 {
//                     neighborNode = new Node(neighbor, currentNode, tentativeGCost, GetHeuristic(neighbor, target));
//                     openList.Add(neighborNode);
//                 }
//                 else if (tentativeGCost < neighborNode.GCost)
//                 {
//                     neighborNode.GCost = tentativeGCost;
//                     neighborNode.Parent = currentNode;
//                 }
//             }
//         }

//         return null; // No path found
//     }

//     private static int GetHeuristic(Vector2Int a, Vector2Int b)
//     {
//         return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
//     }

//     private static Node GetLowestFCostNode(List<Node> nodes)
//     {
//         Node lowest = nodes[0];
//         foreach (Node node in nodes)
//         {
//             if (node.FCost < lowest.FCost || (node.FCost == lowest.FCost && node.HCost < lowest.HCost))
//                 lowest = node;
//         }
//         return lowest;
//     }

//     private static List<Vector2Int> GetNeighbors(Vector2Int position, int[,] grid)
//     {
//         List<Vector2Int> neighbors = new List<Vector2Int>();
//         Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

//         foreach (Vector2Int direction in directions)
//         {
//             Vector2Int neighborPos = position + direction;
//             if (neighborPos.x >= 0 && neighborPos.x < grid.GetLength(0) &&
//                 neighborPos.y >= 0 && neighborPos.y < grid.GetLength(1) &&
//                 grid[neighborPos.x, neighborPos.y] == 0) // 0 indicates walkable
//             {
//                 neighbors.Add(neighborPos);
//             }
//         }

//         return neighbors;
//     }

//     private static List<Vector2Int> RetracePath(Node endNode)
//     {
//         List<Vector2Int> path = new List<Vector2Int>();
//         Node currentNode = endNode;

//         while (currentNode != null)
//         {
//             path.Add(currentNode.Position);
//             currentNode = currentNode.Parent;
//         }

//         path.Reverse();
//         return path;
//     }
// }