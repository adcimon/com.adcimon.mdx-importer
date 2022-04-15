using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, new[] { "mdx", "mdl" })]
public class MdxScriptedImporter : ScriptedImporter
{
    // General.
    public bool importAttachments = false;
    public bool importEvents = false;
    public bool importParticleEmitters = false;
    public bool importCollisionShapes = false;
    public List<int> excludeGeosets = new List<int>();
    public List<string> excludeByTexture = new List<string>() { "gutz.blp" };

    // Materials.
    public bool importMaterials = true;
    public bool addMaterialsToAsset = true;

    // Animations.
    public bool importAnimations = true;
    public bool addAnimationsToAsset = true;
    public bool importTangents = true;
    public float frameRate = 960;
    public List<string> excludeAnimations = new List<string>() { "Decay Bone", "Decay Flesh" };

    public override void OnImportAsset( AssetImportContext context )
    {
        string directoryPath = Path.GetDirectoryName(context.assetPath).Replace('\\', '/');

        MdxModel model = new MdxModel();
        MdxImportSettings settings = new MdxImportSettings()
        {
            importAttachments = importAttachments,
            importEvents = importEvents,
            importParticleEmitters = importParticleEmitters,
            importCollisionShapes = importCollisionShapes,
            excludeGeosets = excludeGeosets,
            excludeByTexture = excludeByTexture,
            importMaterials = importMaterials,
            importAnimations = importAnimations,
            importTangents = importTangents,
            frameRate = frameRate,
            excludeAnimations = excludeAnimations
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
                    context.AddObjectToAsset(material.name, material);
                }
                else
                {
                    string directory = directoryPath + "/Materials/";
                    Directory.CreateDirectory(directory);

                    AssetDatabase.CreateAsset(material, directory + material.name + ".mat");
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