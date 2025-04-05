// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Tilemaps;
//
// public enum BlockType
// {
//     Ocean,
//     Plain
// }
//
// public class Chunk : MonoBehaviour
// {
//     public Tilemap tilemap;
//     
//     // 只有一个基础 Tile
//     public TileBase baseTile;
//
//     // --- 噪声参数 ---
//     public float continentScale = 0.001f;
//     public Vector2 continentOffset = new Vector2(1000f, 1000f);
//     public float continentThreshold = 0.5f;
//
//     public float ridgedScale = 0.005f;
//     public float ridgedWeight = 0.10f;
//     public float ridgedOffset = 500f;
//
//     public float warpScale1 = 0.01f;
//     public float warpStrength1 = 20f;
//     public float warpScale2 = 0.05f;
//     public float warpStrength2 = 5f;
//
//     public int seed = 12345;
//
//     public Vector2Int Pos { get; private set; }
//     public Dictionary<Vector2Int, BlockType> Blocks = new Dictionary<Vector2Int, BlockType>();
//
//     /// <summary>
//     /// 生成一个区块 Tilemap
//     /// </summary>
//     public void GenerateChunk(Vector2Int chunkCoord, int chunkSize)
//     {
//         Pos = chunkCoord;
//         // 遍历区块内每个瓷砖
//         for (int y = 0; y < chunkSize; y++)
//         {
//             for (int x = 0; x < chunkSize; x++)
//             {
//                 int worldX = chunkCoord.x * chunkSize + x;
//                 int worldY = chunkCoord.y * chunkSize + y;
//
//                 // ===== 1. 多层领域扭曲（生成采样坐标） =====
//                 float warpX = worldX + ComputeWarp(worldX, worldY, warpScale1, warpStrength1, 123.45f);
//                 warpX += ComputeWarp(worldX, worldY, warpScale2, warpStrength2, 987.65f);
//                 float warpY = worldY + ComputeWarp(worldX, worldY, warpScale1, warpStrength1, 67.89f);
//                 warpY += ComputeWarp(worldX, worldY, warpScale2, warpStrength2, 543.21f);
//
//                 // ===== 2. 大陆噪声采样 =====
//                 float continentSampleX = (warpX + continentOffset.x + seed) * continentScale;
//                 float continentSampleY = (warpY + continentOffset.y + seed) * continentScale;
//                 float baseContinent = Mathf.PerlinNoise(continentSampleX, continentSampleY);
//
//                 // ===== 3. 脊状噪声混合 =====
//                 float ridgedSampleX = (warpX + ridgedOffset) * ridgedScale;
//                 float ridgedSampleY = (warpY + ridgedOffset) * ridgedScale;
//                 float ridged = 1f - Mathf.Abs(Mathf.PerlinNoise(ridgedSampleX, ridgedSampleY) * 2f - 1f);
//                 float combinedContinent = baseContinent - ridged * ridgedWeight;
//
//                 bool isLand = combinedContinent > continentThreshold;
//
//                 Color color = Color.black;
//
//                 if (!isLand)
//                 {
//                     // 水域区域
//                     // 计算离陆地的“距离”
//                     float distanceFromLand = continentThreshold - combinedContinent;
//                     distanceFromLand = Mathf.Max(distanceFromLand, 0f);
//                     // 这里用 0.3f 控制海岸带宽度，可调
//                     distanceFromLand = Mathf.Clamp01(distanceFromLand / 0.3f);
//                     
//                     // 分段设置海洋颜色：近岸、中间、远海
//                     if (distanceFromLand < 0.33f)
//                     {
//                         color = new Color(0.2f, 0.8f, 1f); // 近岸：浅蓝
//                     }
//                     else if (distanceFromLand < 0.66f)
//                     {
//                         color = new Color(0f, 0.5f, 0.9f); // 中间
//                     }
//                     else
//                     {
//                         color = new Color(0f, 0.2f, 0.6f); // 远海：深蓝
//                     }
//                     
//                     Blocks.Add(new Vector2Int(x, y), BlockType.Ocean);
//                 }
//                 else
//                 {
//                     // 陆地区域：简单通过一个细节噪声决定颜色
//                     float detail = Mathf.PerlinNoise((warpX + seed) / 50f, (warpY + seed) / 50f);
//                     // 根据 detail 值区分：这里简单分为沙滩、草地、山地和雪山四种
//                     if (detail < 0.4f)
//                         color = new Color(1f, 0.9f, 0.4f); // 沙滩
//                     else if (detail < 0.6f)
//                         color = new Color(0.4f, 1f, 0.4f); // 草地
//                     else if (detail < 0.8f)
//                         color = new Color(1f, 0.6f, 0.3f); // 山地
//                     else
//                         color = Color.white;               // 雪山
//                     
//                     Blocks.Add(new Vector2Int(x, y), BlockType.Plain);
//                 }
//
//                 // 设置同一个基础 Tile 到该位置
//                 Vector3Int tilePos = new Vector3Int(x, y, 0);
//                 tilemap.SetTile(tilePos, baseTile);
//                 // 用 SetColor 给该瓷砖设置颜色
//                 tilemap.SetColor(tilePos, color);
//             }
//         }
//     }
//
//     // 辅助函数：计算领域扭曲偏移
//     private float ComputeWarp(int x, int y, float scale, float strength, float offset)
//     {
//         float sampleX = (x + seed) * scale;
//         float sampleY = (y + seed) * scale;
//         return (Mathf.PerlinNoise(sampleX + offset, sampleY + offset) * 2f - 1f) * strength;
//     }
// }