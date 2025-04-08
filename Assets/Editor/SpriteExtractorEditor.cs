using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteExtractorEditor : EditorWindow
{
    // 要提取的 SpriteSheet 纹理
    private Texture2D spriteSheet;
    // 输出文件夹，相对于 Assets 文件夹
    private string outputFolder = "ExtractedSprites";

    [MenuItem("Tools/提取 Sprite 为单独图片")]
    public static void ShowWindow()
    {
        GetWindow(typeof(SpriteExtractorEditor), false, "Sprite 提取器");
    }

    private void OnGUI()
    {
        GUILayout.Label("选择包含多个 Sprite 的图片", EditorStyles.boldLabel);

        // 选择待处理的 SpriteSheet（注意选择的对象必须是 Texture2D 类型）
        spriteSheet = (Texture2D)EditorGUILayout.ObjectField("SpriteSheet 图片", spriteSheet, typeof(Texture2D), false);
        outputFolder = EditorGUILayout.TextField("输出文件夹（相对路径）", outputFolder);

        if (GUILayout.Button("提取并保存 Sprite"))
        {
            if (spriteSheet == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个 SpriteSheet 图片！", "确定");
                return;
            }

            ExtractAndSaveSprites(spriteSheet, outputFolder);
        }
    }

    /// <summary>
    /// 根据 SpriteSheet 中的每个子 Sprite 的信息，提取其对应区域的像素，并另存为 PNG 图片。
    /// </summary>
    /// <param name="texture">SpriteSheet 图片（必须启用可读写，否则无法获取像素数据）</param>
    /// <param name="folderPath">保存图片的文件夹（相对于 Assets 文件夹）</param>
    private void ExtractAndSaveSprites(Texture2D texture, string folderPath)
    {
        // 获取该图片在项目中的路径
        string assetPath = AssetDatabase.GetAssetPath(texture);
        // 加载该图片文件中的所有子资源
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        // 构建输出目录的完整路径
        string outputPath = Path.Combine(Application.dataPath, folderPath);
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        int count = 0;
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                Rect rect = sprite.rect;
                // 创建一个新的 Texture2D，其尺寸为当前 Sprite 的区域尺寸
                Texture2D newTex = new Texture2D((int)rect.width, (int)rect.height, texture.format, false);

                // 获取原始纹理中对应 Sprite 区域的像素数据
                // 注意：sprite.rect 的 x、y 坐标默认以图片左下角为原点
                Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                newTex.SetPixels(pixels);
                newTex.Apply();

                // 编码为 PNG 格式
                byte[] pngData = newTex.EncodeToPNG();
                if (pngData != null)
                {
                    // 拼接输出文件的完整路径，例如：Assets/ExtractedSprites/SpriteName.png
                    string filePath = Path.Combine(outputPath, sprite.name + ".png");
                    File.WriteAllBytes(filePath, pngData);
                    count++;
                }

                // 释放临时纹理
                DestroyImmediate(newTex);
            }
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"成功提取并保存了 {count} 个 Sprite 到:\n{outputPath}", "确定");
    }
}