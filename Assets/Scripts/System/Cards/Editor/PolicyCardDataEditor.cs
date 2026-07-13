using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PolicyCardData))]
public class PolicyCardDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 畫出原本的 Inspector 屬性
        DrawDefaultInspector();

        PolicyCardData card = (PolicyCardData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("新增效果積木 (Add Strategy Effects)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("因為 Unity 預設的陣列 + 號無法選擇介面 (Interface) 的實作型別，請使用下方的按鈕來新增積木。", MessageType.Info);

        GUILayout.BeginHorizontal();

        // 按鈕 1: 新增數值修改積木
        if (GUILayout.Button("＋ 新增 數值修改\n(Stat Modifier)", GUILayout.Height(40)))
        {
            Undo.RecordObject(card, "Add StatModifierEffect");
            card.Effects.Add(new StatModifierEffect());
            EditorUtility.SetDirty(card);
        }

        // 按鈕 2: 新增生成物件積木
        if (GUILayout.Button("＋ 新增 物件生成\n(Spawn Object)", GUILayout.Height(40)))
        {
            Undo.RecordObject(card, "Add SpawnObjectEffect");
            card.Effects.Add(new SpawnObjectEffect());
            EditorUtility.SetDirty(card);
        }

        GUILayout.EndHorizontal();
        
        // 加入清除空積木的功能 (防呆)
        if (card.Effects.Contains(null))
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("清理無效/空的積木 (Clear Nulls)", GUILayout.Height(30)))
            {
                Undo.RecordObject(card, "Clear Null Effects");
                card.Effects.RemoveAll(e => e == null);
                EditorUtility.SetDirty(card);
            }
        }
    }
}
