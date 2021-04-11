using UnityEditor;
using UnityEditor.AssetImporters;

[CustomEditor(typeof(MdxScriptedImporter))]
public class MdxScriptedImporterInspector : ScriptedImporterEditor
{
    // General.
    private SerializedProperty discardTextures;

    // Materials
    private SerializedProperty importMaterials;
    private SerializedProperty addMaterialsToAsset;

    // Animations.
    private SerializedProperty importAnimations;
    private SerializedProperty addAnimationsToAsset;
    private SerializedProperty importTangents;
    private SerializedProperty frameRate;
    private SerializedProperty discardAnimations;

    public override void OnEnable()
    {
        // General.
        discardTextures = serializedObject.FindProperty("discardTextures");

        // Materials.
        importMaterials = serializedObject.FindProperty("importMaterials");
        addMaterialsToAsset = serializedObject.FindProperty("addMaterialsToAsset");

        // Animations.
        importAnimations = serializedObject.FindProperty("importAnimations");
        addAnimationsToAsset = serializedObject.FindProperty("addAnimationsToAsset");
        importTangents = serializedObject.FindProperty("importTangents");
        frameRate = serializedObject.FindProperty("frameRate");
        discardAnimations = serializedObject.FindProperty("discardAnimations");

        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // General.
        CustomGUILayout.Title("General");
        EditorGUILayout.PropertyField(discardTextures, true);
        EditorGUILayout.HelpBox("Geosets and materials that contains any discarded texture will be discarded too.", MessageType.Warning);
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
            EditorGUILayout.PropertyField(discardAnimations, true);
        }

        serializedObject.ApplyModifiedProperties();
        ApplyRevertGUI();
    }
}