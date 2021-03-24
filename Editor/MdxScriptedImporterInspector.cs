using UnityEditor;
using UnityEditor.AssetImporters;

[CustomEditor(typeof(MdxScriptedImporter))]
public class MdxScriptedImporterInspector : ScriptedImporterEditor
{
    private SerializedProperty importMaterials;
    private SerializedProperty importAnimations;
    private SerializedProperty importTangents;
    private SerializedProperty frameRate;

    public override void OnEnable()
    {
        importMaterials = serializedObject.FindProperty("importMaterials");
        importAnimations = serializedObject.FindProperty("importAnimations");
        importTangents = serializedObject.FindProperty("importTangents");
        frameRate = serializedObject.FindProperty("frameRate");
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        importMaterials.boolValue = EditorGUILayout.Toggle("Import Materials", importMaterials.boolValue);
        importAnimations.boolValue = EditorGUILayout.Toggle("Import Animations", importAnimations.boolValue);
        importTangents.boolValue = EditorGUILayout.Toggle("Import Tangents", importTangents.boolValue);
        frameRate.floatValue = EditorGUILayout.Slider("Frame Rate", frameRate.floatValue, 480, 1920);
        serializedObject.ApplyModifiedProperties();
        ApplyRevertGUI();
    }
}