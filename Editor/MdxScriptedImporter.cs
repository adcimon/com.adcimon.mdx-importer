using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

[ScriptedImporter(1, new[] { "mdx", "mdl" })]
public class MdxScriptedImporter : ScriptedImporter
{
    public bool importMaterials = true;
    public bool importAnimations = true;
    public float frameRate = 960;

    public override void OnImportAsset( AssetImportContext context )
    {
        MdxModel model = new MdxModel();
        model.Import(context.assetPath, importMaterials, importAnimations, frameRate);

        context.AddObjectToAsset("prefab", model.gameObject);
        context.SetMainObject(model.gameObject);
        context.AddObjectToAsset("mesh", model.mesh);

        if( importMaterials )
        {
            for( int i = 0; i < model.materials.Count; i++ )
            {
                Material material = model.materials[i];
                context.AddObjectToAsset(i.ToString(), material);
            }
        }

        if( importAnimations )
        {
            foreach( AnimationClip clip in model.clips )
            {
                context.AddObjectToAsset(clip.name, clip);
            }
        }
    }
}