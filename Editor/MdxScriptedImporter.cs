using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, new[] { "mdx", "mdl" })]
public class MdxScriptedImporter : ScriptedImporter
{
    // Geosets.
    public List<string> discardTextures = new List<string>() { "gutz" };

    // Materials.
    public bool importMaterials = true;
    public bool addMaterialsToAsset = true;

    // Animations.
    public bool importAnimations = true;
    public bool addAnimationsToAsset = true;
    public bool importTangents = true;
    public float frameRate = 960;
    public List<string> discardAnimations = new List<string>() { "Decay Bone" };

    public override void OnImportAsset( AssetImportContext context )
    {
        string directoryPath = Path.GetDirectoryName(context.assetPath).Replace('\\', '/');

        MdxModel model = new MdxModel();
        MdxImportSettings settings = new MdxImportSettings()
        {
            discardTextures = discardTextures,
            importMaterials = importMaterials,
            importAnimations = importAnimations,
            importTangents = importTangents,
            frameRate = frameRate,
            discardAnimations = discardAnimations
        };
        model.Import(context.assetPath, settings);

        context.AddObjectToAsset("prefab", model.gameObject);
        context.SetMainObject(model.gameObject);
        context.AddObjectToAsset("mesh", model.mesh);

        if( importMaterials )
        {
            for( int i = 0; i < model.materials.Count; i++ )
            {
                Material material = model.materials[i];
                if( addMaterialsToAsset )
                {
                    context.AddObjectToAsset(i.ToString(), material);
                }
                else
                {
                    string directory = directoryPath + "/Materials/";
                    Directory.CreateDirectory(directory);

                    AssetDatabase.CreateAsset(material, directory + i.ToString() + ".mat");
                    AssetDatabase.SaveAssets();
                }
            }
        }

        if( importAnimations )
        {
            foreach( AnimationClip clip in model.clips )
            {
                if( addAnimationsToAsset )
                {
                    context.AddObjectToAsset(clip.name, clip);
                }
                else
                {
                    string directory = directoryPath + "/Animations/";
                    Directory.CreateDirectory(directory);

                    AssetDatabase.CreateAsset(clip, directory + clip.name + ".anim");
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}