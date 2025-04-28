using System;
using System.Collections.Generic;
using AI;
using GameItem;
using Map;
using UnityEngine;

public class BuildingManager : MonoSingleton<BuildingManager>
{
    [SerializeField] private GameObject blueprintPrefab;
    public Camera mainCamera; // 主摄像机
    public bool CraftMode { get; private set; }
    private BuildingConfig _currentBuildingConfig; // 当前建筑配置
    private bool _isBuilding = false;  // 标记是否正在进行拖动建造

    // 建造模式
    public enum BuildMode
    {
        Single,  // 单格点击
        Line,    // 线性拖动（现有实现）
        Area     // 矩形范围拖动
    }

    // 当前建造模式，默认为线性拖动
    public BuildMode CurrentBuildMode = BuildMode.Line;

    /// <summary>
    /// 清除所有蓝图预览对象
    /// </summary>
    private void ClearSigns()
    {
        foreach (var bp in _signs.Values)
        {
            if (bp != null) Destroy(bp);
        }
        _signs.Clear();
    }

    private Dictionary<Vector2Int, GameObject> _signs = new Dictionary<Vector2Int, GameObject>(); // 存储预览的 sign
    private Dictionary<Vector2Int, GameObject> _blueprints = new Dictionary<Vector2Int, GameObject>(); // 存储已建造的建筑物

    private Vector2Int _originPos; // 起始格子坐标

    private IGameItem _sign;


    // 检查是否满足建造条件（这里仅作示例，实际可加入判断逻辑）
    private bool CheckCanBuild(BlockType blockType)
    {
        // 这里可以添加更多的条件判断
        if (blockType == BlockType.Plain)
        {
            return true;
        }
        else if (blockType == BlockType.Ocean)
        {
            return false;
        }
        else
        {
            // 其他类型的判断...
            return true;
        }
    }

    public void StartBuildingMode(BuildingConfig config)
    {
        CraftMode = true;
        _isBuilding = false;
        _currentBuildingConfig = config;
        if (_currentBuildingConfig.type == "Wall")
        {
            CurrentBuildMode = BuildMode.Line; // 墙体默认线性建造
        }
        else if (_currentBuildingConfig.type == "Floor")
        {
            CurrentBuildMode = BuildMode.Area; // 地板默认矩形建造
        }
        else
        {
            CurrentBuildMode = BuildMode.Single; // 其他类型默认单格建造
        }

        _sign = GenerateBlueprint(UIManager.I.MousePosToWorldPos());
    }

    public void StopBuildingMode()
    {
        CraftMode = false;
        _isBuilding = false;
    }

    private BlueprintItem GenerateBlueprint(Vector3 pos)
    {
       var item = GameItemManager.CreateGameItem<BlueprintItem>(
            _currentBuildingConfig,
            pos,
            GameItemType.Dynamic
        );
        item.ShowUI();

        return item;
    }

    private void GenerateBlueprint()
    {
        foreach (var sign in _signs.Values)
        {
            GenerateBlueprint(sign.transform.position);
        }
    }

    void Update()
    {
        if (_sign != null)
        {
            var cellPos = MapManager.I.WorldPosToCellPos(UIManager.I.MousePosToWorldPos());
            _sign.Pos = new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0);
        }
        if (!CraftMode)
            return;
            
        if (Input.GetMouseButtonDown(1))
        {
            StopBuildingMode();
            return;
        }

        // 鼠标左键按下
        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = UIManager.I.MousePosToWorldPos();
            var blockType = MapManager.I.CheckBlockType(mousePos);

            if (!CheckCanBuild(blockType))
                return;

            // 清除旧预览
            ClearSigns();

            if (CurrentBuildMode == BuildMode.Single)
            {
                // 单格建造无需进入拖动状态
                
                GameManager.I.CurrentAgent.Citizen.Family.Actions.Add(ActionPool.Get<CraftBuildingItemAction>(_sign));
                StopBuildingMode();
                GameManager.I.GameItemManager.SwitchType(_sign, GameItemType.Static);
                _sign = null;
                return;
            }
            else
            {
                // Line / Area 模式进入拖动
                _originPos = MapManager.I.WorldPosToCellPos(UIManager.I.MousePosToWorldPos());
                _isBuilding = true;
            }
        }

        if (_isBuilding)
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            Vector2Int currentGrid = new Vector2Int(Mathf.FloorToInt(mouseWorld.x), Mathf.FloorToInt(mouseWorld.y));

            // 清除旧预览以便重新生成
            ClearSigns();

            if (CurrentBuildMode == BuildMode.Line)
            {
                // 线性（水平或垂直）拖动
                int deltaX = currentGrid.x - _originPos.x;
                int deltaY = currentGrid.y - _originPos.y;

                if (Mathf.Abs(deltaX) >= Mathf.Abs(deltaY))
                    currentGrid.y = _originPos.y;     // 锁 y，水平线
                else
                    currentGrid.x = _originPos.x;     // 锁 x，垂直线

                if (_originPos.x == currentGrid.x)
                {
                    int startY = Mathf.Min(_originPos.y, currentGrid.y);
                    int endY   = Mathf.Max(_originPos.y, currentGrid.y);
                    for (int y = startY; y <= endY; y++)
                    {
                        Vector2Int cell = new Vector2Int(_originPos.x, y);
                        Vector2    bpPos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                        var bpGo = Instantiate(blueprintPrefab, bpPos, Quaternion.identity);
                        var bp = bpGo.GetComponent<Blueprint>();
                        var sprite = Resources.Load<Sprite>(_currentBuildingConfig.icon);
                        // bp.Place(cell, sprite);
                        _signs[cell] = bpGo;
                    }
                }
                else
                {
                    int startX = Mathf.Min(_originPos.x, currentGrid.x);
                    int endX   = Mathf.Max(_originPos.x, currentGrid.x);
                    for (int x = startX; x <= endX; x++)
                    {
                        Vector2Int cell = new Vector2Int(x, _originPos.y);
                        Vector2    bpPos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                        var bpGo = Instantiate(blueprintPrefab, bpPos, Quaternion.identity);
                        var bp = bpGo.GetComponent<Blueprint>();
                        var sprite = Resources.Load<Sprite>(_currentBuildingConfig.icon);
                        // bp.Place(cell, sprite);
                        _blueprints[cell] = bpGo;
                    }
                }
            }
            else if (CurrentBuildMode == BuildMode.Area)
            {
                // 矩形范围拖动
                int startX = Mathf.Min(_originPos.x, currentGrid.x);
                int endX   = Mathf.Max(_originPos.x, currentGrid.x);
                int startY = Mathf.Min(_originPos.y, currentGrid.y);
                int endY   = Mathf.Max(_originPos.y, currentGrid.y);

                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        Vector2    bpPos = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                        var bpGo = Instantiate(blueprintPrefab, bpPos, Quaternion.identity);
                        var bp = bpGo.GetComponent<Blueprint>();
                        var sprite = Resources.Load<Sprite>(_currentBuildingConfig.icon);
                        // bp.Place(cell, sprite);
                        _signs[cell] = bpGo;
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isBuilding = false;
            // TODO: 在此将 _blueprints 中的预览转换为正式建筑
        }
    }
}