using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class RoomEditorWindow : EditorWindow
{
    private int width = 9, height = 7;
    private int newWidth = 9, newHeight = 7;
    private int[,] layout;
    private List<FurnitureData> furnitures = new();

    private Vector2 scroll;
    private int currentTile = 1; // 默认地板
    private string roomType = "House";
    private string roomName = "Room";
    private bool gridInitialized = false;

    // 新增：当前选择的房间索引和已加载的Wrapper
    private int selectedRoomIndex = 0;
    private RoomWrapper loadedWrapper;

    [MenuItem("Tools/Room Editor")]
    public static void ShowWindow() => GetWindow<RoomEditorWindow>("Room Editor");

    private void OnEnable()
    {
        gridInitialized = false;
        newWidth = width;
        newHeight = height;

        string path = "Assets/Resources/Configs/RoomConfig.json";
        if (File.Exists(path))
        {
            loadedWrapper = JsonUtility.FromJson<RoomWrapper>(File.ReadAllText(path));
            if (loadedWrapper.items.Count > 0)
                LoadRoom(0); // 默认加载第一个
        }
        else
        {
            loadedWrapper = new RoomWrapper();
        }
    }

    private void OnGUI()
    {
        // 顶部房间选择器
        if (loadedWrapper != null && loadedWrapper.items.Count > 0)
        {
            string[] options = loadedWrapper.items
                .ConvertAll((r) => $"[{loadedWrapper.items.IndexOf(r)}] {r.name} ({r.type})")
                .ToArray();
            int newIndex = EditorGUILayout.Popup("Select Room", selectedRoomIndex, options);
            if (newIndex != selectedRoomIndex)
            {
                LoadRoom(newIndex);
            }
        }
        // "New Room" 按钮
        if (GUILayout.Button("New Room"))
        {
            roomType = "NewRoom";
            width = newWidth = 9;
            height = newHeight = 7;
            layout = new int[width, height];
            furnitures = new List<FurnitureData>();
            selectedRoomIndex = -1;
            gridInitialized = true;
        }
        // 刷新按钮，放在房间选择器下方或“New Room”按钮之后
        if (GUILayout.Button("Refresh Configs"))
        {
            string path = "Assets/Resources/Configs/RoomConfig.json";
            if (File.Exists(path))
            {
                loadedWrapper = JsonUtility.FromJson<RoomWrapper>(File.ReadAllText(path));
                if (loadedWrapper.items.Count > 0)
                    LoadRoom(0);
                else
                    gridInitialized = false;
            }
            else
            {
                loadedWrapper = new RoomWrapper();
                gridInitialized = false;
            }
        }

        EditorGUILayout.LabelField("Room Type");
        roomType = EditorGUILayout.TextField(roomType);
        EditorGUILayout.LabelField("Room Name");
        roomName = EditorGUILayout.TextField(roomName);

        newWidth = EditorGUILayout.IntField("Width", newWidth);
        newHeight = EditorGUILayout.IntField("Height", newHeight);

        if (GUILayout.Button("Resize Grid"))
        {
            width = newWidth;
            height = newHeight;
            int[,] newLayout = new int[width, height];
            if (layout != null)
            {
                int minWidth = Mathf.Min(width, layout.GetLength(0));
                int minHeight = Mathf.Min(height, layout.GetLength(1));
                for (int y = 0; y < minHeight; y++)
                    for (int x = 0; x < minWidth; x++)
                        newLayout[x, y] = layout[x, y];
            }
            layout = newLayout;
            gridInitialized = true;
        }

        EditorGUILayout.Space();
        currentTile = EditorGUILayout.IntField("Current Tile Type (0=墙, 1=地板, 2=门...)", currentTile);

        // 坐标轴横向标注
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(30)); // 左上角空格
        for (int x = 0; x < width; x++)
            EditorGUILayout.LabelField(x.ToString(), GUILayout.Width(30));
        EditorGUILayout.EndHorizontal();

        if (gridInitialized)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int y = 0; y < height; y++)
            {
                EditorGUILayout.BeginHorizontal();
                int labelY = height - 1 - y;
                EditorGUILayout.LabelField(labelY.ToString(), GUILayout.Width(30)); // 正确的Y坐标
                for (int x = 0; x < width; x++)
                {
                    GUIStyle style = new GUIStyle(GUI.skin.button);
                    style.normal.textColor = Color.white;
                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = layout[x, y] == 0 ? Color.gray :
                                          layout[x, y] == 1 ? Color.green :
                                          layout[x, y] == 2 ? Color.yellow : Color.white;

                    string label = layout[x, y].ToString();
                    int displayY = height - 1 - y;
                    if (furnitures.Exists(f => f.pos.x == x && f.pos.y == displayY))
                        label += "F";
                    if (GUILayout.Button(label, style, GUILayout.Width(30), GUILayout.Height(30)))
                    {
                        layout[x, y] = currentTile;
                    }
                    GUI.backgroundColor = oldColor;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("Click 'Resize Grid' to initialize the layout.", MessageType.Info);
        }

        if (GUILayout.Button("Add Furniture"))
        {
            furnitures.Add(new FurnitureData { id = "BUILDING_BED", pos = new Vector2Int(0, 0) });
        }

        EditorGUILayout.LabelField("Furnitures:");
        for (int i = 0; i < furnitures.Count; i++)
        {
            var f = furnitures[i];
            EditorGUILayout.BeginHorizontal();
            f.id = EditorGUILayout.TextField(f.id);
            f.pos = EditorGUILayout.Vector2IntField("", f.pos);
            if (GUILayout.Button("X", GUILayout.Width(20)))
                furnitures.RemoveAt(i);
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Export to RoomConfig.json"))
        {
            ExportJson();
        }

        // 删除当前选中房间按钮
        if (GUILayout.Button("Delete Selected Room") && selectedRoomIndex >= 0)
        {
            if (EditorUtility.DisplayDialog("Confirm Delete", $"Are you sure you want to delete room #{selectedRoomIndex}?", "Yes", "No"))
            {
                loadedWrapper.items.RemoveAt(selectedRoomIndex);
                selectedRoomIndex = 0;
                if (loadedWrapper.items.Count > 0)
                    LoadRoom(0);
                else
                    gridInitialized = false;
                // 同步写回文件
                string path = "Assets/Resources/Configs/RoomConfig.json";
                File.WriteAllText(path, JsonUtility.ToJson(loadedWrapper, true));
                AssetDatabase.Refresh();
            }
        }
    }

    // 新增：加载已有房间
    private void LoadRoom(int index)
    {
        if (loadedWrapper == null || index < 0 || index >= loadedWrapper.items.Count)
            return;

        var item = loadedWrapper.items[index];
        selectedRoomIndex = index;
        roomType = item.type;
        roomName = item.name;
        width = item.width;
        height = item.height;
        newWidth = width;
        newHeight = height;
        layout = new int[width, height];
        // 还原 layout 时，保持 (0,0) 为左下角，兼容 Unity 坐标系
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                layout[x, y] = item.layout[(height - 1 - y) * width + x];
        furnitures = new List<FurnitureData>(item.furnitures);
        // 不修改 layout，用于在绘制阶段显示叠加标记
        gridInitialized = true;
    }

    private void ExportJson()
    {
        string path = "Assets/Resources/Configs/RoomConfig.json";

        // 移除导出时将家具信息写入 layout 的逻辑

        RoomItem item = new()
        {
            name = roomName,
            type = roomType,
            width = width,
            height = height,
            layout = Flatten(layout),
            furnitures = furnitures
        };

        RoomWrapper wrapper;
        if (File.Exists(path))
        {
            wrapper = JsonUtility.FromJson<RoomWrapper>(File.ReadAllText(path));
            if (selectedRoomIndex >= 0 && selectedRoomIndex < wrapper.items.Count)
                wrapper.items[selectedRoomIndex] = item;
            else
                wrapper.items.Add(item);
        }
        else
        {
            wrapper = new RoomWrapper { items = new List<RoomItem> { item } };
        }

        File.WriteAllText(path, JsonUtility.ToJson(wrapper, true));
        AssetDatabase.Refresh();
        Debug.Log("Room config exported.");

        // 调试导出layout为文本
        ExportDebugLayout(layout, "Assets/Resources/Configs/RoomLayoutDebug.txt");
    }

    // 调试导出函数：将layout输出为易读二维文本
    private void ExportDebugLayout(int[,] layout, string debugPath)
    {
        using StreamWriter writer = new StreamWriter(debugPath);
        for (int y = height - 1; y >= 0; y--) // 从上往下打印
        {
            string line = "";
            for (int x = 0; x < width; x++)
            {
                line += layout[x, y].ToString().PadLeft(2) + " ";
            }
            writer.WriteLine(line);
        }
    }

    private List<int> Flatten(int[,] grid)
    {
        var list = new List<int>();
        if (grid == null) return list;
        int gridWidth = grid.GetLength(0);
        int gridHeight = grid.GetLength(1);
        // 导出 layout 时，保持 (0,0) 为左下角，行序为从下到上
        for (int y = 0; y < gridHeight; y++)
            for (int x = 0; x < gridWidth; x++)
                list.Add(grid[x, gridHeight - 1 - y]);
        return list;
    }

    [System.Serializable]
    public class RoomWrapper
    {
        public List<RoomItem> items = new();
    }

    [System.Serializable]
    public class RoomItem
    {
        public string name;
        public string type;
        public int width;
        public int height;
        public List<int> layout;
        public List<FurnitureData> furnitures;
    }

    [System.Serializable]
    public class FurnitureData
    {
        public string id;
        public Vector2Int pos;
    }
}