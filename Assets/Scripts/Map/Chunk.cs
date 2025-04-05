using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// public enum BlockType
// {
//     Ocean,
//     Plain,
//     Village  // 用于标记村庄区域
// }

// public class House {
//     public Vector2Int pos; // 房屋左下角在全局坐标下的位置
//     public int width;
//     public int height;
//     
//     public Vector2Int Center {
//         get {
//             return new Vector2Int(pos.x + width / 2, pos.y + height / 2);
//         }
//     }
//     
//     public bool Overlaps(House other, int margin = 4)
//     {
//         // 将自身和对方都向外扩张 margin
//         int leftA   = pos.x - margin;
//         int rightA  = pos.x + width + margin;
//         int bottomA = pos.y - margin;
//         int topA    = pos.y + height + margin;
//
//         int leftB   = other.pos.x - margin;
//         int rightB  = other.pos.x + other.width + margin;
//         int bottomB = other.pos.y - margin;
//         int topB    = other.pos.y + other.height + margin;
//
//         // 若矩形A在水平方向或垂直方向与B完全分离，则不重叠
//         if (rightA < leftB || leftA > rightB) return false;
//         if (topA < bottomB || bottomA > topB) return false;
//
//         // 否则判定为重叠
//         return true;
//     }
// }

public class Chunk : MonoBehaviour
{
    [Header("Tilemap 相关")]
    public Tilemap tilemap;
    public TileBase baseTile;

    [Header("地形噪声参数")]
    public float continentScale = 0.001f;
    public Vector2 continentOffset = new Vector2(1000f, 1000f);
    public float continentThreshold = 0.5f;
    public float ridgedScale = 0.005f;
    public float ridgedWeight = 0.10f;
    public float ridgedOffset = 500f;
    public float warpScale1 = 0.01f;
    public float warpStrength1 = 20f;
    public float warpScale2 = 0.05f;
    public float warpStrength2 = 5f;
    public int seed = 12345;

    public Vector2Int Pos { get; private set; }
    public Dictionary<Vector2Int, BlockType> Blocks = new Dictionary<Vector2Int, BlockType>();

    [Header("村庄生成参数")]
    public bool generateVillage = true; // 是否生成村庄
    // 村庄区域在当前 Chunk 内的局部坐标（建议不要超过 Chunk 的尺寸）
    public int villageAreaWidth = 50;    
    public int villageAreaHeight = 50;   
    public float minDistanceBetweenHouses = 3f; // 泊松采样最小距离

    [Header("房屋尺寸设置")]
    public int minHouseWidth = 3, maxHouseWidth = 8;
    public int minHouseHeight = 3, maxHouseHeight = 8;

    [Header("村庄生成控制")]
    [Tooltip("村庄生成概率，0～1之间，控制当前 Chunk 是否生成村庄")]
    public float villageSpawnProbability = 0.5f;
    [Tooltip("两个村庄之间的最小距离（全局坐标）")]
    public int minVillageDistance = 100;

    // 用于跨 Chunk 控制村庄间隔的全局列表
    public static List<Vector2Int> globalVillageCenters = new List<Vector2Int>();

    // 内部存储房屋与道路信息（针对当前 Chunk）
    private List<House> houses = new List<House>();
    private List<(Vector2Int start, Vector2Int end)> roads = new List<(Vector2Int, Vector2Int)>();

    /// <summary>
    /// 生成基础区块地形
    /// </summary>
    public void GenerateChunk(Vector2Int chunkCoord, int chunkSize)
    {
        // 每次生成新 Chunk 前清空前一 Chunk 的数据
        houses.Clear();
        roads.Clear();
        Blocks.Clear();

        Pos = chunkCoord;
        // 遍历区块内每个瓷砖
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int worldX = chunkCoord.x * chunkSize + x;
                int worldY = chunkCoord.y * chunkSize + y;

                // ===== 1. 多层领域扭曲，生成采样坐标 =====
                float warpX = worldX + ComputeWarp(worldX, worldY, warpScale1, warpStrength1, 123.45f);
                warpX += ComputeWarp(worldX, worldY, warpScale2, warpStrength2, 987.65f);
                float warpY = worldY + ComputeWarp(worldX, worldY, warpScale1, warpStrength1, 67.89f);
                warpY += ComputeWarp(worldX, worldY, warpScale2, warpStrength2, 543.21f);

                // ===== 2. 大陆噪声采样 =====
                float continentSampleX = (warpX + continentOffset.x + seed) * continentScale;
                float continentSampleY = (warpY + continentOffset.y + seed) * continentScale;
                float baseContinent = Mathf.PerlinNoise(continentSampleX, continentSampleY);

                // ===== 3. 脊状噪声混合 =====
                float ridgedSampleX = (warpX + ridgedOffset) * ridgedScale;
                float ridgedSampleY = (warpY + ridgedOffset) * ridgedScale;
                float ridged = 1f - Mathf.Abs(Mathf.PerlinNoise(ridgedSampleX, ridgedSampleY) * 2f - 1f);
                float combinedContinent = baseContinent - ridged * ridgedWeight;

                bool isLand = combinedContinent > continentThreshold;
                Color color = Color.black;
                if (!isLand)
                {
                    // 水域区域
                    float distanceFromLand = continentThreshold - combinedContinent;
                    distanceFromLand = Mathf.Max(distanceFromLand, 0f);
                    distanceFromLand = Mathf.Clamp01(distanceFromLand / 0.3f);
                    if (distanceFromLand < 0.33f)
                    {
                        color = new Color(0.2f, 0.8f, 1f); // 浅蓝
                    }
                    else if (distanceFromLand < 0.66f)
                    {
                        color = new Color(0f, 0.5f, 0.9f); // 中蓝
                    }
                    else
                    {
                        color = new Color(0f, 0.2f, 0.6f); // 深蓝
                    }
                    Blocks.Add(new Vector2Int(x, y), BlockType.Ocean);
                }
                else
                {
                    // 陆地区域，通过细节噪声决定颜色
                    float detail = Mathf.PerlinNoise((warpX + seed) / 50f, (warpY + seed) / 50f);
                    if (detail < 0.4f)
                        color = new Color(1f, 0.9f, 0.4f); // 沙滩
                    else if (detail < 0.6f)
                        color = new Color(0.4f, 1f, 0.4f); // 草地
                    else if (detail < 0.8f)
                        color = new Color(1f, 0.6f, 0.3f); // 山地
                    else
                        color = Color.white;              // 雪山
                    Blocks.Add(new Vector2Int(x, y), BlockType.Plain);
                }

                Vector3Int tilePos = new Vector3Int(x, y, 0);
                tilemap.SetTile(tilePos, baseTile);
                tilemap.SetColor(tilePos, color);
            }
        }

        // 计算当前 Chunk 在全局的偏移（世界坐标原点）
        int worldOffsetX = chunkCoord.x * chunkSize;
        int worldOffsetY = chunkCoord.y * chunkSize;
        // 根据生成概率决定是否生成村庄
        if (generateVillage && Random.value < villageSpawnProbability)
        {
            // 检查当前 Chunk 是否已有足够远的村庄：以当前 Chunk 中心为参考
            Vector2Int chunkCenter = new Vector2Int(worldOffsetX + villageAreaWidth / 2, worldOffsetY + villageAreaHeight / 2);
            bool tooClose = false;
            foreach (Vector2Int vCenter in globalVillageCenters)
            {
                if (Vector2Int.Distance(chunkCenter, vCenter) < minVillageDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
            {
                // 记录当前村庄中心到全局列表中
                globalVillageCenters.Add(chunkCenter);
                GenerateVillageNatural(worldOffsetX, worldOffsetY);
            }
        }
    }

    // 计算领域扭曲偏移
    private float ComputeWarp(int x, int y, float scale, float strength, float offset)
    {
        float sampleX = (x + seed) * scale;
        float sampleY = (y + seed) * scale;
        return (Mathf.PerlinNoise(sampleX + offset, sampleY + offset) * 2f - 1f) * strength;
    }

    /// <summary>
    /// 生成自然村庄：利用泊松圆盘采样、噪声过滤、房屋随机尺寸和最小生成树生成道路网络\n    /// worldOffsetX/Y 用于将局部村庄坐标转换为全局坐标\n    /// 这里不再固定限制房屋数量，房屋数量将由村庄区域面积自动决定\n    /// </summary>
    public void GenerateVillageNatural(int worldOffsetX, int worldOffsetY)
    {
        // 1. 在村庄区域（局部坐标 0 ~ villageAreaWidth/Height 内）利用泊松采样生成候选点
        List<Vector2> candidatePoints = PoissonDiskSampling(minDistanceBetweenHouses, villageAreaWidth, villageAreaHeight);
        List<Vector2Int> houseCenters = new List<Vector2Int>();
        foreach (Vector2 pt in candidatePoints)
        {
            // 使用 Perlin 噪声过滤，让部分区域更容易生成房屋
            float noise = Mathf.PerlinNoise(pt.x * 0.1f, pt.y * 0.1f);
            if (noise > 0.3f)
            {
                // 注意：候选点为局部村庄区域坐标
                houseCenters.Add(new Vector2Int(Mathf.RoundToInt(pt.x), Mathf.RoundToInt(pt.y)));
            }
        }
        
        // 2. 根据候选中心随机生成房屋尺寸，并检测重叠
        // 房屋生成时将局部坐标转换为全局坐标（加上 worldOffset）
        foreach (Vector2Int center in houseCenters)
        {
            bool placed = false;
            int attempts = 0;
            while (!placed && attempts < 10)
            {
                attempts++;
                int width = Random.Range(minHouseWidth, maxHouseWidth + 1);
                int height = Random.Range(minHouseHeight, maxHouseHeight + 1);
                // 根据中心点反推房屋左下角在局部区域内的位置
                int localPosX = center.x - width / 2;
                int localPosY = center.y - height / 2;
                // 检查房屋是否在村庄局部区域内
                if (localPosX < 0 || localPosY < 0 || localPosX + width > villageAreaWidth || localPosY + height > villageAreaHeight)
                    continue;
                // 转换为全局坐标（加上 worldOffset）
                int posX = worldOffsetX + localPosX;
                int posY = worldOffsetY + localPosY;
                
                // 检查房屋覆盖区域是否全部为 Plain
                bool isAllPlain = true;
                for (int hx = 0; hx < width; hx++)
                {
                    for (int hy = 0; hy < height; hy++)
                    {
                        int globalX = posX + hx;
                        int globalY = posY + hy;
                        int tileX = globalX - worldOffsetX;
                        int tileY = globalY - worldOffsetY;
                        Vector2Int tilePos = new Vector2Int(tileX, tileY);
                        if (!Blocks.ContainsKey(tilePos) || Blocks[tilePos] != BlockType.Plain)
                        {
                            isAllPlain = false;
                            break;
                        }
                    }
                    if (!isAllPlain)
                        break;
                }
                if (!isAllPlain)
                    continue;

                // { pos = , width = width, height = height };
                House candidate = new House(new Vector2Int(posX, posY));
                bool overlap = false;
                foreach (House h in houses)
                {
                    // 使用较大的 margin 以预留更多缓冲区域
                    if (candidate.Overlaps(h, margin: 1))
                    {
                        overlap = true;
                        break;
                    }
                }
                if (!overlap)
                {
                    houses.Add(candidate);
                    placed = true;
                }
            }
        }
        
        // 3. 使用 Prim 算法连接各房屋中心生成道路网络
        GenerateRoadNetwork();
        VisualizeVillage();
    }

    /// <summary>
    /// 简单实现泊松圆盘采样，返回区域内候选点（局部坐标）
    /// </summary>
    List<Vector2> PoissonDiskSampling(float minDist, int width, int height, int newPointsCount = 30)
    {
        List<Vector2> points = new List<Vector2>();
        List<Vector2> processList = new List<Vector2>();
        
        Vector2 initialPoint = new Vector2(Random.Range(0, width), Random.Range(0, height));
        points.Add(initialPoint);
        processList.Add(initialPoint);
        
        while (processList.Count > 0)
        {
            int index = Random.Range(0, processList.Count);
            Vector2 point = processList[index];
            bool found = false;
            for (int i = 0; i < newPointsCount; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                float radius = Random.Range(minDist, 2 * minDist);
                Vector2 newPoint = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (newPoint.x >= 0 && newPoint.x < width && newPoint.y >= 0 && newPoint.y < height)
                {
                    bool valid = true;
                    foreach (Vector2 p in points)
                    {
                        if (Vector2.Distance(newPoint, p) < minDist)
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (valid)
                    {
                        points.Add(newPoint);
                        processList.Add(newPoint);
                        found = true;
                    }
                }
            }
            if (!found)
            {
                processList.RemoveAt(index);
            }
        }
        return points;
    }

    /// <summary>
    /// 使用 Prim 算法将所有房屋中心连接，生成道路网络
    /// </summary>
    void GenerateRoadNetwork()
    {
        if (houses.Count == 0) return;
        List<Vector2Int> nodes = new List<Vector2Int>();
        foreach (House h in houses)
        {
            nodes.Add(h.Center);
        }
        List<Vector2Int> connected = new List<Vector2Int>();
        List<Vector2Int> remaining = new List<Vector2Int>(nodes);
        connected.Add(remaining[0]);
        remaining.RemoveAt(0);
        while (remaining.Count > 0)
        {
            float bestDist = float.MaxValue;
            Vector2Int bestFrom = Vector2Int.zero;
            Vector2Int bestTo = Vector2Int.zero;
            foreach (Vector2Int c in connected)
            {
                foreach (Vector2Int r in remaining)
                {
                    float dist = Vector2Int.Distance(c, r);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestFrom = c;
                        bestTo = r;
                    }
                }
            }
            roads.Add((bestFrom, bestTo));
            connected.Add(bestTo);
            remaining.Remove(bestTo);
        }
    }

    /// <summary>
    /// 可视化房屋和道路：本示例使用 Debug 绘制，可根据需要实例化预制体
    /// </summary>
    void VisualizeVillage()
    {
        // 绘制房屋边界（红色线条）
        foreach (House h in houses)
        {
            Vector3 bottomLeft = new Vector3(h.Pos.x, h.Pos.y, 0);
            Vector3 bottomRight = new Vector3(h.Pos.x + h.Size.x, h.Pos.y, 0);
            Vector3 topLeft = new Vector3(h.Pos.x, h.Pos.y + h.Size.y, 0);
            Vector3 topRight = new Vector3(h.Pos.x + h.Size.y, h.Pos.y + h.Size.y, 0);
            Debug.DrawLine(bottomLeft, bottomRight, Color.red, 100f);
            Debug.DrawLine(bottomLeft, topLeft, Color.red, 100f);
            Debug.DrawLine(topLeft, topRight, Color.red, 100f);
            Debug.DrawLine(bottomRight, topRight, Color.red, 100f);
        }
        // 绘制道路（黄色线条）
        foreach (var road in roads)
        {
            Debug.DrawLine(new Vector3(road.start.x, road.start.y, 0),
                           new Vector3(road.end.x, road.end.y, 0),
                           Color.yellow, 100f);
        }
    }
    
    // 编辑器中使用 Gizmos 辅助显示房屋和道路
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (House h in houses)
        {
            Vector3 pos = new Vector3(h.Pos.x, h.Pos.y, 0);
            Vector3 size = new Vector3(h.Size.x, h.Size.y, 0);
            Gizmos.DrawWireCube(pos + size / 2, size);
        }
        Gizmos.color = Color.yellow;
        foreach (var road in roads)
        {
            Gizmos.DrawLine(new Vector3(road.start.x, road.start.y, 0),
                            new Vector3(road.end.x, road.end.y, 0));
        }
    }
}