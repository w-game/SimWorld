using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

#region 数据结构

public enum BlockType
{
    Ocean,
    Plain,
    Mountain,
    City,
    Room,
    Road,
    River,
    Desert,   // 新增示例：沙漠
    Forest    // 新增示例：森林
}

// 小顶堆示例，如无需要可不改动
public class MinHeap<T>
{
    private List<(T item, float priority)> heap = new();

    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add((item, priority));
        HeapifyUp(heap.Count - 1);
    }

    public T Dequeue()
    {
        if (heap.Count == 0) return default;
        T result = heap[0].item;
        heap[0] = heap[^1];
        heap.RemoveAt(heap.Count - 1);
        HeapifyDown(0);
        return result;
    }

    private void HeapifyUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (heap[i].priority >= heap[parent].priority) break;
            (heap[i], heap[parent]) = (heap[parent], heap[i]);
            i = parent;
        }
    }

    private void HeapifyDown(int i)
    {
        int count = heap.Count;
        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int smallest = i;

            if (left < count && heap[left].priority < heap[smallest].priority)
                smallest = left;
            if (right < count && heap[right].priority < heap[smallest].priority)
                smallest = right;

            if (smallest == i) break;
            (heap[i], heap[smallest]) = (heap[smallest], heap[i]);
            i = smallest;
        }
    }
}

// 存储单个 Chunk 数据，包括地形类型、高度图等
public class ChunkData
{
    public Vector2Int Pos { get; }        // 区块在网格中的坐标
    public int Size { get; }
    public Vector2Int WorldPos => Pos * Size;

    public BlockType[,] Blocks { get; }
    public City City { get; set; }        // 当前区块内的城市（如有）
    public float[,] heightMap;
    public float[,] moistureMap;          // 湿度
    public float[,] temperatureMap;       // 温度

    public ChunkData(Vector2Int pos, int size)
    {
        Pos = pos;
        Size = size;
        Blocks = new BlockType[Size, Size];
    }

    internal void MarkHomesInCity()
    {
        if (City == null) return;

        foreach (var house in City.Houses)
        {
            foreach (var block in house.RoomBlocks)
            {
                var blockPos = block - (Pos * Size);
                if (blockPos.x >= 0 && blockPos.x < Size && blockPos.y >= 0 && blockPos.y < Size)
                {
                    Blocks[blockPos.x, blockPos.y] = BlockType.Room;
                }
            }
        }
    }

    public void MarkRoadsInCity()
    {
        if (City == null) return;

        foreach (var road in City.Roads)
        {
            foreach (var point in road)
            {
                var blockPos = point - WorldPos;
                if (blockPos.x >= 0 && blockPos.x < Size && blockPos.y >= 0 && blockPos.y < Size)
                {
                    Blocks[blockPos.x, blockPos.y] = BlockType.Road;
                }
            }
        }
    }
}

#endregion

#region Chunk生成器

/// <summary>
/// 负责生成单个 Chunk 的地形、城市和道路数据
/// </summary>
public class ChunkGenerator
{
    // 全局记录已生成的城市，避免重复
    public static Dictionary<Vector2Int, City> PersistentCities = new Dictionary<Vector2Int, City>();

    private int seed;

    public ChunkGenerator(int seed)
    {
        this.seed = seed;
    }

    public ChunkData GenerateChunk(int chunkX, int chunkY, int chunkSize)
    {
        ChunkData chunk = new ChunkData(new Vector2Int(chunkX, chunkY), chunkSize);

        // 生成基础地形
        GenerateTerrain(chunk);

        // 额外：生成河流
        ApplyRiverMask(chunk);

        // 生成城市
        GenerateCities(chunk);

        return chunk;
    }

    /// <summary>
    /// 生成更自然的地形：
    /// - 多层噪声
    /// - 简单热侵蚀
    /// - 温度 & 湿度图
    /// </summary>
    private void GenerateTerrain(ChunkData chunk)
    {
        float[,] heightMap = GenerateBaseHeightMap(chunk.Pos, chunk.Size);
        ApplyThermalErosion(heightMap, iterations: 20, talus: 0.01f);

        float[,] temperatureMap = GenerateTemperatureMap(chunk.Pos, chunk.Size);
        float[,] moistureMap = GenerateMoistureMap(chunk.Pos, chunk.Size);

        chunk.heightMap = heightMap;
        chunk.temperatureMap = temperatureMap;
        chunk.moistureMap = moistureMap;

        for (int y = 0; y < chunk.Size; y++)
        {
            for (int x = 0; x < chunk.Size; x++)
            {
                float h = heightMap[x, y];
                float t = temperatureMap[x, y];
                float m = moistureMap[x, y];

                if (h < 0.45f)
                {
                    chunk.Blocks[x, y] = BlockType.Ocean;
                }
                else if (h < 0.85f)
                {
                    // 根据湿度区分：Desert / Forest / Plain
                    if (m < 0.3f)
                    {
                        chunk.Blocks[x, y] = BlockType.Desert;
                    }
                    else if (m > 0.6f)
                    {
                        chunk.Blocks[x, y] = BlockType.Forest;
                    }
                    else
                    {
                        chunk.Blocks[x, y] = BlockType.Plain;
                    }
                }
                else
                {
                    chunk.Blocks[x, y] = BlockType.Mountain;
                }
            }
        }
    }

    /// <summary>
    /// 生成基础高度图：多层Perlin噪声 + 大陆掩模
    /// </summary>
    private float[,] GenerateBaseHeightMap(Vector2Int chunkPos, int size)
    {
        float[,] map = new float[size, size];
        int worldOffsetX = chunkPos.x * size;
        int worldOffsetY = chunkPos.y * size;

        int octaves = 5;
        float persistence = 0.5f;
        float lacunarity = 2.0f;
        float frequencyBase = 0.005f; // 低频率 -> 大尺度地形

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int worldX = worldOffsetX + x;
                int worldY = worldOffsetY + y;
                float noiseValue = 0f;
                float amplitude = 1f;
                float frequency = frequencyBase;

                for (int i = 0; i < octaves; i++)
                {
                    noiseValue += Mathf.PerlinNoise((worldX + seed) * frequency, (worldY + seed) * frequency)
                                  * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // 大陆掩模
                float continentMask = Mathf.PerlinNoise((worldX + seed) * 0.0005f, (worldY + seed) * 0.0005f);
                noiseValue *= continentMask;

                map[x, y] = noiseValue;
            }
        }

        return map;
    }

    /// <summary>
    /// 简单热侵蚀：让地形更平滑自然
    /// </summary>
    private void ApplyThermalErosion(float[,] heightMap, int iterations, float talus)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float current = heightMap[x, y];
                    float[] neighbors =
                    {
                        heightMap[x - 1, y],
                        heightMap[x + 1, y],
                        heightMap[x, y - 1],
                        heightMap[x, y + 1]
                    };

                    float maxDiff = 0f;
                    int target = -1;
                    for (int n = 0; n < 4; n++)
                    {
                        float diff = current - neighbors[n];
                        if (diff > maxDiff)
                        {
                            maxDiff = diff;
                            target = n;
                        }
                    }

                    if (maxDiff > talus && target != -1)
                    {
                        float sediment = 0.5f * (maxDiff - talus);
                        heightMap[x, y] -= sediment;
                        switch (target)
                        {
                            case 0:
                                heightMap[x - 1, y] += sediment;
                                break;
                            case 1:
                                heightMap[x + 1, y] += sediment;
                                break;
                            case 2:
                                heightMap[x, y - 1] += sediment;
                                break;
                            case 3:
                                heightMap[x, y + 1] += sediment;
                                break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 生成温度图：演示用。可根据世界坐标模拟纬度等因素
    /// </summary>
    private float[,] GenerateTemperatureMap(Vector2Int chunkPos, int size)
    {
        float[,] tempMap = new float[size, size];
        int worldOffsetY = chunkPos.y * size;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 简易：越靠近 y=5000 越热，且加上少量噪声
                float latitudeFactor = 1f - Mathf.InverseLerp(-5000, 5000, worldOffsetY + y);
                float noiseVal = Mathf.PerlinNoise((x + seed) * 0.01f, (y + seed) * 0.01f);
                float t = latitudeFactor * 0.8f + noiseVal * 0.2f;
                tempMap[x, y] = t; // 0~1
            }
        }
        return tempMap;
    }

    /// <summary>
    /// 生成湿度图：示例
    /// </summary>
    private float[,] GenerateMoistureMap(Vector2Int chunkPos, int size)
    {
        float[,] moistMap = new float[size, size];
        int worldOffsetX = chunkPos.x * size;
        int worldOffsetY = chunkPos.y * size;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noiseVal = Mathf.PerlinNoise((worldOffsetX + x + seed + 9999) * 0.01f,
                                                   (worldOffsetY + y + seed + 9999) * 0.01f);
                moistMap[x, y] = noiseVal;
            }
        }
        return moistMap;
    }

    /// <summary>
    /// 维持原先的噪声掩模方式生成河流；若需更真实可改成坡度流动
    /// </summary>
    private void ApplyRiverMask(ChunkData chunk)
    {
        float riverScale = 0.01f;
        float riverThreshold = 0.12f;
        float maxElevationForRiver = 0.45f;

        for (int y = 0; y < chunk.Size; y++)
        {
            for (int x = 0; x < chunk.Size; x++)
            {
                float h = chunk.heightMap[x, y];
                if (h > maxElevationForRiver) continue; // 山地过高不生成河流

                var worldPos = chunk.WorldPos + new Vector2Int(x, y);
                float riverNoise = Mathf.PerlinNoise((worldPos.x + seed + 10000) * riverScale,
                                                     (worldPos.y + seed + 10000) * riverScale);
                if (riverNoise < riverThreshold)
                {
                    if (chunk.Blocks[x, y] != BlockType.Ocean)
                    {
                        chunk.Blocks[x, y] = BlockType.River;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 在适宜地形中生成城市，若已有城市则跳过
    /// </summary>
    private void GenerateCities(ChunkData chunk)
    {
        int suitableCount = 0;
        for (int y = 0; y < chunk.Size; y++)
        {
            for (int x = 0; x < chunk.Size; x++)
            {
                BlockType bt = chunk.Blocks[x, y];
                if (bt == BlockType.Plain || bt == BlockType.Desert || bt == BlockType.Forest)
                {
                    suitableCount++;
                }
            }
        }

        // 要求至少 80% 区域可用，否则不生成城市
        if (suitableCount < chunk.Size * chunk.Size * 0.8f)
            return;

        Vector2Int candidateCenter = chunk.WorldPos + new Vector2Int(chunk.Size / 2, chunk.Size / 2);
        float cityNoise = Mathf.PerlinNoise(candidateCenter.x * 0.001f, candidateCenter.y * 0.001f);
        // 随机决定要不要生成
        if (cityNoise >= 0.5f) return;

        // 若已有城市太近就复用
        foreach (var kvp in PersistentCities)
        {
            if (Vector2Int.Distance(kvp.Key, candidateCenter) < 100)
            {
                chunk.City = kvp.Value;
                chunk.MarkHomesInCity();
                chunk.MarkRoadsInCity();
                return;
            }
        }

        // 生成新城市
        City city = new City(candidateCenter, chunk);
        chunk.City = city;
        PersistentCities[candidateCenter] = city;

        int citySeed = candidateCenter.x + candidateCenter.y * 10000 + seed;
        Random.InitState(citySeed);

        // 主干道 -> 房屋 -> 道路网络
        city.GenerateTrunkRoad();
        city.GenerateHouses();
        chunk.MarkHomesInCity();
        city.GenerateCityRoadNetwork();
        chunk.MarkRoadsInCity();
    }
}

#endregion

#region Chunk 实例管理

public class ChunkInstance
{
    public ChunkData data;
    public GameObject visual;
}

#endregion

#region 无限地图管理器

/// <summary>
/// 根据玩家位置加载/卸载区块，可视化并管理生成过程
/// </summary>
public class InfiniteWorldManager : MonoSingleton<InfiniteWorldManager>
{
    [Header("区块设置")]
    public int viewRange = 3;      // 视野范围（以区块为单位）

    [Header("引用设置")]
    public Transform player;       // 玩家 Transform
    public TileBase tileBase;      // 用于 Tilemap 显示

    private Dictionary<Vector2Int, ChunkInstance> loadedChunks = new Dictionary<Vector2Int, ChunkInstance>();
    private ChunkGenerator generator;

    public const int ChunkSize = 64;

    private void Start()
    {
        // 可自行修改种子
        generator = new ChunkGenerator(seed: 12345);
    }

    private void Update()
    {
        // 计算玩家当前在哪个区块
        int playerChunkX = Mathf.FloorToInt(player.position.x / ChunkSize);
        int playerChunkY = Mathf.FloorToInt(player.position.y / ChunkSize);

        // 加载视野范围内区块
        for (int cx = playerChunkX - viewRange; cx <= playerChunkX + viewRange; cx++)
        {
            for (int cy = playerChunkY - viewRange; cy <= playerChunkY + viewRange; cy++)
            {
                Vector2Int coord = new Vector2Int(cx, cy);
                if (!loadedChunks.ContainsKey(coord))
                {
                    ChunkData data = generator.GenerateChunk(cx, cy, ChunkSize);
                    GameObject visual = BuildChunkVisual(data);
                    loadedChunks.Add(coord, new ChunkInstance { data = data, visual = visual });
                }
            }
        }

        // 卸载超出视野范围的区块
        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var kvp in loadedChunks)
        {
            Vector2Int coord = kvp.Key;
            if (Mathf.Abs(coord.x - playerChunkX) > viewRange ||
                Mathf.Abs(coord.y - playerChunkY) > viewRange)
            {
                Destroy(kvp.Value.visual);
                toRemove.Add(coord);
            }
        }

        foreach (var coord in toRemove)
        {
            loadedChunks.Remove(coord);
        }
    }

    private GameObject BuildChunkVisual(ChunkData chunkData)
    {
        var pos = new Vector3(chunkData.Pos.x * ChunkSize, chunkData.Pos.y * ChunkSize, 0);

        // 创建 Tilemap
        GameObject tilemapGO = new GameObject($"Tilemap_{chunkData.Pos.x}_{chunkData.Pos.y}");
        tilemapGO.transform.parent = transform;
        tilemapGO.transform.localPosition = pos;
        Tilemap tilemap = tilemapGO.AddComponent<Tilemap>();
        tilemapGO.AddComponent<TilemapRenderer>();

        for (int y = 0; y < chunkData.Size; y++)
        {
            for (int x = 0; x < chunkData.Size; x++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                tilemap.SetTile(tilePos, tileBase);

                BlockType bt = chunkData.Blocks[x, y];
                float h = chunkData.heightMap[x, y];

                Color col = Color.white;
                switch (bt)
                {
                    case BlockType.Ocean:
                        // 简单区分深海/浅海
                        if (h < 0.4f)
                            col = new Color(0f, 0f, 0.5f);
                        else
                            col = new Color(0.2f, 0.8f, 1f);
                        break;
                    case BlockType.Plain:
                        col = Color.green;
                        break;
                    case BlockType.Mountain:
                        col = Color.gray;
                        break;
                    case BlockType.Room:
                        col = new Color(0.8f, 0.5f, 0.2f);
                        break;
                    case BlockType.Road:
                        col = Color.yellow;
                        break;
                    case BlockType.River:
                        col = new Color(0f, 0.8f, 0.8f);
                        break;
                    case BlockType.Desert:
                        col = new Color(0.9f, 0.8f, 0.5f);
                        break;
                    case BlockType.Forest:
                        col = new Color(0.2f, 0.6f, 0.2f);
                        break;
                }

                tilemap.SetColor(tilePos, col);
            }
        }

        return tilemapGO;
    }

    public ChunkData GetChunkByWorldPos(Vector3 pos, bool allowGenerate = true)
    {
        int chunkX = Mathf.FloorToInt(pos.x / ChunkSize);
        int chunkY = Mathf.FloorToInt(pos.y / ChunkSize);
        Vector2Int coord = new Vector2Int(chunkX, chunkY);

        if (loadedChunks.TryGetValue(coord, out ChunkInstance instance))
        {
            return instance.data;
        }
        else
        {
            if (!allowGenerate) return null;

            // 按需生成新的区块
            ChunkData data = generator.GenerateChunk(chunkX, chunkY, ChunkSize);
            GameObject visual = BuildChunkVisual(data);
            ChunkInstance newInstance = new ChunkInstance { data = data, visual = visual };
            loadedChunks.Add(coord, newInstance);
            return data;
        }
    }
}

#endregion