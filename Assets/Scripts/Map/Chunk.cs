using System.Collections.Generic;
using GameItem;
using UnityEngine;

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
        public BlockType Type { get; private set; }

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

                    MapManager.I.TryGetBuildingItem(new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0), out var buildingItem);
                    if (buildingItem != null)
                    {
                        continue;
                    }

                    float scale = 0.9f;
                    float frequency = 20f;
                    float noiseValue = frequency * Mathf.PerlinNoise(blockWorldPos.x * scale, blockWorldPos.y * scale);

                    if (Blocks[i, j] == BlockType.Plain)
                    {
                        if (noiseValue > 0.9f * frequency)
                        {
                            GameItemManager.CreateGameItem<TreeItem>(
                                GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_TREE"),
                                new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0),
                                GameItemType.Static
                            );
                        }
                        else if (noiseValue > 0.8f * frequency)
                        {
                        }
                        else if (noiseValue > 0.6f * frequency)
                        {
                            GameItemManager.CreateGameItem<PlantItem>(
                                GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_GRASS"),
                                new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0),
                                GameItemType.Static,
                                false
                            );
                        }
                        else if (noiseValue > 0.4f * frequency)
                        {
                        }
                        else
                        {
                        }
                    }
                    else if (Blocks[i, j] == BlockType.Forest)
                    {
                        // --- Forest generation (more natural clusters) ---
                        //
                        // 思路：
                        // 1. 使用 Perlin 值控制宏观密度（高值→密林，低值→稀疏）。
                        // 2. 在同一密度区块内，加入一层局部随机，避免完全规则的条纹分布。
                        // 3. 让树木与灌木、草地、岩石按概率共存，形成更真实的混合生态。
                        //
                        // 额外随机因子（0‑1）
                        float localRand = (float)_chunkRand.NextDouble();

                        if (noiseValue > 0.8f * frequency)            // 【密林核心】大量树 + 少量灌木
                        {
                            if (localRand < 0.75f)                    // 75% 树
                            {
                                GameItemManager.CreateGameItem<TreeItem>(
                                    GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_TREE"),
                                    new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0),
                                    GameItemType.Static
                                );
                            }
                            else if (localRand < 0.85f)                                      // 10% 灌木
                            {
                                GameItemManager.CreateGameItem<PlantItem>(
                                    GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_BUSH"),
                                    new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0),
                                    GameItemType.Static,
                                    false
                                );
                            }
                        }
                        else if (noiseValue > 0.6f * frequency)       // 【稀疏树林】树与灌木各半
                        {
                            if (localRand < 0.5f)                     // 50% 树
                            {
                                GameItemManager.CreateGameItem<TreeItem>(
                                    GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_TREE"),
                                    new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0),
                                    GameItemType.Static
                                );
                            }
                            else if (localRand < 0.55f)                                     // 5% 灌木
                            {
                                GameItemManager.CreateGameItem<PlantItem>(
                                    GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_BUSH"),
                                    new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0),
                                    GameItemType.Static,
                                    false
                                );
                            }
                        }
                        else if (noiseValue > 0.45f * frequency)      // 【林缘与空地】少量草 + 零星岩石
                        {
                            if (localRand < 0.3f)                     // 30% 草
                            {
                                GameItemManager.CreateGameItem<PlantItem>(
                                    GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_GRASS"),
                                    new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0),
                                    GameItemType.Static,
                                    false
                                );
                            }
                            else if (localRand < 0.35f)               // 5% 岩石
                            {
                                GameItemManager.CreateGameItem<SmallRockItem>(
                                    GameManager.I.ConfigReader.GetConfig<ResourceConfig>("RESOURCE_ROCK"),
                                    new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0),
                                    GameItemType.Static
                                );
                            }
                            // 其余 55% 留空，形成空地
                        }
                        else if (noiseValue > 0.3f * frequency)       // 【过渡带】以草地为主，偶有灌木
                        {
                            if (localRand < 0.6f)                     // 60% 草
                            {
                                GameItemManager.CreateGameItem<PlantItem>(
                                    GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_GRASS"),
                                    new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0),
                                    GameItemType.Static,
                                    false
                                );
                            }
                            else if (localRand < 0.65f)               // 5% 灌木
                            {
                                GameItemManager.CreateGameItem<PlantItem>(
                                    GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_BUSH"),
                                    new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0),
                                    GameItemType.Static,
                                    false
                                );
                            }
                            // 其余 25% 留空
                        }
                        // noiseValue ≤ 0.3f * frequency 时留为空地，形成自然空隙
                    }
                    else if (Blocks[i, j] == BlockType.Mountain)
                    {
                        if (noiseValue > 0.9f * frequency)
                        {
                        }
                        else if (noiseValue > 0.5f * frequency)
                        {
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                    }
                }
            }
        }
    }
}