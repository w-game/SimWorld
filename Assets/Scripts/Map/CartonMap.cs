using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Map
{
    public enum BlackType
    {
        None,
        Plain,
        Ocean,
        Room,
        Road
    }

    public class House
    {
        public List<Vector2Int> Blocks { get; private set; } // 房屋的所有块
        public Vector2Int Size { get; private set; } // 房屋大小，影响房屋半径
        public City City { get; private set; } // 房屋所在的城市
        public bool IsAvailable { get; set; } // 房屋是否可用

        public House(List<Vector2Int> blocks, Vector2Int size, City city)
        {
            Blocks = blocks; // 转换为全局坐标
            Size = size;
            City = city;
        }

        public void MergeWithBlocks(List<Vector2Int> newBlocks)
        {
            foreach (var block in newBlocks)
            {
                if (!Blocks.Contains(block))
                {
                    Blocks.Add(block);
                }
            }

            // Recalculate Size as the bounding box of all blocks
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var pos in Blocks)
            {
                if (pos.x < minX) minX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y > maxY) maxY = pos.y;
            }

            Size = new Vector2Int(maxX - minX + 1, maxY - minY + 1);
        }
    }

    public class City
    {
        public Vector2Int GlobalPos { get; private set; } // 城市在世界中的绝对位置
        public int Size { get; private set; } // 城市大小，影响城市半径
        public Chunk OriginChunk { get; private set; } // 城市起源的Chunk

        public List<List<Vector2Int>> Roads { get; } = new List<List<Vector2Int>>(); // 存储城市的道路
        public List<House> Houses { get; } = new List<House>(); // 存储城市的房屋

        private bool AreAdjacent(List<Vector2Int> blocks1, List<Vector2Int> blocks2)
        {
            foreach (var b1 in blocks1)
            {
                foreach (var b2 in blocks2)
                {
                    if (Mathf.Abs(b1.x - b2.x) <= 1 && Mathf.Abs(b1.y - b2.y) <= 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private System.Random _cityRand;

        public City(Vector2Int pos, int size, Chunk originChunk)
        {
            GlobalPos = pos + originChunk.WorldPos; // 转换为全局坐标
            Size = size;
            OriginChunk = originChunk;

            _cityRand = new System.Random(originChunk.Map.seed + originChunk.Pos.x * 500 + originChunk.Pos.y +
                                          Size * 100);

            Debug.Log($"City {GlobalPos} Size {Size} OriginChunk {OriginChunk.Pos}");
            // 创建城市
            CreateCity();
        }

        private void CreateCity()
        {
            // 创建跨Chunk的道路
            CreateRoad();

            // 在所有受影响的Chunk中放置建筑
            PutRoom();
        }

        private void CreateRoad()
        {
            List<Vector2Int> horizonalMainRoad = new List<Vector2Int>();
            List<Vector2Int> verticalMainRoad = new List<Vector2Int>();

            // 横向主干道
            for (int x = 0; x < Size; x++)
            {
                // var roadPosUp = new Vector2Int(OriginChunk.WorldPos.x + x, GlobalPos.y - 1);
                var roadPos = new Vector2Int(OriginChunk.WorldPos.x + x, GlobalPos.y);
                // var roadPosDown = new Vector2Int(OriginChunk.WorldPos.x + x, GlobalPos.y + 1);
                horizonalMainRoad.Add(roadPos);
                // horizonalMainRoad.Add(roadPosUp);
                // horizonalMainRoad.Add(roadPosDown);
            }

            // 纵向主干道
            for (int y = 0; y < Size; y++)
            {
                // var roadPosLeft = new Vector2Int(GlobalPos.x - 1, OriginChunk.WorldPos.y + y);
                var roadPos = new Vector2Int(GlobalPos.x, OriginChunk.WorldPos.y + y);
                // var roadPosRight = new Vector2Int(GlobalPos.x + 1, OriginChunk.WorldPos.y + y);
                verticalMainRoad.Add(roadPos);
                // verticalMainRoad.Add(roadPosRight);
                // verticalMainRoad.Add(roadPosLeft);
            }

            Roads.Add(horizonalMainRoad);
            Roads.Add(verticalMainRoad);

            // 每个方向尝试添加1-2条次要道路
            int roadCount = _cityRand.Next(1, 3);

            // 记录已使用的偏移量，防止道路重叠
            HashSet<int> usedHorizontalOffsets = new HashSet<int>();
            HashSet<int> usedVerticalOffsets = new HashSet<int>();

            var localPos = GlobalPos - OriginChunk.WorldPos;

            for (int i = 0; i < roadCount; i++)
            {
                // 横向次要道路 - 确保不重叠
                int offset;
                do
                {
                    offset = _cityRand.Next(0, Size);
                } while (usedHorizontalOffsets.Contains(offset) ||
                         Mathf.Abs(GlobalPos.y - OriginChunk.WorldPos.y - offset) < 7); // 避免与主干道重叠

                usedHorizontalOffsets.Add(offset);


                int roadY = OriginChunk.WorldPos.y + offset;
                int minX, maxX;
                do
                {
                    minX = _cityRand.Next(0, localPos.x);
                    maxX = _cityRand.Next(localPos.x, Size - 1);
                } while (maxX - minX < 8); // 确保道路长度大于3


                List<Vector2Int> road = new List<Vector2Int>();
                for (int x = minX; x < maxX; x++)
                {
                    road.Add(new Vector2Int(x + OriginChunk.WorldPos.x, roadY));
                    // road.Add(new Vector2Int(x + OriginChunk.WorldPos.x, roadY + 1));
                }

                Roads.Add(road);
            }

            roadCount = _cityRand.Next(1, 3);
            for (int i = 0; i < roadCount; i++)
            {
                // 纵向次要道路 - 确保不重叠
                int offset;
                do
                {
                    offset = _cityRand.Next(0, Size);
                } while (usedVerticalOffsets.Contains(offset) ||
                         Mathf.Abs(GlobalPos.x - OriginChunk.WorldPos.x - offset) < 7); // 避免与主干道重叠

                usedVerticalOffsets.Add(offset);

                int roadX = OriginChunk.WorldPos.x + offset;
                int minY, maxY;
                do
                {
                    minY = _cityRand.Next(0, localPos.y);
                    maxY = _cityRand.Next(localPos.y, Size - 1);
                } while (maxY - minY < 8); // 确保道路长度大于3


                List<Vector2Int> road = new List<Vector2Int>();
                for (int y = minY; y < maxY; y++)
                {
                    road.Add(new Vector2Int(roadX, y + OriginChunk.WorldPos.y));
                    // road.Add(new Vector2Int(roadX + 1, y + OriginChunk.WorldPos.y));
                }

                Roads.Add(road);
            }

            Debug.Log($"City {GlobalPos} has {Roads.Count} roads");
        }

        private void PutRoom()
        {
            foreach (var road in Roads)
            {
                // Randomize road points for more varied placement
                List<Vector2Int> roadPoints = new List<Vector2Int>(road);
                roadPoints = roadPoints.OrderBy(x => _cityRand.Next()).ToList();

                // Try placing a limited number of houses per road to avoid clutter
                int housesPlaced = 0;
                int maxHousesPerRoad = 20;

                foreach (var roadPoint in roadPoints)
                {
                    // Try all four directions from the current road point
                    for (int dir = 0; dir < 4; dir++)
                    {
                        // If we've placed enough houses on this road, stop
                        if (housesPlaced >= maxHousesPerRoad)
                            break;

                        var house = PlaceBuilding(roadPoint, dir);
                        if (house != null)
                        {
                            Houses.Add(house);
                            housesPlaced++;
                        }
                    }

                    // If we've placed enough houses on this road, move to the next
                    if (housesPlaced >= maxHousesPerRoad)
                        break;
                }
            }
        }

        private House PlaceBuilding(Vector2Int roadPoint, int direction = -1)
        {
            if (direction == -1)
            {
                direction = _cityRand.Next(0, 4);
            }

            // 随机选择建筑大小
            // var roomSize = _cityRand.Next(1, 4);
            // 随机选择建筑大小
            var roomSize = _cityRand.Next(1, 4);
            int buildingWidth, buildingHeight;
            if (roomSize == 1)
            {
                buildingWidth = _cityRand.Next(3, 5);
                buildingHeight = _cityRand.Next(3, 5);
            }
            else if (roomSize == 2)
            {
                buildingWidth = _cityRand.Next(6, 9);
                buildingHeight = _cityRand.Next(6, 9);
            }
            else
            {
                buildingWidth = _cityRand.Next(10, 15);
                buildingHeight = _cityRand.Next(10, 15);
            }

            // 随机选择建筑方向

            var buildingBlocks = new List<Vector2Int>();
            Vector2Int minPos = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int maxPos = new Vector2Int(int.MinValue, int.MinValue);

            // 先合并计算所有块的坐标，并同步更新边界
            for (int i = 1; i <= buildingWidth; i++)
            {
                for (int j = 1; j <= buildingHeight; j++)
                {
                    Vector2Int pos = roadPoint;
                    switch (direction)
                    {
                        case 0: // 上
                            pos += new Vector2Int(i, j);
                            break;
                        case 1: // 下
                            pos += new Vector2Int(-i, -j);
                            break;
                        case 2: // 左
                            pos += new Vector2Int(-j, i);
                            break;
                        case 3: // 右
                            pos += new Vector2Int(j, -i);
                            break;
                    }

                    minPos = new Vector2Int(Mathf.Min(minPos.x, pos.x), Mathf.Min(minPos.y, pos.y));
                    maxPos = new Vector2Int(Mathf.Max(maxPos.x, pos.x), Mathf.Max(maxPos.y, pos.y));

                    buildingBlocks.Add(pos);
                }
            }

            // 边界检查
            var minLocalPos = minPos - OriginChunk.WorldPos;
            var maxLocalPos = maxPos - OriginChunk.WorldPos;
            if (minLocalPos.x < 0 || maxLocalPos.x >= OriginChunk.Size ||
                minLocalPos.y < 0 || maxLocalPos.y >= OriginChunk.Size)
            {
                return null;
            }

            // 与其他建筑重叠或与道路重叠的检查统一处理
            foreach (var blockPos in buildingBlocks)
            {
                // 检查是否与其他建筑重叠
                foreach (var house in Houses)
                {
                    if (house.Blocks.Contains(blockPos) ||
                        house.Blocks.Contains(blockPos + new Vector2Int(1, 0)) ||
                        house.Blocks.Contains(blockPos + new Vector2Int(-1, 0)) ||
                        house.Blocks.Contains(blockPos + new Vector2Int(0, 1)) ||
                        house.Blocks.Contains(blockPos + new Vector2Int(0, -1)))
                    {
                        return null;
                    }
                }

                // 检查是否与道路重叠
                foreach (var road in Roads)
                {
                    if (road.Contains(blockPos))
                    {
                        return null;
                    }
                }
            }

            return new House(buildingBlocks, new Vector2Int(buildingWidth, buildingHeight), this);
        }
    }

    public class Chunk
    {
        public const int CityLayer = 3;
        public CartonMap Map { get; private set; }

        public Vector2Int Pos { get; private set; }
        public Vector2Int WorldPos => new Vector2Int(Pos.x * Size, Pos.y * Size);
        public Vector2Int CenterPos { get; private set; }
        public BlackType[,] Blocks { get; private set; }
        public City City { get; private set; }
        public int Size { get; private set; }
        public int Layer { get; private set; }
        public Chunk Left => Map.GetChunk(new Vector2Int(Pos.x - 1, Pos.y), Layer);
        public Chunk Right => Map.GetChunk(new Vector2Int(Pos.x + 1, Pos.y), Layer);
        public Chunk Up => Map.GetChunk(new Vector2Int(Pos.x, Pos.y + 1), Layer);
        public Chunk Down => Map.GetChunk(new Vector2Int(Pos.x, Pos.y - 1), Layer);

        public BlackType Type { get; private set; }

        public Chunk(Vector2Int pos, int layer, CartonMap map)
        {
            Pos = pos;
            Layer = layer;
            Map = map;
            Size = (int)Mathf.Pow(2, layer) * CartonMap.NORMAL_CHUNK_SIZE;
            Blocks = new BlackType[Size, Size];

            var centerRandom = new System.Random(Map.seed + layer * 1000 + pos.x * 100 + pos.y);
            CenterPos = new Vector2Int(centerRandom.Next(0, Size), centerRandom.Next(0, Size));
        }

        private Chunk GetNearestChunk(Vector2Int worldPos, bool self, int layer)
        {
            var neighbors = new List<Vector2Int>()
                {
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, -1),
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 1),
                    new Vector2Int(1, -1),
                    new Vector2Int(-1, 1),
                    new Vector2Int(-1, -1)
                };

            if (self)
            {
                neighbors.Add(new Vector2Int(0, 0));
            }

            Chunk closestChunk = null;
            float closestDistance = float.MaxValue;

            var floatSize = (float)((int)Mathf.Pow(2, layer) * CartonMap.NORMAL_CHUNK_SIZE);
            var chunkPos = new Vector2Int(
                Mathf.FloorToInt(worldPos.x / floatSize),
                Mathf.FloorToInt(worldPos.y / floatSize)
            );

            foreach (var neighbor in neighbors)
            {
                var chunk = Map.GetChunk(chunkPos + neighbor, layer);
                if (chunk != null)
                {
                    float distance = Vector2Int.Distance(worldPos, chunk.CenterPos + chunk.WorldPos);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestChunk = chunk;
                    }
                }
            }

            return closestChunk;
        }

        public void CalcBlocks()
        {
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    var pos = new Vector2Int(i, j);
                    var blockWorldPos = pos + WorldPos;

                    var nearestChunk = GetNearestChunk(blockWorldPos, true, Layer);

                    Blocks[i, j] = nearestChunk != null ? nearestChunk.Type : BlackType.Plain;
                }
            }

            var size = (int)Mathf.Pow(2, CityLayer) * CartonMap.NORMAL_CHUNK_SIZE;
            var floatSize = (float)size;
            var cityLayerPos = new Vector2Int(
                Mathf.FloorToInt(WorldPos.x / floatSize),
                Mathf.FloorToInt(WorldPos.y / floatSize)
            );
            var chunk = Map.GetChunk(cityLayerPos, CityLayer);
            if (chunk != null && chunk.City != null)
            {
                Debug.Log($"Chunk {Pos} Layer {Layer} has city {chunk.City.GlobalPos}");
                foreach (var road in chunk.City.Roads)
                {
                    foreach (var roadPos in road)
                    {
                        // 检查道路点是否在当前Chunk内
                        Vector2Int localPos = roadPos - WorldPos;
                        if (localPos.x >= 0 && localPos.x < Size && localPos.y >= 0 && localPos.y < Size)
                        {
                            if (Blocks[localPos.x, localPos.y] == BlackType.Ocean)
                                continue; // 如果是海洋，则不设置
                            Blocks[localPos.x, localPos.y] = BlackType.Road;
                            Debug.Log($"Set road at {roadPos}");
                        }
                    }
                }
            }

            // 检查房屋
            if (chunk != null && chunk.City != null)
            {
                foreach (var house in chunk.City.Houses)
                {
                    bool isAvilable = true;
                    foreach (var housePos in house.Blocks)
                    {
                        // 检查房屋点是否在当前Chunk内
                        Vector2Int localPos = housePos - WorldPos;
                        if (localPos.x >= 0 && localPos.x < Size && localPos.y >= 0 && localPos.y < Size)
                        {
                            if (Blocks[localPos.x, localPos.y] == BlackType.Ocean)
                            {
                                isAvilable = false; // 如果是海洋，则不设置
                                break;
                            }
                        }
                        else
                        {
                            isAvilable = false; // 如果超出范围，则不设置
                            break;
                        }
                    }

                    if (isAvilable)
                    {
                        foreach (var housePos in house.Blocks)
                        {
                            Vector2Int localPos = housePos - WorldPos;
                            Blocks[localPos.x, localPos.y] = BlackType.Room;
                            Debug.Log($"Set room at {housePos}");
                        }
                    }
                    else
                    {
                        house.IsAvailable = false; // 房屋不可用
                    }
                }
            }
        }


        public void CalcChunk()
        {
            if (Layer == CartonMap.LAYER_NUM - 1)
            {
                System.Random rand = new System.Random(Map.seed + Layer * 1000 + Pos.x * 100 + Pos.y);
                var pro = rand.Next(0, 100);

                if (pro < 50)
                {
                    Type = BlackType.Ocean;
                }
                else
                {
                    Type = BlackType.Plain;
                }

                return;
            }

            var worldCenterPos = CenterPos + WorldPos;

            var nearestChunk = GetNearestChunk(worldCenterPos, true, Layer + 1);

            if (nearestChunk != null)
            {
                Type = nearestChunk.Type;
            }
            else
            {
                Type = BlackType.Plain;
            }

            if (Layer == CityLayer)
            {
                CheckCreateCity();
            }
        }

        public void CheckCreateCity()
        {
            Debug.Log($"CheckCreateCity {Pos} Layer {Layer} Type {Type}");
            if (Type == BlackType.Plain)
            {
                System.Random rand = new System.Random(Map.seed + Layer * 1000 + Pos.x * 100 + Pos.y);

                if (rand.Next(0, 100) < 20)
                {
                    City = new City(CenterPos, Size, this);
                    Debug.Log($"Create City at {CenterPos} in Chunk {Pos} Layer {Layer}");
                }
            }
        }
    }

    public class CartonMap : MonoBehaviour
    {
        public const int LAYER_NUM = 5;
        public const int NORMAL_CHUNK_SIZE = 8;
        public int seed = 12412;
        public Tilemap tilemap;
        public TileBase tile;

        public Dictionary<int, Dictionary<Vector2Int, Chunk>> chunks =
            new Dictionary<int, Dictionary<Vector2Int, Chunk>>();

        public Transform player;

        private Dictionary<Vector2Int, Chunk> _chunkActive = new Dictionary<Vector2Int, Chunk>();

        private void Awake()
        {
            for (int i = 0; i < LAYER_NUM; i++)
            {
                chunks.Add(i, new Dictionary<Vector2Int, Chunk>());
            }
        }

        private System.Random _managerRand;

        private void Start()
        {
            _managerRand = new System.Random(seed);
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    var chunk = GetChunk(new Vector2Int(i, j), 0);
                    chunk.CalcBlocks();
                    VisualChunk(chunk);
                    _chunkActive.Add(new Vector2Int(i, j), chunk);
                }
            }

            var city = FindNearestCity(new Vector2Int(0, 0));
            player.transform.position = new Vector3(city.GlobalPos.x, city.GlobalPos.y, 0);
        }

        private City FindNearestCity(Vector2Int pos)
        {
            var neighbor = new List<Vector2Int>()
                {
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, -1),
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 1),
                    new Vector2Int(1, -1),
                    new Vector2Int(-1, 1),
                    new Vector2Int(-1, -1)
                };

            var chunk = GetChunk(pos, Chunk.CityLayer);
            if (chunk.City != null)
            {
                return chunk.City;
            }

            var index = _managerRand.Next(0, neighbor.Count);

            return FindNearestCity(pos + neighbor[index]);
        }

        void Update()
        {
            var playerChunkPos = new Vector2Int((int)player.position.x / NORMAL_CHUNK_SIZE,
                (int)player.position.y / NORMAL_CHUNK_SIZE);
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    var pos = playerChunkPos + new Vector2Int(x - 3, y - 3);
                    if (_chunkActive.ContainsKey(pos))
                    {
                        continue;
                    }

                    var chunk = GetChunk(pos, 0);
                    if (chunk != null)
                    {
                        chunk.CalcBlocks();
                        VisualChunk(chunk);
                        if (!_chunkActive.ContainsKey(pos))
                        {
                            _chunkActive.Add(pos, chunk);
                        }
                    }
                }
            }

            foreach (var chunk in new Dictionary<Vector2Int, Chunk>(_chunkActive))
            {
                if (chunk.Value != null)
                {
                    if (Vector2Int.Distance(chunk.Value.Pos, playerChunkPos) > 8)
                    {
                        _chunkActive.Remove(chunk.Key);
                        UnVisualChunk(chunk.Value);
                    }
                }
            }
        }

        public Chunk GetChunk(Vector2Int pos, int layer)
        {
            if (layer < 0 || layer >= LAYER_NUM)
            {
                return null;
            }

            if (chunks.ContainsKey(layer))
            {
                if (chunks[layer].ContainsKey(pos))
                {
                    return chunks[layer][pos];
                }
                else
                {
                    return CreateChunk(pos, layer);
                }
            }

            return null;
        }

        public Chunk CreateChunk(Vector2Int pos, int layer)
        {
            Chunk chunk = new Chunk(pos, layer, this);
            chunk.CalcChunk();

            chunks[layer].Add(pos, chunk);

            return chunk;
        }

        private void VisualChunk(Chunk chunk)
        {
            for (int i = 0; i < chunk.Size; i++)
            {
                for (int j = 0; j < chunk.Size; j++)
                {
                    var pos = new Vector2Int(i, j);
                    var blockWorldPos = pos + chunk.WorldPos;
                    tilemap.SetTile(new Vector3Int(blockWorldPos.x, blockWorldPos.y, 0), tile);

                    Color plainColor = new Color(0.6f, 1f, 0.6f);
                    Color oceanColor = new Color(0.4f, 0.6f, 1f);
                    Color roomColor = new Color(1f, 0.6f, 0.6f);
                    Color roadColor = new Color(1f, 1f, 0.5f);
                    Color defaultColor = new Color(0.8f, 0.8f, 0.8f);

                    switch (chunk.Blocks[i, j])
                    {
                        case BlackType.Plain:
                            tilemap.SetColor(new Vector3Int(blockWorldPos.x, blockWorldPos.y, 0), plainColor);
                            break;
                        case BlackType.Ocean:
                            tilemap.SetColor(new Vector3Int(blockWorldPos.x, blockWorldPos.y, 0), oceanColor);
                            break;
                        case BlackType.Room:
                            tilemap.SetColor(new Vector3Int(blockWorldPos.x, blockWorldPos.y, 0), roomColor);
                            break;
                        case BlackType.Road:
                            tilemap.SetColor(new Vector3Int(blockWorldPos.x, blockWorldPos.y, 0), roadColor);
                            Debug.Log($"Set road at {blockWorldPos}");
                            break;
                        default:
                            tilemap.SetColor(new Vector3Int(blockWorldPos.x, blockWorldPos.y, 0), defaultColor);
                            break;
                    }
                }
            }
        }

        private void UnVisualChunk(Chunk chunk)
        {
            for (int i = 0; i < chunk.Size; i++)
            {
                for (int j = 0; j < chunk.Size; j++)
                {
                    var pos = new Vector2Int(i, j);
                    var blockWorldPos = pos + chunk.WorldPos;
                    tilemap.SetTile(new Vector3Int(blockWorldPos.x, blockWorldPos.y, 0), null);
                }
            }
        }
    }
}