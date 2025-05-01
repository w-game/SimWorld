using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Map
{

    public class City
    {
        public Vector2Int GlobalPos { get; private set; } // 城市在世界中的绝对位置
        public int Size { get; private set; } // 城市大小，影响城市半径
        public Chunk OriginChunk { get; private set; } // 城市起源的Chunk

        public List<List<Vector2Int>> Roads { get; } = new List<List<Vector2Int>>(); // 存储城市的道路
        public List<IHouse> Houses { get; } = new List<IHouse>(); // 存储城市的房屋

        public System.Random ChunkRand { get; private set; } // 随机数生成器

        // Minimum empty tiles required between houses
        private const int HOUSE_MARGIN = 1;

        // Cached room configs and occupancy grids
        private List<RoomConfig> _roomConfigs;

        public CityPrice CityPrice { get; private set; }
        public int Population { get; private set; } = 0;
        public event UnityAction<int> OnPopulationChanged;

        public City(Vector2Int pos, int size, Chunk originChunk, System.Random chunkRand)
        {
            GlobalPos = pos + originChunk.WorldPos; // 转换为全局坐标
            Size = size;
            OriginChunk = originChunk;

            ChunkRand = chunkRand;

            Debug.Log($"City {GlobalPos} Size {Size} OriginChunk {OriginChunk.Pos}");
            _roomConfigs = ConfigReader.GetAllConfigs<RoomConfig>();

            // 创建城市
            CreateCity();

            CityPrice = GameManager.I.PriceSystem.AddCity(this);
        }

        private void CreateCity()
        {
            // 创建跨Chunk的道路
            CreateRoad();

            // 在所有受影响的Chunk中放置建筑
            PutRoom();
        }

        public void ChangePopulation(int amount)
        {
            Population += amount;
            OnPopulationChanged?.Invoke(Population);
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
            int roadCount = ChunkRand.Next(4, 8);

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
                    offset = ChunkRand.Next(0, Size);
                } while (usedHorizontalOffsets.Contains(offset) ||
                         Mathf.Abs(GlobalPos.y - OriginChunk.WorldPos.y - offset) < 7); // 避免与主干道重叠

                usedHorizontalOffsets.Add(offset);


                int roadY = OriginChunk.WorldPos.y + offset;
                int minX, maxX;
                do
                {
                    minX = ChunkRand.Next(0, localPos.x);
                    maxX = ChunkRand.Next(localPos.x, Size - 1);
                } while (maxX - minX < 8); // 确保道路长度大于3


                List<Vector2Int> road = new List<Vector2Int>();
                for (int x = minX; x < maxX; x++)
                {
                    road.Add(new Vector2Int(x + OriginChunk.WorldPos.x, roadY));
                    // road.Add(new Vector2Int(x + OriginChunk.WorldPos.x, roadY + 1));
                }

                Roads.Add(road);
            }

            roadCount = ChunkRand.Next(1, 3);
            for (int i = 0; i < roadCount; i++)
            {
                // 纵向次要道路 - 确保不重叠
                int offset;
                do
                {
                    offset = ChunkRand.Next(0, Size);
                } while (usedVerticalOffsets.Contains(offset) ||
                         Mathf.Abs(GlobalPos.x - OriginChunk.WorldPos.x - offset) < 7); // 避免与主干道重叠

                usedVerticalOffsets.Add(offset);

                int roadX = OriginChunk.WorldPos.x + offset;
                int minY, maxY;
                do
                {
                    minY = ChunkRand.Next(0, localPos.y);
                    maxY = ChunkRand.Next(localPos.y, Size - 1);
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
            bool[,] blockOccupancyMap = new bool[Size, Size];
            bool[,] roadMap = new bool[Size, Size];

            foreach (var road in Roads)
            {
                foreach (var worldPos in road)
                {
                    Vector2Int local = worldPos - OriginChunk.WorldPos;
                    if (local.x >= 0 && local.x < Size && local.y >= 0 && local.y < Size)
                        roadMap[local.x, local.y] = true;
                }
            }

            foreach (var road in Roads)
            {
                var roadPoints = new List<Vector2Int>(road);
                Shuffle(roadPoints);
                int housesPlaced = 0;
                const int maxHousesPerRoad = 20;

                foreach (var roadPoint in roadPoints)
                {
                    if (housesPlaced >= maxHousesPerRoad)
                        break;

                    for (int dir = 0; dir < 4; dir++)
                    {
                        var house = PlaceBuilding(roadPoint, blockOccupancyMap, roadMap);
                        if (house != null)
                        {
                            Houses.Add(house);
                            // Mark blocks in occupancy map
                            foreach (var b in house.Blocks)
                            {
                                var local = b - OriginChunk.WorldPos;
                                if (local.x >= 0 && local.x < Size && local.y >= 0 && local.y < Size)
                                    blockOccupancyMap[local.x, local.y] = true;
                            }
                            housesPlaced++;
                            if (housesPlaced >= maxHousesPerRoad)
                                break;
                        }
                    }
                }
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = ChunkRand.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private IHouse PlaceBuilding(Vector2Int roadPoint, bool[,] blockOccupancyMap, bool[,] roadMap)
        {
            // Randomly pick a room config
            var roomConfig = _roomConfigs[ChunkRand.Next(_roomConfigs.Count)];
            int w = roomConfig.width, h = roomConfig.height;
            // Compute candidate block positions
            List<Vector2Int> buildingBlocks = new List<Vector2Int>();
            Vector2Int minPos = roadPoint + new Vector2Int(-w/2, 1);
            for (int dx = -w/2; dx <= w/2; dx++)
                for (int dy = 1; dy <= h; dy++)
                    buildingBlocks.Add(roadPoint + new Vector2Int(dx, dy));
            // Boundary check
            var minLocal = minPos - OriginChunk.WorldPos;
            var maxLocal = new Vector2Int(minLocal.x + w, minLocal.y + h);
            if (minLocal.x < 0 || maxLocal.x >= Size || minLocal.y < 0 || maxLocal.y >= Size)
                return null;

            // Check adjacency to existing houses or roads
            foreach (var b in buildingBlocks)
            {
                var local = b - OriginChunk.WorldPos;
                for (int ix = -HOUSE_MARGIN; ix <= HOUSE_MARGIN; ix++)
                {
                    for (int iy = -HOUSE_MARGIN; iy <= HOUSE_MARGIN; iy++)
                    {
                        int x = local.x + ix;
                        int y = local.y + iy;
                        if (x >= 0 && x < Size && y >= 0 && y < Size)
                        {
                            if (blockOccupancyMap[x, y] || roadMap[local.x, local.y])
                                return null;
                        }
                    }
                }
            }

            return new CartonHouse(buildingBlocks, roomConfig, minPos, this, ChunkRand);
        }

        public List<IHouse> GetHouses(HouseType houseType)
        {
            return Houses.Where(h => h.HouseType == houseType).ToList();
        }
    }
}