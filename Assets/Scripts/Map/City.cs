using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 城市数据，包含城市中心、半径、房屋和道路
public class City
{
    public Vector2Int Center { get; }
    public ChunkData Chunk { get; }
    public int Radius { get; }
    public List<House> Houses { get; } = new List<House>();
    public List<List<Vector2Int>> Roads { get; } = new List<List<Vector2Int>>();

    public List<Vector2Int> VerticalTrunk { get; } = new List<Vector2Int>();
    public List<Vector2Int> HorizontalTrunk { get; } = new List<Vector2Int>();

    public City(Vector2Int center, ChunkData chunk)
    {
        Chunk = chunk;
        Center = center;

        // 根据噪声决定城市 Radius，增加随机性
        float noiseVal = Mathf.PerlinNoise(center.x * 0.01f, center.y * 0.01f);
        Radius = 40 + (int)(noiseVal * 40);
    }

    // 生成主干路
    public void GenerateTrunkRoad()
    {
        int steps = 10;
        // 随机选一个大致在城市范围内的起点
        Vector2Int pos = new Vector2Int(
            Random.Range(Center.x - Radius, Center.x + Radius),
            Random.Range(Center.y - Radius, Center.y + Radius)
        );

        // 垂直干道
        int startY = pos.y - Radius;
        int endY = pos.y + Radius;
        for (int i = 0; i <= steps; i++)
        {
            int y = Mathf.RoundToInt(Mathf.Lerp(startY, endY, i / (float)steps));
            // 加一点 Perlin 噪声做扰动
            int baseX = pos.x + Mathf.RoundToInt(Mathf.PerlinNoise(pos.x * 0.1f, y * 0.1f) * 10 - 5);
            Vector2Int trunkPoint = new Vector2Int(baseX, y);
            VerticalTrunk.Add(trunkPoint);
        }
        Roads.Add(VerticalTrunk);

        // 水平干道
        int startX = pos.x - Radius;
        int endX = pos.x + Radius;
        for (int i = 0; i <= steps; i++)
        {
            int x = Mathf.RoundToInt(Mathf.Lerp(startX, endX, i / (float)steps));
            int baseY = pos.y + Mathf.RoundToInt(Mathf.PerlinNoise(x * 0.1f, pos.y * 0.1f) * 10 - 5);
            Vector2Int trunkPoint = new Vector2Int(x, baseY);
            HorizontalTrunk.Add(trunkPoint);
        }
        Roads.Add(HorizontalTrunk);
    }

    // 检查房屋是否与主干道重叠
    private bool IsHouseOverlappingTrunk(House house, List<Vector2Int> trunk)
    {
        int left = house.Pos.x;
        int right = house.Pos.x + house.Size.x;
        int bottom = house.Pos.y;
        int top = house.Pos.y + house.Size.y;

        foreach (Vector2Int point in trunk)
        {
            if (point.x >= left && point.x <= right && point.y >= bottom && point.y <= top)
            {
                return true;
            }
        }
        return false;
    }

    public void GenerateHouses()
    {
        int maxAttempts = 1000;
        int minDist = 2;         // 保持房屋间一定间距
        int targetHouseCount = 30;

        int attempts = 0;
        while (Houses.Count < targetHouseCount && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int pos = new Vector2Int(
                Random.Range(Center.x - Radius, Center.x + Radius),
                Random.Range(Center.y - Radius, Center.y + Radius)
            );

            // 距城市中心超出半径就跳过
            if (Vector2Int.Distance(pos, Center) > Radius)
                continue;

            House newHouse = new House(pos);

            // 检查与已有房屋的间距
            bool overlap = false;
            foreach (var house in Houses)
            {
                if (newHouse.Overlaps(house, margin: minDist))
                {
                    overlap = true;
                    break;
                }
            }

            // 检查是否与主干道重叠
            if (IsHouseOverlappingTrunk(newHouse, HorizontalTrunk) ||
                IsHouseOverlappingTrunk(newHouse, VerticalTrunk))
            {
                overlap = true;
            }

            if (!overlap)
            {
                Houses.Add(newHouse);
            }
        }
    }

    // 根据起点终点生成曲线并做平滑
    private List<Vector2Int> GenerateSmoothRoad(Vector2Int start, Vector2Int end)
    {
        // 用两个折线段拼成曲线
        Vector2Int mid = new Vector2Int((start.x + end.x) / 2, (start.y + end.y) / 2);
        mid.x += Random.Range(-8, 9);
        mid.y += Random.Range(-8, 9);

        List<Vector2Int> segment1 = BresenhamLine(start, mid);
        List<Vector2Int> segment2 = BresenhamLine(mid, end);

        if (segment2.Count > 0 && segment2[0] == segment1[segment1.Count - 1])
        {
            segment2.RemoveAt(0);
        }

        List<Vector2Int> combined = new List<Vector2Int>(segment1);
        combined.AddRange(segment2);

        return SmoothRoad(combined);
    }

    // 简易平滑：取相邻三点平均
    private List<Vector2Int> SmoothRoad(List<Vector2Int> road)
    {
        if (road.Count < 3) return road;

        List<Vector2Int> smooth = new List<Vector2Int>();
        smooth.Add(road[0]);

        for (int i = 1; i < road.Count - 1; i++)
        {
            int avgX = (road[i - 1].x + road[i].x + road[i + 1].x) / 3;
            int avgY = (road[i - 1].y + road[i].y + road[i + 1].y) / 3;
            smooth.Add(new Vector2Int(avgX, avgY));
        }

        smooth.Add(road[road.Count - 1]);
        return smooth;
    }

    private List<Vector2Int> BresenhamLine(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> points = new List<Vector2Int>();
        int x1 = start.x, y1 = start.y;
        int x2 = end.x, y2 = end.y;
        int dx = Mathf.Abs(x2 - x1);
        int sx = (x1 < x2) ? 1 : -1;
        int dy = -Mathf.Abs(y2 - y1);
        int sy = (y1 < y2) ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            points.Add(new Vector2Int(x1, y1));
            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x1 += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y1 += sy;
            }
        }

        return points;
    }

    public void GenerateCityRoadNetwork()
    {
        // 优先尝试连接主干道，否则连接最近房屋
        foreach (var house in Houses)
        {
            Vector2Int door = house.Door;
            Vector2Int nearestOnTrunk = door;
            float bestDist = float.MaxValue;

            // 找最近的主干道点
            foreach (var trunk in VerticalTrunk.Concat(HorizontalTrunk))
            {
                float d = Vector2Int.Distance(door, trunk);
                if (d < bestDist)
                {
                    bestDist = d;
                    nearestOnTrunk = trunk;
                }
            }

            // 如果足够近就连接主干道
            if (bestDist < 20)
            {
                var road = GenerateSmoothRoad(door, nearestOnTrunk);
                if (road != null && road.Count > 0)
                {
                    Roads.Add(road);
                    continue;
                }
            }

            // 否则连接到最近的其他房屋
            float bestHouseDist = float.MaxValue;
            Vector2Int nearestHouseDoor = door;
            foreach (var other in Houses)
            {
                if (other == house) continue;
                float dist = Vector2Int.Distance(door, other.Door);
                if (dist < bestHouseDist)
                {
                    bestHouseDist = dist;
                    nearestHouseDoor = other.Door;
                }
            }

            if (bestHouseDist < 30)
            {
                // 构建一个栅格来做寻路
                int offsetX = Chunk.WorldPos.x;
                int offsetY = Chunk.WorldPos.y;
                int[,] grid = new int[Chunk.Size, Chunk.Size];

                for (int gx = 0; gx < Chunk.Size; gx++)
                {
                    for (int gy = 0; gy < Chunk.Size; gy++)
                    {
                        BlockType bt = Chunk.Blocks[gx, gy];
                        // 简单判定：非建筑、非河流、非海洋等才可通行
                        if (bt != BlockType.City &&
                            bt != BlockType.Road &&
                            bt != BlockType.River &&
                            bt != BlockType.Ocean &&
                            bt != BlockType.Room)
                        {
                            grid[gx, gy] = 0; // 可通行
                        }
                        else
                        {
                            grid[gx, gy] = 1; // 不可通行
                        }
                    }
                }

                var startGridPos = door - new Vector2Int(offsetX, offsetY);
                var endGridPos = nearestHouseDoor - new Vector2Int(offsetX, offsetY);

                var path = AStar.FindPath(startGridPos, endGridPos, grid);
                if (path is { Count: > 0 })
                {
                    // 转回世界坐标
                    List<Vector2Int> fullPath = path
                        .Select(p => p + new Vector2Int(offsetX, offsetY))
                        .ToList();

                    Roads.Add(fullPath);
                }
            }
        }
    }
}

// 房屋数据
public class House
{
    public Vector2Int Pos { get; }
    public Vector2Int Size { get; }

    public Vector2Int Center => new(Pos.x + Size.x / 2, Pos.y + Size.y / 2);

    public Vector2Int Door
    {
        get
        {
            // 假设房屋门在下边缘中点
            int doorX = Pos.x + Size.x / 2;
            int doorY = Pos.y;
            return new Vector2Int(doorX, doorY);
        }
    }

    public List<Vector2Int> RoomBlocks
    {
        get
        {
            List<Vector2Int> list = new List<Vector2Int>();
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    list.Add(Pos + new Vector2Int(x, y));
                }
            }
            return list;
        }
    }

    public House(Vector2Int pos)
    {
        Pos = pos;
        Size = new Vector2Int(Random.Range(8, 13), Random.Range(8, 13));
    }

    public bool Overlaps(House other, int margin = 1)
    {
        int leftA = Pos.x - margin;
        int rightA = Pos.x + Size.x + margin;
        int bottomA = Pos.y - margin;
        int topA = Pos.y + Size.y + margin;

        int leftB = other.Pos.x - margin;
        int rightB = other.Pos.x + other.Size.x + margin;
        int bottomB = other.Pos.y - margin;
        int topB = other.Pos.y + other.Size.y + margin;

        if (rightA < leftB || leftA > rightB) return false;
        if (topA < bottomB || bottomA > topB) return false;
        return true;
    }
}

// 简易 A* 搜索，可自行根据需求优化
public static class AStar
{
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, int[,] grid)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        if (!InBounds(start.x, start.y, width, height) ||
            !InBounds(end.x, end.y, width, height) ||
            grid[start.x, start.y] == 1 ||
            grid[end.x, end.y] == 1)
        {
            return null;
        }

        var openSet = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float>();
        var fScore = new Dictionary<Vector2Int, float>();

        gScore[start] = 0f;
        fScore[start] = Heuristic(start, end);

        while (openSet.Count > 0)
        {
            // 取 fScore 最小的点
            Vector2Int current = openSet[0];
            float currentF = fScore[current];
            for (int i = 1; i < openSet.Count; i++)
            {
                float testF = fScore[openSet[i]];
                if (testF < currentF)
                {
                    current = openSet[i];
                    currentF = testF;
                }
            }

            if (current == end)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);

            foreach (var neighbor in GetNeighbors(current, width, height))
            {
                // 不可通行就跳过
                if (grid[neighbor.x, neighbor.y] == 1) continue;

                float tentativeG = gScore[current] + Vector2Int.Distance(current, neighbor);
                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, end);
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null;
    }

    private static bool InBounds(int x, int y, int width, int height)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        // 使用欧几里得距离当启发式
        return Vector2Int.Distance(a, b);
    }

    private static List<Vector2Int> GetNeighbors(Vector2Int current, int width, int height)
    {
        List<Vector2Int> dirs = new List<Vector2Int>
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };
        List<Vector2Int> result = new List<Vector2Int>();
        foreach (var d in dirs)
        {
            int nx = current.x + d.x;
            int ny = current.y + d.y;
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                result.Add(new Vector2Int(nx, ny));
            }
        }
        return result;
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}