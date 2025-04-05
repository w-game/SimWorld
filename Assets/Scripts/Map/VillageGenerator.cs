// using System.Collections.Generic;
// using UnityEngine;
//
// public class House {
//     public Vector2Int pos; // 房屋左下角坐标
//     public int width;
//     public int height;
//     
//     public Vector2Int Center {
//         get {
//             return new Vector2Int(pos.x + width / 2, pos.y + height / 2);
//         }
//     }
//     
//     // 检测两个房屋是否重叠，margin 可用于在房屋间预留缓冲区
//     public bool Overlaps(House other, int margin = 1) {
//         return pos.x < other.pos.x + other.width + margin &&
//                pos.x + width + margin > other.pos.x &&
//                pos.y < other.pos.y + other.height + margin &&
//                pos.y + height + margin > other.pos.y;
//     }
// }
//
// public class VillageGenerator : MonoBehaviour {
//     [Header("村庄区域设置")]
//     public int villageAreaWidth = 50;
//     public int villageAreaHeight = 50;
//     public int houseCount = 10;
//     public float minDistanceBetweenHouses = 5f; // 用于泊松圆盘采样
//     
//     [Header("房屋尺寸设置")]
//     public int minWidth = 3, maxWidth = 8;
//     public int minHeight = 3, maxHeight = 8;
//     
//     [Header("预制体引用（可选）")]
//     public GameObject housePrefab;
//     public GameObject roadPrefab;
//     
//     // 内部存储生成的房屋和道路信息
//     private List<House> houses = new List<House>();
//     private List<(Vector2Int start, Vector2Int end)> roads = new List<(Vector2Int, Vector2Int)>();
//
//     void Start() {
//         GenerateVillageNatural();
//         // 此时 houses 和 roads 中已经存有房屋及连接道路的信息
//     }
//
//     /// <summary>
//     /// 使用泊松圆盘采样和 MST 生成自然分布的房屋和道路
//     /// </summary>
//     public void GenerateVillageNatural() {
//         // 1. 先用泊松圆盘采样生成候选点
//         List<Vector2> candidatePoints = PoissonDiskSampling(minDistanceBetweenHouses, villageAreaWidth, villageAreaHeight);
//         // 2. 用 Perlin 噪声过滤候选点，使得部分区域房屋更密集
//         List<Vector2Int> houseCenters = new List<Vector2Int>();
//         foreach (Vector2 pt in candidatePoints) {
//             float noise = Mathf.PerlinNoise(pt.x * 0.1f, pt.y * 0.1f);
//             if (noise > 0.5f) {  // 阈值可调
//                 houseCenters.Add(new Vector2Int(Mathf.RoundToInt(pt.x), Mathf.RoundToInt(pt.y)));
//             }
//             if (houseCenters.Count >= houseCount)
//                 break;
//         }
//         
//         // 3. 根据候选中心生成房屋，随机房屋大小，并检测重叠
//         foreach (Vector2Int center in houseCenters) {
//             bool placed = false;
//             int attempts = 0;
//             while (!placed && attempts < 10) {
//                 attempts++;
//                 int width = Random.Range(minWidth, maxWidth + 1);
//                 int height = Random.Range(minHeight, maxHeight + 1);
//                 // 由中心点反推左下角位置
//                 int posX = center.x - width / 2;
//                 int posY = center.y - height / 2;
//                 // 保证房屋在村庄区域内
//                 if (posX < 0 || posY < 0 || posX + width > villageAreaWidth || posY + height > villageAreaHeight)
//                     continue;
//                 House candidate = new House { pos = new Vector2Int(posX, posY), width = width, height = height };
//                 bool overlap = false;
//                 foreach (House h in houses) {
//                     if (candidate.Overlaps(h))
//                     {
//                         overlap = true;
//                         break;
//                     }
//                 }
//                 if (!overlap) {
//                     houses.Add(candidate);
//                     placed = true;
//                 }
//             }
//         }
//         
//         // 4. 连接房屋中心生成道路网络（采用最小生成树，Prim 算法）
//         GenerateRoadNetwork();
//
//         // 5. 可视化村庄：实例化预制体或者用 Debug 绘制
//         VisualizeVillage();
//     }
//
//     /// <summary>
//     /// 简单实现泊松圆盘采样，返回候选点列表
//     /// </summary>
//     /// <param name="minDist">点与点之间的最小距离</param>
//     /// <param name="width">区域宽度</param>
//     /// <param name="height">区域高度</param>
//     /// <param name="newPointsCount">每个点最多尝试生成的新点数</param>
//     List<Vector2> PoissonDiskSampling(float minDist, int width, int height, int newPointsCount = 30) {
//         List<Vector2> points = new List<Vector2>();
//         List<Vector2> processList = new List<Vector2>();
//         
//         // 从区域内的一个随机点开始
//         Vector2 initialPoint = new Vector2(Random.Range(0, width), Random.Range(0, height));
//         points.Add(initialPoint);
//         processList.Add(initialPoint);
//         
//         while (processList.Count > 0) {
//             int index = Random.Range(0, processList.Count);
//             Vector2 point = processList[index];
//             bool found = false;
//             for (int i = 0; i < newPointsCount; i++) {
//                 float angle = Random.value * Mathf.PI * 2;
//                 float radius = Random.Range(minDist, 2 * minDist);
//                 Vector2 newPoint = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
//                 if (newPoint.x >= 0 && newPoint.x < width && newPoint.y >= 0 && newPoint.y < height) {
//                     bool valid = true;
//                     foreach (Vector2 p in points) {
//                         if (Vector2.Distance(newPoint, p) < minDist) {
//                             valid = false;
//                             break;
//                         }
//                     }
//                     if (valid) {
//                         points.Add(newPoint);
//                         processList.Add(newPoint);
//                         found = true;
//                     }
//                 }
//             }
//             if (!found) {
//                 processList.RemoveAt(index);
//             }
//         }
//         return points;
//     }
//
//     /// <summary>
//     /// 使用 Prim 算法将房屋中心连接起来，生成道路网络
//     /// </summary>
//     void GenerateRoadNetwork() {
//         if (houses.Count == 0) return;
//         List<Vector2Int> nodes = new List<Vector2Int>();
//         foreach (House h in houses) {
//             nodes.Add(h.Center);
//         }
//         
//         List<Vector2Int> connected = new List<Vector2Int>();
//         List<Vector2Int> remaining = new List<Vector2Int>(nodes);
//         connected.Add(remaining[0]);
//         remaining.RemoveAt(0);
//         
//         while (remaining.Count > 0) {
//             float bestDist = float.MaxValue;
//             Vector2Int bestFrom = Vector2Int.zero;
//             Vector2Int bestTo = Vector2Int.zero;
//             foreach (Vector2Int c in connected) {
//                 foreach (Vector2Int r in remaining) {
//                     float dist = Vector2Int.Distance(c, r);
//                     if (dist < bestDist) {
//                         bestDist = dist;
//                         bestFrom = c;
//                         bestTo = r;
//                     }
//                 }
//             }
//             roads.Add((bestFrom, bestTo));
//             connected.Add(bestTo);
//             remaining.Remove(bestTo);
//         }
//     }
//
//     /// <summary>
//     /// 可视化生成的村庄：实例化预制体或用 Debug 绘制（此处使用 Debug 绘制房屋矩形与道路线段）
//     /// </summary>
//     void VisualizeVillage() {
//         // 可视化房屋
//         foreach (House h in houses) {
//             // 如果指定了 housePrefab，则在房屋中心位置生成预制体
//             if (housePrefab != null) {
//                 Vector3 pos = new Vector3(h.Center.x, h.Center.y, 0);
//                 Instantiate(housePrefab, pos, Quaternion.identity);
//             }
//             // 用 Debug 绘制房屋矩形边界（红色）
//             Vector3 bottomLeft = new Vector3(h.pos.x, h.pos.y, 0);
//             Vector3 bottomRight = new Vector3(h.pos.x + h.width, h.pos.y, 0);
//             Vector3 topLeft = new Vector3(h.pos.x, h.pos.y + h.height, 0);
//             Vector3 topRight = new Vector3(h.pos.x + h.width, h.pos.y + h.height, 0);
//             Debug.DrawLine(bottomLeft, bottomRight, Color.red, 100f);
//             Debug.DrawLine(bottomLeft, topLeft, Color.red, 100f);
//             Debug.DrawLine(topLeft, topRight, Color.red, 100f);
//             Debug.DrawLine(bottomRight, topRight, Color.red, 100f);
//         }
//         
//         // 可视化道路：黄色直线连接各房屋中心
//         foreach (var road in roads) {
//             Debug.DrawLine(new Vector3(road.start.x, road.start.y, 0),
//                            new Vector3(road.end.x, road.end.y, 0),
//                            Color.yellow, 100f);
//             // 若 roadPrefab 存在，可在两点间实例化或铺设道路预制体
//         }
//     }
//     
//     // 可选：在编辑器中用 Gizmos 辅助显示
//     private void OnDrawGizmos() {
//         Gizmos.color = Color.red;
//         foreach (House h in houses) {
//             Vector3 pos = new Vector3(h.pos.x, h.pos.y, 0);
//             Vector3 size = new Vector3(h.width, h.height, 0);
//             Gizmos.DrawWireCube(pos + size / 2, size);
//         }
//         Gizmos.color = Color.yellow;
//         foreach (var road in roads) {
//             Gizmos.DrawLine(new Vector3(road.start.x, road.start.y, 0),
//                             new Vector3(road.end.x, road.end.y, 0));
//         }
//     }
// }