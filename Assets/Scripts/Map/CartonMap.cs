using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public enum BlockType
    {
        None,
        Plain,
        Ocean,
        Road,
        Forest,
        Mountain,
        Desert
    }

    public class CartonMap
    {
        public const int LAYER_NUM = 6;
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

        public Chunk GetChunk(Vector3 pos, int layer)
        {
            var floatSize = (float)((int)Mathf.Pow(2, layer) * NORMAL_CHUNK_SIZE);
            var chunkPos = new Vector2Int(
                Mathf.FloorToInt(pos.x / floatSize),
                Mathf.FloorToInt(pos.y / floatSize)
            );
            return GetChunk(chunkPos, layer);
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
            if (layer == Chunk.CityLayer && chunk.City != null)
            {
                GameManager.I.CitizenManager.GenerateNPCs(chunk.City);
            }

            return chunk;
        }

        public Vector2Int WorldPosToChunkPos(Vector3 pos, int layer)
        {
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / Mathf.Pow(2, layer) / NORMAL_CHUNK_SIZE),
                Mathf.FloorToInt(pos.y / Mathf.Pow(2, layer) / NORMAL_CHUNK_SIZE));
        }

        internal BlockType GetBlockType(Vector3 pos)
        {
            var chunkPos = WorldPosToChunkPos(pos, 0);

            var chunk = GetChunk(chunkPos, 0);

            var localPos = new Vector2Int(
                            (Mathf.FloorToInt(pos.x) % NORMAL_CHUNK_SIZE + NORMAL_CHUNK_SIZE) % NORMAL_CHUNK_SIZE,
                            (Mathf.FloorToInt(pos.y) % NORMAL_CHUNK_SIZE + NORMAL_CHUNK_SIZE) % NORMAL_CHUNK_SIZE);

            return chunk.Blocks[localPos.x, localPos.y];
        }

        public City GetCity(Vector3 pos)
        {
            var chunkPos = WorldPosToChunkPos(pos, Chunk.CityLayer);

            if (chunks[Chunk.CityLayer].ContainsKey(chunkPos))
            {
                return chunks[Chunk.CityLayer][chunkPos].City;
            }

            return null;
        }
    }
}