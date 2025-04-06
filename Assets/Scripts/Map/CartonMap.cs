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
                        case BlockType.Plain:
                            tilemap.SetColor(new Vector3Int(blockWorldPos.x, blockWorldPos.y, 0), plainColor);
                            break;
                        case BlockType.Ocean:
                            tilemap.SetColor(new Vector3Int(blockWorldPos.x, blockWorldPos.y, 0), oceanColor);
                            break;
                        case BlockType.Room:
                            tilemap.SetColor(new Vector3Int(blockWorldPos.x, blockWorldPos.y, 0), roomColor);
                            break;
                        case BlockType.Road:
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