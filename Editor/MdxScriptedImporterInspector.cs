using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

[CustomEditor(typeof(MdxScriptedImporter))]
public class MdxScriptedImporterInspector : ScriptedImporterEditor
{
    private SerializedProperty discardTextures;

    private SerializedProperty importMaterials;
    private SerializedProperty addMaterialsToAsset;

    private SerializedProperty importAnimations;
    private SerializedProperty addAnimationsToAsset;
    private SerializedProperty importTangents;
    private SerializedProperty frameRate;

    public override void OnEnable()
    {
        discardTextures = serializedObject.FindProperty("discardTextures");

        importMaterials = serializedObject.FindProperty("importMaterials");
        addMaterialsToAsset = serializedObject.FindProperty("addMaterialsToAsset");

        importAnimations = serializedObject.FindProperty("importAnimations");
        addAnimationsToAsset = serializedObject.FindProperty("addAnimationsToAsset");
        importTangents = serializedObject.FindProperty("importTangents");
        frameRate = serializedObject.FindProperty("frameRate");

        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Header.
        EditorGUILayout.HelpBox("Mdx Importer", MessageType.Info);
        EditorGUILayout.Space(10);

        // Geosets.
        GUILayout.Label("Geosets");
        EditorGUILayout.PropertyField(discardTextures, true);
        EditorGUILayout.Space(10);

        // Materials.
        EditorGUILayout.BeginHorizontal();
        {
            importMaterials.boolValue = EditorGUILayout.Toggle(importMaterials.boolValue, GUILayout.Width(15));
            GUILayout.Label("Materials");
        }
        EditorGUILayout.EndHorizontal();
        if( importMaterials.boolValue )
        {
            addMaterialsToAsset.boolValue = EditorGUILayout.Toggle("Add Materials to Asset", addMaterialsToAsset.boolValue);
        }
        EditorGUILayout.Space(20);

        // Animations.
        EditorGUILayout.BeginHorizontal();
        {
            importAnimations.boolValue = EditorGUILayout.Toggle(importAnimations.boolValue, GUILayout.Width(15));
            GUILayout.Label("Animations");
        }
        EditorGUILayout.EndHorizontal();
        if( importAnimations.boolValue )
        {
            addAnimationsToAsset.boolValue = EditorGUILayout.Toggle("Add Animations to Asset", addAnimationsToAsset.boolValue);
            importTangents.boolValue = EditorGUILayout.Toggle("Import Tangents", importTangents.boolValue);
            frameRate.floatValue = EditorGUILayout.Slider("Frame Rate", frameRate.floatValue, 480, 1920);
        }

        serializedObject.ApplyModifiedProperties();
        ApplyRevertGUI();
    }
}