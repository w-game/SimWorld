using System;
using System.Collections.Generic;
using Map;
using UnityEngine;

public class MapManager : MonoSingleton<MapManager>
{
    public Transform player;          // 玩家对象

    private void Start()
    {

    }

    void Update()
    {

    }


    public BlockType CheckClickOnMap(out Vector3 mouseWorldPos)
    {
        mouseWorldPos = default;
        // 将鼠标屏幕坐标转换为世界坐标
        // mouseWorldPos = UIManager.I.mainCamera.ScreenToWorldPoint(Input.mousePosition);
        // mouseWorldPos.z = 0;

        // // 发射射线，注意方向为 Vector2.zero 表示点检测
        // RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
        // if (hit.collider != null)
        // {
        //     Chunk chunk = hit.collider.GetComponent<Chunk>();
        //     if (chunk != null)
        //     {
        //         Vector3Int tilePos = chunk.tilemap.WorldToCell(mouseWorldPos);
        //         Debug.Log("点击的 Tile 坐标: " + tilePos);
        //         return chunk.Blocks[new Vector2Int(tilePos.x, tilePos.y)];
        //     }
        //     Debug.Log("点击到的对象没有 Tilemap 组件");
        // }

        // return BlockType.Ocean;
        return BlockType.Ocean;
    }
}