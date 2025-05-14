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

        public void Init(int seed)
        {
            this.seed = seed;

            for (int i = 0; i < LAYER_NUM; i++)
            {
                chunks.Add(i, new Dictionary<Vector2Int, Chunk>());
            }
        }

        public City FindNearestCity(Vector2Int startPos, int maxStep = 2048)
        {
            // 8‑directional search offsets
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),  new Vector2Int(1, 0),
                new Vector2Int(0,-1),  new Vector2Int(-1,0),
                new Vector2Int(1, 1),  new Vector2Int(1,-1),
                new Vector2Int(-1,1),  new Vector2Int(-1,-1)
            };

            var queue   = new Queue<(Vector2Int pos, int depth)>();
            var visited = new HashSet<Vector2Int>();

            queue.Enqueue((startPos, 0));
            visited.Add(startPos);

            while (queue.Count > 0)
            {
                var (pos, depth) = queue.Dequeue();
                if (depth > maxStep) break;          // safety cap

                var chunk = GetChunk(pos, Chunk.CityLayer);
                if (chunk != null && chunk.City != null)
                    return chunk.City;

                foreach (var dir in directions)
                {
                    var next = pos + dir;
                    if (!visited.Contains(next))
                    {
                        visited.Add(next);
                        queue.Enqueue((next, depth + 1));
                    }
                }
            }

            return null; // No city found within search radius
        }

        public Chunk GetChunk(Vector3 pos, int layer, bool calcBlacks = false)
        {
            var floatSize = (float)((int)Mathf.Pow(2, layer) * NORMAL_CHUNK_SIZE);
            var chunkPos = new Vector2Int(
                Mathf.FloorToInt(pos.x / floatSize),
                Mathf.FloorToInt(pos.y / floatSize)
            );
            return GetChunk(chunkPos, layer, calcBlacks);
        }

        public Chunk GetChunk(Vector2Int pos, int layer, bool calcBlacks = false)
        {
            if (layer < 0 || layer >= LAYER_NUM)
            {
                return null;
            }

            if (chunks.ContainsKey(layer))
            {
                if (chunks[layer].ContainsKey(pos))
                {
                    var chunk = chunks[layer][pos];
                    if (layer == 0 && calcBlacks && chunk.Blocks == null)
                    {
                        chunk.CalcBlocks();
                    }
                    return chunk;
                }
                else
                {
                    return CreateChunk(pos, layer, calcBlacks);
                }
            }

            return null;
        }

        public Chunk CreateChunk(Vector2Int pos, int layer, bool calcBlacks)
        {
            Chunk chunk = new Chunk(pos, layer, this);
            chunk.CalcChunk();
            chunks[layer].Add(pos, chunk);

            if (calcBlacks && layer == 0)
            {
                chunk.CalcBlocks();
            }

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