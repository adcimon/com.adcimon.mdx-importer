using UnityEngine;
using UnityEditor;

public static class CustomGUILayout
{
    public static void Title( string label )
    {
        GUIStyle style = new GUIStyle("ShurikenModuleTitle");
        style.font = new GUIStyle(EditorStyles.label).font;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 22;
        style.contentOffset = new Vector2(20, -2);

        GUILayout.BeginHorizontal(style);
        {
            GUILayout.Label(label);
        }
        GUILayout.EndHorizontal();
    }

    public static bool Foldout( string title, bool display )
    {
        GUIStyle style = new GUIStyle("ShurikenModuleTitle");
        style.font = new GUIStyle(EditorStyles.label).font;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 22;
        style.contentOffset = new Vector2(20, -2);

        Rect rect = GUILayoutUtility.GetRect(16, 22, style);
        GUI.Box(rect, title, style);

        Event e = Event.current;

        Rect toggleRect = new Rect(rect.x + 4, rect.y + 2, 13, 13);
        if( e.type == EventType.Repaint )
        {
            EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
        }

        if( e.type == EventType.MouseDown && rect.Contains(e.mousePosition) )
        {
            display = !display;
            e.Use();
        }

        return display;
    }

    public static bool Toggle( string label, bool value )
    {
        GUIStyle style = new GUIStyle("ShurikenModuleTitle");
        style.font = new GUIStyle(EditorStyles.label).font;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 22;
        style.contentOffset = new Vector2(20, -2);

        GUILayout.BeginHorizontal(style);
        {
            value = EditorGUILayout.Toggle(value, GUILayout.Width(15));
            GUILayout.Label(label);
        }
        GUILayout.EndHorizontal();

        return value;
    }
}