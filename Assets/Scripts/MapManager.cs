using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
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

public enum BuildingType
{
    None,
    Door,
    Wall,
    Farm,
}

public class MapManager : MonoSingleton<MapManager>
{
    public const int SIGHT_RANGE = 10; // 视野范围
    public CartonMap CartonMap { get; private set; } // 地图对象

    public int seed = 123120;             // 随机种子
    public Tilemap tilemap;
    public Tilemap floorLayer;

    public Tilemap layer1;
    public TileBase tile;
    public List<TileBase> farmTiles;
    public List<TileBase> farmWateredTiles;
    public List<TileBase> wallTiles;
    public List<TileBase> floorTiles;
    public TileBase doorTile;
    private Dictionary<Vector2Int, BuildingType> _buildings = new Dictionary<Vector2Int, BuildingType>();

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

                var type = chunk.MapItems[i, j];
                switch (type)
                {
                    case MapItemType.Tree:
                        var config = GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_TREE");
                        var treeItem = new TreeItem(config, new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0));
                        treeItem.ShowUI();
                        break;
                    case MapItemType.Grass:
                        var grassConfig = GameManager.I.ConfigReader.GetConfig<ResourceConfig>("PLANT_GRASS");
                        var grassItem = new PlantItem(grassConfig, new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0), randomStage: true);
                        grassItem.ShowUI();
                        break;
                    case MapItemType.Rock:

                    default:
                        break;
                }
            }
        }

        var cityChunk = CartonMap.GetChunk(new Vector3(chunk.WorldPos.x, chunk.WorldPos.y), Chunk.CityLayer);
        if (cityChunk.City != null)
        {
            var families = GameManager.I.CitizenManager.GetCitizens(cityChunk.City);
            foreach (var family in families)
            {
                var house = family.Houses[0];
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

            foreach (var house in cityChunk.City.Houses)
            {
                if (house.HouseType == HouseType.House)
                {
                    foreach (var cell in house.CellMap)
                    {
                        var worldPos = cell.Key + house.MinPos;
                        var localPos = worldPos - chunk.WorldPos;
                        if (localPos.x < 0 || localPos.x >= chunk.Size || localPos.y < 0 || localPos.y >= chunk.Size)
                        {
                            continue;
                        }

                        if (cell.Value == CellType.Wall)
                        {
                            SetMapTile(worldPos, MapLayer.Building, wallTiles, BuildingType.Wall);
                        }
                        else if (cell.Value == CellType.Room)
                        {
                            SetMapTile(worldPos, MapLayer.Building, floorTiles, BuildingType.None);
                        }
                        else if (cell.Value == CellType.Door)
                        {
                            SetMapTile(worldPos, MapLayer.Building, new List<TileBase> { doorTile }, BuildingType.Door);
                        }
                    }

                    if (house.Furnitures.Count > 0)
                    {
                        foreach (var furniture in house.Furnitures)
                        {
                            var localPos = furniture.Key - chunk.WorldPos;
                            if (localPos.x < 0 || localPos.x >= chunk.Size || localPos.y < 0 || localPos.y >= chunk.Size)
                            {
                                continue;
                            }

                            var config = GameManager.I.ConfigReader.GetConfig<BuildingConfig>(furniture.Value);
                            Debug.Log($"生成家具: {config.name}，位置: {furniture.Key}, 类型: {config.type}");
                            var type = Type.GetType($"GameItem.{config.type}Item");
                            var furnitureItem = Activator.CreateInstance(type, new object[] { config, new Vector3(furniture.Key.x + 0.5f, furniture.Key.y + 0.5f, 0) }) as GameItemBase;

                            furnitureItem.ShowUI();
                        }
                    }
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

    public void SetMapTile(Vector3 pos, BlockType type)
    {
        var cellPos = WorldPosToCellPos(pos);

        var chunkPos = CartonMap.WorldPosToChunkPos(pos);
        var chunk = CartonMap.GetChunk(chunkPos, 0);
        chunk.Blocks[chunk.WorldPos.x, chunk.WorldPos.y] = type;
        SetBlockType(cellPos, type);
    }

    public void SetMapTile(Vector2Int cellPos, MapLayer layer, List<TileBase> tiles, BuildingType buildingType)
    {
        switch (layer)
        {
            case MapLayer.Floor:
                if (tile != null)
                {
                    floorLayer.SetTile(new Vector3Int(cellPos.x, cellPos.y, 0), RandomTile(tiles));
                    if (!_buildings.ContainsKey(cellPos))
                    {
                        _buildings.Add(cellPos, buildingType);
                    }
                    else
                    {

                    }
                }
                break;
            case MapLayer.Building:
                if (tile != null)
                {
                    layer1.SetTile(new Vector3Int(cellPos.x, cellPos.y, 0), RandomTile(tiles));
                    if (!_buildings.ContainsKey(cellPos))
                    {
                        _buildings.Add(cellPos, buildingType);
                    }
                    else
                    {

                    }
                }
                break;
            default:
                break;
        }
    }

    public void SetMapTile(Vector3 pos, MapLayer layer, List<TileBase> tiles, BuildingType buildingType)
    {
        var cellPos = WorldPosToCellPos(pos);
        SetMapTile(cellPos, layer, tiles, buildingType);
    }
}