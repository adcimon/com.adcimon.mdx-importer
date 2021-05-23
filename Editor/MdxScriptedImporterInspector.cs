using UnityEditor;
using UnityEditor.AssetImporters;

[CustomEditor(typeof(MdxScriptedImporter))]
public class MdxScriptedImporterInspector : ScriptedImporterEditor
{
    // General.
    private SerializedProperty excludeGeosets;
    private SerializedProperty excludeTextures;

    // Materials
    private SerializedProperty importMaterials;
    private SerializedProperty addMaterialsToAsset;

    // Animations.
    private SerializedProperty importAnimations;
    private SerializedProperty addAnimationsToAsset;
    private SerializedProperty importTangents;
    private SerializedProperty frameRate;
    private SerializedProperty excludeAnimations;

    public override void OnEnable()
    {
        // General.
        excludeGeosets = serializedObject.FindProperty("excludeGeosets");
        excludeTextures = serializedObject.FindProperty("excludeTextures");

        // Materials.
        importMaterials = serializedObject.FindProperty("importMaterials");
        addMaterialsToAsset = serializedObject.FindProperty("addMaterialsToAsset");

        // Animations.
        importAnimations = serializedObject.FindProperty("importAnimations");
        addAnimationsToAsset = serializedObject.FindProperty("addAnimationsToAsset");
        importTangents = serializedObject.FindProperty("importTangents");
        frameRate = serializedObject.FindProperty("frameRate");
        excludeAnimations = serializedObject.FindProperty("excludeAnimations");

        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // General.
        CustomGUILayout.Title("General");
        EditorGUILayout.PropertyField(excludeGeosets, true);
        EditorGUILayout.PropertyField(excludeTextures, true);
        EditorGUILayout.HelpBox("Geosets and materials that contains any excluded texture won't be imported.", MessageType.Warning);
        EditorGUILayout.Space(10);

        // Materials.
        importMaterials.boolValue = CustomGUILayout.Toggle("Materials", importMaterials.boolValue);
        if( importMaterials.boolValue )
        {
            addMaterialsToAsset.boolValue = EditorGUILayout.Toggle("Add Materials to Asset", addMaterialsToAsset.boolValue);
        }
        EditorGUILayout.Space(20);

        // Animations.
        importAnimations.boolValue = CustomGUILayout.Toggle("Animations", importAnimations.boolValue);
        if( importAnimations.boolValue )
        {
            addAnimationsToAsset.boolValue = EditorGUILayout.Toggle("Add Animations to Asset", addAnimationsToAsset.boolValue);
            importTangents.boolValue = EditorGUILayout.Toggle("Import Tangents", importTangents.boolValue);
            frameRate.floatValue = EditorGUILayout.Slider("Frame Rate", frameRate.floatValue, 480, 1920);
            EditorGUILayout.PropertyField(excludeAnimations, true);
        }

        serializedObject.ApplyModifiedProperties();
        ApplyRevertGUI();
    }
}