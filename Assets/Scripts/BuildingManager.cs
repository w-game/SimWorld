using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public Camera mainCamera; // 主摄像机
    public GameObject buildingSignPrefab;

    private bool _isStartBuild = true; // 是否处于建造模式，可根据需要控制开关
    private bool _isBuilding = false;  // 标记是否正在进行拖动建造

    private List<GameObject> _signs = new List<GameObject>();

    private Vector2Int _originPos; // 起始格子坐标

    // 检查是否满足建造条件（这里仅作示例，实际可加入判断逻辑）
    private bool CheckCanBuild()
    {
        return true;
    }
    
    void Update()
    {
        if (!_isStartBuild)
            return;

        // 鼠标左键按下时，确定起始点并开始建造
        if (Input.GetMouseButtonDown(0))
        {
            var blockType = MapManager.I.CheckClickOnMap(out var mouseWorldPos);
            if (CheckCanBuild())
            {
                _originPos = mouseWorldPos.ToVector2Int();
                _isBuilding = true;
            }
        }

        // 正在建造时，根据鼠标位置实时更新预览的 sign 连线
        if (_isBuilding)
        {
            // 获取当前鼠标在世界中的位置，并转换为格子坐标
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            Vector2Int currentGrid = mouseWorld.ToVector2Int();

            // 锁定移动方向：比较水平和垂直差值，取较大的方向
            int deltaX = currentGrid.x - _originPos.x;
            int deltaY = currentGrid.y - _originPos.y;
            if (Mathf.Abs(deltaX) >= Mathf.Abs(deltaY))
            {
                // 水平移动：锁定y坐标
                currentGrid.y = _originPos.y;
            }
            else
            {
                // 垂直移动：锁定x坐标
                currentGrid.x = _originPos.x;
            }

            // 清除之前生成的 sign（预览线）
            foreach (var sign in _signs)
            {
                Destroy(sign);
            }
            _signs.Clear();

            // 根据起始点与当前格子位置，生成两点之间的连续 sign
            if (_originPos.x == currentGrid.x)
            {
                // 垂直方向：遍历y轴
                int startY = Mathf.Min(_originPos.y, currentGrid.y);
                int endY = Mathf.Max(_originPos.y, currentGrid.y);
                for (int y = startY; y <= endY; y++)
                {
                    Vector2 pos = new Vector2(_originPos.x + 0.5f, y + 0.5f);
                    var sign = Instantiate(buildingSignPrefab, pos, Quaternion.identity);
                    _signs.Add(sign);
                }
            }
            else if (_originPos.y == currentGrid.y)
            {
                // 水平方向：遍历x轴
                int startX = Mathf.Min(_originPos.x, currentGrid.x);
                int endX = Mathf.Max(_originPos.x, currentGrid.x);
                for (int x = startX; x <= endX; x++)
                {
                    Vector2 pos = new Vector2(x + 0.5f, _originPos.y + 0.5f);
                    var sign = Instantiate(buildingSignPrefab, pos, Quaternion.identity);
                    _signs.Add(sign);
                }
            }
        }

        // 鼠标左键抬起后结束建造（也可在此时确认最终建造逻辑）
        if (Input.GetMouseButtonUp(0))
        {
            _isBuilding = false;
            // 此时 _signs 中保存的就是最终预览的所有 sign，
            // 你可以根据需要将其转为正式建筑或进一步处理。
        }
    }
}