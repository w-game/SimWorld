using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public enum MapItemType
    {
        None,
        Tree,
        Stone,
        Iron,
        Grass,
        Rock
    }
    public class Chunk
    {
        public const int CityLayer = 3;
        public CartonMap Map { get; private set; }

        public Vector2Int Pos { get; private set; }
        public Vector2Int WorldPos => new Vector2Int(Pos.x * Size, Pos.y * Size);
        public Vector2Int CenterPos { get; private set; }
        public BlockType[,] Blocks { get; private set; }
        public City City { get; private set; }
        public int Size { get; private set; }
        public int Layer { get; private set; }
        public BlockType Type { get; private set; }
        public MapItemType[,] MapItems { get; private set; }

        private System.Random _chunkRand;

        public Chunk(Vector2Int pos, int layer, CartonMap map)
        {
            Pos = pos;
            Layer = layer;
            Map = map;
            Size = (int)Mathf.Pow(2, layer) * CartonMap.NORMAL_CHUNK_SIZE;
            Blocks = new BlockType[Size, Size];
            MapItems = new MapItemType[Size, Size];

            _chunkRand = new System.Random(Map.seed + layer * 1000 + pos.x * 100 + pos.y);

            CenterPos = new Vector2Int(_chunkRand.Next(0, Size), _chunkRand.Next(0, Size));
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

                    Blocks[i, j] = nearestChunk != null ? nearestChunk.Type : BlockType.Plain;
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
                            if (Blocks[localPos.x, localPos.y] == BlockType.Ocean)
                                continue; // 如果是海洋，则不设置
                            Blocks[localPos.x, localPos.y] = BlockType.Road;
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
                    List<Vector2Int> housePosInChunk = new List<Vector2Int>();
                    foreach (var housePos in house.Blocks)
                    {
                        // 检查房屋点是否在当前Chunk内
                        Vector2Int localPos = housePos - WorldPos;
                        if (localPos.x >= 0 && localPos.x < Size && localPos.y >= 0 && localPos.y < Size)
                        {
                            if (Blocks[localPos.x, localPos.y] == BlockType.Ocean)
                            {
                                break;
                            }
                            housePosInChunk.Add(localPos);
                        }
                    }

                    foreach (var housePos in housePosInChunk)
                    {
                        Blocks[housePos.x, housePos.y] = BlockType.Room;
                        Debug.Log($"Set room at {housePos}");
                    }
                }
            }
        }


        public void CalcChunk()
        {
            if (Layer == CartonMap.LAYER_NUM - 1)
            {
                var pro = _chunkRand.Next(0, 100);

                if (pro < 50)
                {
                    Type = BlockType.Ocean;
                }
                else
                {
                    Type = BlockType.Plain;
                }

                return;
            }

            var worldCenterPos = CenterPos + WorldPos;

            var nearestChunk = GetNearestChunk(worldCenterPos, true, Layer + 1);

            if (nearestChunk != null)
            {
                Type = nearestChunk.Type;
                if (Layer == CartonMap.LAYER_NUM - 2 && Type == BlockType.Plain)
                {
                    var pro = _chunkRand.Next(0, 100);

                    if (pro < 10)
                    {
                        Type = BlockType.Forest;
                    }
                    else if (pro < 30)
                    {
                        Type = BlockType.Mountain;
                    }
                    else if (pro < 50)
                    {
                        Type = BlockType.Desert;
                    }
                    else
                    {
                        Type = BlockType.Plain;
                    }
                }
            }
            else
            {
                Type = BlockType.Plain;
            }

            if (Layer == CityLayer)
            {
                CheckCreateCity();
            }
        }

        public void CheckCreateCity()
        {
            Debug.Log($"CheckCreateCity {Pos} Layer {Layer} Type {Type}");
            if (Type == BlockType.Plain)
            {

                if (_chunkRand.Next(0, 100) < 20)
                {
                    City = new City(CenterPos, Size, this, _chunkRand);
                    Debug.Log($"Create City at {CenterPos} in Chunk {Pos} Layer {Layer}");
                }
            }
        }

        public void CalcMapItems()
        {
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    var pos = new Vector2Int(i, j);
                    var blockWorldPos = pos + WorldPos;

                    float scale = 0.9f;
                    float frequency = 20f;
                    float noiseValue = frequency * Mathf.PerlinNoise(blockWorldPos.x * scale, blockWorldPos.y * scale);

                    if (Blocks[i, j] == BlockType.Plain)
                    {
                        if (noiseValue > 0.9f * frequency)
                        {
                            MapItems[i, j] = MapItemType.Tree;
                        }
                        else if (noiseValue > 0.8f * frequency)
                        {
                            MapItems[i, j] = MapItemType.Stone;
                        }
                        else if (noiseValue > 0.6f * frequency)
                        {
                            MapItems[i, j] = MapItemType.Grass;
                        }
                        else if (noiseValue > 0.4f * frequency)
                        {
                            MapItems[i, j] = MapItemType.Rock;
                        }
                        else
                        {
                            MapItems[i, j] = MapItemType.None;
                        }
                    }
                    else if (Blocks[i, j] == BlockType.Forest)
                    {
                        if (noiseValue > 0.7f * frequency)
                        {
                            MapItems[i, j] = MapItemType.Tree;
                        }
                        else if (noiseValue > 0.3f * frequency)
                        {
                            MapItems[i, j] = MapItemType.Stone;
                        }
                        else if (noiseValue > 0.1f * frequency)
                        {
                            MapItems[i, j] = MapItemType.Grass;
                        }
                        else
                        {
                            MapItems[i, j] = MapItemType.None;
                        }
                    }
                    else if (Blocks[i, j] == BlockType.Mountain)
                    {
                        if (noiseValue > 0.9f * frequency)
                        {
                            MapItems[i, j] = MapItemType.Iron;
                        }
                        else if (noiseValue > 0.5f * frequency)
                        {
                            MapItems[i, j] = MapItemType.Stone;
                        }
                        else
                        {
                            MapItems[i, j] = MapItemType.None;
                        }
                    }
                    else
                    {
                        MapItems[i, j] = MapItemType.None;
                    }
                }
            }
        }
    }
}