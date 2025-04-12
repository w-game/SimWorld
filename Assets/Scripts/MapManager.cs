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
    public const int SIGHT_RANGE = 10; // 视野范围
    public CartonMap CartonMap { get; private set; } // 地图对象

    public int seed = 123120;             // 随机种子
    public Tilemap tilemap;

    public Tilemap layer1;
    public TileBase tile;
    public List<TileBase> farmTiles;
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
                        var grassItem = new PlantItem(grassConfig, new Vector3(blockWorldPos.x + 0.5f, blockWorldPos.y + 0.5f, 0));
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
                if (house != null)
                {
                    var housePos = house.Blocks[house.Blocks.Count / 2];
                    var localPos = housePos - chunk.WorldPos;
                    if (localPos.x < 0 || localPos.x >= chunk.Size || localPos.y < 0 || localPos.y >= chunk.Size)
                    {
                        continue;
                    }
                    GameManager.I.GameItemManager.CreateNPC(housePos);
                }
            }

            foreach (var house in cityChunk.City.Houses)
            {
                if (house.HouseType == HouseType.House)
                {
                    if (house.Furnitures.Count > 0)
                    {
                        foreach (var furniture in house.Furnitures)
                        {
                            // var localPos = WorldPosToCellPos(furniture.Pos) - chunk.WorldPos;
                            // if (localPos.x < 0 || localPos.x >= chunk.Size || localPos.y < 0 || localPos.y >= chunk.Size)
                            // {
                            //     continue;
                            // }

                            furniture.ShowUI();
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
}