using System;
using System.Collections.Generic;
using GameItem;
using Map;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MapLayer
{
    Ground,
    Building,
}

public enum BuildingType
{
    None,
    House,
    Farm,
}

public class MapManager : MonoSingleton<MapManager>
{
    public CartonMap CartonMap { get; private set; } // 地图对象

    public int seed = 123120;             // 随机种子
    public Tilemap tilemap;

    public Tilemap layer1;
    public TileBase tile;
    public List<TileBase> farmTiles;
    private Dictionary<Vector2Int, BuildingType> _buildings = new Dictionary<Vector2Int, BuildingType>();
    private Dictionary<Vector2Int, List<GameItemBase>> _gameItems = new Dictionary<Vector2Int, List<GameItemBase>>();

    private Dictionary<Vector2Int, Chunk> _chunkActive = new Dictionary<Vector2Int, Chunk>();

    private Transform _player;

    private void Start()
    {
        CartonMap = new CartonMap();
        CartonMap.Init(seed);
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                GenerateChunk(new Vector2Int(i, j));
            }
        }

        var city = CartonMap.FindNearestCity(new Vector2Int(0, 0));
        _player = GameManager.I.CurrentAgent.transform;
        _player.position = new Vector3(city.GlobalPos.x, city.GlobalPos.y, 0);

        var foodGo = GameManager.I.InstantiateObject("Prefabs/GameItems/FoodItem", _player.position);
        var foodItem = foodGo.GetComponent<FoodItem>();
        foodItem.Init(new PropConfig("PROP_FOOD_APPLE", "Food", 1), 1);
        RegisterGameItem(foodItem);
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
            Mathf.FloorToInt(_player.position.x / CartonMap.NORMAL_CHUNK_SIZE),
            Mathf.FloorToInt(_player.position.y / CartonMap.NORMAL_CHUNK_SIZE)
        );

        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                var pos = playerChunkPos + new Vector2Int(x - 3, y - 3);
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
                if (Vector2Int.Distance(chunk.Value.Pos, playerChunkPos) > 8)
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
            }
        }

        for (int i = 0; i < chunk.Size; i++)
        {
            for (int j = 0; j < chunk.Size; j++)
            {
                var pos = new Vector2Int(i, j);
                var blockWorldPos = pos + chunk.WorldPos;
                var type = chunk.MapItems[i, j];
                switch (type)
                {
                    case MapItemType.Tree:
                        var treeGo = GameManager.I.InstantiateObject("Prefabs/GameItems/TreeItem", new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0));
                        var config = GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_TREE");
                        var treeItem = treeGo.AddComponent<TreeItem>();
                        treeItem.Init(config);
                        RegisterGameItem(treeItem);
                        break;
                    case MapItemType.Grass:
                        var grassGo = GameManager.I.InstantiateObject("Prefabs/GameItems/PlantItem", new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0));
                        var grassConfig = GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_GRASS");
                        var grassItem = grassGo.AddComponent<PlantItem>();
                        grassItem.Init(grassConfig);
                        RegisterGameItem(grassItem);
                        break;
                    case MapItemType.Rock:

                    default:
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

    public BlockType CheckBlockType(Vector3 pos)
    {
        var blockType = CartonMap.GetBlockType(pos);
        Debug.Log($"点击的 Tile 坐标: {pos}, 类型: {blockType}");

        return blockType;
    }

    public BuildingType CheckBuildingType(Vector3 pos)
    {
        var cellPos = WorldPosToCellPos(pos);
        if (_buildings.ContainsKey(cellPos))
        {
            return _buildings[cellPos];
        }

        return BuildingType.None;
    }

    private void SetBlockType(Vector2Int pos, BlockType type)
    {
        tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), tile);

        Color plainColor = new Color(0.6f, 1f, 0.6f);
        Color oceanColor = new Color(0.4f, 0.6f, 1f);
        Color roomColor = new Color(1f, 0.6f, 0.6f);
        Color roadColor = new Color(1f, 1f, 0.5f);
        Color defaultColor = new Color(0.8f, 0.8f, 0.8f);

        switch (type)
        {
            case BlockType.Plain:
                tilemap.SetColor(new Vector3Int(pos.x, pos.y, 0), plainColor);
                break;
            case BlockType.Ocean:
                tilemap.SetColor(new Vector3Int(pos.x, pos.y, 0), oceanColor);
                break;
            case BlockType.Room:
                tilemap.SetColor(new Vector3Int(pos.x, pos.y, 0), roomColor);
                break;
            case BlockType.Road:
                tilemap.SetColor(new Vector3Int(pos.x, pos.y, 0), roadColor);
                break;
            case BlockType.Forest:
                tilemap.SetColor(new Vector3Int(pos.x, pos.y, 0), new Color(0.4f, 1f, 0.4f));
                break;
            case BlockType.Mountain:
                tilemap.SetColor(new Vector3Int(pos.x, pos.y, 0), new Color(0.6f, 0.4f, 0.4f));
                break;
            case BlockType.Desert:
                tilemap.SetColor(new Vector3Int(pos.x, pos.y, 0), new Color(1f, 1f, 0.4f));
                break;
            default:
                tilemap.SetColor(new Vector3Int(pos.x, pos.y, 0), defaultColor);
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

    internal void SetMapTile(Vector3 mousePos, BlockType type, MapLayer layer, List<TileBase> tiles = null)
    {
        var cellPos = WorldPosToCellPos(mousePos);

        switch (layer)
        {
            case MapLayer.Ground:
                var chunkPos = CartonMap.WorldPosToChunkPos(mousePos);
                var chunk = CartonMap.GetChunk(chunkPos, 0);
                chunk.Blocks[chunk.WorldPos.x, chunk.WorldPos.y] = type;
                SetBlockType(cellPos, type);
                break;
            case MapLayer.Building:
                if (tile != null)
                {
                    layer1.SetTile(new Vector3Int(cellPos.x, cellPos.y, 0), RandomTile(tiles));
                    _buildings.Add(cellPos, BuildingType.Farm);
                }
                break;
            default:
                break;
        }
    }

    internal void RegisterGameItem(GameItemBase gameItem)
    {
        var cellPos = WorldPosToCellPos(gameItem.transform.position);
        if (!_gameItems.ContainsKey(cellPos))
        {
            _gameItems.Add(cellPos, new List<GameItemBase>() { gameItem });
        }
        else
        {
            _gameItems[cellPos].Add(gameItem);
        }
    }

    internal void RemoveGameItem(GameItemBase gameItem)
    {
        var cellPos = WorldPosToCellPos(gameItem.transform.position);
        if (_gameItems.ContainsKey(cellPos) && _gameItems[cellPos].Contains(gameItem))
        {
            _gameItems[cellPos].Remove(gameItem);
            Destroy(gameItem.gameObject);
        }
    }

    internal void RemoveGameItemOnMap(GameItemBase gameItem)
    {
        var cellPos = WorldPosToCellPos(gameItem.transform.position);
        if (_gameItems.ContainsKey(cellPos) && _gameItems[cellPos].Contains(gameItem))
        {
            _gameItems[cellPos].Remove(gameItem);
        }
    }

    internal List<GameItemBase> GetItemsAtPos(Vector3 pos)
    {
        var cellPos = WorldPosToCellPos(pos);
        List<GameItemBase> items = new List<GameItemBase>();

        if (_gameItems.ContainsKey(cellPos))
        {
            items.AddRange(_gameItems[cellPos]);
        }

        return items;
    }
}