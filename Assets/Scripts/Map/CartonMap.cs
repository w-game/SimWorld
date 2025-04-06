using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Map
{
    public enum BlockType
    {
        None,
        Plain,
        Ocean,
        Room,
        Road,
        Farm
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

    public class CartonMap
    {
        public const int LAYER_NUM = 5;
        public const int NORMAL_CHUNK_SIZE = 8;
        public int seed { get; private set; } // 随机种子

        public Dictionary<int, Dictionary<Vector2Int, Chunk>> chunks =
            new Dictionary<int, Dictionary<Vector2Int, Chunk>>();

        private System.Random _managerRand;

        public void Init(int seed)
        {
            this.seed = seed;
            _managerRand = new System.Random(seed);

            for (int i = 0; i < LAYER_NUM; i++)
            {
                chunks.Add(i, new Dictionary<Vector2Int, Chunk>());
            }
        }

        public City FindNearestCity(Vector2Int pos)
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

        public Vector2Int WorldPosToChunkPos(Vector3 pos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / NORMAL_CHUNK_SIZE),
                Mathf.FloorToInt(pos.y / NORMAL_CHUNK_SIZE));
        }

        internal BlockType GetBlockType(Vector3 pos)
        {
            var chunkPos = WorldPosToChunkPos(pos);

            var chunk = GetChunk(chunkPos, 0);

            var localPos = new Vector2Int(
                            (Mathf.FloorToInt(pos.x) % NORMAL_CHUNK_SIZE + NORMAL_CHUNK_SIZE) % NORMAL_CHUNK_SIZE,
                            (Mathf.FloorToInt(pos.y) % NORMAL_CHUNK_SIZE + NORMAL_CHUNK_SIZE) % NORMAL_CHUNK_SIZE);

            return chunk.Blocks[localPos.x, localPos.y];
        }
    }
}