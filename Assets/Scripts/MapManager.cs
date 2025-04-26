using System;
using System.Collections.Generic;
using Citizens;
using GameItem;
using Map;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MapLayer
{
    Ground,
    Floor,
    Building,
}

public class MapManager : MonoSingleton<MapManager>
{
    public const int SIGHT_RANGE = 10; // 视野范围
    public CartonMap CartonMap { get; private set; } // 地图对象

    public int seed = 123120;             // 随机种子
    public Tilemap ocean;
    public Tilemap tilemap;
    public Tilemap floorLayer;

    public Tilemap layer1;
    public List<TileBase> tiles;
    public TileBase oceanTile;
    public TileBase roadTile;
    public List<TileBase> farmTiles;
    public List<TileBase> farmWateredTiles;
    public List<TileBase> wallTiles;
    public List<TileBase> floorTiles;
    public TileBase doorTile;
    private Dictionary<Vector2Int, Chunk> _chunkActive = new Dictionary<Vector2Int, Chunk>();

    private Agent _player;

    private void Start()
    {
        CartonMap = new CartonMap();
        CartonMap.Init(seed);

        var city = CartonMap.FindNearestCity(new Vector2Int(0, 0));
        _player = GameManager.I.CurrentAgent;
        _player.Pos = new Vector3(city.GlobalPos.x, city.GlobalPos.y, 0);

        var playerChunkPos = new Vector2Int(
            Mathf.FloorToInt(_player.Pos.x / CartonMap.NORMAL_CHUNK_SIZE),
            Mathf.FloorToInt(_player.Pos.y / CartonMap.NORMAL_CHUNK_SIZE)
        );

        for (int x = 0; x < SIGHT_RANGE; x++)
        {
            for (int y = 0; y < SIGHT_RANGE; y++)
            {
                var pos = playerChunkPos + new Vector2Int(x - 3, y - 3);
                if (_chunkActive.ContainsKey(pos))
                {
                    continue;
                }

                GenerateChunk(pos);
            }
        }
    }

    void Update()
    {
        UpdateChunk();
    }

    private void GenerateChunk(Vector2Int pos)
    {
        var chunk = CartonMap.GetChunk(pos, 0);
        if (chunk != null)
        {
            chunk.CalcBlocks();
            chunk.CalcMapItems();
            VisualChunk(chunk);
            if (!_chunkActive.ContainsKey(pos))
            {
                _chunkActive.Add(pos, chunk);
            }
        }
    }

    private void UpdateChunk()
    {
        var playerChunkPos = new Vector2Int(
            Mathf.FloorToInt(_player.Pos.x / CartonMap.NORMAL_CHUNK_SIZE),
            Mathf.FloorToInt(_player.Pos.y / CartonMap.NORMAL_CHUNK_SIZE)
        );

        for (int x = 0; x < SIGHT_RANGE; x++)
        {
            for (int y = 0; y < SIGHT_RANGE; y++)
            {
                var pos = playerChunkPos + new Vector2Int(x - SIGHT_RANGE / 2, y - SIGHT_RANGE / 2);
                if (_chunkActive.ContainsKey(pos))
                {
                    continue;
                }

                GenerateChunk(pos);
            }
        }

        foreach (var chunk in new Dictionary<Vector2Int, Chunk>(_chunkActive))
        {
            if (chunk.Value != null)
            {
                if (Vector2Int.Distance(chunk.Value.Pos, playerChunkPos) > SIGHT_RANGE + 2)
                {
                    _chunkActive.Remove(chunk.Key);
                    UnVisualChunk(chunk.Value);
                }
            }
        }
    }

    private void VisualChunk(Chunk chunk)
    {
        for (int i = 0; i < chunk.Size; i++)
        {
            for (int j = 0; j < chunk.Size; j++)
            {
                var pos = new Vector2Int(i, j);
                var blockWorldPos = pos + chunk.WorldPos;
                SetBlockType(blockWorldPos, chunk.Blocks[i, j]);

                var itemsAtPos = GameManager.I.GameItemManager.GetItemsAtPos(new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0));

                foreach (var item in itemsAtPos)
                {
                    item.ShowUI();
                }
            }
        }

        var cityChunk = CartonMap.GetChunk(new Vector3(chunk.WorldPos.x, chunk.WorldPos.y), Chunk.CityLayer);
        if (cityChunk.City != null)
        {
            var families = GameManager.I.CitizenManager.GetCitizens(cityChunk.City);
            foreach (var family in families)
            {
                foreach (var member in family.Members)
                {
                    // var localPos = WorldPosToCellPos(member.Agent.Pos) - chunk.WorldPos;

                    // if (localPos.x < 0 || localPos.x >= chunk.Size || localPos.y < 0 || localPos.y >= chunk.Size)
                    // {
                    //     continue;
                    // }
                    member.Agent.ShowUI();
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
                if (pos.x % 4 == 0)
                {
                    ocean.SetTile(new Vector3Int(pos.x, pos.y, 0), null);
                }
            }
        }
    }

    public BlockType CheckBlockType(Vector3 pos)
    {
        var blockType = CartonMap.GetBlockType(pos);
        Debug.Log($"点击的 Tile 坐标: {pos}, 类型: {blockType}");

        return blockType;
    }

    private void SetBlockType(Vector2Int pos, BlockType type)
    {
        if (pos.x % 4 == 0)
        {
            ocean.SetTile(new Vector3Int(pos.x, pos.y, 0), oceanTile);
        }
        switch (type)
        {
            case BlockType.Plain:
                var prob = UnityEngine.Random.Range(0, 100);
                if (prob < 50)
                {
                    tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), tiles[0]);
                }
                else if (prob < 75)
                {
                    tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), tiles[1]);
                } 
                else if (prob < 100)
                {
                    tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), tiles[2]);
                }
                break;
            case BlockType.Ocean:
                break;
            case BlockType.Road:
                tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), roadTile);
                break;
            case BlockType.Forest:
                tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), RandomTile(tiles));
                break;
            case BlockType.Mountain:
                tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), RandomTile(tiles));
                break;
            case BlockType.Desert:
                tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), RandomTile(tiles));
                break;
            default:
                tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), RandomTile(tiles));
                break;
        }
    }

    private TileBase RandomTile(List<TileBase> tile)
    {
        var index = UnityEngine.Random.Range(0, tile.Count);
        return tile[index];
    }

    public Vector2Int WorldPosToCellPos(Vector3 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
    }

    public void SetMapTile(Vector3 pos, BlockType type)
    {
        var cellPos = WorldPosToCellPos(pos);

        var chunkPos = CartonMap.WorldPosToChunkPos(pos, 0);
        var chunk = CartonMap.GetChunk(chunkPos, 0);
        chunk.Blocks[chunk.WorldPos.x, chunk.WorldPos.y] = type;
        SetBlockType(cellPos, type);
    }

    public void SetMapTile(Vector2Int cellPos, MapLayer layer, List<TileBase> tiles)
    {
        switch (layer)
        {
            case MapLayer.Floor:
                if (tiles != null)
                {
                    floorLayer.SetTile(new Vector3Int(cellPos.x, cellPos.y, 0), RandomTile(tiles));
                }
                break;
            case MapLayer.Building:
                if (tiles != null)
                {
                    layer1.SetTile(new Vector3Int(cellPos.x, cellPos.y, 0), RandomTile(tiles));
                }
                else
                {
                    layer1.SetTile(new Vector3Int(cellPos.x, cellPos.y, 0), null);
                }
                break;
            default:
                break;
        }
    }

    public void SetMapTile(Vector3 pos, MapLayer layer, List<TileBase> tiles)
    {
        var cellPos = WorldPosToCellPos(pos);
        SetMapTile(cellPos, layer, tiles);
    }

    public bool TryGetBuildingItem(Vector3 pos, out BuildingItem buildingItem)
    {
        var items = GameManager.I.GameItemManager.GetItemsAtPos(pos);
        foreach (var item in items)
        {
            if (item is BuildingItem building)
            {
                buildingItem = building;
                return true;
            }
        }

        buildingItem = null;
        return false;
    }

    public bool TryGetResourceItems(Vector3 pos, out List<PlantItem> plantItems)
    {
        plantItems = new List<PlantItem>();
        var items = GameManager.I.GameItemManager.GetItemsAtPos(pos);
        foreach (var item in items)
        {
            if (item is PlantItem plant)
            {
                plantItems.Add(plant);
            }
        }

        return plantItems.Count > 0;
    }

    public HouseType CheckMapAera(Vector3 pos)
    {
        if (TryGetBuildingItem(pos, out var buildingItem))
        {
            return buildingItem.House.HouseType;
        }

        return HouseType.None;
    }

    public bool IsWalkable(Vector3 pos)
    {
        var items = GameManager.I.GameItemManager.GetItemsAtPos(pos);
        foreach (var item in items)
        {
            if (!item.Walkable)
            {
                return false;
            }
        }
        return true;
    }

    public Vector3 GetItemArroundPos(Agent agent, Vector3 targetPos)
    {
        var arroundPos = new List<Vector3>()
        {
            targetPos + new Vector3(1, 0, 0),
            targetPos + new Vector3(-1, 0, 0),
            targetPos + new Vector3(0, 1, 0),
            targetPos + new Vector3(0, -1, 0),
        };
        
        var walkablePos = new List<Vector3>();
        foreach (var pos in arroundPos)
        {
            if (IsWalkable(pos))
            {
                walkablePos.Add(pos);
            }
        }

        float minDistance = float.MaxValue;
        Vector3 closestPos = Vector3.zero;
        foreach (var pos in walkablePos)
        {
            float distance = Vector3.Distance(agent.Pos, pos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPos = pos;
            }
        }

        return closestPos;
    }
}