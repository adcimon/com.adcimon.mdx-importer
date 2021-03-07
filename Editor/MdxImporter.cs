using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using MdxLib.Animator;
using MdxLib.Model;
using MdxLib.ModelFormats;
using MdxLib.Primitives;

[ScriptedImporter(1, new[] { "mdx", "mdl" })]
public class MdxImporter : ScriptedImporter
{
    public bool importMaterials = true;
    public bool importAnimations = true;

    private CModel cmodel;

    private GameObject prefab;
    private MeshFilter filter;
    private SkinnedMeshRenderer renderer;

    private Mesh mesh;
    private Material[] materials;
    private List<AnimationClip> clips = new List<AnimationClip>();

    private GameObject skeleton;
    private SortedDictionary<int, GameObject> bones = new SortedDictionary<int, GameObject>();

    public override void OnImportAsset( AssetImportContext context )
    {
        Import(context);
    }

    private void Import( AssetImportContext context )
    {
        // Read the model file.
        ReadFile(context.assetPath);

        // Create the prefab.
        prefab = new GameObject();
        filter = prefab.AddComponent<MeshFilter>();
        renderer = prefab.AddComponent<SkinnedMeshRenderer>();
        context.AddObjectToAsset("prefab", prefab);
        context.SetMainObject(prefab);

        // Import the mesh.
        ImportMesh();
        filter.sharedMesh = mesh;
        renderer.sharedMesh = mesh;
        context.AddObjectToAsset("mesh", mesh);

        // Import the materials.
        if( importMaterials )
        {
            ImportMaterials();
            for( int i = 0; i < materials.Length; i++ )
            {
                Material material = materials[i];
                context.AddObjectToAsset(i.ToString(), material);
            }
        }

        // Import the skeleton.
        ImportSkeleton();

        // Import the animations.
        if( importAnimations )
        {
            ImportAnimations();
            for( int i = 0; i < clips.Count; i++ )
            {
                AnimationClip animation = clips[i];
                context.AddObjectToAsset(animation.name, animation);
            }
        }
    }

    private void ReadFile( string path )
    {
        try
        {
            cmodel = new CModel();
            using( FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read) )
            {
                string extension = Path.GetExtension(path);
                if( extension.Equals(".mdx") )
                {
                    CMdx cmdx = new CMdx();
                    cmdx.Load(path, stream, cmodel);
                }
                else if( extension.Equals(".mdl") )
                {
                    CMdl cmdl = new CMdl();
                    cmdl.Load(path, stream, cmodel);
                }
                else
                {
                    throw new IOException("Invalid file extension.");
                }
            }
        }
        catch( Exception e )
        {
            cmodel = null;
            Debug.LogError(e.Message);
        }
    }

    private void ImportMesh()
    {
        mesh = new Mesh();
        mesh.name = cmodel.Name;

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
                CGeosetVertex vcertex = cgeoset.Vertices.Get(j);
                vertices[j] = new Vector3(vcertex.Position.X, vcertex.Position.Z, vcertex.Position.Y);
            }

            // Triangles.
            int[] triangles = new int[cgeoset.Faces.Count * 3];
            int t = 0;
            for( int j = 0; j < cgeoset.Faces.Count; j++ )
            {
                // MDX/MDL coordinate system is anti-clockwise.
                // Unity coordinate system is clockwise.
                CGeosetFace cface = cgeoset.Faces.Get(j);
                triangles[t] = cface.Vertex1.ObjectId; t++;
                triangles[t] = cface.Vertex3.ObjectId; t++; // Swap the order of the vertex 2 and 3.
                triangles[t] = cface.Vertex2.ObjectId; t++;
            }

            // Normals.
            Vector3[] normals = new Vector3[cgeoset.Vertices.Count];
            for( int j = 0; j < cgeoset.Vertices.Count; j++ )
            {
                // MDX/MDL up axis is Z.
                // Unity up axis is Y.
                CGeosetVertex cvertex = cgeoset.Vertices.Get(j);
                normals[j] = new Vector3(cvertex.Normal.X, cvertex.Normal.Z, cvertex.Normal.Y);
            }

            // UVs.
            Vector2[] uvs = new Vector2[cgeoset.Vertices.Count];
            for( int j = 0; j < cgeoset.Vertices.Count; j++ )
            {
                // MDX/MDL texture coordinate origin is at top left.
                // Unity texture coordinate origin is at bottom left.
                CGeosetVertex cvertex = cgeoset.Vertices.Get(j);
                uvs[j] = new Vector2(cvertex.TexturePosition.X, Mathf.Abs(cvertex.TexturePosition.Y - 1)); // Vunity = abs(Vmdx - 1)
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
    }

    private void ImportMaterials()
    {
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

        // Add the materials to the renderer in order.
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
        renderer.materials = mats;
    }

    private void ImportSkeleton()
    {
        skeleton = new GameObject("Skeleton");

        // Create the bones.
        // Bones reference geosets with geoanims, if there aren't geoanims, then bones will act as helpers.
        CObjectContainer<CBone> cbones = cmodel.Bones;
        for( int i = 0; i < cbones.Count; i++ )
        {
            CBone cbone = cbones.Get(i);

            // Pivot points are the positions of each object.
            CVector3 cpivot = cbone.PivotPoint;

            // Create the bone gameobject
            GameObject bone = new GameObject(cbone.Name);

            // Set the bone position.
            // MDX/MDL up axis is Z.
            // Unity up axis is Y.
            bone.transform.position = new Vector3(cpivot.X, cpivot.Z, cpivot.Y);

            bones[cbone.NodeId] = bone;
        }

        // Create the helpers.
        // Helpers are only used for doing transformations to their children.
        CObjectContainer<CHelper> chelpers = cmodel.Helpers;
        for( int i = 0; i < chelpers.Count; i++ )
        {
            CHelper chelper = chelpers.Get(i);

            // Pivot points are the positions of each object.
            CVector3 cpivot = chelper.PivotPoint;

            // Create the helper gameobject
            GameObject helper = new GameObject(chelper.Name);

            // Set the helper position.
            // MDX/MDL up axis is Z.
            // Unity up axis is Y.
            helper.transform.position = new Vector3(cpivot.X, cpivot.Z, cpivot.Y);

            bones[chelper.NodeId] = helper;
        }

        // Set the bones' parents.
        for( int i = 0; i < cbones.Count; i++ )
        {
            CBone cbone = cbones.Get(i);

            GameObject bone = bones[cbone.NodeId];
            if( bones.ContainsKey(cbone.Parent.NodeId) )
            {
                GameObject parent = bones[cbone.Parent.NodeId];
                bone.transform.SetParent(parent.transform);
            }
            else
            {
                bone.transform.SetParent(skeleton.transform);
            }
        }

        // Set the helpers' parents.
        for( int i = 0; i < chelpers.Count; i++ )
        {
            CHelper chelper = chelpers.Get(i);

            GameObject helper = bones[chelper.NodeId];
            if( bones.ContainsKey(chelper.Parent.NodeId) )
            {
                GameObject parent = bones[chelper.Parent.NodeId];
                helper.transform.SetParent(parent.transform);
            }
            else
            {
                helper.transform.SetParent(skeleton.transform);
            }
        }

        // Set the skeleton parent.
        skeleton.transform.SetParent(prefab.transform);

        // Get the bone transforms from the bone and helper gameobjects.
        Transform[] boneTransforms = bones.Values.ToArray().Select(go => go.transform).ToArray();
        renderer.bones = boneTransforms;
        renderer.rootBone = skeleton.transform;

        // Calculate the bind poses.
        // The bind pose is the inverse of the transformation matrix of the bone, when the bone is in the bind pose.
        Matrix4x4[] bindposes = new Matrix4x4[boneTransforms.Length];
        for( int i = 0; i < boneTransforms.Length; i++ )
        {
            bindposes[i] = boneTransforms[i].worldToLocalMatrix;
        }
        mesh.bindposes = bindposes;

        // Calculate the bone weights.
        // For each geoset.
        List<BoneWeight> weights = new List<BoneWeight>();
        CObjectContainer<CGeoset> cgeosets = cmodel.Geosets;
        for( int i = 0; i < cgeosets.Count; i++ )
        {
            CGeoset cgeoset = cgeosets.Get(i);

            // For each vertex.
            CObjectContainer<CGeosetVertex> cvertices = cgeoset.Vertices;
            for( int j = 0; j < cvertices.Count; j++ )
            {
                CGeosetVertex cvertex = cvertices.Get(j);
                BoneWeight weight = new BoneWeight();

                // Group.
                // A vertex group reference a group (of matrices).
                CGeosetGroup cgroup = cvertex.Group.Object; // Vertex group reference.

                // Matrices.
                // A matrix reference an object. The bone weights are evenly distributed, each weight is 1/N.
                CObjectContainer<CGeosetGroupNode> cmatrices = cgroup.Nodes;
                for( int k = 0; k < cmatrices.Count; k++ )
                {
                    CGeosetGroupNode cmatrix = cmatrices.Get(k);
                    switch( k )
                    {
                        case 0:
                            weight.boneIndex0 = cmatrix.Node.NodeId;
                            weight.weight0 = 1f / cmatrices.Count;
                            break;
                        case 1:
                            weight.boneIndex1 = cmatrix.Node.NodeId;
                            weight.weight1 = 1f / cmatrices.Count;
                            break;
                        case 2:
                            weight.boneIndex2 = cmatrix.Node.NodeId;
                            weight.weight2 = 1f / cmatrices.Count;
                            break;
                        case 3:
                            weight.boneIndex3 = cmatrix.Node.NodeId;
                            weight.weight3 = 1f / cmatrices.Count;
                            break;
                        default:
                            throw new Exception("Invalid number of bones " + k + " when skining.");
                    }
                }

                weights.Add(weight);
            }
        }

        mesh.boneWeights = weights.ToArray();
    }

    private void ImportAnimations()
    {
        // For each sequence.
        for( int i = 0; i < cmodel.Sequences.Count; i++ )
        {
            CSequence csequence = cmodel.Sequences.Get(i);

            AnimationClip clip = new AnimationClip();
            clip.name = csequence.Name;
            clip.wrapMode = WrapMode.Loop;
            float fps = 500;

            // For each bone.
            for( int j = 0; j < cmodel.Bones.Count; j++ )
            {
                CBone cbone = cmodel.Bones.Get(j);
                string path = GetPath(bones[cbone.NodeId]);

                // Translation.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();

                    CAnimator<CVector3> ctranslations = cbone.Translation;
                    for( int k = 0; k < ctranslations.Count; k++ )
                    {
                        CAnimatorNode<CVector3> ctranslation = ctranslations.Get(k);
                        if( csequence.IntervalStart <= ctranslation.Time && ctranslation.Time <= csequence.IntervalEnd )
                        {
                            float time = ctranslation.Time - csequence.IntervalStart;

                            Keyframe keyX = new Keyframe(time / fps, ctranslation.Value.X);
                            Keyframe keyY = new Keyframe(time / fps, ctranslation.Value.Z);
                            Keyframe keyZ = new Keyframe(time / fps, ctranslation.Value.Y);

                            curveX.AddKey(keyX);
                            curveY.AddKey(keyY);
                            curveZ.AddKey(keyZ);
                        }
                    }

                    clip.SetCurve(path, typeof(Transform), "position.x", curveX);
                    clip.SetCurve(path, typeof(Transform), "position.y", curveY);
                    clip.SetCurve(path, typeof(Transform), "position.z", curveZ);
                }

                // Rotation.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();
                    AnimationCurve curveW = new AnimationCurve();

                    CAnimator<CVector4> crotations = cbone.Rotation;
                    for( int k = 0; k < crotations.Count; k++ )
                    {
                        CAnimatorNode<CVector4> crotation = crotations.Get(k);
                        if( csequence.IntervalStart <= crotation.Time && crotation.Time <= csequence.IntervalEnd )
                        {
                            float time = crotation.Time - csequence.IntervalStart;

                            Keyframe keyX = new Keyframe(time / fps, crotation.Value.X);
                            //keyX.inTangent = crotation.InTangent.X;
                            //keyX.outTangent = crotation.OutTangent.X;
                            Keyframe keyY = new Keyframe(time / fps, crotation.Value.Z);
                            //keyY.inTangent = crotation.InTangent.Z;
                            //keyY.outTangent = crotation.OutTangent.Z;
                            Keyframe keyZ = new Keyframe(time / fps, crotation.Value.Y);
                            //keyZ.inTangent = crotation.InTangent.Y;
                            //keyZ.outTangent = crotation.OutTangent.Y;
                            Keyframe keyW = new Keyframe(time / fps, -crotation.Value.W);
                            //keyW.inTangent = -crotation.InTangent.W;
                            //keyW.outTangent = -crotation.OutTangent.W;

                            curveX.AddKey(keyX);
                            curveY.AddKey(keyY);
                            curveZ.AddKey(keyZ);
                            curveW.AddKey(keyW);
                        }
                    }

                    clip.SetCurve(path, typeof(Transform), "localRotation.x", curveX);
                    clip.SetCurve(path, typeof(Transform), "localRotation.y", curveY);
                    clip.SetCurve(path, typeof(Transform), "localRotation.z", curveZ);
                    clip.SetCurve(path, typeof(Transform), "localRotation.w", curveW);
                }
            }

            // For each helper.
            for( int j = 0; j < cmodel.Helpers.Count; j++ )
            {
                CHelper chelper = cmodel.Helpers.Get(j);
                string path = GetPath(bones[chelper.NodeId]);

                // Translation.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();

                    CAnimator<CVector3> ctranslations = chelper.Translation;
                    for( int k = 0; k < ctranslations.Count; k++ )
                    {
                        CAnimatorNode<CVector3> ctranslation = ctranslations.Get(k);
                        if( csequence.IntervalStart <= ctranslation.Time && ctranslation.Time <= csequence.IntervalEnd )
                        {
                            float time = ctranslation.Time - csequence.IntervalStart;

                            Keyframe keyX = new Keyframe(time / fps, ctranslation.Value.X);
                            Keyframe keyY = new Keyframe(time / fps, ctranslation.Value.Z);
                            Keyframe keyZ = new Keyframe(time / fps, ctranslation.Value.Y);

                            curveX.AddKey(keyX);
                            curveY.AddKey(keyY);
                            curveZ.AddKey(keyZ);
                        }
                    }

                    clip.SetCurve(path, typeof(Transform), "position.x", curveX);
                    clip.SetCurve(path, typeof(Transform), "position.y", curveY);
                    clip.SetCurve(path, typeof(Transform), "position.z", curveZ);
                }

                // Rotation.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();
                    AnimationCurve curveW = new AnimationCurve();

                    CAnimator<CVector4> crotations = chelper.Rotation;
                    for( int k = 0; k < crotations.Count; k++ )
                    {
                        CAnimatorNode<CVector4> crotation = crotations.Get(k);
                        if( csequence.IntervalStart <= crotation.Time && crotation.Time <= csequence.IntervalEnd )
                        {
                            float time = crotation.Time - csequence.IntervalStart;

                            Keyframe keyX = new Keyframe(time / fps, crotation.Value.X);
                            //keyX.inTangent = crotation.InTangent.X;
                            //keyX.outTangent = crotation.OutTangent.X;
                            Keyframe keyY = new Keyframe(time / fps, crotation.Value.Z);
                            //keyY.inTangent = crotation.InTangent.Z;
                            //keyY.outTangent = crotation.OutTangent.Z;
                            Keyframe keyZ = new Keyframe(time / fps, crotation.Value.Y);
                            //keyZ.inTangent = crotation.InTangent.Y;
                            //keyZ.outTangent = crotation.OutTangent.Y;
                            Keyframe keyW = new Keyframe(time / fps, -crotation.Value.W);
                            //keyW.inTangent = -crotation.InTangent.W;
                            //keyW.outTangent = -crotation.OutTangent.W;

                            curveX.AddKey(keyX);
                            curveY.AddKey(keyY);
                            curveZ.AddKey(keyZ);
                            curveW.AddKey(keyW);
                        }
                    }

                    clip.SetCurve(path, typeof(Transform), "localRotation.x", curveX);
                    clip.SetCurve(path, typeof(Transform), "localRotation.y", curveY);
                    clip.SetCurve(path, typeof(Transform), "localRotation.z", curveZ);
                    clip.SetCurve(path, typeof(Transform), "localRotation.w", curveW);
                }
            }

            // Realigns quaternion keys to ensure shortest interpolation paths and avoid rotation glitches.
            clip.EnsureQuaternionContinuity();

            clips.Add(clip);
        }
    }

    private string GetPath( GameObject bone )
    {
        if( !bone )
        {
            return "";
        }

        string path = bone.name;
        while( bone.transform.parent != skeleton.transform && bone.transform.parent != null )
        {
            bone = bone.transform.parent.gameObject;
            path = bone.name + "/" + path;
        }

        path = skeleton.name + "/" + path;

        return path;
    }
}