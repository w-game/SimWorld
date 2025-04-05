using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 用于生成初步地形高度图：
/// - 先区分大陆海洋 (基础噪声)
/// - 再叠加 ridge 噪声塑造山脊
/// - 应用域扭曲等技术
/// </summary>
public class OptimizedTerrainGenerator
{
    public int mapSize = 512;
    public int seed = 42;

    public int noiseOctaves = 2;
    public float noisePersistence = 0.5f;
    public float noiseLacunarity = 2.0f;

    public int ridgeOctaves = 2;
    public float warpScale = 0.5f;
    public float warpFrequency = 0.05f;


    public float[,] GenerateBaseHeightmapJob()
    {
        // 分配 NativeArray 用于结果存储
        NativeArray<float> result = new NativeArray<float>(mapSize * mapSize, Allocator.TempJob);

        // 创建 Job
        ParallelNoiseJob job = new ParallelNoiseJob
        {
            result = result,
            mapSize = mapSize,
            landNoiseFreq = 0.1f,
            seed = seed
        };

        // 调度并等待完成
        JobHandle handle = job.Schedule(mapSize * mapSize, 64);
        handle.Complete();

        // 拷回到普通 2D 数组
        float[,] heightmap = new float[mapSize, mapSize];
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                int index = y * mapSize + x;
                heightmap[x, y] = result[index];
            }
        }

        // 释放
        result.Dispose();

        return heightmap;
    }

    /// <summary>
    /// 生成基础高度图：低频噪声决定海陆
    /// </summary>
    /// 
    public float[,] GenerateBaseHeightmap()
    {
        float[,] heightmap = new float[mapSize, mapSize];
        float landNoiseFreq = 0.1f;

        // 简单示例：PerlinNoise < 0.0f 意味着海洋(这里先保持正值，后面再决定是否海洋)
        // 实际可调整阈值分割
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                // Unity 的 Mathf.PerlinNoise 参数范围通常在 [0,1]
                // 如果需要负值，需要加一些 offset 或使用其他方式
                float n = Mathf.PerlinNoise(i * landNoiseFreq + seed, j * landNoiseFreq + seed);

                // 将 n 映射到 [-1,1] 的范畴（可选步骤，看需求）
                // float mapped = n * 2f - 1f;
                float mapped = (n - 0.3f) * 2f;

                // 这里假设 <0 的部分是海洋
                if (mapped < 0)
                {
                    heightmap[i, j] = mapped * 20f;
                }
                else
                {
                    heightmap[i, j] = mapped * 20f;
                }
            }
        }
        return heightmap;
    }

    /// <summary>
    /// 在基础高度图上叠加更多细节：多层噪声、Ridge噪声、域扭曲等
    /// </summary>
    public float[,] ApplyTerrainNoise(float[,] baseHeightmap)
    {
        int h = baseHeightmap.GetLength(0);
        int w = baseHeightmap.GetLength(1);
        float[,] newMap = new float[h, w];
        Array.Copy(baseHeightmap, newMap, baseHeightmap.Length);

        // 遍历每个像素叠加额外噪声
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                float x = (i + seed) * 0.01f;
                float y = (j + seed) * 0.01f;

                // Domain warping removed; using original coordinates

                // === 多层 fBM 噪声 ===
                float frequency = 1f;
                float amplitude = 1f;
                float fBmVal = 0f;
                for (int o = 0; o < noiseOctaves; o++)
                {
                    float nx = x * frequency;
                    float ny = y * frequency;
                    float val = Mathf.PerlinNoise(nx, ny);
                    fBmVal += val * amplitude;

                    frequency *= noiseLacunarity;
                    amplitude *= noisePersistence;
                }


                // 组合到原高度图
                newMap[i, j] += fBmVal * 10f;     // 普通山丘
            }
        }

        // 简单平滑处理
        newMap = Smooth(newMap, 1);

        return newMap;
    }

    /// <summary>
    /// 邻域平均做简单平滑滤波
    /// </summary>
    private float[,] Smooth(float[,] map, int radius)
    {
        int h = map.GetLength(0);
        int w = map.GetLength(1);
        float[,] smoothed = new float[h, w];

        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                float sum = 0f;
                int count = 0;
                for (int di = -radius; di <= radius; di++)
                {
                    for (int dj = -radius; dj <= radius; dj++)
                    {
                        int ni = i + di;
                        int nj = j + dj;
                        if (ni >= 0 && ni < h && nj >= 0 && nj < w)
                        {
                            sum += map[ni, nj];
                            count++;
                        }
                    }
                }
                smoothed[i, j] = sum / count;
            }
        }
        return smoothed;
    }
}

/// <summary>
/// 用于在高度图上执行：
/// 1. 计算流向 FlowDirection
/// 2. 计算汇流累积量 Flow Accumulation
/// 3. 根据汇流量刻画河道
/// 4. 检测封闭盆地并填湖
/// 5. 简单热力侵蚀
/// </summary>
public class HydrologyAndErosion
{
    public float riverThreshold = 100f;  // 超过此汇流量就视为河流
    public float lakeFillDepth = 2f;     // 湖泊填充深度
    public float talusAngle = 2f;       // 热侵蚀临界

    // 存流向，每个像素指向其最大下降邻居
    private Vector2Int[,] flowDir;

    /// <summary>
    /// 外部调用：输入高度图，返回修改后的高度图
    /// （内部会生成水体信息也可以做成单独返回）
    /// </summary>
    public (float[,], int[,]) ApplyHydrology(float[,] heightmap)
    {
        int size = heightmap.GetLength(0);
        // 1. 计算流向
        flowDir = ComputeFlowDirections(heightmap);
        // 2. 计算汇流量
        float[,] flowAcc = ComputeFlowAccumulation(heightmap, flowDir);
        // 3. 雕刻河道 & 湖泊
        float[,] newHeightmap = (float[,])heightmap.Clone();
        int[,] waterMap = CarveRivers(newHeightmap, flowAcc);
        // 4. 轻微热力侵蚀
        newHeightmap = ErodeTerrain(newHeightmap, 2);

        return (newHeightmap, waterMap); // 返回最终地形
    }

    /// <summary>
    /// 计算每个格点流向 (D8邻居)
    /// </summary>
    private Vector2Int[,] ComputeFlowDirections(float[,] heightmap)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        Vector2Int[,] directions = new Vector2Int[h, w];

        // 八方向
        Vector2Int[] dirs = {
            new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(0, -1), new Vector2Int(0, 1),
            new Vector2Int(-1, -1), new Vector2Int(-1, 1),
            new Vector2Int(1, -1), new Vector2Int(1, 1)
        };

        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                float currentH = heightmap[i, j];
                float maxDrop = 0f;
                Vector2Int bestDir = Vector2Int.zero;

                foreach (var d in dirs)
                {
                    int ni = i + d.x;
                    int nj = j + d.y;
                    if (ni >= 0 && ni < h && nj >= 0 && nj < w)
                    {
                        float drop = currentH - heightmap[ni, nj];
                        if (drop > maxDrop)
                        {
                            maxDrop = drop;
                            bestDir = d;
                        }
                    }
                }
                directions[i, j] = bestDir; // 如果=zero表示无更低邻居(封闭盆地)
            }
        }

        return directions;
    }

    /// <summary>
    /// 拓扑排序计算汇流累积量
    /// </summary>
    private float[,] ComputeFlowAccumulation(float[,] heightmap, Vector2Int[,] flowDir)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        float[,] flowAcc = new float[h, w];
        int[,] indeg = new int[h, w];

        // init
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                flowAcc[i, j] = 1f;
                indeg[i, j] = 0;
            }
        }

        // 计算入度
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                Vector2Int d = flowDir[i, j];
                if (d != Vector2Int.zero)
                {
                    int nx = i + d.x;
                    int ny = j + d.y;
                    indeg[nx, ny]++;
                }
            }
        }

        // 拓扑排序
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                if (indeg[i, j] == 0)
                {
                    queue.Enqueue(new Vector2Int(i, j));
                }
            }
        }

        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            int cx = c.x;
            int cy = c.y;

            Vector2Int d = flowDir[cx, cy];
            if (d != Vector2Int.zero)
            {
                int nx = cx + d.x;
                int ny = cy + d.y;
                flowAcc[nx, ny] += flowAcc[cx, cy];
                indeg[nx, ny]--;
                if (indeg[nx, ny] == 0)
                {
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        return flowAcc;
    }

    /// <summary>
    /// 根据汇流量雕刻河道，并检测封闭盆地填充湖泊
    /// </summary>
    private int[,] CarveRivers(float[,] heightmap, float[,] flowAcc)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        int[,] waterMap = new int[h, w]; // 0=陆地,1=河流,2=湖/海

        // 标记初始海洋
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                if (heightmap[i, j] < 0)
                {
                    waterMap[i, j] = 2;
                }
                else
                {
                    waterMap[i, j] = 0;
                }
            }
        }

        // 雕刻河道
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                if (flowAcc[i, j] >= riverThreshold && waterMap[i, j] == 0)
                {
                    waterMap[i, j] = 1;
                    // 将高度略微降低
                    heightmap[i, j] -= 1f;
                }
            }
        }

        // 检测封闭盆地填湖
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                // 如果流向=zero，且高度>0，说明可能是封闭高地
                if (flowDir[i, j] == Vector2Int.zero && heightmap[i, j] > 0)
                {
                    FloodFillLake(heightmap, waterMap, i, j);
                }
            }
        }

        return waterMap;
    }

    /// <summary>
    /// 在封闭盆地处“抬高”到 lakeFillDepth (简单模拟湖泊)
    /// </summary>
    private void FloodFillLake(float[,] heightmap, int[,] waterMap, int startx, int starty)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        float baseLevel = heightmap[startx, starty] + lakeFillDepth;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(new Vector2Int(startx, starty));
        visited.Add(new Vector2Int(startx, starty));

        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            int cx = c.x;
            int cy = c.y;

            if (heightmap[cx, cy] < baseLevel)
            {
                heightmap[cx, cy] = baseLevel;
                waterMap[cx, cy] = 2;

                // 四方向填充
                Vector2Int[] dirs = {
                    new Vector2Int(-1,0), new Vector2Int(1,0),
                    new Vector2Int(0,-1), new Vector2Int(0,1)
                };
                foreach (var d in dirs)
                {
                    int nx = cx + d.x;
                    int ny = cy + d.y;
                    if (nx >= 0 && nx < h && ny >= 0 && ny < w)
                    {
                        Vector2Int nn = new Vector2Int(nx, ny);
                        if (!visited.Contains(nn))
                        {
                            visited.Add(nn);
                            queue.Enqueue(nn);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 简易热侵蚀，减少陡坡
    /// </summary>
    private float[,] ErodeTerrain(float[,] heightmap, int iterations)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    float ch = heightmap[i, j];
                    float lowest = ch;
                    int li = i, lj = j;

                    Vector2Int[] dirs = {
                        new Vector2Int(-1,0), new Vector2Int(1,0),
                        new Vector2Int(0,-1), new Vector2Int(0,1)
                    };
                    foreach (var d in dirs)
                    {
                        int nx = i + d.x;
                        int ny = j + d.y;
                        if (nx >= 0 && nx < h && ny >= 0 && ny < w)
                        {
                            float nh = heightmap[nx, ny];
                            if (nh < lowest)
                            {
                                lowest = nh;
                                li = nx;
                                lj = ny;
                            }
                        }
                    }

                    if (li != i || lj != j)
                    {
                        float diff = ch - lowest;
                        if (diff > talusAngle)
                        {
                            float half = diff * 0.5f;
                            heightmap[i, j] -= half;
                            heightmap[li, lj] += half;
                        }
                    }
                }
            }
        }

        return heightmap;
    }
}

/// <summary>
/// 根据地形平坦度、水源距离等因素计算适宜度，
/// 再挑选若干最高适宜度的坐标作为城市中心。
/// </summary>
public class CityPlanner
{
    public int numCities = 10;

    public float weightFlat = 0.5f;   // 平坦度权重
    public float weightWater = 0.3f;  // 水源邻近权重
    public float weightRandom = 0.2f; // 随机扰动

    public List<Vector2Int> SelectCities(float[,] heightmap, int[,] waterMap)
    {
        int mapSize = heightmap.GetLength(0);

        // 先计算适宜度
        float[,] suitability = ComputeSuitability(heightmap, waterMap);

        // 记录哪些格已被占用
        bool[,] used = new bool[mapSize, mapSize];
        List<Vector2Int> cityPositions = new List<Vector2Int>();

        // 贪心多次选择最高得分点
        for (int c = 0; c < numCities; c++)
        {
            Vector2Int bestPos = Vector2Int.zero;
            float bestVal = -999f;

            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (!used[i, j])
                    {
                        float val = suitability[i, j];
                        if (val > bestVal)
                        {
                            bestVal = val;
                            bestPos = new Vector2Int(i, j);
                        }
                    }
                }
            }

            if (bestVal <= 0f) break;
            cityPositions.Add(bestPos);

            // 屏蔽周围一定半径
            int blockRadius = 10;
            for (int di = -blockRadius; di <= blockRadius; di++)
            {
                for (int dj = -blockRadius; dj <= blockRadius; dj++)
                {
                    int nx = bestPos.x + di;
                    int ny = bestPos.y + dj;
                    if (nx >= 0 && nx < mapSize && ny >= 0 && ny < mapSize)
                    {
                        used[nx, ny] = true;
                    }
                }
            }
        }

        return cityPositions;
    }

    /// <summary>
    /// 根据平坦度、水源距离、随机扰动综合得到适宜度。
    /// </summary>
    private float[,] ComputeSuitability(float[,] heightmap, int[,] waterMap)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        float[,] suit = new float[h, w];

        float[,] waterDist = ComputeWaterDistance(waterMap);

        // 四方向用来估算平坦度
        Vector2Int[] dirs = {
            new Vector2Int(-1,0), new Vector2Int(1,0),
            new Vector2Int(0,-1), new Vector2Int(0,1)
        };

        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                // 忽略海洋（<0）
                if (heightmap[i, j] < 0)
                {
                    suit[i, j] = 0f;
                    continue;
                }
                // 平坦度 (与邻居高度差越小越好)
                float sumNeigh = 0f;
                int count = 0;
                foreach (var d in dirs)
                {
                    int nx = i + d.x;
                    int ny = j + d.y;
                    if (nx >= 0 && nx < h && ny >= 0 && ny < w)
                    {
                        sumNeigh += heightmap[nx, ny];
                        count++;
                    }
                    else
                    {
                        sumNeigh += heightmap[i, j];
                        count++;
                    }
                }
                float avgNeigh = sumNeigh / count;
                float diff = Mathf.Abs(heightmap[i, j] - avgNeigh);
                float flatScore = Mathf.Max(0f, 1f - diff / 10f);

                // 水源邻近度
                float dWater = waterDist[i, j];
                float waterScore = 1f / (1f + dWater); // 离水越近分越高

                // 随机扰动
                float randVal = UnityEngine.Random.value;

                float val = weightFlat * flatScore +
                            weightWater * waterScore +
                            weightRandom * randVal;

                suit[i, j] = val;
            }
        }

        return suit;
    }

    /// <summary>
    /// BFS 计算到最近水源(河/湖)的距离
    /// waterMap: 1=河流, 2=湖海, 0=陆地
    /// </summary>
    private float[,] ComputeWaterDistance(int[,] waterMap)
    {
        int h = waterMap.GetLength(0);
        int w = waterMap.GetLength(1);
        float[,] dist = new float[h, w];

        // 初始化
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                dist[i, j] = 999999f;
            }
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        // 将所有水格作为起点，距离=0
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                if (waterMap[i, j] > 0) // 1或2都是水
                {
                    dist[i, j] = 0f;
                    queue.Enqueue(new Vector2Int(i, j));
                }
            }
        }

        // 扩散
        Vector2Int[] dirs = {
            new Vector2Int(-1,0), new Vector2Int(1,0),
            new Vector2Int(0,-1), new Vector2Int(0,1)
        };

        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            float cDist = dist[c.x, c.y];

            foreach (var d in dirs)
            {
                int nx = c.x + d.x;
                int ny = c.y + d.y;
                if (nx >= 0 && nx < h && ny >= 0 && ny < w)
                {
                    float nd = cDist + 1f;
                    if (nd < dist[nx, ny])
                    {
                        dist[nx, ny] = nd;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }

        return dist;
    }
}

public class RoadPlanner
{
    /// <summary>
    /// 外部调用：输入城市坐标和地形信息，返回每条道路的路径坐标序列
    /// </summary>
    public List<List<Vector2Int>> ConnectCities(float[,] heightmap, int[,] waterMap, List<Vector2Int> cities)
    {
        // 1. MST 或其他方法决定要连通的城市对
        List<(Vector2Int, Vector2Int)> cityPairs = PlanNetwork(cities);

        // 2. 对每对城市执行A*寻路
        List<List<Vector2Int>> roads = new List<List<Vector2Int>>();
        foreach (var pair in cityPairs)
        {
            List<Vector2Int> path = FindPathAStar(heightmap, waterMap, pair.Item1, pair.Item2);
            roads.Add(path);
        }

        return roads;
    }

    /// <summary>
    /// 用 Kruskal/Prim 算法在城市点集上生成 MST，减少不必要的连线
    /// </summary>
    private List<(Vector2Int, Vector2Int)> PlanNetwork(List<Vector2Int> cities)
    {
        List<(Vector2Int, Vector2Int)> pairs = new List<(Vector2Int, Vector2Int)>();
        if (cities.Count < 2) return pairs;

        // 构建所有城市对边
        List<(int, int, float)> edges = new List<(int, int, float)>();
        for (int i = 0; i < cities.Count; i++)
        {
            for (int j = i + 1; j < cities.Count; j++)
            {
                float dx = (cities[i].x - cities[j].x);
                float dy = (cities[i].y - cities[j].y);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                edges.Add((i, j, dist));
            }
        }
        // 按距离排序
        edges.Sort((a, b) => a.Item3.CompareTo(b.Item3));

        // 并查集
        int[] parent = new int[cities.Count];
        for (int i = 0; i < parent.Length; i++) parent[i] = i;

        System.Func<int, int> find = null;
        find = (x) =>
        {
            if (parent[x] != x)
            {
                parent[x] = find(parent[x]);
            }
            return parent[x];
        };
        System.Action<int, int> union = (x, y) =>
        {
            int rx = find(x);
            int ry = find(y);
            if (rx != ry)
            {
                parent[rx] = ry;
            }
        };

        int ecount = 0;
        // Kruskal
        foreach (var edge in edges)
        {
            int a = edge.Item1;
            int b = edge.Item2;
            float d = edge.Item3;

            if (find(a) != find(b))
            {
                union(a, b);
                pairs.Add((cities[a], cities[b]));
                ecount++;
                if (ecount == cities.Count - 1) break;
            }
        }

        return pairs;
    }

    public class PriorityQueue<T>
    {
        private List<T> data;
        private Comparison<T> comparer;

        public PriorityQueue(Comparison<T> comparer)
        {
            this.data = new List<T>();
            this.comparer = comparer;
        }

        public void Enqueue(T item)
        {
            data.Add(item);
            int ci = data.Count - 1;
            while (ci > 0)
            {
                int pi = (ci - 1) / 2;
                if (comparer(data[ci], data[pi]) >= 0) break;
                T tmp = data[ci];
                data[ci] = data[pi];
                data[pi] = tmp;
                ci = pi;
                // Custom PriorityQueue implementation
                PriorityQueue<Node> pq = new PriorityQueue<Node>((x, y) => x.priority.CompareTo(y.priority));
            }
        }


        public T Dequeue()
        {
            int li = data.Count - 1;
            T frontItem = data[0];
            data[0] = data[li];
            data.RemoveAt(li);

            --li;
            int pi = 0;
            while (true)
            {
                int ci = pi * 2 + 1;
                if (ci > li) break;
                int rc = ci + 1;
                if (rc <= li && comparer(data[rc], data[ci]) < 0)
                    ci = rc;
                if (comparer(data[pi], data[ci]) <= 0) break;
                T tmp = data[pi];
                data[pi] = data[ci];
                data[ci] = tmp;
                pi = ci;
            }
            return frontItem;
        }

        public int Count => data.Count;
    }

    /// <summary>
    /// 在高度图上执行 A* 寻路，考虑坡度和水体惩罚
    /// </summary>
    private List<Vector2Int> FindPathAStar(float[,] heightmap, int[,] waterMap,
                                           Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);

        bool[,] visited = new bool[h, w];
        float[,] costSoFar = new float[h, w];
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                costSoFar[i, j] = float.MaxValue;
            }
        }
        costSoFar[start.x, start.y] = 0f;

        Vector2Int[,] cameFrom = new Vector2Int[h, w];
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                cameFrom[i, j] = new Vector2Int(-1, -1);
            }
        }

        PriorityQueue<Node> pq = new PriorityQueue<Node>((x, y) => new NodeComparer().Compare(x, y));
        pq.Enqueue(new Node(start, 0f));

        Vector2Int[] dirs = {
            new Vector2Int(-1,0), new Vector2Int(1,0),
            new Vector2Int(0,-1), new Vector2Int(0,1)
        };

        while (pq.Count > 0)
        {
            Node current = pq.Dequeue();
            Vector2Int cpos = current.pos;

            if (cpos == goal)
            {
                break;
            }
            if (visited[cpos.x, cpos.y]) continue;
            visited[cpos.x, cpos.y] = true;

            foreach (var d in dirs)
            {
                int nx = cpos.x + d.x;
                int ny = cpos.y + d.y;
                if (nx >= 0 && nx < h && ny >= 0 && ny < w)
                {
                    float newCost = costSoFar[cpos.x, cpos.y] + 1f;
                    // 根据高度差增加额外开销 (越陡越贵)
                    float dh = Mathf.Abs(heightmap[nx, ny] - heightmap[cpos.x, cpos.y]);
                    newCost += dh * 5f;

                    // 若水Map==2 (湖海), 惩罚很大，避免穿越
                    if (waterMap[nx, ny] == 2)
                    {
                        newCost += 100f;
                    }
                    // 若水Map==1 (河流), 惩罚中等
                    if (waterMap[nx, ny] == 1)
                    {
                        newCost += 10f;
                    }

                    if (newCost < costSoFar[nx, ny])
                    {
                        costSoFar[nx, ny] = newCost;
                        float priority = newCost + Heuristic(new Vector2Int(nx, ny), goal);
                        pq.Enqueue(new Node(new Vector2Int(nx, ny), priority));
                        cameFrom[nx, ny] = cpos;
                    }
                }
            }
        }

        // 重建路径
        if (cameFrom[goal.x, goal.y].x == -1)
        {
            // 没有找到可行路径
            return path;
        }

        Vector2Int cur = goal;
        while (cur.x != -1 && cur.y != -1 && !(cur == start))
        {
            path.Add(cur);
            cur = cameFrom[cur.x, cur.y];
        }
        path.Add(start);
        path.Reverse();

        return path;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        // 简单 Manhattan
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // === 辅助类 ===
    private class Node
    {
        public Vector2Int pos;
        public float priority;

        public Node(Vector2Int pos, float priority)
        {
            this.pos = pos;
            this.priority = priority;
        }
    }

    private class NodeComparer : IComparer<Node>
    {
        public int Compare(Node x, Node y)
        {
            return x.priority.CompareTo(y.priority);
        }
    }
}

public class WorldGenerationOrManager : MonoBehaviour
{
    public int mapSize = 128;
    public int seed = 42;
    // 其他自定义参数

    // 组件
    private OptimizedTerrainGenerator terrainGen;
    private HydrologyAndErosion hydroErosion;
    private CityPlanner cityPlanner;
    private RoadPlanner roadPlanner;

    private float[,] heightmap;
    private int[,] waterMap;  // 0=陆地,1=河,2=湖海

    public Tilemap terrainTilemap;  // Tilemap引用
    public TileBase baseTile;

    void Start()
    {
        // 1. 初始化各生成器
        terrainGen = new OptimizedTerrainGenerator()
        {
            mapSize = mapSize,
            seed = seed
            // 设置其他参数...
        };
        hydroErosion = new HydrologyAndErosion()
        {
            riverThreshold = 120f,
            lakeFillDepth = 3f
        };
        cityPlanner = new CityPlanner()
        {
            numCities = 10
        };
        roadPlanner = new RoadPlanner();

        // 2. 生成基础地形
        float[,] baseMap = terrainGen.GenerateBaseHeightmapJob();
        float[,] combined = terrainGen.ApplyTerrainNoise(baseMap);

        // 3. 水流和侵蚀
        (float[,] finalHeightmap, int[,] waterMap) = hydroErosion.ApplyHydrology(combined);
 
        // Find min & max
        float minH = float.MaxValue;
        float maxH = float.MinValue;
        for(int i = 0; i < mapSize; i++) {
            for(int j = 0; j < mapSize; j++) {
                float val = finalHeightmap[i, j];
                if(val < minH) minH = val;
                if(val > maxH) maxH = val;
            }
        }
        Debug.Log($"Height range before offset: {minH} ~ {maxH}");
 
        // If minH < 0, shift all up
        if (minH < 0f) {
            float offset = -minH;
            for(int i = 0; i < mapSize; i++) {
                for(int j = 0; j < mapSize; j++) {
                    finalHeightmap[i, j] += offset;
                }
            }
        }
 
        // Recompute range after offset
        minH = float.MaxValue;
        maxH = float.MinValue;
        for(int i = 0; i < mapSize; i++) {
            for(int j = 0; j < mapSize; j++) {
                float val = finalHeightmap[i, j];
                if(val < minH) minH = val;
                if(val > maxH) maxH = val;
            }
        }
        Debug.Log($"Height range after offset: {minH} ~ {maxH}");
 
        // 如果你需要 waterMap 直接可视化，可以让 CarveRivers 等方法单独返回
        // 这里示例把 waterMap 拿出来
        // （根据你的 HydrologyAndErosion 实现，可能要改动一下）
        // waterMap = hydroErosion.GetWaterMap(); // 需要你自己实现获取

        // 4. 城市选址
        List<Vector2Int> cities = cityPlanner.SelectCities(finalHeightmap, waterMap);

        // 5. 道路规划
        List<List<Vector2Int>> roads = roadPlanner.ConnectCities(finalHeightmap, waterMap, cities);

        // 6. 后续可将 finalHeightmap, waterMap, cities, roads 传给可视化或其他逻辑
        // 比如生成对应的 Tilemap 或 Mesh
        RenderToTilemapWithColor(finalHeightmap, waterMap);
    }

    private void RenderToTilemapWithColor(float[,] finalHeightmap, int[,] waterMap)
    {
        // 先清空之前的Tile
        terrainTilemap.ClearAllTiles();

        int size = finalHeightmap.GetLength(0);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                // 1) 设置同一个Tile
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                terrainTilemap.SetTile(cellPos, baseTile);

                // 2) 确保可修改颜色
                terrainTilemap.SetTileFlags(cellPos, TileFlags.None);

                // 3) 计算颜色
                float h = finalHeightmap[x, y];
                int waterType = waterMap[x, y];
                Color c;

                if (waterType == 2)
                {
                    // 湖泊 / 海洋
                    c = Color.blue;
                }
                else if (waterType == 1)
                {
                    // 河流
                    c = new Color(0.2f, 0.4f, 1f);  // 浅蓝色
                }
                else
                {
                    // For a cartoony style, use discrete bands:
                    if (h < 10) {
                        c = Color.green; // low land
                    } else if (h < 20) {
                        c = Color.yellow; // mid land
                    } else {
                        c = Color.gray; // high land
                    }
                }

                // 4) 应用颜色
                terrainTilemap.SetColor(cellPos, c);
            }
        }
    }
}