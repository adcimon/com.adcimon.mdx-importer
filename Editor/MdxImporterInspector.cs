using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

[CustomEditor(typeof(MdxImporter))]
public class MdxImporterInspector : ScriptedImporterEditor
{
    private SerializedProperty importMaterials;

    public override void OnEnable()
    {
        importMaterials = serializedObject.FindProperty("importMaterials");
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        importMaterials.boolValue = EditorGUILayout.Toggle("Import Materials", importMaterials.boolValue);
        serializedObject.ApplyModifiedProperties();
        ApplyRevertGUI();
    }
}