using System;
using System.IO;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using MdxLib.Model;
using MdxLib.ModelFormats;

[ScriptedImporter(1, new[] { "mdx", "mdl" })]
public class MdxImporter : ScriptedImporter
{
    public bool importMaterials = true;

    public override void OnImportAsset( AssetImportContext context )
    {
        Import(context);
    }

    private void Import( AssetImportContext context )
    {
        CModel cmodel;
        try
        {
            cmodel = new CModel();
            using( var stream = new FileStream(context.assetPath, FileMode.Open, FileAccess.Read) )
            {
                string extension = Path.GetExtension(context.assetPath);
                if( extension.Equals(".mdx") )
                {
                    CMdx cmdx = new CMdx();
                    cmdx.Load(context.assetPath, stream, cmodel);
                }
                else if( extension.Equals(".mdl") )
                {
                    CMdl cmdl = new CMdl();
                    cmdl.Load(context.assetPath, stream, cmodel);
                }
            }
        }
        catch( Exception e )
        {
            Debug.LogError(e.Message);
            return;
        }

        // GameObject.
        GameObject gameObject = new GameObject();
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        context.AddObjectToAsset("prefab", gameObject);
        context.SetMainObject(gameObject);

        // Mesh.
        Mesh mesh = ImportMesh(cmodel);
        meshFilter.sharedMesh = mesh;
        context.AddObjectToAsset("mesh", mesh);

        // Materials.
        if( importMaterials )
        {
            Material[] materials = ImportMaterials(cmodel);

            Material[] mats = new Material[cmodel.Geosets.Count];
            for( int i = 0; i < cmodel.Geosets.Count; i++ )
            {
                CGeoset cgeoset = cmodel.Geosets.Get(i);

                for( int j = 0; j < cmodel.Materials.Count; j++ )
                {
                    CMaterial cmaterial = cmodel.Materials.Get(j);

                    if( cgeoset.Material.Object.ObjectId == cmaterial.ObjectId )
                    {
                        mats[i] = materials[j];
                    }
                }
            }
            meshRenderer.materials = mats;

            for( int i = 0; i < materials.Length; i++ )
            {
                context.AddObjectToAsset(i.ToString(), materials[i]);
            }
        }
    }

    private Mesh ImportMesh( CModel cmodel )
    {
        Mesh mesh = new Mesh();
        mesh.name = cmodel.Name;

        if ( !cmodel.HasGeosets )
        {
            return mesh;
        }

        // For each geoset.
        CombineInstance[] combine = new CombineInstance[cmodel.Geosets.Count];
        for( int i = 0; i < cmodel.Geosets.Count; i++ )
        {
            CGeoset cgeoset = cmodel.Geosets.Get(i);
            Mesh submesh = new Mesh();

            // Vertices.
            Vector3[] vertices = new Vector3[cgeoset.Vertices.Count];
            for( int j = 0; j < cgeoset.Vertices.Count; j++ )
            {
                // MDX/MDL up axis is Z.
                // Unity up axis is Y.
                CGeosetVertex vertex = cgeoset.Vertices.Get(j);
                vertices[j] = new Vector3(vertex.Position.X, vertex.Position.Z, vertex.Position.Y);
            }

            // Triangles.
            int[] triangles = new int[cgeoset.Faces.Count * 3];
            int t = 0;
            for( int j = 0; j < cgeoset.Faces.Count; j++ )
            {
                // MDX/MDL coordinate system is anti-clockwise.
                // Unity coordinate system is clockwise.
                CGeosetFace face = cgeoset.Faces.Get(j);
                triangles[t] = face.Vertex1.ObjectId; t++;
                triangles[t] = face.Vertex3.ObjectId; t++; // Swap the order of the vertex 2 and 3.
                triangles[t] = face.Vertex2.ObjectId; t++;
            }

            // Normals.
            Vector3[] normals = new Vector3[cgeoset.Vertices.Count];
            for( int j = 0; j < cgeoset.Vertices.Count; j++ )
            {
                // MDX/MDL up axis is Z.
                // Unity up axis is Y.
                CGeosetVertex vertex = cgeoset.Vertices.Get(j);
                normals[j] = new Vector3(vertex.Normal.X, vertex.Normal.Z, vertex.Normal.Y);
            }

            // UVs.
            Vector2[] uvs = new Vector2[cgeoset.Vertices.Count];
            for( int j = 0; j < cgeoset.Vertices.Count; j++ )
            {
                // MDX/MDL texture coordinate origin is at top left.
                // Unity texture coordinate origin is at bottom left.
                CGeosetVertex vertex = cgeoset.Vertices.Get(j);
                uvs[j] = new Vector2(vertex.TexturePosition.X, Mathf.Abs(vertex.TexturePosition.Y - 1)); // Vunity = abs(Vmdx - 1)
            }

            submesh.vertices = vertices;
            submesh.triangles = triangles;
            submesh.normals = normals;
            submesh.uv = uvs;
            combine[i].mesh = submesh;
            combine[i].transform = Matrix4x4.identity;
        }

        // Combine the submeshes.
        mesh.CombineMeshes(combine, false);

        return mesh;
    }

    private Material[] ImportMaterials( CModel cmodel )
    {
        Material[] materials = null;

        if( !cmodel.HasMaterials )
        {
            return materials;
        }

        // For each material.
        materials = new Material[cmodel.Materials.Count];
        for( int i = 0; i < cmodel.Materials.Count; i++ )
        {
            CMaterial cmaterial = cmodel.Materials.Get(i);
            Material material = new Material(Shader.Find("MDX/Unlit"));
            material.name = i.ToString();

            if( cmaterial.HasLayers )
            {
                // For each layer.
                for( int j = 0; j < cmaterial.Layers.Count; j++ )
                {
                    CMaterialLayer clayer = cmaterial.Layers[j];

                    // Two Sided.
                    if( clayer.TwoSided )
                    {
                        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    }

                    // Team color.
                    if( clayer.Texture.Object.ReplaceableId > 0 )
                    {
                        material.SetInt("_AlphaMode", 2); // TeamColor
                    }
                }
            }

            materials[i] = material;
        }

        return materials;
    }
}