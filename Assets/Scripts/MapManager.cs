using System;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoSingleton<MapManager>
{
    public Transform player;          // 玩家对象
    public GameObject chunkPrefab;    // 预制体上挂有 ChunkTilemap 脚本和 Tilemap 组件
    public const int ChunkSize = 16;        // Tilemap 块尺寸（单位为瓷砖）
    public int viewDistance = 3;      // 以区块为单位的加载半径
    public const int MaxChunkSearchRadius = 10;

    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    private void Start()
    {
        for (int y = -MaxChunkSearchRadius; y <= MaxChunkSearchRadius; y++)
        {
            for (int x = -MaxChunkSearchRadius; x <= MaxChunkSearchRadius; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (!chunks.ContainsKey(coord))
                {
                    GenerateChunk(coord);
                }
            }
        }
        
        player.position = FindNearestLand(Vector3.zero);
    }

    void Update()
    {
        Vector2 playerPos = player.position;
        Vector2Int playerChunkCoord = new Vector2Int(
            Mathf.FloorToInt(playerPos.x / ChunkSize),
            Mathf.FloorToInt(playerPos.y / ChunkSize)
        );

        // 加载视野内区块
        for (int y = -viewDistance; y <= viewDistance; y++)
        {
            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                Vector2Int coord = new Vector2Int(playerChunkCoord.x + x, playerChunkCoord.y + y);
                if (!chunks.ContainsKey(coord))
                {
                    GenerateChunk(coord);
                }
            }
        }

        // 卸载超出视野范围的区块
        List<Vector2Int> keys = new List<Vector2Int>(chunks.Keys);
        foreach (Vector2Int key in keys)
        {
            if (Vector2Int.Distance(key, playerChunkCoord) > viewDistance * 3)
            {
                Destroy(chunks[key].gameObject);
                chunks.Remove(key);
            }
        }
    }

    void GenerateChunk(Vector2Int chunkCoord)
    {
        Vector3 pos = new Vector3(chunkCoord.x * ChunkSize, chunkCoord.y * ChunkSize, 0);
        GameObject chunkObj = Instantiate(chunkPrefab, pos, Quaternion.identity, transform);
        Chunk chunk = chunkObj.GetComponent<Chunk>();
        chunk.GenerateChunk(chunkCoord, ChunkSize);
        chunks.Add(chunkCoord, chunk);
    }
    
    public Vector3 FindNearestLand(Vector3 worldPos)
    {
        // 计算玩家所在的 Chunk 坐标
        Vector2Int playerChunkCoord = new Vector2Int(
            Mathf.FloorToInt(worldPos.x / ChunkSize),
            Mathf.FloorToInt(worldPos.y / ChunkSize)
        );

        float bestDistance = float.MaxValue;
        Chunk bestChunk = null;
        bool found = false;

        // 从半径 0 开始向外搜索
        for (int r = 0; r <= MaxChunkSearchRadius; r++)
        {
            // 遍历当前搜索半径内所有 Chunk（使用二维循环）
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    Vector2Int searchChunkCoord = playerChunkCoord + new Vector2Int(dx, dy);
                    // 判断该 Chunk 是否已经生成
                    if (chunks.TryGetValue(searchChunkCoord, out Chunk chunk))
                    {
                        // 遍历该 Chunk 内所有记录的 Tile（Blocks 字典中存储了相对于 Chunk 内部的坐标）
                        foreach (var kvp in chunk.Blocks)
                        {
                            if (kvp.Value == BlockType.Plain)
                            {
                                // 将 Chunk 内部的 Tile 坐标转换为全局世界坐标
                                Vector2Int tileCoordInChunk = kvp.Key;
                                Vector3 tileWorldPos = new Vector3(
                                    searchChunkCoord.x * ChunkSize + tileCoordInChunk.x,
                                    searchChunkCoord.y * ChunkSize + tileCoordInChunk.y,
                                    0f
                                );
                                // 计算该位置与起始点的距离
                                float distance = Vector3.Distance(worldPos, tileWorldPos);
                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    bestChunk = chunk;
                                    found = true;
                                }
                            }
                        }
                    }
                }
            }
            // 如果在当前半径内找到陆地，则提前退出循环
            if (found)
                break;
        }

        // 如果找到了陆地，则将玩家放在该 tile 所在 Chunk 的中心
        if (found)
        {
            List<Vector2Int> landTiles = new List<Vector2Int>();

            foreach (var kvp in bestChunk.Blocks)
            {
                if (kvp.Value == BlockType.Plain)
                    landTiles.Add(kvp.Key);
            }
            
            int randomIndex = UnityEngine.Random.Range(0, landTiles.Count);
            Vector2Int randomTileLocal = landTiles[randomIndex];
            
            Vector3 randomTileWorldPos = new Vector3(
                bestChunk.Pos.x * ChunkSize + randomTileLocal.x,
                bestChunk.Pos.y * ChunkSize + randomTileLocal.y,
                0f
            );
            return randomTileWorldPos;
        }
        
        return worldPos;
    }

    public BlockType CheckClickOnMap(out Vector3 mouseWorldPos)
    {
        // 将鼠标屏幕坐标转换为世界坐标
        mouseWorldPos = UIManager.I.mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
            
        // 发射射线，注意方向为 Vector2.zero 表示点检测
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
        if (hit.collider != null)
        {
            Chunk chunk = hit.collider.GetComponent<Chunk>();
            if (chunk != null)
            {
                Vector3Int tilePos = chunk.tilemap.WorldToCell(mouseWorldPos);
                Debug.Log("点击的 Tile 坐标: " + tilePos);
                return chunk.Blocks[new Vector2Int(tilePos.x, tilePos.y)];
            }
            Debug.Log("点击到的对象没有 Tilemap 组件");
        }

        return BlockType.Ocean;
    }
}