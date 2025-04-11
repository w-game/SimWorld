using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Map
{

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

        public System.Random ChunkRand { get; private set; } // 随机数生成器

        public City(Vector2Int pos, int size, Chunk originChunk, System.Random chunkRand)
        {
            GlobalPos = pos + originChunk.WorldPos; // 转换为全局坐标
            Size = size;
            OriginChunk = originChunk;

            ChunkRand = chunkRand;

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

            foreach (var house in Houses)
            {
                house.CalcHouseType();
            }
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
            int roadCount = ChunkRand.Next(1, 3);

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
            foreach (var road in Roads)
            {
                // Randomize road points for more varied placement
                List<Vector2Int> roadPoints = new List<Vector2Int>(road);
                roadPoints = roadPoints.OrderBy(x => ChunkRand.Next()).ToList();

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
                direction = ChunkRand.Next(0, 4);
            }

            // 随机选择建筑大小
            // var roomSize = ChunkRand.Next(1, 4);
            // 随机选择建筑大小
            var roomSize = ChunkRand.Next(1, 4);
            int buildingWidth, buildingHeight;
            if (roomSize == 1)
            {
                buildingWidth = ChunkRand.Next(3, 5);
                buildingHeight = ChunkRand.Next(3, 5);
            }
            else if (roomSize == 2)
            {
                buildingWidth = ChunkRand.Next(6, 9);
                buildingHeight = ChunkRand.Next(6, 9);
            }
            else
            {
                buildingWidth = ChunkRand.Next(10, 15);
                buildingHeight = ChunkRand.Next(10, 15);
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

            return new House(buildingBlocks, new Vector2Int(buildingWidth, buildingHeight), this, ChunkRand);
        }
    }
}