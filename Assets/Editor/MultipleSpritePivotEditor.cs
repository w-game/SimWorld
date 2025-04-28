using UnityEditor;
using UnityEngine;

public class MultipleSpritePivotWindow : EditorWindow
{
    Vector2 _pivot = Vector2.one * 0.5f;
    Texture2D _selectedTex;

    [MenuItem("Tools/Set Multiple Sprite Pivot...")]
    public static void Open() => GetWindow<MultipleSpritePivotWindow>("Set Sprite Pivot");

    void OnGUI()
    {
        GUILayout.Label("设置 Multiple 模式 Sprite 的所有子图 Pivot", EditorStyles.boldLabel);
        _selectedTex = EditorGUILayout.ObjectField("目标贴图", _selectedTex, typeof(Texture2D), false) as Texture2D;
        _pivot = EditorGUILayout.Vector2Field("Pivot (0~1)", _pivot);

        if (_selectedTex == null)
        {
            EditorGUILayout.HelpBox("请先选择一个 Texture2D 资源。", MessageType.Info);
            return;
        }

        if (GUILayout.Button("应用 Pivot"))
        {
            string path = AssetDatabase.GetAssetPath(_selectedTex);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null || ti.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.LogError("选中的不是 Multiple 模式的贴图");
                return;
            }

            var metas = ti.spritesheet;
            for (int i = 0; i < metas.Length; i++)
            {
                metas[i].alignment = (int)SpriteAlignment.Custom;
                // Convert normalized pivot (0~1) to pixel coordinates based on each sprite's size
                Vector2 spriteSize = metas[i].rect.size;
                metas[i].pivot = _pivot;
            }
            ti.spritesheet = metas;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            Debug.Log($"已将 {path} 中 {metas.Length} 个子 Sprite 的 pivot 设为 {_pivot}");
        }
    }
}