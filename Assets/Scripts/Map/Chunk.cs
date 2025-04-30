using System;
using System.Linq;
using UnityEngine;
using GameItem;
using System.Collections.Generic;

namespace Map
{
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
        public BlockType ChunkType { get; private set; }

        private System.Random _chunkRand;

        public Chunk(Vector2Int pos, int layer, CartonMap map)
        {
            Pos = pos;
            Layer = layer;
            Map = map;
            Size = (int)Mathf.Pow(2, layer) * CartonMap.NORMAL_CHUNK_SIZE;
            Blocks = new BlockType[Size, Size];

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

                    Blocks[i, j] = nearestChunk != null ? nearestChunk.ChunkType : BlockType.Plain;
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
                        }
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
                    ChunkType = BlockType.Ocean;
                }
                else
                {
                    ChunkType = BlockType.Plain;
                }

                return;
            }

            var worldCenterPos = CenterPos + WorldPos;

            var nearestChunk = GetNearestChunk(worldCenterPos, true, Layer + 1);

            if (nearestChunk != null)
            {
                ChunkType = nearestChunk.ChunkType;
                if (Layer == CartonMap.LAYER_NUM - 2 && ChunkType == BlockType.Plain)
                {
                    var pro = _chunkRand.Next(0, 100);

                    if (pro < 10)
                    {
                        ChunkType = BlockType.Forest;
                    }
                    else if (pro < 30)
                    {
                        ChunkType = BlockType.Mountain;
                    }
                    else if (pro < 50)
                    {
                        ChunkType = BlockType.Desert;
                    }
                    else
                    {
                        ChunkType = BlockType.Plain;
                    }
                }
            }
            else
            {
                ChunkType = BlockType.Plain;
            }

            if (Layer == CityLayer)
            {
                CheckCreateCity();
            }
        }

        public void CheckCreateCity()
        {
            Debug.Log($"CheckCreateCity {Pos} Layer {Layer} ChunkType {ChunkType}");
            if (ChunkType == BlockType.Plain)
            {

                if (_chunkRand.Next(0, 100) < 20)
                {
                    City = new City(CenterPos, Size, this, _chunkRand);
                    Debug.Log($"Create City at {CenterPos} in Chunk {Pos} Layer {Layer}");
                }
            }
        }

        public void CheckCityItems()
        {

        }

        public void CalcMapItems()
        {
            List<(string id, Vector3 pos, string mode)> spawnList = new();

            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    var pos2 = new Vector2Int(i, j);
                    var worldPos = new Vector3(pos2.x + WorldPos.x + 0.5f, pos2.y + WorldPos.y + 0.5f, 0);
                    if (MapManager.I.TryGetBuildingItem(worldPos, out _)) continue;

                    var biomeConfig = ConfigReader.GetConfig<BiomeConfig>(Blocks[i, j].ToString().ToUpper());
                    if (biomeConfig == null) continue;

                    float noise = biomeConfig.frequency * Mathf.PerlinNoise((WorldPos.x + i) * biomeConfig.scale, (WorldPos.y + j) * biomeConfig.scale);
                    var layerList = biomeConfig.layers.OrderByDescending(l => l.threshold).ToArray();

                    var layer = layerList.FirstOrDefault(l => noise >= l.threshold);
                    if (layer == null) continue;

                    // weighted selection
                    float r = (float)_chunkRand.NextDouble() * 100f;
                    var c = 0f;
                    foreach (var it in layer.items)
                    {
                        c += it.weight;
                        if (c >= r)
                        {
                            spawnList.Add((it.id, worldPos, layer.mode));
                            break;
                        }
                    }
                }
            }

            // cluster mode using Poisson disc (optional: implement elsewhere)
            foreach (var spawn in spawnList)
            {
                if (spawn.mode == "cluster")
                {
                }
                var config = ConfigReader.GetConfig<ResourceConfig>(spawn.id);
                var type = Type.GetType($"GameItem.{config.type}Item");
                GameItemManager.CreateGameItem<IGameItem>(
                  type,
                  config, spawn.pos, GameItemType.Static, true
                );
            }
        }
    }
}