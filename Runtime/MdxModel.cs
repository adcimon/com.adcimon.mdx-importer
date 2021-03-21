using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using MdxLib.Animator;
using MdxLib.Model;
using MdxLib.ModelFormats;
using MdxLib.Primitives;

public class MdxModel
{
    public GameObject gameObject { get; private set; }

    public Mesh mesh;
    public List<Material> materials = new List<Material>();
    public List<AnimationClip> clips = new List<AnimationClip>();

    private CModel cmodel;
    private GameObject skeleton;
    private SortedDictionary<int, GameObject> bones = new SortedDictionary<int, GameObject>();

    public void Import( string path, bool importMaterials, bool importAnimations, float frameRate = 960 )
    {
        // Read the model file.
        ReadFile(path);

        // Import the mesh.
        ImportMesh();
        gameObject = new GameObject();
        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        SkinnedMeshRenderer renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
        filter.sharedMesh = mesh;
        renderer.sharedMesh = mesh;

        // Import the materials.
        if( importMaterials )
        {
            ImportMaterials(renderer);
        }

        // Import the skeleton.
        ImportSkeleton(renderer);

        // Import the animations.
        if( importAnimations )
        {
            ImportAnimations(frameRate);
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

    private void ImportMaterials( SkinnedMeshRenderer renderer )
    {
        // For each material.
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

            materials.Add(material);
        }

        // Add the materials to the renderer in order.
        List<Material> rendererMaterials = new List<Material>();
        for( int i = 0; i < cmodel.Geosets.Count; i++ )
        {
            CGeoset cgeoset = cmodel.Geosets.Get(i);

            for( int j = 0; j < cmodel.Materials.Count; j++ )
            {
                CMaterial cmaterial = cmodel.Materials.Get(j);

                if( cgeoset.Material.Object.ObjectId == cmaterial.ObjectId )
                {
                    rendererMaterials.Add(materials[j]);
                }
            }
        }

        renderer.materials = rendererMaterials.ToArray();
    }

    private void ImportSkeleton( SkinnedMeshRenderer renderer )
    {
        skeleton = new GameObject("Skeleton");
        skeleton.transform.SetParent(gameObject.transform);

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

        // Add the root bone.
        bones.Add(bones.Keys.Max() + 1, skeleton);

        // Set the bones' parents.
        for ( int i = 0; i < cbones.Count; i++ )
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

        // Set the bones to the skinned mesh renderer.
        renderer.bones = bones.Values.ToArray().Select(go => go.transform).ToArray();
        renderer.rootBone = skeleton.transform;

        // Calculate the bind poses.
        // The bind pose is the inverse of the transformation matrix of the bone when the bone is in the bind pose.
        mesh.bindposes = renderer.bones.Select(transform => transform.worldToLocalMatrix).ToArray();

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

    private void ImportAnimations( float frameRate )
    {
        // For each sequence.
        for( int i = 0; i < cmodel.Sequences.Count; i++ )
        {
            CSequence csequence = cmodel.Sequences.Get(i);
            AnimationClip clip = new AnimationClip();
            clip.name = csequence.Name;

            // Set the loop mode.
            if( !csequence.NonLooping )
            {
                clip.wrapMode = WrapMode.Loop;
            }

            // For each bone.
            for( int j = 0; j < cmodel.Bones.Count; j++ )
            {
                CBone cbone = cmodel.Bones.Get(j);
                GameObject bone = bones[cbone.NodeId];
                string path = GetPath(bone);

                // Translation.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();

                    CAnimator<CVector3> ctranslations = cbone.Translation;
                    for( int k = 0; k < ctranslations.Count; k++ )
                    {
                        CAnimatorNode<CVector3> node = ctranslations.Get(k);
                        if( csequence.IntervalStart <= node.Time && node.Time <= csequence.IntervalEnd )
                        {
                            float time = node.Time - csequence.IntervalStart;
                            Vector3 position = bone.transform.worldToLocalMatrix * node.Value.ToVector3();

                            Keyframe keyX = new Keyframe(time / frameRate, position.x);
                            keyX.inTangent = node.InTangent.X;
                            keyX.outTangent = node.OutTangent.X;
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / frameRate, position.z);
                            keyY.inTangent = node.InTangent.Z;
                            keyY.outTangent = node.OutTangent.Z;
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / frameRate, position.y);
                            keyZ.inTangent = node.InTangent.Y;
                            keyZ.outTangent = node.OutTangent.Y;
                            curveZ.AddKey(keyZ);
                        }
                    }

                    if( curveX.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localPosition.x", curveX);
                    if( curveY.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localPosition.y", curveY);
                    if( curveZ.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localPosition.z", curveZ);
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

                            Keyframe keyX = new Keyframe(time / frameRate, crotation.Value.X);
                            keyX.inTangent = crotation.InTangent.X;
                            keyX.outTangent = crotation.OutTangent.X;
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / frameRate, crotation.Value.Z);
                            keyY.inTangent = crotation.InTangent.Z;
                            keyY.outTangent = crotation.OutTangent.Z;
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / frameRate, crotation.Value.Y);
                            keyZ.inTangent = crotation.InTangent.Y;
                            keyZ.outTangent = crotation.OutTangent.Y;
                            curveZ.AddKey(keyZ);

                            Keyframe keyW = new Keyframe(time / frameRate, -crotation.Value.W);
                            keyW.inTangent = crotation.InTangent.W;
                            keyW.outTangent = crotation.OutTangent.W;
                            curveW.AddKey(keyW);
                        }
                    }

                    if( curveX.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localRotation.x", curveX);
                    if( curveY.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localRotation.y", curveY);
                    if( curveZ.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localRotation.z", curveZ);
                    if( curveW.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localRotation.w", curveW);
                }

                // Scaling.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();

                    CAnimator<CVector3> cscalings = cbone.Scaling;
                    for( int k = 0; k < cscalings.Count; k++ )
                    {
                        CAnimatorNode<CVector3> cscaling = cscalings.Get(k);
                        if( csequence.IntervalStart <= cscaling.Time && cscaling.Time <= csequence.IntervalEnd )
                        {
                            float time = cscaling.Time - csequence.IntervalStart;

                            Keyframe keyX = new Keyframe(time / frameRate, cscaling.Value.X);
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / frameRate, cscaling.Value.Z);
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / frameRate, cscaling.Value.Y);
                            curveZ.AddKey(keyZ);
                        }
                    }

                    if( curveX.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localScale.x", curveX);
                    if( curveY.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localScale.y", curveY);
                    if( curveZ.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localScale.z", curveZ);
                }
            }

            // For each helper.
            for( int j = 0; j < cmodel.Helpers.Count; j++ )
            {
                CHelper chelper = cmodel.Helpers.Get(j);
                GameObject bone = bones[chelper.NodeId];
                string path = GetPath(bone);

                // Translation.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();

                    CAnimator<CVector3> ctranslations = chelper.Translation;
                    for( int k = 0; k < ctranslations.Count; k++ )
                    {
                        CAnimatorNode<CVector3> node = ctranslations.Get(k);
                        if( csequence.IntervalStart <= node.Time && node.Time <= csequence.IntervalEnd )
                        {
                            float time = node.Time - csequence.IntervalStart;
                            Vector3 position = bone.transform.worldToLocalMatrix * node.Value.ToVector3();

                            Keyframe keyX = new Keyframe(time / frameRate, position.x);
                            keyX.inTangent = node.InTangent.X;
                            keyX.outTangent = node.OutTangent.X;
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / frameRate, position.z);
                            keyY.inTangent = node.InTangent.Z;
                            keyY.outTangent = node.OutTangent.Z;
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / frameRate, position.y);
                            keyZ.inTangent = node.InTangent.Y;
                            keyZ.outTangent = node.OutTangent.Y;
                            curveZ.AddKey(keyZ);
                        }
                    }

                    if( curveX.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localPosition.x", curveX);
                    if( curveY.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localPosition.y", curveY);
                    if( curveZ.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localPosition.z", curveZ);
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

                            Keyframe keyX = new Keyframe(time / frameRate, crotation.Value.X);
                            keyX.inTangent = crotation.InTangent.X;
                            keyX.outTangent = crotation.OutTangent.X;
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / frameRate, crotation.Value.Z);
                            keyY.inTangent = crotation.InTangent.Z;
                            keyY.outTangent = crotation.OutTangent.Z;
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / frameRate, crotation.Value.Y);
                            keyZ.inTangent = crotation.InTangent.Y;
                            keyZ.outTangent = crotation.OutTangent.Y;
                            curveZ.AddKey(keyZ);

                            Keyframe keyW = new Keyframe(time / frameRate, -crotation.Value.W);
                            keyW.inTangent = crotation.InTangent.W;
                            keyW.outTangent = crotation.OutTangent.W;
                            curveW.AddKey(keyW);
                        }
                    }

                    if( curveX.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localRotation.x", curveX);
                    if( curveY.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localRotation.y", curveY);
                    if( curveZ.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localRotation.z", curveZ);
                    if( curveW.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localRotation.w", curveW);
                }

                // Scaling.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();

                    CAnimator<CVector3> cscalings = chelper.Scaling;
                    for( int k = 0; k < cscalings.Count; k++ )
                    {
                        CAnimatorNode<CVector3> cscaling = cscalings.Get(k);
                        if( csequence.IntervalStart <= cscaling.Time && cscaling.Time <= csequence.IntervalEnd )
                        {
                            float time = cscaling.Time - csequence.IntervalStart;

                            Keyframe keyX = new Keyframe(time / frameRate, cscaling.Value.X);
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / frameRate, cscaling.Value.Z);
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / frameRate, cscaling.Value.Y);
                            curveZ.AddKey(keyZ);
                        }
                    }

                    if( curveX.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localScale.x", curveX);
                    if( curveY.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localScale.y", curveY);
                    if( curveZ.length > 0 )
                        clip.SetCurve(path, typeof(Transform), "localScale.z", curveZ);
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