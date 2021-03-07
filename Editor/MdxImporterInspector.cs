using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

[CustomEditor(typeof(MdxImporter))]
public class MdxImporterInspector : ScriptedImporterEditor
{
    private SerializedProperty importMaterials;
    private SerializedProperty importAnimations;

    public override void OnEnable()
    {
        importMaterials = serializedObject.FindProperty("importMaterials");
        importAnimations = serializedObject.FindProperty("importAnimations");
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        importMaterials.boolValue = EditorGUILayout.Toggle("Import Materials", importMaterials.boolValue);
        importAnimations.boolValue = EditorGUILayout.Toggle("Import Animations", importAnimations.boolValue);
        serializedObject.ApplyModifiedProperties();
        ApplyRevertGUI();
    }
}